using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSManager
{
    interface IConnectable
    {
        bool IsConnected { get; }
        event Action Connected;
        event Action Disconnected;
    }
}
