using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} App starting{Environment.NewLine}");
            base.OnStartup(e);
            File.AppendAllText("startup.log", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} OnStartup OK{Environment.NewLine}");
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.AppendAllText("startup.log",
                $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} DispatcherUnhandledException: {e.Exception}{Environment.NewLine}");
            MessageBox.Show(e.Exception.ToString(), "DispatcherUnhandledException");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.AppendAllText("startup.log",
                $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} UnhandledException: {e.ExceptionObject}{Environment.NewLine}");
        }
    }
}