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

namespace GPSManager
{
    class PolygonEditing
    {
        private MapControl mapControl;
        private WritableLayer polygonLayer;
        private WritableLayer draggingPointsLayer;
        private Polygon editedPolygon;
        private DraggingFeature draggingFeature;
        private Point draggingOffset; 

        public PolygonEditing(MapControl mapControl, WritableLayer polygonLayer)
        {
            this.mapControl = mapControl;
            this.polygonLayer = polygonLayer;

            draggingPointsLayer = new WritableLayer { Style = CreateLayerStyle(0.8f) };
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
            if(draggingFeature != null)
            {
                Point mousePoint = ScreenPointToGlobal(e.GetPosition(mapControl).ToMapsui());
                draggingFeature.Vertex = mousePoint - draggingOffset;
                draggingPointsLayer.Refresh();
                polygonLayer.Refresh();
            }
        }

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            draggingFeature = null;
        }

        private static IStyle CreateLayerStyle(double scale)
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
            var globalPosition = mapControl.Map.Viewport.ScreenToWorld(screenPoint);
            return globalPosition;
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
