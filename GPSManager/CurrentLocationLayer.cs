using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GPSManager
{
    class CurrentLocationLayer : MemoryLayer
    {
        private const string PinResourcePath = "GPSManager.Resources.Pin.png";
        private Features features;

        public CurrentLocationLayer(IGgaProvider locationProvider)
        {
            if(locationProvider == null)
            {
                throw new ArgumentNullException(nameof(locationProvider));
            }
            locationProvider.GgaProvided += OnGgaProvided;

            features = new Features();

            Name = "Points";
            Style = CreatePointStyle(scale: 0.8);
        }

        private void OnGgaProvided(Gga gga)
        {
            features.Clear();
            features.Add(new Feature
            {
                Geometry = Gga.ToMapsuiPoint(gga)
            });
            DataSource = new MemoryProvider(features);
        }

        private static IStyle CreatePointStyle(double scale)
        {
            try
            {
                var bitmapId = GetBitmapIdForEmbeddedResource(PinResourcePath);
                return new SymbolStyle
                {
                    BitmapId = bitmapId,
                    SymbolType = SymbolType.Bitmap,
                    SymbolScale = scale,
                    SymbolOffset = new Offset(0.0, 0.5, true)
                };
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Resource loading failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new LabelStyle
                {
                    BackColor = new Brush(Color.Transparent),
                    ForeColor = Color.White
                };
            }
        }

        private static int GetBitmapIdForEmbeddedResource(string imagePath)
        {
            var assembly = typeof(MainWindow).Assembly;
            var image = assembly.GetManifestResourceStream(imagePath);
            return BitmapRegistry.Instance.Register(image);
        }
    }
}
