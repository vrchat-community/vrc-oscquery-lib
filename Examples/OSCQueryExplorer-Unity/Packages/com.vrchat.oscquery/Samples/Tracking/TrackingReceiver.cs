using System.Collections.Generic;
using OscCore;
using UnityEngine;
using UnityEngine.UI;
using VRC.OSCQuery.Samples.Shared;

#pragma warning disable 4014

namespace VRC.OSCQuery.Samples.Tracking
{
    public class TrackingReceiver : MonoBehaviour
    {
        // Scene Objects
        public Text HeaderText;

        // OSCQuery and OSC members
        private OSCQueryService _oscQuery;
        private OscServer _receiver;
        
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> rotations = new List<Vector3>();

        public List<Transform> trackers;

        void Start()
        {
            StartService();
        }

        /// <summary>
        /// Creates new OSCQuery and OSC services, advertises itself to receive messages
        /// </summary>
        private void StartService()
        {
            // Construct unique server name
            var w = new Bogus.DataSets.Hacker();
            var w2 = new Bogus.DataSets.Lorem();
            var serverName = $"Tracking-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

            // Create OSC Server on available port
            var port = Extensions.GetAvailableTcpPort();
            var udpPort = Extensions.GetAvailableUdpPort();
            _receiver = OscServer.GetOrCreate(udpPort);

            var logger = new UnityMSLogger();


#if UNITY_ANDROID
            IDiscovery discovery = new AndroidDiscovery();
#else
            IDiscovery discovery = new MeaModDiscovery(logger);
#endif
            
            _oscQuery = new OSCQueryServiceBuilder()
                .WithServiceName(serverName)
                .WithHostIP(VRC.OSCQuery.Samples.Shared.Extensions.GetLocalIPAddress())
                .WithTcpPort(port)
                .WithUdpPort(udpPort)
                .WithLogger(logger)
                .WithDiscovery(discovery)
                .StartHttpServer()
                .AdvertiseOSC()
                .AdvertiseOSCQuery()
                .Build();

            _oscQuery.RefreshServices();

            // Show server name and chosen port
            HeaderText.text = $"{serverName} running at {_oscQuery.HostIP} tcp:{port} osc: {udpPort}";

            for (int i = 0; i < trackers.Count; i++)
            {
                positions.Add(Vector3.zero);
                rotations.Add(Vector3.zero);
                SetupTracker(i);
            }
        }
        
        private void SetupTracker(int index)
        {
            string trackerName = index == 0 ? "head" : index.ToString();
            _oscQuery.AddEndpoint($"{TrackingCanvas.TRACKERS_ROOT}/{trackerName}/{TrackingCanvas.TRACKERS_POSITION}","fff", Attributes.AccessValues.WriteOnly);
            _oscQuery.AddEndpoint($"{TrackingCanvas.TRACKERS_ROOT}/{trackerName}/{TrackingCanvas.TRACKERS_ROTATION}","fff", Attributes.AccessValues.WriteOnly);
            
            _receiver.TryAddMethod($"{TrackingCanvas.TRACKERS_ROOT}/{trackerName}/{TrackingCanvas.TRACKERS_POSITION}",
                (message) =>
                {
                    positions[index] = new Vector3(message.ReadFloatElement(0), message.ReadFloatElement(1), message.ReadFloatElement(2));
                }
            );
            
            _receiver.TryAddMethod($"{TrackingCanvas.TRACKERS_ROOT}/{trackerName}/{TrackingCanvas.TRACKERS_ROTATION}",
                (message) =>
                {
                    rotations[index] = new Vector3(message.ReadFloatElement(0), message.ReadFloatElement(1), message.ReadFloatElement(2));
                }
            );
        }
        
        private void Update()
        {
            for(int i = 0; i < trackers.Count; i++)
            {
                if (trackers.Count > i)
                {
                    trackers[i].SetPositionAndRotation(positions[i], Quaternion.Euler(rotations[i]));
                }
            }
        }

        private void OnDestroy()
        {
            _receiver?.Dispose();
            _oscQuery?.Dispose();
        }
    }
}