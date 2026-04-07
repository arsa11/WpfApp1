using System.Collections.Generic;
using WpfApp1.Models;

namespace WpfApp1.Config
{
    public class KeyBoxConfig
    {
        public int Index { get; set; }
        public LedMode Mode { get; set; }

        public KeyBoxType Type { get; set; } = KeyBoxType.App;

        // App-режим
        public string AppPath { get; set; }

        // Custom-режим
        public string CustomTitle { get; set; }
        public string CustomImagePath { get; set; }
    }

    public class AppConfig
    {
        public string ComPort { get; set; }
        public List<KeyBoxConfig> Keys { get; set; } = new List<KeyBoxConfig>();
    }
}
