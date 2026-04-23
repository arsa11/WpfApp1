using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfApp1.Config;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<KeyBox> KeysBoxes { get; } = new ObservableCollection<KeyBox>();

        private readonly LedPortService _ledPort = new LedPortService();

        private readonly string _configDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "F13Launcher", "Configs");

        private Forms.NotifyIcon? _trayIcon;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc? _proc;
        private bool _isLoadingConfigList;
        private bool _isApplyingConfig;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private const double FixedAspectRatio = 780.0 / 1000.0;
        private bool _isResizing;
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isResizing)
                return;

            _isResizing = true;

            try
            {
                // Если ширина менялась сильнее — пересчитываем высоту
                if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) >
                    Math.Abs(e.NewSize.Height - e.PreviousSize.Height))
                {
                    Height = Width / FixedAspectRatio;
                }
                else
                {
                    Width = Height * FixedAspectRatio;
                }
            }
            finally
            {
                _isResizing = false;
            }
        }
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public MainWindow()
        {
            try
            {
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} MainWindow ctor start{Environment.NewLine}");

                InitializeComponent();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} InitializeComponent OK{Environment.NewLine}");

                try
                {
                    var uri = new Uri("pack://application:,,,/Assets/arsadeck.ico", UriKind.Absolute);
                    var sri = Application.GetResourceStream(uri);
                    if (sri?.Stream != null)
                    {
                        this.Icon = BitmapFrame.Create(sri.Stream);
                    }
                }
                catch
                {
                }

                DataContext = this;
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} DataContext OK{Environment.NewLine}");

                InitKeyBoxes();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} InitKeyBoxes OK{Environment.NewLine}");

                HookKeyboard();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} HookKeyboard OK{Environment.NewLine}");

                InitTrayIcon();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} InitTrayIcon OK{Environment.NewLine}");

                RefreshPorts();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} RefreshPorts OK{Environment.NewLine}");

                RefreshConfigList();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} RefreshConfigList OK{Environment.NewLine}");

                LoadLastOrFirstConfig();
                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} LoadLastOrFirstConfig OK{Environment.NewLine}");

                foreach (var box in KeysBoxes)
                {
                    box.PropertyChanged += KeyBox_PropertyChanged;
                }

                File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} MainWindow ctor finish{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup.log",
                    $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} MainWindow ctor ERROR: {ex}{Environment.NewLine}");

                MessageBox.Show(ex.ToString(), "MainWindow ctor error");
                throw;
            }
        }

        private void InitKeyBoxes()
        {
            var startKey = Forms.Keys.F13;
            for (int i = 0; i < 12; i++)
            {
                KeysBoxes.Add(new KeyBox
                {
                    Index = i,
                    KeyCode = startKey + i,
                    Title = (startKey + i).ToString(),
                    AppName = "Нет программы",
                    Type = KeyBoxType.App,
                    CustomTitle = null
                });
            }
        }

        private void HookKeyboard()
        {
            _proc = HookCallback;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;

            if (curModule != null)
            {
                _hookId = SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _proc,
                    GetModuleHandle(curModule.ModuleName),
                    0);
            }
        }

        private void UnhookKeyboard()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Forms.Keys)vkCode;

                if (key >= Forms.Keys.F13 && key <= Forms.Keys.F24)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnGlobalKeyPressed(key);
                    });
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void OnGlobalKeyPressed(Forms.Keys key)
        {
            var box = KeysBoxes.FirstOrDefault(b => b.KeyCode == key);
            if (box == null)
                return;

            if (box.Type == KeyBoxType.App && !string.IsNullOrWhiteSpace(box.AppPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = box.AppPath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                }
            }

            box.IsActive = true;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };

            timer.Tick += (s, e) =>
            {
                box.IsActive = false;
                timer.Stop();
            };

            timer.Start();

            
        }

        private void KeyBox_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isApplyingConfig)
                return;

            if (e.PropertyName == nameof(KeyBox.Mode) && sender is KeyBox box)
            {
                _ledPort.SendMode(box.Index, box.Mode);
            }
        }

        private static KeyBox? GetBoxFromMenu(object sender)
        {
            if (sender is not System.Windows.Controls.MenuItem mi) return null;
            if (mi.Parent is not System.Windows.Controls.ContextMenu cm) return null;

            if (cm.DataContext is KeyBox b) return b;
            if (cm.PlacementTarget is FrameworkElement fe) return fe.DataContext as KeyBox;

            return null;
        }

        private void PickExeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null)
                return;

            PickExeForBox(box);
        }

        private void PickImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null)
                return;

            PickImageForBox(box);
        }

        private void RenameCustomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null)
                return;

            var current = box.CustomTitle ?? box.AppName ?? box.Title ?? "";
            var text = Microsoft.VisualBasic.Interaction.InputBox(
                "Новое отображаемое имя:",
                "Переименовать",
                current);

            if (string.IsNullOrWhiteSpace(text))
                return;

            box.CustomTitle = text.Trim();

            // НИЧЕГО больше не меняем:
            // не трогаем Type
            // не трогаем AppPath
            // не трогаем Icon
            // не трогаем CustomImage
            // не трогаем CustomImagePath
        }

        private void ResetCustomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null)
                return;

            // Сбрасываем только пользовательское имя.
            // Программа / картинка остаются.
            box.CustomTitle = null;
        }

        private static void PickExeForBox(KeyBox box)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Приложения (*.exe)|*.exe|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true)
                return;

            box.AppPath = dlg.FileName;
            box.AppName = Path.GetFileNameWithoutExtension(dlg.FileName);
            box.Icon = ExtractIconSafe(dlg.FileName);

            // Если раньше была кастомная картинка, не удаляем имя,
            // но переключаем отображение плитки в режим программы.
            box.Type = KeyBoxType.App;
        }

        private static void PickImageForBox(KeyBox box)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Картинки|*.png;*.jpg;*.jpeg;*.bmp;*.ico|Все файлы|*.*"
            };

            if (dlg.ShowDialog() != true)
                return;

            box.CustomImagePath = dlg.FileName;
            box.CustomImage = LoadImageNoLock(dlg.FileName);

            if (string.IsNullOrWhiteSpace(box.CustomTitle))
                box.CustomTitle = Path.GetFileNameWithoutExtension(dlg.FileName);

            box.Type = KeyBoxType.Custom;
        }

        private static ImageSource? ExtractIconSafe(string exePath)
        {
            try
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (icon == null)
                    return null;

                using var bmp = icon.ToBitmap();
                IntPtr hBitmap = bmp.GetHbitmap();

                try
                {
                    var img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    img.Freeze();
                    return img;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            catch
            {
                return null;
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private static BitmapImage? LoadImageNoLock(string filePath)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(filePath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }

        private string GetConfigPath(string configName)
        {
            return Path.Combine(_configDirectory, configName + ".json");
        }

        private void EnsureConfigDirectory()
        {
            if (!Directory.Exists(_configDirectory))
                Directory.CreateDirectory(_configDirectory);
        }

        private void RefreshConfigList()
        {
            EnsureConfigDirectory();

            _isLoadingConfigList = true;

            var names = Directory.GetFiles(_configDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x)
                .ToList();

            ConfigComboBox.ItemsSource = names;

            _isLoadingConfigList = false;
        }

        private void LoadLastOrFirstConfig()
        {
            RefreshConfigList();

            var items = ConfigComboBox.ItemsSource as System.Collections.IEnumerable;
            if (items == null)
                return;

            var first = items.Cast<string>().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                ConfigComboBox.SelectedItem = first;
                LoadConfig(first);
            }
        }

        private AppConfig BuildCurrentConfig()
        {
            var cfg = new AppConfig
            {
                ComPort = _ledPort.CurrentPortName
            };

            foreach (var box in KeysBoxes)
            {
                cfg.Keys.Add(new KeyBoxConfig
                {
                    Index = box.Index,
                    Mode = box.Mode,
                    Type = box.Type,
                    AppPath = box.AppPath,
                    CustomTitle = box.CustomTitle,
                    CustomImagePath = box.CustomImagePath
                });
            }

            return cfg;
        }

        private void SaveConfig(string configName)
        {
            try
            {
                EnsureConfigDirectory();

                var cfg = BuildCurrentConfig();
                var path = GetConfigPath(configName);

                var json = JsonSerializer.Serialize(cfg, _jsonOptions);
                File.WriteAllText(path, json);

                RefreshConfigList();
                ConfigComboBox.SelectedItem = configName;
            }
            catch
            {
            }
        }

        private void LoadConfig(string configName)
        {
            try
            {
                var path = GetConfigPath(configName);
                if (!File.Exists(path))
                    return;

                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg == null)
                    return;

                _isApplyingConfig = true;

                RefreshPorts();

                if (!string.IsNullOrEmpty(cfg.ComPort))
                    PortComboBox.SelectedItem = cfg.ComPort;

                foreach (var kc in cfg.Keys)
                {
                    var box = KeysBoxes.FirstOrDefault(b => b.Index == kc.Index);
                    if (box == null)
                        continue;

                    box.Mode = kc.Mode;
                    box.Type = kc.Type;

                    box.AppPath = kc.AppPath;
                    if (!string.IsNullOrWhiteSpace(kc.AppPath) && File.Exists(kc.AppPath))
                    {
                        box.AppName = Path.GetFileNameWithoutExtension(kc.AppPath);
                        box.Icon = ExtractIconSafe(kc.AppPath);
                    }
                    else
                    {
                        box.AppName = "Нет программы";
                        box.Icon = null;
                    }

                    box.CustomTitle = string.IsNullOrWhiteSpace(kc.CustomTitle) ? null : kc.CustomTitle;

                    box.CustomImagePath = kc.CustomImagePath;
                    if (!string.IsNullOrWhiteSpace(kc.CustomImagePath) && File.Exists(kc.CustomImagePath))
                    {
                        box.CustomImage = LoadImageNoLock(kc.CustomImagePath);
                    }
                    else
                    {
                        box.CustomImage = null;
                    }

                    // Если тип невалидный после загрузки, восстанавливаем логично.
                    if (box.Type == KeyBoxType.Custom && box.CustomImage == null)
                    {
                        if (!string.IsNullOrWhiteSpace(box.AppPath))
                            box.Type = KeyBoxType.App;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _isApplyingConfig = false;
            }
        }

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedName = ConfigComboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                SaveConfigAsButton_Click(sender, e);
                return;
            }

            SaveConfig(selectedName);
            PortStatusText.Text = "Конфигурация сохранена";
            PortStatusText.Foreground = WpfBrushes.LightGreen;
        }

        private void SaveConfigAsButton_Click(object sender, RoutedEventArgs e)
        {
            var text = Microsoft.VisualBasic.Interaction.InputBox(
                "Имя новой конфигурации:",
                "Сохранить конфиг как",
                ConfigComboBox.SelectedItem as string ?? "Default");

            if (string.IsNullOrWhiteSpace(text))
                return;

            var configName = text.Trim();
            SaveConfig(configName);

            PortStatusText.Text = $"Конфиг \"{configName}\" сохранён";
            PortStatusText.Foreground = WpfBrushes.LightGreen;
        }

        private void DeleteConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedName = ConfigComboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedName))
                return;

            var result = MessageBox.Show(
                $"Удалить конфиг \"{selectedName}\"?",
                "Удаление",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var path = GetConfigPath(selectedName);
                if (File.Exists(path))
                    File.Delete(path);

                RefreshConfigList();
                LoadLastOrFirstConfig();

                PortStatusText.Text = "Конфигурация удалена";
                PortStatusText.Foreground = WpfBrushes.OrangeRed;
            }
            catch
            {
            }
        }

        private void ConfigComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoadingConfigList || _isApplyingConfig)
                return;

            var selectedName = ConfigComboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selectedName))
                return;

            LoadConfig(selectedName);

            PortStatusText.Text = $"Загружен конфиг: {selectedName}";
            PortStatusText.Foreground = WpfBrushes.LightGreen;
        }

        private void SendConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_ledPort.IsOpen)
            {
                PortStatusText.Text = "Сначала выбери COM-порт";
                PortStatusText.Foreground = WpfBrushes.OrangeRed;
                return;
            }

            var selectedName = ConfigComboBox.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedName))
                SaveConfig(selectedName);

            _ledPort.SendAllKeyConfigs(KeysBoxes);

            PortStatusText.Text = "Конфигурация отправлена на контроллер";
            PortStatusText.Foreground = WpfBrushes.LightGreen;
        }

        private void RefreshPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                Array.Sort(ports);
                PortComboBox.ItemsSource = ports;
            }
            catch
            {
                PortComboBox.ItemsSource = null;
            }
        }

        private void RefreshPortsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshPorts();
        }

        private void PortComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var portName = PortComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(portName))
            {
                try
                {
                    _ledPort.Open(portName);
                    PortStatusText.Text = "Открыт " + portName;
                    PortStatusText.Foreground = WpfBrushes.LightGreen;
                }
                catch (Exception ex)
                {
                    PortStatusText.Text = "Ошибка: " + ex.Message;
                    PortStatusText.Foreground = WpfBrushes.OrangeRed;
                }
            }
            else
            {
                _ledPort.Close();
                PortStatusText.Text = "Порт не выбран";
                PortStatusText.Foreground = WpfBrushes.LightGray;
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            PortComboBox.SelectedItem = null;
            _ledPort.Close();
            PortStatusText.Text = "Порт не выбран";
            PortStatusText.Foreground = WpfBrushes.LightGray;

            foreach (var box in KeysBoxes)
            {
                box.AppPath = null;
                box.AppName = "Нет программы";
                box.Icon = null;

                box.CustomTitle = null;
                box.CustomImage = null;
                box.CustomImagePath = null;

                box.Type = KeyBoxType.App;
                box.Mode = LedMode.Toggle;
            }
        }

        private void InitTrayIcon()
        {
            _trayIcon = new Forms.NotifyIcon();

            try
            {
                var uri = new Uri("pack://application:,,,/Assets/arsadeck.ico", UriKind.Absolute);
                var sri = Application.GetResourceStream(uri);

                if (sri?.Stream != null)
                {
                    using var iconStream = sri.Stream;
                    _trayIcon.Icon = new System.Drawing.Icon(iconStream);
                }
                else
                {
                    _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _trayIcon.Visible = true;
            _trayIcon.Text = "ARSADECK";

            _trayIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
                Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            var selectedName = ConfigComboBox.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedName))
                SaveConfig(selectedName);

            UnhookKeyboard();
            _ledPort.Dispose();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (e.OriginalSource is DependencyObject d)
            {
                var current = d;

                while (current != null)
                {
                    if (current is System.Windows.Controls.Button ||
                        current is System.Windows.Controls.ComboBox ||
                        current is System.Windows.Controls.TextBox)
                        return;

                    current = VisualTreeHelper.GetParent(current);
                }
            }

            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}