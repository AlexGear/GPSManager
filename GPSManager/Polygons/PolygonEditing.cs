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
using System.Windows.Input;

namespace GPSManager.Polygons
{
    class PolygonEditing
    {
        private const double VertexInsertionDistance = 10;

        private MapControl mapControl;
        private WritableLayer polygonLayer;
        private WritableLayer draggingPointsLayer;
        private DraggingFeature draggingFeature;
        private Point draggingOffset;
        private Feature insertionPreviewFeature;

        public Polygon editedPolygon { get; private set; }

        public PolygonEditing(MapControl mapControl, WritableLayer polygonLayer)
        {
            this.mapControl = mapControl;
            this.polygonLayer = polygonLayer;

            draggingPointsLayer = new WritableLayer { Style = CreateDraggingLayerStyle(0.8f) };
            this.mapControl.Map.Layers.Add(draggingPointsLayer);

            this.mapControl.PreviewMouseLeftButtonDown += OnPreviewMouseDown;
            this.mapControl.PreviewMouseMove += OnPreviewMouseMove;
            this.mapControl.PreviewMouseLeftButtonUp += OnPreviewMouseUp;
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
            foreach(var vertex in polygon.Vertices)
            {
                draggingPointsLayer.Add(new DraggingFeature(polygon, vertex));
            }
            draggingPointsLayer.Refresh();
        }

        public bool EndEditing()
        {
            draggingPointsLayer.Clear();
            draggingPointsLayer.Refresh();
            if (editedPolygon != null)
            {
                DB.UpdatePolygon(editedPolygon);
                editedPolygon = null;
                return true;
            }
            return false;
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var mouseScreenPoint = e.GetPosition(mapControl).ToMapsui();
            var mouseWorldPoint = ScreenPointToGlobal(mouseScreenPoint);
            var extent = mouseWorldPoint - ScreenPointToGlobal(mouseScreenPoint - new Point(10, 10));
            var boundingBox = new BoundingBox(mouseWorldPoint - extent, mouseWorldPoint + extent);

            var features = draggingPointsLayer.GetFeaturesInView(boundingBox, resolution: 1f);

            var draggingFeature = features.OfType<DraggingFeature>().FirstOrDefault();
            if(draggingFeature != null)
            {
                // Preventing map panning
                e.Handled = true;

                this.draggingFeature = draggingFeature;
                draggingOffset = mouseWorldPoint - draggingFeature.Vertex;
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {   
            if (draggingFeature != null)
            {
                Point mousePoint = ScreenPointToGlobal(e.GetPosition(mapControl).ToMapsui());
                draggingFeature.Vertex = mousePoint - draggingOffset;
                draggingPointsLayer.Refresh();
                polygonLayer.Refresh();
            }
            else if(editedPolygon != null)
            {
                Point mouseScreenPoint = e.GetPosition(mapControl).ToMapsui();
                Point mouseWorldPoint = ScreenPointToGlobal(mouseScreenPoint);
                double distance = Math.Abs(editedPolygon.Geometry.Distance(mouseWorldPoint));
                double screenDistance = GlobalPointToScreen(mouseWorldPoint + new Point(distance, 0)).X - mouseScreenPoint.X;
                if(screenDistance > VertexInsertionDistance)
                {
                    if(insertionPreviewFeature != null)
                    {
                        draggingPointsLayer.TryRemove(insertionPreviewFeature);
                        insertionPreviewFeature = null;
                    }
                    return;
                }

                IList<Point> vertices = editedPolygon.Vertices;
                Point prevVertex = vertices[vertices.Count - 1];
                foreach(var vertex in vertices)
                {
                    Point perpendicularBase = GetPerpendicularBase(mouseWorldPoint, prevVertex, vertex);
                    double perpendicularLength = mouseWorldPoint.Distance(perpendicularBase);
                    bool isThisSegemntClosest = Math.Abs(perpendicularLength - distance) <= 0.1;
                    if (isThisSegemntClosest)
                    {
                        if(insertionPreviewFeature == null)
                        {
                            insertionPreviewFeature = new Feature();
                        }
                        insertionPreviewFeature.Geometry = perpendicularBase;
                        draggingPointsLayer.Add(insertionPreviewFeature);
                    }
                    prevVertex = vertex;
                }
            }
        }

        private static Point GetPerpendicularBase(Point m, Point a, Point b)
        {
            Point am = m - a;
            Point bm = m - b;
            Point bmNorm = bm * (1 / Math.Sqrt(bm.X * bm.X + bm.Y * bm.Y));
            Point ah = bmNorm * (am.X * bmNorm.X + am.Y * bmNorm.Y);
            return a + ah;
        }

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            draggingFeature = null;
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

        private Point ScreenPointToGlobal(Point screenPoint)
        {
            return mapControl.Map.Viewport.ScreenToWorld(screenPoint);
        }

        private Point GlobalPointToScreen(Point worldPoint)
        {
            return mapControl.Map.Viewport.WorldToScreen(worldPoint);
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

                this.vertices = polygon.Vertices;
                this.vertex = vertex;

                Geometry = vertex;
            }
        }
    }
}
