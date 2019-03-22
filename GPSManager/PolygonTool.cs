using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
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
    class PolygonTool
    {
        private MapControl mapControl;
        private MemoryLayer mapLayer;
        private Features features;

        private System.Windows.Point mouseDownPos;
        private Polygon currentPolygon;
        private Feature currentPolygonFeature;
        private Point previewPoint;
        private Feature previewPointFeature;

        public bool IsInDrawingMode { get; private set; }

        public PolygonTool(MapControl mapControl)
        {
            this.mapControl = mapControl;
            this.mapControl.MouseLeftButtonDown += OnLeftMouseDown;
            this.mapControl.MouseLeftButtonUp += OnLeftMouseUp;
            this.mapControl.MouseMove += OnMouseMove;

            features = new Features();
            mapLayer = new MemoryLayer
            {
                Name = "Polygons",
                Style = CreateLayerStyle()
            };
            this.mapControl.Map.Layers.Add(mapLayer);
        }

        private void OnFeatureInfo(object sender, Mapsui.UI.FeatureInfoEventArgs e)
        {
            foreach(var pair in e.FeatureInfo)
            {
                Console.WriteLine($"{pair.Key} = {pair.Value}");
            }
        }

        private IStyle CreateLayerStyle()
        {
            return new VectorStyle
            {
                Fill = new Brush(new Color(240, 20, 20, 70)),
                Outline = new Pen
                {
                    Color = new Color(240, 20, 20),
                    Width = 2
                }
            };
        }

        private void OnLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(!IsInDrawingMode)
            {
                if(e.RightButton == MouseButtonState.Pressed)
                {
                    var info = mapControl.GetMapInfo(e.GetPosition(mapControl).ToMapsui());
                    // TODO: show context menu
                }
                return;
            }
            mouseDownPos = e.GetPosition(mapControl);
        }

        private void OnLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(!IsInDrawingMode)
            {
                return;
            }
            const double tolerance = 5;
            var pos = e.GetPosition(mapControl);
            if (Math.Abs(pos.X - mouseDownPos.X) > tolerance ||
                Math.Abs(pos.Y - mouseDownPos.Y) > tolerance)
            {
                return;
            }
            // previewPoint is already in currentPolygon.
            // Setting it null makes OnMouseMove not remove it from currentPolygon
            // thus it stays persistently
            previewPoint = null;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(!IsInDrawingMode)
            {
                return;
            }

            bool updateProvider = false;

            if(currentPolygon == null)
            {
                currentPolygon = new Polygon();
                features.Add(currentPolygonFeature = new Feature { Geometry = currentPolygon });
                updateProvider = true;
            }
            var vertices = currentPolygon.ExteriorRing.Vertices;
            if (previewPoint != null)
            {
                vertices.Remove(previewPoint);
            }
            previewPoint = GetGlobalPointFromEvent(e);
            vertices.Add(previewPoint);

            if(previewPointFeature == null)
            {
                features.Add(previewPointFeature = new Feature { Geometry = previewPoint });
                updateProvider = true;
            }
            previewPointFeature.Geometry = previewPoint;
            
            Update(updateProvider);
        }

        private Point GetGlobalPointFromEvent(MouseEventArgs e)
        {
            var screenPosition = e.GetPosition(mapControl).ToMapsui();
            var globalPosition = mapControl.Map.Viewport.ScreenToWorld(screenPosition);
            return globalPosition;
        }

        private void Update(bool updateProvider = false)
        {
            if (updateProvider)
            {
                mapLayer.DataSource = new MemoryProvider(features);
            }
            mapLayer.ViewChanged(true, mapLayer.Envelope, 1);
        }

        public void BeginDrawing()
        {
            if (IsInDrawingMode)
            {
                return;
            }

            IsInDrawingMode = true;
            mapControl.Cursor = Cursors.Pen;
        }

        public IEnumerable<Gga> EndDrawing()
        {
            if(!IsInDrawingMode)
            {
                return null;
            }

            IsInDrawingMode = false;
            Polygon result = currentPolygon;

            mapControl.Cursor = Cursors.Arrow;

            if(currentPolygon != null)
            {
                if (previewPoint != null)
                {
                    currentPolygon.ExteriorRing.Vertices.Remove(previewPoint);
                }
                if(currentPolygon.ExteriorRing.Vertices.Count == 0)
                {
                    features.Delete(currentPolygonFeature);
                }
            }
            if(previewPointFeature != null)
            {
                features.Delete(previewPointFeature);
            }
            Update(true);

            currentPolygon = null;
            currentPolygonFeature = null;
            previewPoint = null;
            previewPointFeature = null;

            return result.ExteriorRing.Vertices.Select(Gga.FromMapsuiPoint);
        }
    }
}
