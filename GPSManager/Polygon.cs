using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MapsuiPolygon = Mapsui.Geometries.Polygon;

namespace GPSManager
{
    class Polygon : Feature
    {
        private readonly MapsuiPolygon mapsuiPolygon;

        public IList<Point> Vertices => mapsuiPolygon.ExteriorRing.Vertices;

        public IEnumerable<Gga> GgaVeritces => Vertices.Select(Gga.FromMapsuiPoint);

        public Polygon()
        {
            Geometry = mapsuiPolygon = new MapsuiPolygon();
        }

        public Polygon(IEnumerable<Point> vertices)
        {
            Geometry = mapsuiPolygon = new MapsuiPolygon(new LinearRing(vertices));
        }

        public Polygon(IEnumerable<Gga> ggaVertices)
            : this(ggaVertices.Select(Gga.ToMapsuiPoint))
        {
        }
    }
}
