using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GPSManager.Location;
using GPSManager.Storage;
using GPSManager.Util;
using GPSManager.Polygons;
using Mapsui.Layers;
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
        private PolygonEditing polygonEditing;

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
            polygonEditing = new PolygonEditing(mapControl, polygonLayer);

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

        private ContextMenu CreatePolygonContextMenu(Polygon polygon)
        {
            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(CreateRenameItem());
            contextMenu.Items.Add(CreateModifyItem());
            contextMenu.Items.Add(CreateRemoveItem());
            return contextMenu;

            ///////// BUTTONS CREATION LOCAL FUNCS
            MenuItem CreateRenameItem()
            {
                var item = new MenuItem
                {
                    Header = "Переименовать",
                    Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Rename.png")) }
                };
                item.Click += (s, e) =>
                {
                    RenamePolygonDialog(polygon);
                    DB.UpdatePolygon(polygon);
                };
                return item;
            }
            MenuItem CreateRemoveItem()
            {
                var item = new MenuItem
                {
                    Header = "Удалить полигон",
                    Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Delete.png")) }
                };
                item.Click += (s, e) =>
                {
                    if (DB.RemovePolygon(polygon))
                    {
                        polygonLayer.TryRemove(polygon);
                        polygonLayer.Refresh();
                    }
                };
                return item;
            }
            MenuItem CreateModifyItem()
            {
                var item = new MenuItem
                {
                    Header = "Редактировать",
                    Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Edit.png")) }
                };
                item.Click += (s, e) =>
                {
                    polygonEditing.BeginEditing(polygon);
                };
                return item;
            }
            ///////// END BUTTONS CREATION LOCAL FUNCS
        }

        private void OnGgaProvided(Gga gga)
        {
            ggaProvider.GgaProvided -= OnGgaProvided;
            ZoomToPoint(gga);
        }

        private void ZoomToPoint(Gga gga)
        {
            var center = Gga.ToMapsuiPoint(gga);
            var extent = new Mapsui.Geometries.Point(1000, 1000);
            mapControl.ZoomToBox(center - extent, center + extent);
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
            polygonEditing.EndEditing();
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
                
                if(polygon != null)
                {
                    RenamePolygonDialog(polygon);

                    try
                    {
                        DB.InsertPolygonAndAssingID(polygon);
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Ошибка при добавлении записи в базу данных:\n" + ex.ToString(),
                            "Ошибка добавления в БД",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                return polygon;
            }
            return null;
        }

        private bool RenamePolygonDialog(Polygon polygon)
        {
            var nameDialog = new PolygonNameDialog(polygon.Name);
            if (true == nameDialog.ShowDialog())
            {
                polygon.Name = nameDialog.PolygonName;
                polygonLayer.Refresh();
                return true;
            }
            return false;
        }

        private void OnMapRightClick(object sender, MouseButtonEventArgs e)
        {
            UnhighlightAllPolygons();

            bool wasntDrawing = EndPolygonDrawing() == null;
            bool wasntEditing = !polygonEditing.EndEditing();
            if (wasntDrawing && wasntEditing)
            {
                var clickScreenPos = e.GetPosition(mapControl).ToMapsui();
                var clickWorldPos = mapControl.Map.Viewport.ScreenToWorld(clickScreenPos);
                IEnumerable<Polygon> polygons = GetPolygonsAt(clickWorldPos);
                //var info = mapControl.GetMapInfo(e.GetPosition(mapControl).ToMapsui());
                int count = polygons.Count();
                if (count == 0)
                {
                    return;
                }
                if (count == 1)
                {
                    OnPolygonRightClick(polygons.First());
                }
                else
                {
                    var contextMenu = new ContextMenu();
                    contextMenu.Items.Add(new Label
                    {
                        Content = "Выберете полигон:",
                        IsEnabled = false
                    });
                    foreach (var polygon in polygons) {
                        var item = new MenuItem
                        {
                            Header = string.IsNullOrWhiteSpace(polygon.Name) ? "<безымянный полигон>" : polygon.Name
                        };
                        item.GotFocus += (s, ee) => { polygon.IsHighlighted = true; polygonLayer.Refresh(); };
                        item.LostFocus += (s, ee) => { polygon.IsHighlighted = false; polygonLayer.Refresh(); };
                        item.Click += (s, ee) => OnPolygonRightClick(polygon);
                        contextMenu.Items.Add(item);
                    }
                    contextMenu.IsOpen = true;
                }
            }

            IEnumerable<Polygon> GetPolygonsAt(Mapsui.Geometries.Point point)
            {
                var boundingBox = new Mapsui.Geometries.BoundingBox(point, point);
                var polygons = polygonLayer.GetFeaturesInView(boundingBox, resolution: 1f).OfType<Polygon>();
                return polygons.Where(p => p.Geometry.Distance(point) <= 0);
            }
        }

        private void OnMapLeftClick(object sender, MouseButtonEventArgs e)
        {
            UnhighlightAllPolygons();
        }

        private void OnPolygonRightClick(Polygon polygon)
        {
            polygon.IsHighlighted = true;
            polygonLayer.Refresh();

            var contextMenu = CreatePolygonContextMenu(polygon);
            contextMenu.IsOpen = true;
        }

        private void UnhighlightAllPolygons()
        {
            foreach (var polygon in polygonLayer.GetFeatures().OfType<Polygon>())
            {
                polygon.IsHighlighted = false;
            }
            polygonLayer.Refresh();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ggaProvider?.Dispose();
            polygonEditing.EndEditing();
        }
    }
}
