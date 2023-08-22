using OscCore;
using UnityEngine;
using UnityEngine.UI;
using VRC.OSCQuery.Samples.Shared;

#pragma warning disable 4014

namespace VRC.OSCQuery.Samples.Chatbox
{
    public class ChatboxReceiver : MonoBehaviour
    {
        // Scene Objects
        public Text HeaderText;
        public Text DisplayField;
        public Image TypingIndicator;

        // OSCQuery and OSC members
        private OSCQueryService _oscQuery;
        private OscServer _receiver;
        
        private int _typing = -1;
        private string _lastMessage;

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
            var serverName = $"Chatbox-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

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

            // Show server name and chosen port
            HeaderText.text = $"{serverName} running at {_oscQuery.HostIP} tcp:{port} osc: {udpPort}";

            _oscQuery.AddEndpoint<bool>(ChatboxSender.OSC_PATH_CHATBOX_TYPING, Attributes.AccessValues.WriteOnly, new object[]{false},
                "Whether to show the typing indicator");
            _receiver.TryAddMethod(ChatboxSender.OSC_PATH_CHATBOX_TYPING,
                (message) =>
                {
                    _typing = message.ReadBooleanElement(0) ? 1 : 0;
                }
            );
            
            _oscQuery.AddEndpoint(ChatboxSender.OSC_PATH_CHATBOX_INPUT, "sT", Attributes.AccessValues.WriteOnly);
            _receiver.TryAddMethod(ChatboxSender.OSC_PATH_CHATBOX_INPUT,
                values => _lastMessage = values.ReadStringElement(0));
        }

        private void Update()
        {
            if (_lastMessage != null && _lastMessage.Length > 0)
            {
                DisplayField.text = _lastMessage;
                _lastMessage = null;
            }

            if (_typing > -1)
            {
                TypingIndicator.enabled = _typing == 1;
                _typing = -1;
            }
        }

        private void OnDestroy()
        {
            _receiver?.Dispose();
            _oscQuery?.Dispose();
        }
    }
}