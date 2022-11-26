using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;

namespace VRC.OSCQuery.Samples.Shared
{
    public class AndroidDiscovery : IDiscovery
    {
        #region Android Multicast

        private float multicastDelay = 1;
        private bool stopAcquiringLock = false;
        private bool _multicastLockStatus;
        AndroidJavaObject multicastLock;
        private AndroidJavaObject discoveryJava = null;
        private AndroidJavaObject activityContext = null;
        private JavaBridge javaBridge = null;
        int port = 9876;
        private bool advertisingReady;
        
        public bool getMulticastLock(string lockTag)
        {
            try
            {
                using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
                {
             
                    using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                    {
                        multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", lockTag);
                        multicastLock.Call("acquire");
                        bool refCounted = true;
                        multicastLock.Call("setReferenceCounted", refCounted);
                        return multicastLock.Call<bool>("isHeld");
                    }
             
                }
            }
            catch (Exception err)
            {
                Debug.Log(err.ToString());
            }
            return false;
        }
        
        public bool MulticastLockStatus
        {
            get => _multicastLockStatus;
            set
            {
                // notify of changes
                if (_multicastLockStatus != value)
                {
                    MulticastLockStatusChanged?.Invoke(value);
                }
                _multicastLockStatus = value;
            }
        }
        
        public event Action<bool> MulticastLockStatusChanged;
        
        private void OnMulticastLockStatusChanged(bool value)
        {
            Debug.Log($"Multicast Lock Status Changed: {value}");
            
            if (value)
            {
                stopAcquiringLock = true;
                StartService();
            }
            else
            {
                stopAcquiringLock = false;
            }
        }

        private async UniTask acquireMultiCastPeriodically()
        {
            while (!stopAcquiringLock)
            {
                MulticastLockStatus = getMulticastLock("debugMulticast");
                await UniTask.Delay(TimeSpan.FromSeconds(multicastDelay));
            }
        }
        
        Queue<string> errors = new Queue<string>();

        IEnumerator ProcessErrors()
        {
            while (true)
            {
                yield return new WaitUntil(() => errors.Count > 0);
                Debug.Log(errors.Dequeue());
            }
        }

        #endregion

        #region Permissions

        private HashSet<string> _requiredPermissions = new HashSet<string>()
        {
            "android.permission.INTERNET",
            "android.permission.CHANGE_WIFI_MULTICAST_STATE",
            "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.ACCESS_WIFI_STATE",
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.WRITE_EXTERNAL_STORAGE",
            "android.permission.MANAGE_EXTERNAL_STORAGE",
        };

        #endregion

        public AndroidDiscovery()
        {
            foreach (string permission in _requiredPermissions)
            {
                Debug.Log($"Checking for permission {permission}");
                // Request all required permissions
                if (!Permission.HasUserAuthorizedPermission(permission))
                {
                    Debug.Log($"{permission} not authorized. Requesting it.");
                    Permission.RequestUserPermission(permission);   
                }
                else
                {
                    Debug.Log($"{permission} is authorized!.");
                }
            }
            
            MulticastLockStatusChanged += OnMulticastLockStatusChanged;
            UniTask.Create(acquireMultiCastPeriodically);
        }

        private void StartService()
        {
            using(AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
            }

            javaBridge = new JavaBridge();
            javaBridge.GotJavaCallback += GotJavaCallback;
            
            discoveryJava = new AndroidJavaObject("vrc.oscquery.examples.AndroidDiscoveryJava", activityContext, javaBridge);
            // discoveryJava.Call("initializeDiscoveryListener"); // listen for services

            // Start advertising, process queue if needed
            advertisingReady = true;
            while (_profilesToAdvertise.Count > 0)
            {
                Advertise(_profilesToAdvertise.Dequeue());
            }
        }

        public Action<string> JavaCallback;

        private void GotJavaCallback(string obj)
        {
            JavaCallback?.Invoke(obj);
            Debug.Log($"Got callback from Java with {obj}");
        }

        // Dispose of the two items we created in Start
        private void OnDestroy()
        {
            Dispose();
        }

        #region IDiscovery

        public void Dispose()
        {
            discoveryJava.Call("unregisterService");
            multicastLock?.Call("release");
            if (javaBridge != null)
            {            
                javaBridge.GotJavaCallback -= GotJavaCallback;
            }
        }

        public void RefreshServices()
        {
            //throw new NotImplementedException();
        }

        public HashSet<OSCQueryServiceProfile> GetOSCQueryServices()
        {
            throw new NotImplementedException();
        }

        public HashSet<OSCQueryServiceProfile> GetOSCServices()
        {
            throw new NotImplementedException();
        }

        private Queue<OSCQueryServiceProfile> _profilesToAdvertise = new Queue<OSCQueryServiceProfile>();

        public void Advertise(OSCQueryServiceProfile profile)
        {
            if (advertisingReady)
            {
                discoveryJava.Call("registerService", profile.name, profile.GetServiceTypeString(), profile.port);
            }
            else
            {
                _profilesToAdvertise.Enqueue(profile);
            }
        }

        public void Unadvertise(OSCQueryServiceProfile profile)
        {
            // would need to include registrationListener, or pass along way to look it up
            // throw new NotImplementedException();
        }

        public event Action<OSCQueryServiceProfile> OnOscServiceAdded;
        public event Action<OSCQueryServiceProfile> OnOscQueryServiceAdded;

        #endregion
    }
}