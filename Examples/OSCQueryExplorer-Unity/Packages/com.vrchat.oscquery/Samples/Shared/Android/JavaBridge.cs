using UnityEngine;
using System;

public class JavaBridge : AndroidJavaProxy
{
    public Action<AndroidJavaObject> GotJavaCallback;

    public JavaBridge() : base("vrc.oscquery.examples.AndroidPluginCallback") {}

    // This method will be invoked from the plugin
    public void OnJavaServiceInfo(AndroidJavaObject service)
    {
        // Pass the result to the C# event that we register to in the UI class
        GotJavaCallback?.Invoke(service);
    }
}