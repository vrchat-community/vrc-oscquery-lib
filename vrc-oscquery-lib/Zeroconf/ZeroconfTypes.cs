using System;
using System.Collections.Generic;

namespace VRC.OSCQuery
{
    public interface IDiscovery : IDisposable
    {
        void Initialize();
        void RefreshServices();
        event Action<OSCQueryServiceProfile> OnOscServiceAdded;
        event Action<OSCQueryServiceProfile> OnOscQueryServiceAdded;
        HashSet<OSCQueryServiceProfile> GetOSCQueryServices();
        HashSet<OSCQueryServiceProfile> GetOSCServices();
        
    }

    public interface IAdvertiser
    {
        void Advertise(OSCQueryServiceProfile profile);
        void Unadvertise(OSCQueryServiceProfile profile);
    }
}