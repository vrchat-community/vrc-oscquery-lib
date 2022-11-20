using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BlobHandles;
using OscCore;
using VRC.OSCQuery.Samples.Shared;

#pragma warning disable 4014

namespace VRC.OSCQuery.Examples.OSCQueryExplorerUnity
{
    public class ReceiverCanvas : MonoBehaviour
    {
        // Scnene references
        public Text HeaderText;
        public Text InfoText;
        
        // OSCQuery and OSC members
        private OSCQueryService _oscQuery;
        private OscServer _receiver;
        
        // Message members
        private Dictionary<BlobString, string> _messagesValues = new Dictionary<BlobString, string>();
        private bool _messagesDirty;
        public int maxMessages = 10;
        
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
            
            // Listen to all incoming messages
            _receiver.AddMonitorCallback(OnMessageReceived);

            var logger = new UnityMSLogger();
            
            _oscQuery = new OSCQueryServiceBuilder()
                .WithServiceName(serverName)
                .WithTcpPort(port)
                .WithOscPort(udpPort)
                .WithLogger(logger)
                .WithDiscovery(new MeaModDiscovery(logger))
                .StartHttpServer()
                .AdvertiseOSC()
                .AdvertiseOSCQuery()
                .Build();
            
            _oscQuery.RefreshServices();
            
            // Show server name and chosen port
            HeaderText.text = $"{serverName} running at tcp:{port} osc: {udpPort}";
        }

        // Process incoming messages, add to message queue
        private void OnMessageReceived(BlobString address, OscMessageValues values)
        {
            Debug.Log($"Received {address}");
            string message = $"{address} : ";
            values.ForEachElement((i, typeTag) => message += $" {GetStringForValue(values, i, typeTag)}");

            if (_messagesValues.ContainsKey(address))
            {
                _messagesValues[address] = message;
            }
            else
            {
                _messagesValues.Add(address, message);
            }
            _messagesDirty = true;
        }

        // Gets string representations of some OSC types for display
        private string GetStringForValue(OscMessageValues values, int i, TypeTag typeTag)
        {
            switch (typeTag)
            {
                case TypeTag.Int32:
                    return values.ReadIntElement(i).ToString();
                case TypeTag.String:
                    return values.ReadStringElement(i);
                case TypeTag.True:
                case TypeTag.False:
                    return values.ReadBooleanElement(i).ToString();
                case TypeTag.Float32:
                    return values.ReadFloatElement(i).ToString();
                default:
                    return "";
            }
        }

        // Check for message updates, which can happen on a background thread.
        private void Update()
        {
            if (_messagesDirty)
            {
                _messagesDirty = false;
                
                while (_messagesValues.Count > maxMessages)
                {
                    _messagesValues.Remove(_messagesValues.Keys.First());
                }
                InfoText.text = string.Join(Environment.NewLine, _messagesValues.Values);
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