using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using Mapsui.UI.Wpf;
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

        private IGgaProvider ggaProvider;

        private WritableLayer polygonLayer;

        private PolygonTool polygonTool;

        public MainWindow()
        {
            InitializeComponent();

            connectStatusEllipse.Fill = DisconnectedBrush;
            connectStatusLabel.Content = DisconnectedStatusText;
        }

        class Placeholder : IConnectable, IGgaProvider
        {
            public bool IsConnected => true;

            public event Action Connected;
            public event Action Disconnected;
            public event Action<Gga> GgaProvided;

            public Placeholder()
            {
                F();
            }

            async void F()
            {
                await Task.Delay(2000);
                Connected?.Invoke();
                GgaProvided?.Invoke(new Gga(55.046307, 82.963026));
                await Task.Delay(4000);
                Disconnected?.Invoke();
            }

            public void Dispose()
            {

            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                EndPolygonDrawing();
            }
        }

        private void OnWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            InitializeGgaProvieder();
            polygonLayer = new PolygonLayer();
            InitializeMapControl();
            polygonTool = new PolygonTool(mapControl, polygonLayer);

            try
            {
                DB.Load();
                polygonLayer.AddRange(DB.Polygons);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Ошибка загрузки базы данных:\n" + ex.ToString(), "Ошибка загрузки БД",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeGgaProvieder()
        {
            var tcpGgaProvider = new Placeholder();
            //var tcpGgaProvider = new TcpGgaProvider(Host, Port);

            ggaProvider = tcpGgaProvider;
            ggaProvider.GgaProvided += OnGgaProvided;

            tcpGgaProvider.Connected += OnGgaProviderConnected;
            tcpGgaProvider.Disconnected += OnGgaProviderDisconnected;
        }

        private void InitializeMapControl()
        {
            mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            mapControl.Map.Layers.Add(new CurrentLocationLayer(ggaProvider));

            mapControl.Map.Layers.Add(polygonLayer);
            mapControl.Map.InfoLayers.Add(polygonLayer);

            mapControl.MouseLeftButtonDown += OnMapLeftClick;
            mapControl.MouseRightButtonDown += OnMapRightClick;
        }

        private void OnGgaProvided(Gga gga)
        {
            ggaProvider.GgaProvided -= OnGgaProvided;
            ZoomToPoint(gga);
        }

        private void ZoomToPoint(Gga gga)
        {
            var center = Gga.ToMapsuiPoint(gga);
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

        private void PolygonTool_Checked(object sender, RoutedEventArgs e)
        {
            polygonTool.BeginDrawing();
        }

        private void PolygonTool_Unchecked(object sender, RoutedEventArgs e)
        {
            EndPolygonDrawing();
        }
        
        private Polygon EndPolygonDrawing()
        {
            if(polygonTool.IsInDrawingMode)
            {
                var polygon = polygonTool.EndDrawing();

                polygonToolButton.IsChecked = false;

                var id = DB.InsertPolygon(polygon);
                Console.WriteLine(id);
                return polygon;
            }
            return null;
        }

        private void OnMapRightClick(object sender, MouseButtonEventArgs e)
        {
            UnhighlightAllPolygons();

            bool wasntDrawing = EndPolygonDrawing() == null;
            if (wasntDrawing)
            {
                var info = mapControl.GetMapInfo(e.GetPosition(mapControl).ToMapsui());
                if (info.Feature is Polygon polygon)
                {
                    OnPolygonRightClick(polygon);
                }
            }
        }

        private void OnMapLeftClick(object sender, MouseButtonEventArgs e)
        {
            UnhighlightAllPolygons();
        }

        private void OnPolygonRightClick(Polygon polygon)
        {
            polygon.IsHighlighed = true;
            polygonLayer.ViewChanged(true, polygonLayer.Envelope, resolution: 1);
        }

        private void UnhighlightAllPolygons()
        {
            foreach (var polygon in polygonLayer.GetFeatures().OfType<Polygon>())
            {
                polygon.IsHighlighed = false;
            }
            polygonLayer.ViewChanged(true, polygonLayer.Envelope, resolution: 1);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ggaProvider?.Dispose();
        }
    }
}
