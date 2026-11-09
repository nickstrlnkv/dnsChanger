using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DnsChanger.Models;

namespace DnsChanger.Services
{
    public class NetworkDnsService
    {
        public bool IsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public void RestartAsAdministrator()
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(psi);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось перезапустить от имени администратора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public List<NetworkAdapterItem> GetNetworkAdapters()
        {
            var result = new List<NetworkAdapterItem>();

            try
            {
                var allInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in allInterfaces)
                {
                    // Skip loopback, tunnels, and virtual pseudo-interfaces
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    int ifIndex = -1;
                    var ipProps = ni.GetIPProperties();
                    try
                    {
                        var ipv4Props = ipProps.GetIPv4Properties();
                        if (ipv4Props != null) ifIndex = ipv4Props.Index;
                    }
                    catch { }

                    if (ifIndex <= 0)
                    {
                        try
                        {
                            var ipv6Props = ipProps.GetIPv6Properties();
                            if (ipv6Props != null) ifIndex = ipv6Props.Index;
                        }
                        catch { }
                    }

                    if (ifIndex <= 0) continue;

                    var dnsList = new List<string>();
                    try
                    {
                        foreach (var dns in ipProps.DnsAddresses)
                        {
                            dnsList.Add(dns.ToString());
                        }
                    }
                    catch { }

                    bool hasGateway = false;
                    try
                    {
                        hasGateway = ipProps.GatewayAddresses.Any(g => g.Address != null && !g.Address.Equals(IPAddress.Any));
                    }
                    catch { }

                    result.Add(new NetworkAdapterItem
                    {
                        InterfaceIndex = ifIndex,
                        Id = ni.Id,
                        Name = ni.Name,
                        Description = ni.Description,
                        Status = ni.OperationalStatus,
                        InterfaceType = ni.NetworkInterfaceType,
                        HasGateway = hasGateway,
                        CurrentDnsAddresses = dnsList
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving network adapters: {ex.Message}");
            }

            // Order by connected with internet (gateway) first, then connected, then name
            return result
                .OrderByDescending(a => a.IsConnected && a.HasGateway)
                .ThenByDescending(a => a.IsConnected)
                .ThenBy(a => a.Name)
                .ToList();
        }

        public async Task<(bool Success, string Message)> ApplyDnsAsync(int interfaceIndex, DnsPreset preset)
        {
            var addresses = new List<string>();
            if (!string.IsNullOrWhiteSpace(preset.IPv4Primary)) addresses.Add(preset.IPv4Primary.Trim());
            if (!string.IsNullOrWhiteSpace(preset.IPv4Secondary)) addresses.Add(preset.IPv4Secondary.Trim());
            if (!string.IsNullOrWhiteSpace(preset.IPv6Primary)) addresses.Add(preset.IPv6Primary.Trim());
            if (!string.IsNullOrWhiteSpace(preset.IPv6Secondary)) addresses.Add(preset.IPv6Secondary.Trim());

            if (addresses.Count == 0)
            {
                return (false, "Не указан ни один адрес DNS (IPv4 или IPv6).");
            }

            var sb = new StringBuilder();
            sb.AppendLine("$ErrorActionPreference = 'Stop'");

            // 1. Configure DoH / DoT in Windows 11 table if specified
            bool hasDoh = !string.IsNullOrWhiteSpace(preset.DoHTemplate);
            bool hasDot = !string.IsNullOrWhiteSpace(preset.DoTHost);

            if (hasDoh || hasDot)
            {
                var dohTemplate = preset.DoHTemplate?.Trim() ?? "";
                var dotHost = preset.DoTHost?.Trim() ?? "";

                foreach (var addr in addresses)
                {
                    // PowerShell DoH cmdlets
                    if (hasDoh)
                    {
                        sb.AppendLine($"try {{ Add-DnsClientDohServerAddress -ServerAddress '{addr}' -DohTemplate '{dohTemplate}' -AllowFallbackToUdp $true -AutoUpgrade $true -ErrorAction SilentlyContinue }} catch {{}}");
                        sb.AppendLine($"try {{ Set-DnsClientDohServerAddress -ServerAddress '{addr}' -DohTemplate '{dohTemplate}' -AllowFallbackToUdp $true -AutoUpgrade $true -ErrorAction SilentlyContinue }} catch {{}}");
                    }

                    // netsh DoT / DoH encryption
                    var netshArgs = $"server=\"{addr}\"";
                    if (hasDoh) netshArgs += $" dohtemplate=\"{dohTemplate}\"";
                    if (hasDot) netshArgs += $" dothost=\"{dotHost}\"";
                    netshArgs += " autoupgrade=yes udpfallback=yes";

                    sb.AppendLine($"try {{ netsh dns add encryption {netshArgs} 2>$null }} catch {{}}");
                    sb.AppendLine($"try {{ netsh dns set encryption {netshArgs} 2>$null }} catch {{}}");
                }
            }

            // 2. Set interface DNS addresses
            var addressArrayLiteral = string.Join(",", addresses.Select(a => $"'{a}'"));
            sb.AppendLine($"Set-DnsClientServerAddress -InterfaceIndex {interfaceIndex} -ServerAddresses @({addressArrayLiteral})");

            // 3. Clear DNS cache
            sb.AppendLine("Clear-DnsClientCache");

            return await ExecutePowerShellScriptAsync(sb.ToString(), "Применение DNS");
        }

        public async Task<(bool Success, string Message)> ResetToDhcpAsync(int interfaceIndex)
        {
            var script = $@"
$ErrorActionPreference = 'Stop'
Set-DnsClientServerAddress -InterfaceIndex {interfaceIndex} -ResetServerAddresses
Clear-DnsClientCache
";
            return await ExecutePowerShellScriptAsync(script, "Сброс DNS на DHCP");
        }

        public async Task<(bool Success, string Message)> FlushDnsCacheAsync()
        {
            var script = @"
$ErrorActionPreference = 'Stop'
Clear-DnsClientCache
ipconfig /flushdns
";
            return await ExecutePowerShellScriptAsync(script, "Очистка кэша DNS");
        }

        public async Task<long?> PingAddressAsync(string hostOrIp, int timeoutMs = 1200)
        {
            if (string.IsNullOrWhiteSpace(hostOrIp)) return null;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(hostOrIp.Trim(), timeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch
            {
                // Ping failed or timeout
            }

            return null;
        }

        private async Task<(bool Success, string Message)> ExecutePowerShellScriptAsync(string script, string operationName)
        {
            return await Task.Run(() =>
            {
                bool isAdmin = IsAdministrator();
                var tempScriptPath = Path.Combine(Path.GetTempPath(), $"dns_changer_{Guid.NewGuid():N}.ps1");

                try
                {
                    File.WriteAllText(tempScriptPath, script, Encoding.UTF8);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                        UseShellExecute = !isAdmin, // When not admin, shell execute triggers UAC
                        Verb = isAdmin ? "" : "runas",
                        CreateNoWindow = isAdmin,
                        WindowStyle = isAdmin ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
                        RedirectStandardOutput = isAdmin,
                        RedirectStandardError = isAdmin,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using var process = Process.Start(psi);
                    if (process == null)
                    {
                        return (false, "Не удалось запустить процесс PowerShell.");
                    }

                    string stdout = "";
                    string stderr = "";
                    if (isAdmin)
                    {
                        stdout = process.StandardOutput.ReadToEnd();
                        stderr = process.StandardError.ReadToEnd();
                    }

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return (true, $"{operationName} успешно выполнено.");
                    }
                    else
                    {
                        var err = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                        if (string.IsNullOrWhiteSpace(err))
                            err = $"Код ошибки: {process.ExitCode}";
                        return (false, $"Ошибка выполнения: {err}");
                    }
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    // Error 1223: The operation was canceled by the user (UAC prompt dismissed)
                    return (false, "Операция отменена пользователем (отказ в UAC).");
                }
                catch (Exception ex)
                {
                    return (false, $"Исключение при {operationName}: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempScriptPath)) File.Delete(tempScriptPath);
                    }
                    catch { }
                }
            });
        }
    }
}
