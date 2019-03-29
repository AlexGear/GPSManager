using Mapsui.Styles;

namespace GPSManager
{
    static class BitmapResources
    {
        public static int GetBitmapIdForEmbeddedResource(string imagePath)
        {
            var assembly = typeof(MainWindow).Assembly;
            var image = assembly.GetManifestResourceStream(imagePath);
            return BitmapRegistry.Instance.Register(image);
        }
    }
}
