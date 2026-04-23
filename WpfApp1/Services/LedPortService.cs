using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class LedPortService : IDisposable
    {
        private SerialPort? _port;

        public string? CurrentPortName => _port?.PortName;
        public bool IsOpen => _port != null && _port.IsOpen;

        public void Open(string portName, int baudRate = 115200)
        {
            Close();

            _port = new SerialPort(portName, baudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                NewLine = "\n"
            };

            _port.Open();
        }

        public void Close()
        {
            if (_port != null)
            {
                try
                {
                    if (_port.IsOpen)
                        _port.Close();
                }
                catch
                {
                }
                finally
                {
                    _port.Dispose();
                    _port = null;
                }
            }
        }

        public void SendMode(int index, LedMode mode)
        {
            if (!IsOpen) return;

            string textMode = mode == LedMode.Toggle ? "TOGGLE" : "HOLD";
            string line = $"MODE {index} {textMode}";

            try
            {
                _port!.WriteLine(line);
            }
            catch
            {
            }
        }

        public void SendKeyConfig(int buttonNumber, string title)
        {
            if (!IsOpen) return;

            string safeTitle = NormalizeTitle(title);
            string line = $"D_{buttonNumber}_{safeTitle}";

            try
            {
                _port!.WriteLine(line);
            }
            catch
            {
            }
        }

        public void SendAllKeyConfigs(IEnumerable<KeyBox> boxes)
        {
            if (!IsOpen) return;

            foreach (var box in boxes.OrderBy(b => b.Index))
            {
                string title = string.IsNullOrWhiteSpace(box.DisplayName)
                    ? $"Key{box.Index + 1}"
                    : box.DisplayName;

                SendKeyConfig(box.Index + 1, title);
            }
        }

        private static string NormalizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Empty";

            string result = title.Trim();

            result = result.Replace("\r", " ");
            result = result.Replace("\n", " ");
            result = result.Replace("_", "-");

            while (result.Contains("  "))
                result = result.Replace("  ", " ");

            return result;
        }

        public void Dispose()
        {
            Close();
        }
    }
}