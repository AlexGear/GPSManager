using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSManager.Location
{
    interface IGgaProvider : IDisposable
    {
        event Action<Gga> GgaProvided;
    }
}
