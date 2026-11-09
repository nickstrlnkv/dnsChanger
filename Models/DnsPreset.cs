using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DnsChanger.Models
{
    public class DnsPreset : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _description = "";
        private string _ipv4Primary = "";
        private string _ipv4Secondary = "";
        private string _ipv6Primary = "";
        private string _ipv6Secondary = "";
        private string _dohTemplate = "";
        private string _dotHost = "";
        private bool _isBuiltIn;
        private string _pingDisplay = "";
        private bool _isPinging;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string IPv4Primary
        {
            get => _ipv4Primary;
            set { _ipv4Primary = value; OnPropertyChanged(); }
        }

        public string IPv4Secondary
        {
            get => _ipv4Secondary;
            set { _ipv4Secondary = value; OnPropertyChanged(); }
        }

        public string IPv6Primary
        {
            get => _ipv6Primary;
            set { _ipv6Primary = value; OnPropertyChanged(); }
        }

        public string IPv6Secondary
        {
            get => _ipv6Secondary;
            set { _ipv6Secondary = value; OnPropertyChanged(); }
        }

        public string DoHTemplate
        {
            get => _dohTemplate;
            set { _dohTemplate = value; OnPropertyChanged(); }
        }

        public string DoTHost
        {
            get => _dotHost;
            set { _dotHost = value; OnPropertyChanged(); }
        }

        public bool IsBuiltIn
        {
            get => _isBuiltIn;
            set { _isBuiltIn = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public string PingDisplay
        {
            get => _pingDisplay;
            set { _pingDisplay = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsPinging
        {
            get => _isPinging;
            set { _isPinging = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool HasIPv4 => !string.IsNullOrWhiteSpace(IPv4Primary);

        [JsonIgnore]
        public bool HasIPv6 => !string.IsNullOrWhiteSpace(IPv6Primary);

        [JsonIgnore]
        public bool HasDoH => !string.IsNullOrWhiteSpace(DoHTemplate);

        [JsonIgnore]
        public bool HasDoT => !string.IsNullOrWhiteSpace(DoTHost);

        public DnsPreset Clone()
        {
            return new DnsPreset
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                IPv4Primary = this.IPv4Primary,
                IPv4Secondary = this.IPv4Secondary,
                IPv6Primary = this.IPv6Primary,
                IPv6Secondary = this.IPv6Secondary,
                DoHTemplate = this.DoHTemplate,
                DoTHost = this.DoTHost,
                IsBuiltIn = this.IsBuiltIn
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(IPv4Primary)) OnPropertyChanged(nameof(HasIPv4));
            if (propertyName == nameof(IPv6Primary)) OnPropertyChanged(nameof(HasIPv6));
            if (propertyName == nameof(DoHTemplate)) OnPropertyChanged(nameof(HasDoH));
            if (propertyName == nameof(DoTHost)) OnPropertyChanged(nameof(HasDoT));
        }
    }
}
