using System;
using System.IO.Ports;
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

            _port.Open(); // откроется или кинет исключение
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
                    // игнорируем
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
                // можно добавить лог/сообщение
            }
        }

        public void SendKeyPress(int index)
        {
            if (!IsOpen) return;

            string line = $"KEY {index} PRESS";
            try
            {
                _port!.WriteLine(line);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
