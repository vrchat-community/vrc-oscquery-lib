package vrc.oscquery.examples;

import android.content.Context;
import android.net.nsd.NsdServiceInfo;
import android.net.nsd.NsdManager;
import android.net.nsd.NsdManager.RegistrationListener;
import android.net.nsd.NsdManager.DiscoveryListener;
import android.net.nsd.NsdManager.ResolveListener;
import android.util.Log;
import java.net.InetAddress;
import java.util.Hashtable;

public class AndroidDiscoveryJava {
    
    private Context context;
    private NsdManager nsdManager;
    private Hashtable<NsdServiceInfo, NsdManager.RegistrationListener> registrationListeners;
    
    // Advertising
    private String serviceName;

    public AndroidPluginCallback Callback;
    
    private boolean startedDiscovery = false;
    
    public AndroidDiscoveryJava(Context context, AndroidPluginCallback callback){

        this.context = context;
        this.Callback = callback;
        nsdManager = (NsdManager)context.getSystemService(Context.NSD_SERVICE);
        registrationListeners = new Hashtable<NsdServiceInfo, NsdManager.RegistrationListener>();
    }
    
    public void registerService(String name, String type, int port) {
    
        this.serviceName = name;
        // Create the NsdServiceInfo object, and populate it.
        NsdServiceInfo serviceInfo = new NsdServiceInfo();
    
        // The name is subject to change based on conflicts
        // with other services advertised on the same network.
        serviceInfo.setServiceName(name);
        serviceInfo.setServiceType(type);
        serviceInfo.setPort(port);
       
        // Create and cache registration listener
        NsdManager.RegistrationListener listener = initializeRegistrationListener();
        registrationListeners.put(serviceInfo, listener);
        nsdManager.registerService(serviceInfo, NsdManager.PROTOCOL_DNS_SD, listener);
        
        if(!startedDiscovery){
            startedDiscovery = true;
            initializeDiscoveryListeners();
        }
    }
    
    public void unregisterService(){
//         nsdManager.unregisterService(registrationListener);
    }
    
    public NsdManager.RegistrationListener initializeRegistrationListener() {
        return new NsdManager.RegistrationListener() {
    
            @Override
            public void onServiceRegistered(NsdServiceInfo NsdServiceInfo) {
                // Save the service name. Android may have changed it in order to
                // resolve a conflict, so update the name you initially requested
                // with the name Android actually used.
                serviceName = NsdServiceInfo.getServiceName();
                Log.i("Unity OSCQuery", "Service Registered: " + serviceName);
            }
    
            @Override
            public void onRegistrationFailed(NsdServiceInfo serviceInfo, int errorCode) {
                // Registration failed! Put debugging code here to determine why.
                Log.i("Unity OSCQuery", "Service Registration failed: " + errorCode);
            }
    
            @Override
            public void onServiceUnregistered(NsdServiceInfo arg0) {
                // Service has been unregistered. This only happens when you call
                // NsdManager.unregisterService() and pass in this listener.
            }
    
            @Override
            public void onUnregistrationFailed(NsdServiceInfo serviceInfo, int errorCode) {
                // Unregistration failed. Put debugging code here to determine why.
            }
        };
    }
    
    // Discovery
    private DiscoveryListener discoveryListenerOSCQuery;
    private DiscoveryListener discoveryListenerOSC;
    private String SERVICE_TYPE_OSCJSON = "_oscjson._tcp."; // Note: had to add dot at end
    private String SERVICE_TYPE_OSC = "_osc._udp."; // Note: had to add dot at end
    private String TAG = "Unity OSCQuery";
    
