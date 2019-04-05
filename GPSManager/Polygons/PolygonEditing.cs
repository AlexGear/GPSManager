using GPSManager.Storage;
using GPSManager.Util;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GPSManager.Polygons
{
    class PolygonEditing
    {
        private const double VertexInsertionDistance = 30;

        private IPolygonStorage storage;
        private MapControl mapControl;
        private WritableLayer polygonLayer;
        private WritableLayer draggingFeaturesLayer;
        private DraggingFeature draggingFeature;
        private Point draggingOffset;
        private InsertionPreviewFeature insertionPreviewFeature;

        public Polygon editedPolygon { get; private set; }

        public PolygonEditing(IPolygonStorage polygonStorage, MapControl mapControl, WritableLayer polygonLayer)
        {
            this.storage = polygonStorage;
            this.mapControl = mapControl;
            this.polygonLayer = polygonLayer;

            draggingFeaturesLayer = new WritableLayer { Style = CreateDraggingLayerStyle(0.8f) };
            this.mapControl.Map.Layers.Add(draggingFeaturesLayer);

            this.mapControl.PreviewMouseLeftButtonDown += OnPreviewLeftMouseDown;
            this.mapControl.PreviewMouseRightButtonDown += OnPreviewRightMouseDown;
            this.mapControl.PreviewMouseMove += OnPreviewMouseMove;
            this.mapControl.PreviewMouseLeftButtonUp += OnPreviewLeftMouseUp;
        }

        public void BeginEditing(Polygon polygon)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon));
            }

            if (editedPolygon != null)
            {
                EndEditing();
            }
            editedPolygon = polygon;
            foreach (var vertex in polygon.Vertices)
            {
                draggingFeaturesLayer.Add(new DraggingFeature(polygon, vertex));
            }
            draggingFeaturesLayer.Refresh();
        }

        public bool EndEditing()
        {
            draggingFeaturesLayer.Clear();
            draggingFeaturesLayer.Refresh();
            insertionPreviewFeature = null;
            if (editedPolygon != null)
            {
                storage.UpdatePolygon(editedPolygon);
                editedPolygon = null;
                return true;
            }
            return false;
        }

        private void OnPreviewLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (editedPolygon == null)
            {
                return;
            }

            Point mouseScreenPoint = e.GetPosition(mapControl).ToMapsui();
            var draggingFeature = GetFeaturesAtScreenPoint(mouseScreenPoint).OfType<DraggingFeature>().FirstOrDefault();

            if (draggingFeature == null && insertionPreviewFeature != null)
            {
                int insertedIndex = insertionPreviewFeature.Index;
                Point insertedVertex = insertionPreviewFeature.Vertex;
                draggingFeature = InsertVertex(insertedIndex, insertedVertex);

                draggingFeaturesLayer.TryRemove(insertionPreviewFeature);
                insertionPreviewFeature = null;
            }

            if (draggingFeature != null)
            {
                // Preventing map panning
                e.Handled = true;

                this.draggingFeature = draggingFeature;
                Point mouseWorldPoint = ScreenPointToGlobal(mouseScreenPoint);
                draggingOffset = mouseWorldPoint - draggingFeature.Vertex;
                return;
            }
        }

        private void OnPreviewRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mouseScreenPoint = e.GetPosition(mapControl).ToMapsui();
            var draggingFeature = GetFeaturesAtScreenPoint(mouseScreenPoint).OfType<DraggingFeature>().FirstOrDefault();
            if(draggingFeature != null)
            {
                // Preventing editing end
                e.Handled = true;

                var contextMenu = new ContextMenu();
                var deleteItem = new MenuItem {
                    Header = "Удалить вершину",
                    Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Delete.png")) }
                };
                deleteItem.Click += (_s, _e) => RemoveVertex(draggingFeature.Vertex);
                contextMenu.Items.Add(deleteItem);
                contextMenu.IsOpen = true;
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {   
            if(editedPolygon == null)
            {
                return;
            }
            if (draggingFeature != null)
            {
                Point mousePoint = ScreenPointToGlobal(e.GetPosition(mapControl).ToMapsui());
                draggingFeature.Vertex = mousePoint - draggingOffset;
                draggingFeaturesLayer.Refresh();
                polygonLayer.Refresh();
            }
            else
            {
                Point mouseScreenPoint = e.GetPosition(mapControl).ToMapsui();

                if (GetFeaturesAtScreenPoint(mouseScreenPoint).OfType<DraggingFeature>().Any())
                {
                    TryRemoveInsertionPreviewFeature();
                    return;
                }

                Point mouseWorldPoint = ScreenPointToGlobal(mouseScreenPoint);
                Point previewPoint;
                int index;
                GetInsertionPreviewPoint(mouseScreenPoint, mouseWorldPoint, out previewPoint, out index);

                if (previewPoint != null)
                {
                    UpdateInsertionPreviewFeature(previewPoint, index);
                }
                else
                {
                    TryRemoveInsertionPreviewFeature();
                }
            }

            ///////// Helper local funcs
            void TryRemoveInsertionPreviewFeature()
            {
                if (insertionPreviewFeature != null)
                {
                    draggingFeaturesLayer.TryRemove(insertionPreviewFeature);
                    insertionPreviewFeature = null;
                    draggingFeaturesLayer.Refresh();
                }
            }

            void UpdateInsertionPreviewFeature(Point previewPoint, int index)
            {
                if (insertionPreviewFeature == null)
                {
                    insertionPreviewFeature = new InsertionPreviewFeature(editedPolygon, previewPoint, index);
                    draggingFeaturesLayer.Add(insertionPreviewFeature);
                }
                insertionPreviewFeature.Update(previewPoint, index);
                draggingFeaturesLayer.Refresh();
            }

            void GetInsertionPreviewPoint(Point mouseScreenPoint, Point mouseWorldPoint, out Point previewPoint, out int index)
            {
                IList<Point> vertices = editedPolygon.Vertices;
                previewPoint = null;
                index = -1;
                double minDistance = double.PositiveInfinity;
                int prevI = vertices.Count - 1;
                for (int i = 0; i < vertices.Count; i++)
                {

                    Point closestPoint = GetClosestPoint(mouseWorldPoint, vertices[prevI], vertices[i]);
                    Point screenPerpBase = GlobalPointToScreen(closestPoint);
                    double screenDistance = screenPerpBase.Distance(mouseScreenPoint);
                    if (screenDistance < VertexInsertionDistance && screenDistance < minDistance)
                    {
                        minDistance = screenDistance;
                        previewPoint = closestPoint;
                        index = i;
                    }
                    prevI = i;
                }
            }
            ///////// End helper local funcs
        }

        private void OnPreviewLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            draggingFeature = null;
        }

        private IEnumerable<IFeature> GetFeaturesAtScreenPoint(Point point)
        {
            var worldPoint = ScreenPointToGlobal(point);
            var extent = worldPoint - ScreenPointToGlobal(point - new Point(10, 10));
            var boundingBox = new BoundingBox(worldPoint - extent, worldPoint + extent);
            var features = draggingFeaturesLayer.GetFeaturesInView(boundingBox, resolution: 1f);
            return features;
        }

        private DraggingFeature InsertVertex(int index, Point vertex)
        {
            DraggingFeature draggingFeature;
            editedPolygon.Vertices.Insert(index, vertex);
            draggingFeature = new DraggingFeature(editedPolygon, vertex);
            draggingFeaturesLayer.Add(draggingFeature);
            draggingFeaturesLayer.Refresh();
            return draggingFeature;
        }

        private void RemoveVertex(Point vertex)
        {
            editedPolygon.Vertices.Remove(vertex);
            var draggingFeatures = draggingFeaturesLayer.GetFeatures().OfType<DraggingFeature>();
            var correspondingDraggingFeature = draggingFeatures.FirstOrDefault(f => f.Vertex == vertex);
            if(correspondingDraggingFeature != null)
            {
                draggingFeaturesLayer.TryRemove(correspondingDraggingFeature);
                draggingFeaturesLayer.Refresh();
            }
        }

        private Point ScreenPointToGlobal(Point screenPoint)
        {
            return mapControl.Map.Viewport.ScreenToWorld(screenPoint);
        }

        private Point GlobalPointToScreen(Point worldPoint)
        {
            return mapControl.Map.Viewport.WorldToScreen(worldPoint);
        }

        private static Point GetClosestPoint(Point m, Point a, Point b)
        {
            Point am = m - a;
            Point bm = m - b;
            Point ab = b - a;

            if(Dot(am, ab) < 0)
            {
                return a;
            }
            // the same as (Dot(bm, -ab) < 0)
            else if(Dot(bm, ab) > 0)
            {
                return b;
            }

            Point abNorm = ab * (1 / Math.Sqrt(Dot(ab, ab)));
            Point ah = abNorm * Dot(am, abNorm);
            return a + ah;
        }

        private static double Dot(Point a, Point b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static IStyle CreateDraggingLayerStyle(double scale)
        {
            return new SymbolStyle
            {
                BitmapId = BitmapResources.GetBitmapIdForEmbeddedResource("GPSManager.Resources.Dragging.png"),
                SymbolType = SymbolType.Bitmap,
                SymbolScale = scale
            };
        }

        private class DraggingFeature : Feature
        {
            private readonly IList<Point> vertices;
            private Point vertex;

            public Point Vertex
            {
                get => vertex;
                set
                {
                    int index = vertices.IndexOf(Vertex);
                    if (index != -1)
                    {
                        vertices.RemoveAt(index);
                        vertices.Insert(index, value);
                    }
                    vertex = value;
                    Geometry = value;
                }
            }

            public DraggingFeature(Polygon polygon, Point vertex)
            {
                if (polygon == null)
                {
                    throw new ArgumentNullException(nameof(polygon));
                }

                this.vertex = vertex ?? throw new ArgumentNullException(nameof(vertex));
                this.vertices = polygon.Vertices;

                Geometry = vertex;
            }
        }

        private class InsertionPreviewFeature : Feature
        {
            private readonly IList<Point> vertices;
            public Point Vertex { get; private set; }
            public int Index { get; private set; }

            public InsertionPreviewFeature(Polygon polygon, Point vertex, int index)
            {
                if (polygon == null)
                {
                    throw new ArgumentNullException(nameof(polygon));
                }
                vertices = polygon.Vertices;
                Update(vertex, index);
            }

            public void Update(Point vertex, int index)
            {
                if (index < 0 || index >= vertices.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                Geometry = Vertex = vertex ?? throw new ArgumentNullException(nameof(vertex));
                Index = index;
            }
        }
    }
}
