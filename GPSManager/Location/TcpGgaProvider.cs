using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using GPSManager.Util;

namespace GPSManager.Location
{
    class TcpGgaProvider : IGgaProvider, IConnectable
    {
        private const int ReadingTimeout = 2000;
        private const int ConnectionRetryInterval = 1000;

        private TcpClient client;
        private NetworkStream stream;
        private TextReader reader;
        private volatile bool run;
        private bool isConnected;

        public string Host { get; set; }
        public ushort Port { get; set; }
        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if(value != isConnected)
                {
                    isConnected = value;
                    (isConnected ? Connected : Disconnected)?.Invoke();
                }
            }
        }

        public event Action<Gga> GgaProvided;
        public event Action Connected;
        public event Action Disconnected;

        public TcpGgaProvider(string host, ushort port)
        {
            Host = host;
            Port = port;
            Start();
        }

        private async void Start()
        {
            run = true;
            while (run)
            {
                try
                {
                    await ProcessClient();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    await Task.Delay(ConnectionRetryInterval);
                }
            }
        }

        private async Task ProcessClient()
        {
            using (client = new TcpClient())
            {
                IsConnected = false;
                await client.ConnectAsync(Host, Port);
                IsConnected = true;

                using (stream = client.GetStream())
                using (reader = new StreamReader(stream))
                {
                    while (client.Connected)
                    {
                        var line = await ReadLineAsyncWithTimeout(reader, ReadingTimeout);
                        if(line == null)
                        {
                            break;
                        }
                        if (Gga.TryParse(line, out var gga))
                        {
                            GgaProvided?.Invoke(gga);
                        }
                    }
                }
            }
        }

        private static async Task<string> ReadLineAsyncWithTimeout(TextReader reader, int timeout)
        {
            using (var tokenSource = new CancellationTokenSource(timeout))
            {
                try
                {
                    return await reader.ReadLineAsync().WithCancellation(tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов
        
        public void Dispose()
        {
            if (disposedValue)
            {
                return;
            }

            run = false;
            IsConnected = false;
            stream?.Close();
            client?.Close();

            disposedValue = true;
        }
        #endregion
    }
}
