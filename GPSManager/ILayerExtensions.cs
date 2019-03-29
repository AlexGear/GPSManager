using Mapsui.Layers;

namespace GPSManager
{
    static class ILayerExtensions
    {
        public static void Refresh(this ILayer layer)
        {
            layer.ViewChanged(true, layer.Envelope, resolution: 1);
        }
    }
}
