using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnsChanger.Models;
using DnsChanger.Services;

namespace DnsChanger
{
    public partial class MainWindow : Window
    {
        private readonly NetworkDnsService _networkService;
        private readonly PresetStorageService _storageService;

        private List<DnsPreset> _allPresets = new();
        private readonly ObservableCollection<DnsPreset> _filteredPresets = new();
        private readonly ObservableCollection<NetworkAdapterItem> _adapters = new();

        public MainWindow()
        {
            InitializeComponent();

            _networkService = new NetworkDnsService();
            _storageService = new PresetStorageService();

            LstPresets.ItemsSource = _filteredPresets;
            CmbAdapters.ItemsSource = _adapters;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAdminStatus();
            LoadPresets();
            await RefreshAdaptersAsync();
        }

        private void UpdateAdminStatus()
        {
            bool isAdmin = _networkService.IsAdministrator();
            if (isAdmin)
            {
                AdminBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(6, 78, 59)); // #064E3B
                AdminBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105)); // #059669
                AdminBadgeBorder.BorderThickness = new Thickness(1);
                AdminBadgeText.Text = "🛡️ Администратор";
                AdminBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(110, 231, 183)); // #6EE7B7
                BtnElevate.Visibility = Visibility.Collapsed;
            }
            else
            {
                AdminBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(120, 53, 15)); // #78350F
                AdminBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6)); // #D97706
                AdminBadgeBorder.BorderThickness = new Thickness(1);
                AdminBadgeText.Text = "⚠️ Обычный запуск";
                AdminBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(252, 211, 77)); // #FCD34D
                BtnElevate.Visibility = Visibility.Visible;
            }
        }

        private void RestartElevated_Click(object sender, RoutedEventArgs e)
        {
            _networkService.RestartAsAdministrator();
        }

        private void LoadPresets()
        {
            _allPresets = _storageService.LoadPresets();
            FilterPresets();

            if (_filteredPresets.Count > 0)
            {
                LstPresets.SelectedIndex = 0;
            }
        }

        private void FilterPresets()
        {
            var filter = TxtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
            _filteredPresets.Clear();

            foreach (var preset in _allPresets)
            {
                if (string.IsNullOrEmpty(filter) ||
                    preset.Name.ToLowerInvariant().Contains(filter) ||
                    preset.Description.ToLowerInvariant().Contains(filter) ||
                    preset.IPv4Primary.Contains(filter))
                {
                    _filteredPresets.Add(preset);
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterPresets();
        }

        private async Task RefreshAdaptersAsync()
        {
            var selectedId = (CmbAdapters.SelectedItem as NetworkAdapterItem)?.Id;
            var adapters = await Task.Run(() => _networkService.GetNetworkAdapters());

            _adapters.Clear();
            foreach (var ad in adapters)
            {
                _adapters.Add(ad);
            }

            if (_adapters.Count > 0)
            {
                var match = _adapters.FirstOrDefault(a => a.Id == selectedId) ?? _adapters[0];
                CmbAdapters.SelectedItem = match;
                UpdateCurrentDnsDisplay(match);
            }
            else
            {
                TxtCurrentDns.Text = "Сетевые адаптеры не обнаружены";
            }
        }

        private void UpdateCurrentDnsDisplay(NetworkAdapterItem? adapter)
        {
            if (adapter == null)
            {
                TxtCurrentDns.Text = "—";
                return;
            }

            TxtCurrentDns.Text = adapter.CurrentDnsSummary;
        }

        private void CmbAdapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CmbAdapters.SelectedItem as NetworkAdapterItem;
            UpdateCurrentDnsDisplay(selected);
        }

        private async void RefreshAdapters_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Обновление списка адаптеров...", "⏳", false);
            await RefreshAdaptersAsync();
            SetStatus("Список сетевых адаптеров обновлен.", "✅", true);
        }

        private void LstPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstPresets.SelectedItem is DnsPreset preset)
            {
                TxtName.Text = preset.Name;
                TxtDescription.Text = preset.Description;
                TxtIPv4Primary.Text = preset.IPv4Primary;
                TxtIPv4Secondary.Text = preset.IPv4Secondary;
                TxtIPv6Primary.Text = preset.IPv6Primary;
                TxtIPv6Secondary.Text = preset.IPv6Secondary;
                TxtDoHTemplate.Text = preset.DoHTemplate;
                TxtDoTHost.Text = preset.DoTHost;
            }
        }

        private DnsPreset BuildPresetFromInputs(string? id = null)
        {
            return new DnsPreset
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = TxtName.Text.Trim(),
                Description = TxtDescription.Text.Trim(),
                IPv4Primary = TxtIPv4Primary.Text.Trim(),
                IPv4Secondary = TxtIPv4Secondary.Text.Trim(),
                IPv6Primary = TxtIPv6Primary.Text.Trim(),
                IPv6Secondary = TxtIPv6Secondary.Text.Trim(),
                DoHTemplate = TxtDoHTemplate.Text.Trim(),
                DoTHost = TxtDoTHost.Text.Trim(),
                IsBuiltIn = false
            };
        }

        private void SavePresetChanges_Click(object sender, RoutedEventArgs e)
        {
            if (LstPresets.SelectedItem is DnsPreset selected)
            {
                selected.Name = TxtName.Text.Trim();
                selected.Description = TxtDescription.Text.Trim();
                selected.IPv4Primary = TxtIPv4Primary.Text.Trim();
                selected.IPv4Secondary = TxtIPv4Secondary.Text.Trim();
                selected.IPv6Primary = TxtIPv6Primary.Text.Trim();
                selected.IPv6Secondary = TxtIPv6Secondary.Text.Trim();
                selected.DoHTemplate = TxtDoHTemplate.Text.Trim();
                selected.DoTHost = TxtDoTHost.Text.Trim();

                _storageService.SavePresets(_allPresets);
                FilterPresets();
                LstPresets.SelectedItem = selected;

                SetStatus($"Пресет «{selected.Name}» сохранен в presets.json.", "💾", true);
            }
            else
            {
                AddNewPreset_Click(sender, e);
            }
        }

        private void AddNewPreset_Click(object sender, RoutedEventArgs e)
        {
            var newPreset = new DnsPreset
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Новый DNS пресет",
                Description = "Пользовательский DNS",
                IPv4Primary = "1.1.1.1",
                IPv4Secondary = "1.0.0.1",
                IsBuiltIn = false
            };

            _allPresets.Insert(0, newPreset);
            _storageService.SavePresets(_allPresets);
            FilterPresets();

            LstPresets.SelectedItem = newPreset;
            TxtName.Focus();
            TxtName.SelectAll();

            SetStatus("Создан новый пресет. Заполните параметры и нажмите «Сохранить изменения».", "➕", true);
        }

        private void DuplicatePreset_Click(object sender, RoutedEventArgs e)
        {
            var current = BuildPresetFromInputs();
            current.Id = Guid.NewGuid().ToString("N");
            current.Name += " (Копия)";
            current.IsBuiltIn = false;

            _allPresets.Insert(0, current);
            _storageService.SavePresets(_allPresets);
            FilterPresets();

            LstPresets.SelectedItem = current;
            SetStatus($"Создана копия пресета «{current.Name}».", "📑", true);
        }

        private void DeletePreset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var presetToDelete = button?.Tag as DnsPreset ?? LstPresets.SelectedItem as DnsPreset;

            if (presetToDelete == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить пресет «{presetToDelete.Name}»?",
                "Удаление пресета",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _allPresets.Remove(presetToDelete);
                _storageService.SavePresets(_allPresets);
                FilterPresets();

                if (_filteredPresets.Count > 0)
                {
                    LstPresets.SelectedIndex = 0;
                }

                SetStatus($"Пресет «{presetToDelete.Name}» удален.", "🗑️", true);
            }
        }

        private void OpenPresetsJson_Click(object sender, RoutedEventArgs e)
        {
            _storageService.OpenPresetsFile();
            SetStatus("Файл presets.json открыт в редакторе.", "📂", true);
        }

        private async void FlushDns_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Очистка системного кэша DNS...", "⏳", false);
            var (success, msg) = await _networkService.FlushDnsCacheAsync();
            SetStatus(msg, success ? "🧹" : "❌", success);
        }

        private async void PingCurrentPreset_Click(object sender, RoutedEventArgs e)
        {
            var target = !string.IsNullOrWhiteSpace(TxtIPv4Primary.Text) 
                ? TxtIPv4Primary.Text.Trim() 
                : TxtIPv6Primary.Text.Trim();

            if (string.IsNullOrWhiteSpace(target))
            {
                SetStatus("Укажите IP-адрес для измерения пинга.", "⚠️", false);
                return;
            }

            SetStatus($"Измерение пинга до {target}...", "⚡", false);
            var latency = await _networkService.PingAddressAsync(target);

            if (latency.HasValue)
            {
                var speedQuality = latency.Value < 20 ? "Отлично" : latency.Value < 60 ? "Хорошо" : "Высокий пинг";
                SetStatus($"Пинг до {target}: {latency.Value} мс ({speedQuality})", "⚡", true);

                if (LstPresets.SelectedItem is DnsPreset selected)
                {
                    selected.PingDisplay = $"⚡ {latency.Value} мс";
                }
            }
            else
            {
                SetStatus($"Сервер {target} не ответил на пинг (таймаут).", "⚠️", false);
                if (LstPresets.SelectedItem is DnsPreset selected)
                {
                    selected.PingDisplay = "— таймаут";
                }
            }
        }

        private async void PingAllPresets_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Тестирование задержки для всех DNS-серверов...", "⚡", false);

            var tasks = _allPresets.Select(async preset =>
            {
                var ip = !string.IsNullOrWhiteSpace(preset.IPv4Primary) ? preset.IPv4Primary : preset.IPv6Primary;
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    var latency = await _networkService.PingAddressAsync(ip);
                    preset.PingDisplay = latency.HasValue ? $"⚡ {latency.Value} мс" : "—";
                }
            });

            await Task.WhenAll(tasks);
            SetStatus("Измерение задержки для всех серверов завершено.", "✅", true);
        }

        private async void ApplyDns_Click(object sender, RoutedEventArgs e)
        {
            var adapter = CmbAdapters.SelectedItem as NetworkAdapterItem;
            if (adapter == null)
            {
                SetStatus("Выберите сетевой адаптер для применения DNS.", "⚠️", false);
                return;
            }

            var preset = BuildPresetFromInputs(
                (LstPresets.SelectedItem as DnsPreset)?.Id
            );

            if (!preset.HasIPv4 && !preset.HasIPv6)
            {
                SetStatus("Укажите хотя бы один DNS-адрес (IPv4 или IPv6).", "⚠️", false);
                return;
            }

            SetBusy(true, $"Применение DNS «{preset.Name}» на интерфейс {adapter.Name}...");

            try
            {
                var (success, msg) = await _networkService.ApplyDnsAsync(adapter.InterfaceIndex, preset);

                if (success)
                {
                    SetStatus($"✅ DNS успешно переключен на «{preset.Name}»! Кэш очищен.", "✅", true);
                    await RefreshAdaptersAsync();
                }
                else
                {
                    SetStatus($"❌ {msg}", "❌", false);
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void ResetDhcp_Click(object sender, RoutedEventArgs e)
        {
            var adapter = CmbAdapters.SelectedItem as NetworkAdapterItem;
            if (adapter == null)
            {
                SetStatus("Выберите сетевой адаптер для сброса DNS.", "⚠️", false);
                return;
            }

            SetBusy(true, $"Сброс DNS на автоматический (DHCP) для {adapter.Name}...");

            try
            {
                var (success, msg) = await _networkService.ResetToDhcpAsync(adapter.InterfaceIndex);

                if (success)
                {
                    SetStatus("✅ DNS успешно сброшен на DHCP (Автоматически от роутера/провайдера).", "✅", true);
                    await RefreshAdaptersAsync();
                }
                else
                {
                    SetStatus($"❌ {msg}", "❌", false);
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy, string notice = "")
        {
            IsEnabled = !isBusy;
            TxtActionNotice.Text = isBusy ? notice : "Нажмите «Применить» для настройки активного сетевого подключения";
            if (isBusy)
            {
                SetStatus(notice, "⏳", false);
            }
        }

        private void SetStatus(string message, string icon, bool isSuccess)
        {
            TxtStatusIcon.Text = icon;
            TxtStatusMessage.Text = message;
            TxtStatusMessage.Foreground = isSuccess 
                ? new SolidColorBrush(Color.FromRgb(110, 231, 183)) // #6EE7B7
                : new SolidColorBrush(Color.FromRgb(248, 113, 113)); // #F87171
        }
    }
}