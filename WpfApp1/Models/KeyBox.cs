using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Media;

namespace WpfApp1.Models
{
    public enum LedMode
    {
        Toggle,
        Hold
    }

    public enum KeyBoxType
    {
        App,
        Custom
    }

    public class KeyBox : INotifyPropertyChanged
    {
        private bool _isActive;
        private string _title = "";
        private string _appName = "Нет программы";
        private string? _appPath;
        private ImageSource? _icon;
        private LedMode _mode = LedMode.Toggle;

        private KeyBoxType _type = KeyBoxType.App;
        private string _customTitle = "Пусто";
        private string? _customImagePath;
        private ImageSource? _customImage;

        public int Index { get; set; }            // 0..11
        public Keys KeyCode { get; set; }         // F13..F24

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string AppName
        {
            get => _appName;
            set { _appName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string? AppPath
        {
            get => _appPath;
            set { _appPath = value; OnPropertyChanged(); }
        }

        public ImageSource? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayIcon)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public LedMode Mode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); }
        }

        public KeyBoxType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayIcon));
            }
        }

        public string CustomTitle
        {
            get => _customTitle;
            set { _customTitle = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string? CustomImagePath
        {
            get => _customImagePath;
            set { _customImagePath = value; OnPropertyChanged(); }
        }

        public ImageSource? CustomImage
        {
            get => _customImage;
            set { _customImage = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayIcon)); }
        }

        public string DisplayName => Type == KeyBoxType.App ? AppName : CustomTitle;
        public ImageSource? DisplayIcon => Type == KeyBoxType.App ? Icon : (CustomImage ?? Icon);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
