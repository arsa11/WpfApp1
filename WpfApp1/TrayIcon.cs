using System.Windows.Forms;
using System;
using System.Windows.Forms;

namespace WpfApp1
{
    public class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;

        public event EventHandler ShowRequested;
        public event EventHandler ExitRequested;

        public TrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "F13-F24 Launcher"
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Открыть", null, (_, __) => ShowRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("Выход", null, (_, __) => ExitRequested?.Invoke(this, EventArgs.Empty));
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, __) => ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Flash()
        {
            _notifyIcon.BalloonTipTitle = "Кнопка сработала";
            _notifyIcon.BalloonTipText = "Запуск приложения...";
            _notifyIcon.ShowBalloonTip(1000);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
