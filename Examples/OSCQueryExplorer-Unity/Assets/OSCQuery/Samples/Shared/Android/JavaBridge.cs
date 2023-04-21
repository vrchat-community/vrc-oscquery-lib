using UnityEngine;
using System;
using System.Net;
using VRC.OSCQuery;

public class JavaBridge : AndroidJavaProxy
{
    public Action<OSCQueryServiceProfile> OnServiceProfileFound;

    public JavaBridge() : base("vrc.oscquery.examples.AndroidPluginCallback") {}

    // This method will be invoked from the plugin
    public void OnJavaServiceInfo(AndroidJavaObject service)
    {
        string serviceName = service.Call<string>("getServiceName");
        
        // Service Type
        string serviceTypeString = service.Call<string>("getServiceType");
        OSCQueryServiceProfile.ServiceType serviceType = GetServiceTypeFromJavaString(serviceTypeString);
        if (serviceType == OSCQueryServiceProfile.ServiceType.Unknown) return;
        
        // IP
        AndroidJavaObject inetAddress = service.Call<AndroidJavaObject>("getHost");
        string ipAddress = inetAddress.Call<string>("getHostAddress");
        int port = service.Call<int>("getPort");

        OSCQueryServiceProfile profile =
            new OSCQueryServiceProfile(serviceName, IPAddress.Parse(ipAddress), port, serviceType);

        OnServiceProfileFound?.Invoke(profile);
    }

    public OSCQueryServiceProfile.ServiceType GetServiceTypeFromJavaString(string value)
    {
        // Android returns a dot after the service name like "_oscjson._tcp." so we check for contains instead of ==
        if (value.Contains(Attributes.SERVICE_OSCJSON_TCP))
        {
            return OSCQueryServiceProfile.ServiceType.OSCQuery;
        }
        else if (value.Contains(Attributes.SERVICE_OSC_UDP))
        {
            return OSCQueryServiceProfile.ServiceType.OSC;
        }
        else
        {
            return OSCQueryServiceProfile.ServiceType.Unknown;
        }
    }
}