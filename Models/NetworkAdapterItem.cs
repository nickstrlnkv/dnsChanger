using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace DnsChanger.Models
{
    public class NetworkAdapterItem
    {
        public int InterfaceIndex { get; set; }
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public OperationalStatus Status { get; set; }
        public NetworkInterfaceType InterfaceType { get; set; }
        public bool IsConnected => Status == OperationalStatus.Up;
        public bool HasGateway { get; set; }
        public List<string> CurrentDnsAddresses { get; set; } = new();

        public string CurrentDnsSummary =>
            CurrentDnsAddresses.Count > 0
                ? string.Join(", ", CurrentDnsAddresses)
                : "DHCP (Автоматически)";

        public string DisplayName
        {
            get
            {
                var icon = IsConnected ? "🟢" : "⚪";
                var typeStr = InterfaceType switch
                {
                    NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                    NetworkInterfaceType.Ethernet => "Ethernet",
                    _ => InterfaceType.ToString()
                };
                return $"{icon} {Name} ({typeStr}) — {Description}";
            }
        }
    }
}