    public void initializeDiscoveryListeners() {
    
        NsdManager.ResolveListener resolveListener = initializeResolveListener();
        NsdManager.ResolveListener resolveListenerOSC = initializeResolveListener();
        
        // Instantiate a new DiscoveryListener for SERVICE_TYPE_OSCJSON
        discoveryListenerOSCQuery = new NsdManager.DiscoveryListener() {
    
            // Called as soon as service discovery begins.
            @Override
            public void onDiscoveryStarted(String regType) {
                Log.d(TAG, "OSCQuery Service discovery started");
            }
    
            @Override
            public void onServiceFound(NsdServiceInfo service) {
                // A service was found! Do something with it.
                Log.d(TAG, "OSCQuery Service discovery success " + service.toString());
                
                String discoveredName = service.getServiceName();
                if(discoveredName.equals(serviceName)){
                    Log.d(TAG, "OSCQuery Service is Self, Skipping.");
                    return;
                }
                
                String serviceType = service.getServiceType();
                
                if (serviceType.equals(SERVICE_TYPE_OSCJSON)) {
                    // found OSCQuery Service
                    Log.d(TAG, "Found OSCQuery Service: " + discoveredName);
                    nsdManager.resolveService(service, resolveListener);
                }
                else {
                    // not a recognized service
                    Log.d(TAG, "Unknown Service Type: " + serviceType);
                }
            }
    
            @Override
            public void onServiceLost(NsdServiceInfo service) {
                // When the network service is no longer available.
                // Internal bookkeeping code goes here.
                Log.e(TAG, "service lost: " + service);
            }
    
            @Override
            public void onDiscoveryStopped(String serviceType) {
                Log.i(TAG, "OSCQuery Discovery stopped: " + serviceType);
            }
    
            @Override
            public void onStartDiscoveryFailed(String serviceType, int errorCode) {
                Log.e(TAG, "OSCQuery Discovery failed: Error code:" + errorCode);
                nsdManager.stopServiceDiscovery(this);
            }
    
            @Override
            public void onStopDiscoveryFailed(String serviceType, int errorCode) {
                Log.e(TAG, "OSCQuery Discovery failed: Error code:" + errorCode);
                nsdManager.stopServiceDiscovery(this);
            }
        };

        // Instantiate a new DiscoveryListener for SERVICE_TYPE_OSC
        discoveryListenerOSC = new NsdManager.DiscoveryListener() {

            // Called as soon as service discovery begins.
            @Override
            public void onDiscoveryStarted(String regType) {
                Log.d(TAG, "OSC Service discovery started");
            }

            @Override
            public void onServiceFound(NsdServiceInfo service) {
                // A service was found! Do something with it.
                Log.d(TAG, "OSC Service discovery success " + service.toString());

                String discoveredName = service.getServiceName();
                if(discoveredName.equals(serviceName)){
                    Log.d(TAG, "OSC Service is Self, Skipping.");
                    return;
                }

                String serviceType = service.getServiceType();

                if (serviceType.equals(SERVICE_TYPE_OSC)) {
                    // found OSC Service
                    Log.d(TAG, "Found OSC Service: " + discoveredName);
                    nsdManager.resolveService(service, resolveListenerOSC);
                }
                else {
                    // not a recognized service
                    Log.d(TAG, "Unknown Service Type: " + serviceType);
                }
            }

            @Override
            public void onServiceLost(NsdServiceInfo service) {
                // When the network service is no longer available.
                // Internal bookkeeping code goes here.
                Log.e(TAG, "service lost: " + service);
            }

            @Override
            public void onDiscoveryStopped(String serviceType) {
                Log.i(TAG, "OSC Discovery stopped: " + serviceType);
            }

            @Override
            public void onStartDiscoveryFailed(String serviceType, int errorCode) {
                Log.e(TAG, "OSC Discovery failed: Error code:" + errorCode);
                nsdManager.stopServiceDiscovery(this);
            }

            @Override
            public void onStopDiscoveryFailed(String serviceType, int errorCode) {
                Log.e(TAG, "OSC Discovery failed: Error code:" + errorCode);
                nsdManager.stopServiceDiscovery(this);
            }
        };
        
        nsdManager.discoverServices(
                SERVICE_TYPE_OSCJSON, NsdManager.PROTOCOL_DNS_SD, discoveryListenerOSCQuery);
                
        nsdManager.discoverServices(
                SERVICE_TYPE_OSC, NsdManager.PROTOCOL_DNS_SD, discoveryListenerOSC);
    }

    public NsdManager.ResolveListener initializeResolveListener(){
        return new NsdManager.ResolveListener(){

            @Override
            public void	onResolveFailed(NsdServiceInfo serviceInfo, int errorCode){
                // Called when the resolve fails. Use the error code to debug.
                Log.e(TAG, "Resolve failed: " + errorCode);
            }

            @Override
            public void onServiceResolved(NsdServiceInfo serviceInfo){
                if (serviceInfo.getServiceName().equals(serviceName)) {
                    Log.d(TAG, "Same IP.");
                    return;
                }
                NsdServiceInfo mService = serviceInfo;
                int port = mService.getPort();
                InetAddress host = mService.getHost();

                String newServiceName = serviceInfo.getServiceName();

                Callback.OnJavaServiceInfo(serviceInfo);

                Log.i(TAG, "Resolve Succeeded" + newServiceName);
            }
        };
    }
/*
    //Shutdown stuff

    @Override
    protected void onPause() {
        if (nsdHelper != null) {
            nsdHelper.tearDown();
        }
        super.onPause();
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (nsdHelper != null) {
            nsdHelper.registerService(connection.getLocalPort());
            nsdHelper.discoverServices();
        }
    }

    @Override
    protected void onDestroy() {
        nsdHelper.tearDown();
        connection.tearDown();
        super.onDestroy();
    }

    // NsdHelper's tearDown method
    public void tearDown() {
        nsdManager.unregisterService(registrationListener);
        nsdManager.stopServiceDiscovery(discoveryListenerOSCQuery);
    }
*/
}