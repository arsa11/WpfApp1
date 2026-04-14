using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace WpfApp1.Models
{
    public enum KeyBoxType
    {
        App,
        Custom
    }

    public enum LedMode
    {
        Toggle,
        Hold
    }

    public class KeyBox : INotifyPropertyChanged
    {
        private int _index;
        private Forms.Keys _keyCode;
        private string _title = "";
        private string _appName = "Нет программы";
        private string? _appPath;
        private ImageSource? _icon;
        private string? _customTitle;
        private ImageSource? _customImage;
        private string? _customImagePath;
        private KeyBoxType _type = KeyBoxType.App;
        private LedMode _mode = LedMode.Toggle;
        private bool _isActive;

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        public Forms.Keys KeyCode
        {
            get => _keyCode;
            set
            {
                _keyCode = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string AppName
        {
            get => _appName;
            set
            {
                _appName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string? AppPath
        {
            get => _appPath;
            set
            {
                _appPath = value;
                OnPropertyChanged();
            }
        }

        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public string? CustomTitle
        {
            get => _customTitle;
            set
            {
                _customTitle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public ImageSource? CustomImage
        {
            get => _customImage;
            set
            {
                _customImage = value;
                OnPropertyChanged();
            }
        }

        public string? CustomImagePath
        {
            get => _customImagePath;
            set
            {
                _customImagePath = value;
                OnPropertyChanged();
            }
        }

        public KeyBoxType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        public LedMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CustomTitle))
                    return CustomTitle!;

                if (Type == KeyBoxType.App && !string.IsNullOrWhiteSpace(AppName))
                    return AppName;

                if (Type == KeyBoxType.Custom && !string.IsNullOrWhiteSpace(CustomImagePath))
                    return "Картинка";

                return "Пусто";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}