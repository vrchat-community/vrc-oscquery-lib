using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRC.OSCQuery.Examples.OSCQueryExplorerUnity;

#pragma warning disable 4014

namespace VRC.OSCQuery.Samples.Chatbox
{
    public class ChatboxCanvas : MonoBehaviour
    {
        // Scene Objects
        public Text HeaderText;
        public InputField InputField;
        
        // Connects to default OSC endpoint instead of searching via OSCQuery
        public bool connectToDefaultVRCEndpoint;

        // Service
        private OSCQueryService _oscQueryService;
        // List of receivers to send to
        private List<OscClientPlus> _receivers = new List<OscClientPlus>();
        private string _serverName = "ChatboxServer";
        private const int RefreshServicesInterval = 10;

        // Constant strings
        private const string OSC_PATH_CHATBOX = "/chatbox";
        private const string OSC_PATH_CHATBOX_TYPING = "/chatbox/typing";
        private const string OSC_PATH_CHATBOX_INPUT = "/chatbox/input";

        void Start()
        {

            InputField.enabled = false;
            
            // Starts an OSCQuery Server with Bogus Name, listens for Chatbox Services
            StartService();

            // Subscribes to each change made to the input field in order to show 'typing' indicator
            InputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            
            // Subscribes to Input Field submit to send data
            InputField.onEndEdit.AddListener(OnInputFieldSubmit);

            // Connects to default local VRC client for direct testing
            if (connectToDefaultVRCEndpoint)
            {
                AddChatboxReceiver(IPAddress.Loopback, 9000);
            }
            
            InvokeRepeating(nameof(RefreshServices), 1, RefreshServicesInterval);
        }

        private void RefreshServices()
        {
            _oscQueryService.RefreshServices();
        }

        // Creates a new OSCClient for each new Chatbox-capable receiver found
        private async void OnOscQueryServiceFound(OSCQueryServiceProfile profile)
        {
            await UniTask.SwitchToMainThread();
            
            if (await ServiceSupportsChatbox(profile))
            {
                var hostInfo = await OSCQuery.Extensions.GetHostInfo(profile.address, profile.port);
                HeaderText.text =
                    $"Sending to {profile.name} at {profile.address}:{hostInfo.oscPort}";
                AddChatboxReceiver(profile.address, hostInfo.oscPort);
            }
        }

        // Does the actual construction of the OSC Client, and advertises this service
        private void AddChatboxReceiver(IPAddress address, int port)
        {
            var receiver = new OscClientPlus(address.ToString(), port);
            _receivers.Add(receiver);
            _oscQueryService.AdvertiseOSCService(_serverName, port);
            
            // Enable inputfield to communicate with client
            InputField.enabled = true;
        }

        // Checks for compatibility by looking for matching Chatbox root node
        private async Task<bool> ServiceSupportsChatbox(OSCQueryServiceProfile profile)
        {
            var tree = await OSCQuery.Extensions.GetOSCTree(profile.address, profile.port);
            return tree.GetNodeWithPath(OSC_PATH_CHATBOX) != null;
        }

        // Sends 'typing' message to each receiver when input field value is changed
        private void OnInputFieldValueChanged(string value)
        {
            foreach (var receiver in _receivers)
            {
                receiver.Send(OSC_PATH_CHATBOX_TYPING, true);
            }
        }


        // Sends message immediately when field is submitted
        private void OnInputFieldSubmit(string value)
        {
            foreach (var receiver in _receivers)
            {
                string address = OSC_PATH_CHATBOX_INPUT;
                receiver.Send(address, value, true);
            }

            InputField.text = "";
        }

        private void StartService()
        {
            // Create a new OSCQueryService, advertise
            var port = VRC.OSCQuery.Extensions.GetAvailableTcpPort();
            _oscQueryService = new OSCQueryService(_serverName, new UnityMSLogger());
            _oscQueryService.StartOSCQueryService(_serverName, port);
            
            // Listen for other services
            _oscQueryService.OnOscQueryServiceAdded += OnOscQueryServiceFound;

            var services = _oscQueryService.GetOSCQueryServices();
            
            // Trigger event for any existing OSCQueryServices
            foreach (var profile in services)
            {
                OnOscQueryServiceFound(profile);
            }
            
            // Query network for services
            _oscQueryService.RefreshServices();
        }

        private void OnDestroy()
        {
            // Removes listeners from Scene Objects
            InputField.onValueChanged.RemoveAllListeners();
            InputField.onEndEdit.RemoveAllListeners();
            
            _oscQueryService.Dispose();
        }
    }

}