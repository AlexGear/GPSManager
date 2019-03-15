using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BruTile.Predefined;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Utilities;

namespace GPSManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Host = "192.168.55.250";
        private const ushort Port = 5555;

        private static readonly SolidColorBrush DisconnectedBrush = new SolidColorBrush(Color.FromRgb(228, 21, 21));
        private static readonly SolidColorBrush ConnectedBrush = new SolidColorBrush(Color.FromRgb(21, 228, 30));
        private const string ConnectedStatusText = "Подключен";
        private const string DisconnectedStatusText = "Нет подключения";
        private const string PinResourcePath = "GPSManager.Resources.Pin.png";
        private IGgaProvider ggaProvider;
        private ILayer oldLayer;
        private bool ggaProvidedFirstTime = true;

        private Mapsui.Styles.IStyle pointStyle;

        public MainWindow()
        {
            InitializeComponent();
            pointStyle = CreatePointStyle(scale: 0.8);
            InitializeMapControl();
            OnGgaProviderDisconnected();
        }

        private void InitializeMapControl()
        {
            mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        }

        private static Mapsui.Styles.IStyle CreatePointStyle(double scale)
        {
            try
            {
                var bitmapId = GetBitmapIdForEmbeddedResource(PinResourcePath);
                return new Mapsui.Styles.SymbolStyle
                {
                    BitmapId = bitmapId,
                    SymbolType = Mapsui.Styles.SymbolType.Bitmap,
                    SymbolScale = scale,
                    SymbolOffset = new Mapsui.Styles.Offset(0.0, 0.5, true)
                };
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Resource loading failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new Mapsui.Styles.LabelStyle
                {
                    BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Transparent),
                    ForeColor = Mapsui.Styles.Color.White,
                };
            }
        }

        private static int GetBitmapIdForEmbeddedResource(string imagePath)
        {
            var assembly = typeof(MainWindow).Assembly;
            var image = assembly.GetManifestResourceStream(imagePath);
            return Mapsui.Styles.BitmapRegistry.Instance.Register(image);
        }

        //class Placeholder : IConnectable, IGgaProvider
        //{
        //    public bool IsConnected => true;

        //    public event Action Connected;
        //    public event Action Disconnected;
        //    public event Action<Gga> GgaProvided;

        //    public Placeholder()
        //    {
        //        F();
        //    }

        //    async void F()
        //    {
        //        await Task.Delay(2000);
        //        Connected?.Invoke();
        //        GgaProvided?.Invoke(new Gga(55.046307, 82.963026));
        //        await Task.Delay(4000);
        //        Disconnected?.Invoke();
        //    }

        //    public void Dispose()
        //    {

        //    }
        //}

        private void OnWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //var tcpGgaProvider = new Placeholder();
            var tcpGgaProvider = new TcpGgaProvider(Host, Port);

            ggaProvider = tcpGgaProvider;
            ggaProvider.GgaProvided += OnGgaProvided;

            tcpGgaProvider.Connected += OnGgaProviderConnected;
            tcpGgaProvider.Disconnected += OnGgaProviderDisconnected;
        }

        private void OnGgaProvided(Gga gga)
        {
            if(oldLayer != null)
            {
                mapControl.Map.Layers.Remove(oldLayer);
            }
            mapControl.Map.Layers.Add(oldLayer = CreateLayer(gga));

            if (ggaProvidedFirstTime)
            {
                ggaProvidedFirstTime = false;
                ZoomToPoint(gga);
            }
        }

        private void ZoomToPoint(Gga gga)
        {
            var center = GgaToPoint(gga);
            var offset = new Mapsui.Geometries.Point(1000, 1000);
            mapControl.ZoomToBox(center - offset, center + offset);
        }

        private void OnGgaProviderConnected()
        {
            connectStatusEllipse.Fill = ConnectedBrush;
            connectStatusLabel.Content = ConnectedStatusText;
        }

        private void OnGgaProviderDisconnected()
        {
            connectStatusEllipse.Fill = DisconnectedBrush;
            connectStatusLabel.Content = DisconnectedStatusText;
        }

        private ILayer CreateLayer(Gga gga)
        {
            var features = new Features
            {
                CreatePoint(gga)
            };

            var memoryProvider = new MemoryProvider(features);
            return new MemoryLayer
            {
                Name = "Points",
                DataSource = memoryProvider,
                Style = null
            };
        }

        private Feature CreatePoint(Gga gga)
        {
            var feature = new Feature
            {
                Geometry = GgaToPoint(gga)
            };
            feature.Styles.Add(pointStyle);
            return feature;
        }

        private static Mapsui.Geometries.Point GgaToPoint(Gga gga) {
            return SphericalMercator.FromLonLat(gga.Longitude, gga.Latitude);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ggaProvider?.Dispose();
        }
    }
}
