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

        private TcpGgaProvider ggaProvider;
        private ILayer oldLayer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMapControl();
            OnGgaProviderDisconnected();
        }

        private void InitializeMapControl()
        {
            mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        }

        private void OnWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ggaProvider = new TcpGgaProvider(Host, Port);
            ggaProvider.GgaProvided += OnGgaProvided;
            ggaProvider.Connected += OnGgaProviderConnected;
            ggaProvider.Disconnected += OnGgaProviderDisconnected;
        }

        private void OnGgaProvided(Gga gga)
        {
            if(oldLayer != null)
            {
                mapControl.Map.Layers.Remove(oldLayer);
            }
            mapControl.Map.Layers.Add(oldLayer = CreateLayer(gga));
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

        private static ILayer CreateLayer(Gga gga)
        {
            var features = new Features
            {
                CreatePoint(gga)
            };

            var memoryProvider = new MemoryProvider(features);
            return new MemoryLayer { Name = "Points with labels", DataSource = memoryProvider };
        }

        private static Feature CreatePoint(Gga gga)
        {
            var point = SphericalMercator.FromLonLat(gga.Longitude, gga.Latitude);
            var feature = new Feature { Geometry = point };
            feature.Styles.Add(new Mapsui.Styles.LabelStyle { Text = "Default Label" });
            return feature;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ggaProvider?.Dispose();
        }
    }
}
