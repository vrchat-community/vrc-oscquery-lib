using System;
using System.Collections.Generic;

namespace VRC.OSCQuery
{
    public interface IDiscovery : IDisposable
    {
        void RefreshServices();
        event Action<OSCQueryServiceProfile> OnOscServiceAdded;
        event Action<OSCQueryServiceProfile> OnOscQueryServiceAdded;
        HashSet<OSCQueryServiceProfile> GetOSCQueryServices();
        HashSet<OSCQueryServiceProfile> GetOSCServices();
        
        void Advertise(OSCQueryServiceProfile profile);
        void Unadvertise(OSCQueryServiceProfile profile);
    }
}