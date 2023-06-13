using System.Collections.Generic;
using OscCore;
using UnityEngine;
using UnityEngine.UI;
using VRC.OSCQuery.Samples.Shared;
using MiniNtp;

#pragma warning disable 4014

namespace VRC.OSCQuery.Samples.Tracking
{
    public class HeadAndWristsReceiver : MonoBehaviour
    {
        // Scene Objects
        public Text HeaderText;
        public Text TimestampText;

        // OSCQuery and OSC members
        private OSCQueryService _oscQuery;
        private OscServer _receiver;

        public Transform HeadTransform;
        public Transform LeftWristTransform;
        public Transform RightWristTransform;

        private Vector3 incomingHeadPosition;
        private Vector3 incomingHeadRotation;

        private Vector3 incomingLeftWristPosition;
        private Vector3 incomingLeftWristRotation;

        private Vector3 incomingRightWristPosition;
        private Vector3 incomingRightWristRotation;

        private string incomingTimestamp = ".";

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
            var serverName = $"HeadAndWrists-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

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
                .WithHostIP(Samples.Shared.Extensions.GetLocalIPAddress())
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

            SetupEndpoints();
        }

        private void SetupEndpoints()
        {
            _oscQuery.AddEndpoint("/tracking/vrsystem/head/pose", "ffffff", Attributes.AccessValues.WriteOnly);
            _receiver.TryAddMethod("/tracking/vrsystem/head/pose",
                (message) =>
                {
                    incomingTimestamp = $"Timestamp: {_receiver.LastBundleTimestamp.ToDateTime().ToString("HH:mm:ss.fff")}";
                    incomingHeadPosition = new Vector3(message.ReadFloatElement(0), message.ReadFloatElement(1), message.ReadFloatElement(2));
                    incomingHeadRotation = new Vector3(message.ReadFloatElement(3), message.ReadFloatElement(4), message.ReadFloatElement(5));
                }
            );

            _oscQuery.AddEndpoint("/tracking/vrsystem/leftwrist/pose", "ffffff", Attributes.AccessValues.WriteOnly);
            _receiver.TryAddMethod("/tracking/vrsystem/leftwrist/pose",
                (message) =>
                {
                    incomingLeftWristPosition = new Vector3(message.ReadFloatElement(0), message.ReadFloatElement(1), message.ReadFloatElement(2));
                    incomingLeftWristRotation = new Vector3(message.ReadFloatElement(3), message.ReadFloatElement(4), message.ReadFloatElement(5));
                }
            );

            _oscQuery.AddEndpoint("/tracking/vrsystem/rightwrist/pose", "ffffff", Attributes.AccessValues.WriteOnly);
            _receiver.TryAddMethod("/tracking/vrsystem/rightwrist/pose",
                (message) =>
                {
                    incomingRightWristPosition = new Vector3(message.ReadFloatElement(0), message.ReadFloatElement(1), message.ReadFloatElement(2));
                    incomingRightWristRotation = new Vector3(message.ReadFloatElement(3), message.ReadFloatElement(4), message.ReadFloatElement(5));
                }
            );
        }
        
        private void Update()
        {
            HeadTransform.SetPositionAndRotation(incomingHeadPosition, Quaternion.Euler(incomingHeadRotation));
            LeftWristTransform.SetPositionAndRotation(incomingLeftWristPosition, Quaternion.Euler(incomingLeftWristRotation));
            RightWristTransform.SetPositionAndRotation(incomingRightWristPosition, Quaternion.Euler(incomingRightWristRotation));
            TimestampText.text = incomingTimestamp;
        }

        private void OnDestroy()
        {
            _receiver?.Dispose();
            _oscQuery?.Dispose();
        }
    }
}