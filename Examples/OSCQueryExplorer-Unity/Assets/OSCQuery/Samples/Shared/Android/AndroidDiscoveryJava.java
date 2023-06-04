package vrc.oscquery.examples;

import android.content.Context;
import android.net.nsd.NsdManager;
import android.net.nsd.NsdServiceInfo;
import android.util.Log;

import java.util.Hashtable;
import java.util.HashSet;

public class AndroidDiscoveryJava {
    private final String TAG = "Unity OSCQuery";
    private final String SERVICE_TYPE_OSCJSON = "_oscjson._tcp.";
    private final String SERVICE_TYPE_OSC = "_osc._udp.";

    private Context context;
    private NsdManager nsdManager;
    private Hashtable<NsdServiceInfo, NsdManager.RegistrationListener> registrationListeners;
    private HashSet<String> registeredServices;

    public AndroidPluginCallback Callback;

    private boolean startedDiscovery = false;

    public AndroidDiscoveryJava(Context context, AndroidPluginCallback callback) {
        this.context = context;
        this.Callback = callback;
        nsdManager = (NsdManager) context.getSystemService(Context.NSD_SERVICE);
        registrationListeners = new Hashtable<>();
        registeredServices = new HashSet<>();
    }

    // Register a new OSC/OSCJSON service with a given name, type, and port
    public void registerService(String name, String type, int port) {
       
        NsdServiceInfo serviceInfo = new NsdServiceInfo();
        serviceInfo.setServiceName(name);
        serviceInfo.setServiceType(type);
        serviceInfo.setPort(port);

        NsdManager.RegistrationListener listener = initializeRegistrationListener();
        registrationListeners.put(serviceInfo, listener);
        nsdManager.registerService(serviceInfo, NsdManager.PROTOCOL_DNS_SD, listener);        

        if(!startedDiscovery){
            startedDiscovery = true;
            discoverServices();
        }
    }

    // Initialize a new RegistrationListener to handle service registration events
    private NsdManager.RegistrationListener initializeRegistrationListener() {
        return new NsdManager.RegistrationListener() {
            @Override
            public void onServiceRegistered(NsdServiceInfo serviceInfo) {
                Log.i(TAG, "Service registered: " + serviceInfo);
                registeredServices.add(serviceInfo.getServiceName());
            }

            @Override
            public void onRegistrationFailed(NsdServiceInfo serviceInfo, int errorCode) {
                Log.e(TAG, "Service registration failed: " + errorCode);
            }

            @Override
            public void onServiceUnregistered(NsdServiceInfo serviceInfo) {
                Log.i(TAG, "Service unregistered: " + serviceInfo);
                
                // Search for the service in the hashmap and remove it
                registeredServices.remove(serviceInfo.getServiceName());
            }

            @Override
            public void onUnregistrationFailed(NsdServiceInfo serviceInfo, int errorCode) {
                Log.e(TAG, "Service unregistration failed: " + errorCode);
            }
        };
    }

    // Start the discovery process for OSC/OSCJSON services
    public void discoverServices() {
        nsdManager.discoverServices(SERVICE_TYPE_OSCJSON, NsdManager.PROTOCOL_DNS_SD, createDiscoveryListener(SERVICE_TYPE_OSCJSON));
        nsdManager.discoverServices(SERVICE_TYPE_OSC, NsdManager.PROTOCOL_DNS_SD, createDiscoveryListener(SERVICE_TYPE_OSC));
    }

    // Create a new DiscoveryListener for a given service type
    private NsdManager.DiscoveryListener createDiscoveryListener(final String serviceType) {
        return new NsdManager.DiscoveryListener() {
            @Override
            public void onDiscoveryStarted(String regType) {
                Log.d(TAG, "Service discovery started: " + serviceType);
            }

            @Override
            public void onDiscoveryStopped(String serviceType) {
                Log.i(TAG, "Discovery stopped: " + serviceType);
            }

            @Override
            public void onStopDiscoveryFailed(String serviceType, int errorCode) {
                Log.e(TAG, "Stop discovery failed: Error code:" + errorCode);
                nsdManager.stopServiceDiscovery(this);
            }

            @Override
            public void onStartDiscoveryFailed(String serviceType, int errorCode) {
                Log.e(TAG, "Start discovery failed: Error code:" + errorCode);
                nsdManager.stopServiceDiscovery(this);
            }

            @Override
            public void onServiceFound(NsdServiceInfo service) {
                Log.d(TAG, "Service found: " + service);

                String serviceName = service.getServiceName();
                // Search for the service in the hashmap, return early if found.
                if (registeredServices.contains(serviceName)) {
                    // This service is already registered
                    Log.d(TAG, "Not resolving registered service " + serviceName);
                    return;
                }
                
                if (service.getServiceType().equals(serviceType)) {
                    Log.d(TAG, "Trying to resolve service " + serviceName);
                    nsdManager.resolveService(service, initializeResolveListener());
                }
            }

            @Override
            public void onServiceLost(NsdServiceInfo service) {
                Log.d(TAG, "Service lost: " + service);
            }

            // Initialize a new ResolveListener to handle service resolution events
            private NsdManager.ResolveListener initializeResolveListener() {
                return new NsdManager.ResolveListener() {
                    int resolveAttempts = 0;
                    int maxResolveAttempts = 3;

                    @Override
                    public void onResolveFailed(NsdServiceInfo serviceInfo, int errorCode) {
                        Log.e(TAG, "Resolve failed: " + errorCode);
                        resolveAttempts++;
                        if (resolveAttempts <= maxResolveAttempts) {
                            Log.d(TAG, "Retrying resolve attempt " + resolveAttempts + " for service: " + serviceInfo.getServiceName());
                            nsdManager.resolveService(serviceInfo, this);
                        } else {
                            Log.e(TAG, "Max resolve attempts reached for service: " + serviceInfo.getServiceName());
                            resolveAttempts = 0;
                        }
                    }

                    @Override
                    public void onServiceResolved(NsdServiceInfo serviceInfo) {
                        Log.d(TAG, "Resolve Succeeded: " + serviceInfo);
                        resolveAttempts = 0;
                        Callback.OnJavaServiceInfo(serviceInfo);
                    }
                };
            }

        };

    }

    // Pause discovery process and unregister services
    public void pause() {
        for (NsdServiceInfo serviceInfo : registrationListeners.keySet()) {
            nsdManager.unregisterService(registrationListeners.get(serviceInfo));
        }
        nsdManager.stopServiceDiscovery(createDiscoveryListener(SERVICE_TYPE_OSCJSON));
        nsdManager.stopServiceDiscovery(createDiscoveryListener(SERVICE_TYPE_OSC));
    }

    // Resume discovery process and register services
    public void resume(int localPort) {
        for (NsdServiceInfo serviceInfo : registrationListeners.keySet()) {
            registerService(serviceInfo.getServiceName(), serviceInfo.getServiceType(), localPort);
        }
        discoverServices();
    }

    // Tear down the helper, stopping discovery and unregistering services
    public void tearDown() {
        pause();
        registrationListeners.clear();
    }

}

