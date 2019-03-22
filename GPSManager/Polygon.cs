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
        private static readonly IStyle highlightedStyle = CreateHighlighedStyle();
        private readonly MapsuiPolygon mapsuiPolygon;
        private bool isHighlighed;

        public int ID { get; set; }
        public string Name { get; set; }

        public IList<Point> Vertices => mapsuiPolygon.ExteriorRing.Vertices;

        public IEnumerable<Gga> GgaVertices => Vertices.Select(Gga.FromMapsuiPoint);

        public string GeometryText => mapsuiPolygon.AsText();

        public bool IsHighlighed
        {
            get => isHighlighed;
            set
            {
                if (value == isHighlighed) return;

                isHighlighed = value;
                if(isHighlighed)
                {
                    if (!Styles.Contains(highlightedStyle))
                    {
                        Styles.Add(highlightedStyle);
                    }
                }
                else
                {
                    Styles.Remove(highlightedStyle);
                }
            }
        }

        public Polygon(IEnumerable<Point> vertices, int id = 0, string name = null)
            : this(new MapsuiPolygon(new LinearRing(vertices)), id, name)
        {
        }

        private Polygon(MapsuiPolygon mapsuiPolygon, int id, string name)
        {
            ID = id;
            Name = name;
            Geometry = this.mapsuiPolygon = mapsuiPolygon;
        }

        public static Polygon FromGeomText(string geometryText, int id = 0, string name = null)
        {
            if (string.IsNullOrWhiteSpace(geometryText))
            {
                throw new ArgumentException("geometryText is null or blank");
            }
            try
            {
                var geometry = Mapsui.Geometries.Geometry.GeomFromText(geometryText) as MapsuiPolygon;
                if (geometry is MapsuiPolygon mp)
                {
                    return new Polygon(mp, id, name);
                }
                else
                {
                    throw new ArgumentException($"Geometry text is invalid: {geometryText}", nameof(geometryText));
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Geometry text is invalid: {geometryText}", nameof(geometryText), ex);
            }
        }

        private static VectorStyle CreateHighlighedStyle()
        {
            return new VectorStyle
            {
                Fill = new Brush(new Color(240, 240, 20, 70)),
                Outline = new Pen
                {
                    Color = new Color(240, 20, 20),
                    Width = 2
                }
            };
        }
    }
}
