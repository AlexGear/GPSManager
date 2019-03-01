using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BruTile.Predefined;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Utilities;

namespace GPSManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const string Host = "192.168.55.250";
        private const ushort Port = 5555;
        private IGgaProvider ggaProvider;
        private ILayer oldLayer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMapControl();
        }

        private void InitializeMapControl()
        {
            mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        }

        private void OnGgaProvided(Gga gga)
        {
            if(oldLayer != null)
            {
                mapControl.Map.Layers.Remove(oldLayer);
            }
            mapControl.Map.Layers.Add(oldLayer = CreateLayer(gga));
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
            feature.Styles.Add(new LabelStyle { Text = "Default Label" });
            return feature;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ggaProvider?.Dispose();
        }

        private void OnWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ggaProvider = new TcpGgaProvider(Host, Port);
            ggaProvider.GgaProvided += OnGgaProvided;
        }
    }
}
