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
        public MainWindow()
        {
            InitializeComponent();
            InitializeMapControl();
        }

        private void InitializeMapControl()
        {
            mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            mapControl.Map.Layers.Add(CreateLayer());
        }

        public static ILayer CreateLayer()
        {
            var features = new Features
            {
                CreateSimplePoint()
            };

            var memoryProvider = new MemoryProvider(features);
            return new MemoryLayer { Name = "Points with labels", DataSource = memoryProvider };
        }

        private static Feature CreateSimplePoint()
        {
            var gga = GGA.Parse("$GNGGA,033632.40,5458.46129,N,08255.44558,E,1,04,4.21,103.1,M,-38.0,M,,*61");
            var point = SphericalMercator.FromLonLat(gga.Longitude, gga.Latitude);
            var feature = new Feature { Geometry = point };
            feature.Styles.Add(new LabelStyle { Text = "Default Label" });
            return feature;
        }
    }
}
