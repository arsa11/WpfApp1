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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using WpfApp1.Config;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<KeyBox> KeysBoxes { get; } = new ObservableCollection<KeyBox>();

        private readonly LedPortService _ledPort = new LedPortService();
        private readonly string _configPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "F13Launcher", "config.json");

        private NotifyIcon _trayIcon;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            InitKeyBoxes();
            HookKeyboard();
            InitTrayIcon();
            RefreshPorts();
            LoadConfig();

            foreach (var box in KeysBoxes)
            {
                box.PropertyChanged += KeyBox_PropertyChanged;
            }
        }

        private void InitKeyBoxes()
        {
            var startKey = Keys.F13;
            for (int i = 0; i < 12; i++)
            {
                KeysBoxes.Add(new KeyBox
                {
                    Index = i,
                    KeyCode = startKey + i,
                    Title = (startKey + i).ToString(),
                    AppName = "Нет программы",
                    Type = KeyBoxType.App,
                    CustomTitle = "Пусто"
                });
            }
        }

        #region Keyboard hook

        private void HookKeyboard()
        {
            _proc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule!.ModuleName), 0);
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
                var key = (Keys)vkCode;

                if (key >= Keys.F13 && key <= Keys.F24)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnGlobalKeyPressed(key);
                    });
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #endregion

        private void OnGlobalKeyPressed(Keys key)
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

            _ledPort.SendKeyPress(box.Index);
            _ledPort.SendMode(box.Index, box.Mode);
        }

        private void KeyBox_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyBox.Mode))
            {
                if (sender is KeyBox box)
                {
                    _ledPort.SendMode(box.Index, box.Mode);
                }
            }
        }

        #region Context menu handlers

        private KeyBox? GetBoxFromMenu(object sender)
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
            if (box == null) return;
            PickExeForBox(box);
        }

        private void PickImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null) return;
            PickImageForBox(box);
        }

        private void RenameCustomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null) return;

            var current = box.CustomTitle ?? "";
            var text = Microsoft.VisualBasic.Interaction.InputBox(
                "Название для клетки:",
                "Переименовать",
                current);

            if (!string.IsNullOrWhiteSpace(text))
            {
                box.CustomTitle = text.Trim();
                box.Type = KeyBoxType.Custom;
            }
        }

        private void ResetCustomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var box = GetBoxFromMenu(sender);
            if (box == null) return;

            box.CustomTitle = "Пусто";
            box.CustomImage = null;
            box.CustomImagePath = null;

            box.Type = KeyBoxType.App;
        }

        #endregion

        #region Pick helpers

        private void PickExeForBox(KeyBox box)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Приложения (*.exe)|*.exe|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true)
                return;

            box.AppPath = dlg.FileName;
            box.AppName = Path.GetFileNameWithoutExtension(dlg.FileName);
            box.Type = KeyBoxType.App;

            try
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(dlg.FileName);
                if (icon != null)
                {
                    using (var bmp = icon.ToBitmap())
                    {
                        var hBitmap = bmp.GetHbitmap();
                        var img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap, IntPtr.Zero, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        box.Icon = img;
                    }
                }
            }
            catch
            {
                box.Icon = null;
            }
        }

        private void PickImageForBox(KeyBox box)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Картинки|*.png;*.jpg;*.jpeg;*.bmp;*.ico|Все файлы|*.*"
            };

            if (dlg.ShowDialog() != true)
                return;

            box.CustomImagePath = dlg.FileName;
            box.CustomImage = LoadImageNoLock(dlg.FileName);
            box.Type = KeyBoxType.Custom;

            if (string.IsNullOrWhiteSpace(box.CustomTitle) || box.CustomTitle == "Пусто")
            {
                box.CustomTitle = Path.GetFileNameWithoutExtension(dlg.FileName);
            }
        }

        private static ImageSource? LoadImageNoLock(string filePath)
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

        #endregion

        #region Config save/load

        private void SaveConfig()
        {
            try
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

                var dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, json);
            }
            catch
            {
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return;

                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg == null)
                    return;

                RefreshPorts();
                if (!string.IsNullOrEmpty(cfg.ComPort))
                {
                    PortComboBox.SelectedItem = cfg.ComPort;
                }

                foreach (var kc in cfg.Keys)
                {
                    var box = KeysBoxes.FirstOrDefault(b => b.Index == kc.Index);
                    if (box == null) continue;

                    box.Mode = kc.Mode;
                    box.Type = kc.Type;

                    // App
                    box.AppPath = kc.AppPath;
                    if (!string.IsNullOrWhiteSpace(kc.AppPath))
                    {
                        box.AppName = Path.GetFileNameWithoutExtension(kc.AppPath);
                        try
                        {
                            var icon = System.Drawing.Icon.ExtractAssociatedIcon(kc.AppPath);
                            if (icon != null)
                            {
                                using (var bmp = icon.ToBitmap())
                                {
                                    var hBitmap = bmp.GetHbitmap();
                                    var img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                        hBitmap, IntPtr.Zero, Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions());
                                    box.Icon = img;
                                }
                            }
                        }
                        catch
                        {
                            box.Icon = null;
                        }
                    }
                    else
                    {
                        box.AppName = "Нет программы";
                    }

                    // Custom
                    box.CustomTitle = string.IsNullOrWhiteSpace(kc.CustomTitle) ? "Пусто" : kc.CustomTitle;
                    box.CustomImagePath = kc.CustomImagePath;

                    if (!string.IsNullOrWhiteSpace(kc.CustomImagePath) && File.Exists(kc.CustomImagePath))
                    {
                        box.CustomImage = LoadImageNoLock(kc.CustomImagePath);
                    }
                    else
                    {
                        box.CustomImage = null;
                    }

                    if (!string.IsNullOrWhiteSpace(box.CustomImagePath) && box.CustomImage != null)
                        box.Type = KeyBoxType.Custom;
                    else if (!string.IsNullOrWhiteSpace(box.AppPath))
                        box.Type = KeyBoxType.App;
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Serial port UI

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

        private void PortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var portName = PortComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(portName))
            {
                try
                {
                    _ledPort.Open(portName);
                    PortStatusText.Text = "Открыт " + portName;
                    PortStatusText.Foreground = Brushes.LightGreen;
                }
                catch (Exception ex)
                {
                    PortStatusText.Text = "Ошибка: " + ex.Message;
                    PortStatusText.Foreground = Brushes.OrangeRed;
                }
            }
            else
            {
                _ledPort.Close();
                PortStatusText.Text = "Порт не выбран";
                PortStatusText.Foreground = Brushes.LightGray;
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            PortComboBox.SelectedItem = null;
            _ledPort.Close();
            PortStatusText.Text = "Порт не выбран";
            PortStatusText.Foreground = Brushes.LightGray;

            foreach (var box in KeysBoxes)
            {
                box.AppPath = null;
                box.AppName = "Нет программы";
                box.Icon = null;

                box.CustomTitle = "Пусто";
                box.CustomImage = null;
                box.CustomImagePath = null;

                box.Type = KeyBoxType.App;
                box.Mode = LedMode.Toggle;
            }

            try
            {
                if (File.Exists(_configPath))
                    File.Delete(_configPath);
            }
            catch
            {
            }
        }

        #endregion

        #region Tray

        private void InitTrayIcon()
        {
            _trayIcon = new NotifyIcon();

            try
            {
                _trayIcon.Icon = new System.Drawing.Icon("Assets/arsadeck.ico");
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
            {
                Hide();
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            SaveConfig();
            UnhookKeyboard();
            _ledPort.Dispose();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }

        // Перетаскивание окна за верхнюю область
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
