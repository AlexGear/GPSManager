using GPSManager.Polygons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSManager.Storage
{
    interface IPolygonStorage
    {
        IReadOnlyList<Polygon> Polygons { get; }
        int InsertPolygonAndAssignID(Polygon polygon);
        bool RemovePolygon(Polygon polygon);
        bool UpdatePolygon(Polygon polygon);
    }
}
