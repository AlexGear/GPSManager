using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace GPSManager
{
    class TcpGgaProvider : IGgaProvider
    {
        public string Host { get; set; }
        public ushort Port { get; set; }

        private const int ReadingTimeout = 2000;
        private const int ConnectionRetryInterval = 1000;

        private TcpClient client;
        private NetworkStream stream;
        private TextReader reader;
        private volatile bool run;

        public event Action<Gga> GgaProvided;

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
                await client.ConnectAsync(Host, Port);

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
            stream?.Close();
            client?.Close();

            disposedValue = true;
        }
        #endregion
    }
}
