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

        public IList<Point> Vertices => mapsuiPolygon.ExteriorRing.Vertices;

        public IEnumerable<Gga> GgaVertices => Vertices.Select(Gga.FromMapsuiPoint);

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
