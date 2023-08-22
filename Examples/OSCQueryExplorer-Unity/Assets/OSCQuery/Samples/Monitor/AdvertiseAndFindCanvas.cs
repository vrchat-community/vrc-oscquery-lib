using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OscCore;
using VRC.OSCQuery.Samples.Shared;

#pragma warning disable 4014

namespace VRC.OSCQuery.Examples.Monitor
{
    public class AdvertiseAndFindCanvas : MonoBehaviour
    {
        // Scnene references
        public Text HeaderText;
        public Text InfoText;
        
        // OSCQuery and OSC members
        private OSCQueryService _oscQuery;
        private OscServer _receiver;
        
        // Message members
        private List<string> _messages = new List<string>();
        HashSet<OSCQueryServiceProfile> _profiles = new HashSet<OSCQueryServiceProfile>();
        
        private bool _messagesDirty;
        public int maxMessages = 10;
        
        private const int RefreshServicesInterval = 5;
        
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
            var serverName = $"{w.IngVerb().UpperCaseFirstChar()}-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

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
                .WithOscIP(VRC.OSCQuery.Samples.Shared.Extensions.GetLocalIPAddressNonLoopback())
                .WithTcpPort(port)
                .WithUdpPort(udpPort)
                .WithLogger(logger)
                .WithDiscovery(discovery)
                .StartHttpServer()
                .AdvertiseOSC()
                .AdvertiseOSCQuery()
                .Build();
            
            _oscQuery.RefreshServices();

            _oscQuery.OnOscQueryServiceAdded += LogDiscoveredService;
            _oscQuery.OnOscServiceAdded += LogDiscoveredService;

            // Show server name and chosen port
            HeaderText.text = $"{serverName} running at tcp:{port} osc: {udpPort}";
            
            // Query network for services
            InvokeRepeating(nameof(RefreshServices), 1, RefreshServicesInterval);
        }
        private void RefreshServices()
        {
            _oscQuery.RefreshServices();
        }

        private void LogDiscoveredService(OSCQueryServiceProfile profile)
        {
            if(profile.name == _oscQuery.ServerName || _profiles.Any(p=>p.name == profile.name && p.port == profile.port))
                return;
            
            string message = $"Found service {profile.name} at {profile.address}:{profile.port} with type {profile.serviceType}";
            Debug.Log(message);

            _profiles.Add(profile);
            _messages.Add(message);
            _messagesDirty = true;
        }
        
        // Check for message updates, which can happen on a background thread.
        private void Update()
        {
            if (_messagesDirty)
            {
                _messagesDirty = false;
                
                while (_messages.Count > maxMessages)
                {
                    _messages.Remove(_messages.First());
                }
                InfoText.text = string.Join(Environment.NewLine, _messages);
            }
            
        }

        // Dispose of the two items we created in Start
        private void OnDestroy()
        {
            _receiver.Dispose();
            _oscQuery.Dispose();
        }
    }
}