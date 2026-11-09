using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DnsChanger.Models;

namespace DnsChanger.Services
{
    public class PresetStorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string GetFilePath()
        {
            // Prefer application directory, fall back to current directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "presets.json");
        }

        public List<DnsPreset> LoadPresets()
        {
            var filePath = GetFilePath();
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var list = JsonSerializer.Deserialize<List<DnsPreset>>(json, JsonOptions);
                    if (list != null && list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load presets: {ex.Message}");
            }

            var defaults = GetDefaultPresets();
            SavePresets(defaults);
            return defaults;
        }

        public void SavePresets(IEnumerable<DnsPreset> presets)
        {
            var filePath = GetFilePath();
            try
            {
                var json = JsonSerializer.Serialize(presets, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save presets: {ex.Message}");
            }
        }

        public void OpenPresetsFile()
        {
            var filePath = GetFilePath();
            if (!File.Exists(filePath))
            {
                SavePresets(GetDefaultPresets());
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch
            {
                Process.Start("notepad.exe", filePath);
            }
        }

        public static List<DnsPreset> GetDefaultPresets()
        {
            return new List<DnsPreset>
            {
                new()
                {
                    Id = "cloudflare",
                    Name = "Cloudflare DNS",
                    Description = "Самый быстрый и приватный. Без цензуры.",
                    IPv4Primary = "1.1.1.1",
                    IPv4Secondary = "1.0.0.1",
                    IPv6Primary = "2606:4700:4700::1111",
                    IPv6Secondary = "2606:4700:4700::1001",
                    DoHTemplate = "https://cloudflare-dns.com/dns-query",
                    DoTHost = "one.one.one.one:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "google",
                    Name = "Google Public DNS",
                    Description = "Максимальная надежность и совместимость во всем мире.",
                    IPv4Primary = "8.8.8.8",
                    IPv4Secondary = "8.8.4.4",
                    IPv6Primary = "2001:4860:4860::8888",
                    IPv6Secondary = "2001:4860:4860::8844",
                    DoHTemplate = "https://dns.google/dns-query",
                    DoTHost = "dns.google:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "adguard-default",
                    Name = "AdGuard DNS (Блокировка рекламы)",
                    Description = "Автоматически блокирует рекламу, счетчики и фишинговые сайты.",
                    IPv4Primary = "94.140.14.14",
                    IPv4Secondary = "94.140.15.15",
                    IPv6Primary = "2a10:50c0::ad1:ff",
                    IPv6Secondary = "2a10:50c0::ad2:ff",
                    DoHTemplate = "https://dns.adguard-dns.com/dns-query",
                    DoTHost = "dns.adguard-dns.com:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "adguard-family",
                    Name = "AdGuard DNS (Семейный)",
                    Description = "Блокировка рекламы + безопасный поиск и защита от сайтов 18+.",
                    IPv4Primary = "94.140.14.15",
                    IPv4Secondary = "94.140.15.16",
                    IPv6Primary = "2a10:50c0::bad1:ff",
                    IPv6Secondary = "2a10:50c0::bad2:ff",
                    DoHTemplate = "https://dns.adguard-dns.com/dns-query",
                    DoTHost = "dns.adguard-dns.com:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "quad9",
                    Name = "Quad9 (Безопасный)",
                    Description = "Блокирует вредоносное ПО, вирусы, фишинг и шпионские домены.",
                    IPv4Primary = "9.9.9.9",
                    IPv4Secondary = "149.112.112.112",
                    IPv6Primary = "2620:fe::fe",
                    IPv6Secondary = "2620:fe::9",
                    DoHTemplate = "https://dns.quad9.net/dns-query",
                    DoTHost = "dns.quad9.net:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "opendns",
                    Name = "OpenDNS (Cisco Umbrella)",
                    Description = "Скорость и фильтрация фишинга от компании Cisco.",
                    IPv4Primary = "208.67.222.222",
                    IPv4Secondary = "208.67.220.220",
                    IPv6Primary = "2620:119:35::35",
                    IPv6Secondary = "2620:119:53::53",
                    DoHTemplate = "https://doh.opendns.com/dns-query",
                    DoTHost = "dns.opendns.com:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "yandex-basic",
                    Name = "Яндекс DNS (Базовый)",
                    Description = "Серверы в России. Высокая скорость доступа к сайтам Рунета.",
                    IPv4Primary = "77.88.8.8",
                    IPv4Secondary = "77.88.8.1",
                    IPv6Primary = "2a02:6b8::feed:0ff",
                    IPv6Secondary = "2a02:6b8:0:1::feed:0ff",
                    DoHTemplate = "https://common.dot.dns.yandex.net/dns-query",
                    DoTHost = "common.dot.dns.yandex.net:853",
                    IsBuiltIn = true
                },
                new()
                {
                    Id = "controld",
                    Name = "Control D (Anycast)",
                    Description = "Сверхбыстрый современный DNS без цензуры и логов.",
                    IPv4Primary = "76.76.2.0",
                    IPv4Secondary = "76.76.10.0",
                    IPv6Primary = "2606:1a40::",
                    IPv6Secondary = "2606:1a40:1::",
                    DoHTemplate = "https://freedns.controld.com/p0",
                    DoTHost = "p0.freedns.controld.com:853",
                    IsBuiltIn = true
                }
            };
        }
    }
}
