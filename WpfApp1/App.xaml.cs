using System;
using System.Windows;
using System.IO;

namespace WpfApp1
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                File.AppendAllText("startup.log", DateTime.Now + " App starting" + Environment.NewLine);
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                File.AppendAllText("startup.log", DateTime.Now + " OnStartup OK" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup.log", DateTime.Now + " ERROR: " + ex + Environment.NewLine);
                throw;
            }
        }
    }

}
