using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRC.OSCQuery.Samples.Shared;

#pragma warning disable 4014

namespace VRC.OSCQuery.Samples.Chatbox
{
    public class ChatboxSender : MonoBehaviour
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
        private const int RefreshServicesInterval = 5;
        private const int TypingIndicatorTimeout = 2;

        // Constant strings
        public const string OSC_PATH_CHATBOX = "/chatbox";
        public const string OSC_PATH_CHATBOX_TYPING = "/chatbox/typing";
        public const string OSC_PATH_CHATBOX_INPUT = "/chatbox/input";

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
        }

        private void RefreshServices()
        {
            _oscQueryService.RefreshServices();
        }

        // Creates a new OSCClient for each new Chatbox-capable receiver found
        private async void OnOscQueryServiceFound(OSCQueryServiceProfile profile)
        {
            Debug.Log($"Found profile {profile.name}");
            if(_profiles.Contains(profile))
                return;
            
            await UniTask.SwitchToMainThread();
            
            Debug.Log($"Checking for Chatbox compatibility in {profile.name}");
            if (await ServiceSupportsChatbox(profile))
            {
                Debug.Log($"{profile.name} compatible!");
                var hostInfo = await OSCQuery.Extensions.GetHostInfo(profile.address, profile.port);
                HeaderText.text =
                    $"Sending to {profile.name} at {profile.address}:{hostInfo.oscPort}";
                AddChatboxReceiver(profile.address, hostInfo.oscPort);
                _profiles.Add(profile);
            }
            else
            {
                Debug.Log($"{profile.name} NOT compatible!");
            }
        }

        List<OSCQueryServiceProfile> _profiles = new List<OSCQueryServiceProfile>();
        
        // Does the actual construction of the OSC Client, and advertises this service
        private void AddChatboxReceiver(IPAddress address, int port)
        {
            var receiver = new OscClientPlus(address.ToString(), port);
            _receivers.Add(receiver);

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
            
            CancelInvoke(nameof(ClearTypingIndicator));
            Invoke(nameof(ClearTypingIndicator), TypingIndicatorTimeout);
        }

        public void ClearTypingIndicator()
        {
            foreach (var receiver in _receivers)
            {
                receiver.Send(OSC_PATH_CHATBOX_TYPING, false);
            }
        }


        // Sends message immediately when field is submitted
        private void OnInputFieldSubmit(string value)
        {
            foreach (var receiver in _receivers)
            {
                receiver.Send(OSC_PATH_CHATBOX_INPUT, value, true);
            }

            InputField.text = "";
        }

        private void StartService()
        {
            var logger = new UnityMSLogger();
            
#if UNITY_ANDROID
            IDiscovery discovery = new AndroidDiscovery();
#else
            IDiscovery discovery = new MeaModDiscovery(logger);
#endif
            
            // Create a new OSCQueryService for the discovery
            _oscQueryService = new OSCQueryServiceBuilder()
                .WithServiceName(_serverName)
                .WithLogger(logger)
                .WithDiscovery(discovery)
                .Build();

            // Listen for other services
            _oscQueryService.OnOscQueryServiceAdded += OnOscQueryServiceFound;

            var services = _oscQueryService.GetOSCQueryServices();
            
            // Trigger event for any existing OSCQueryServices
            foreach (var profile in services)
            {
                OnOscQueryServiceFound(profile);
            }
            
            // Query network for services
            InvokeRepeating(nameof(RefreshServices), 1, RefreshServicesInterval);
        }

        private void OnDestroy()
        {
            // Removes listeners from Scene Objects
            InputField.onValueChanged.RemoveAllListeners();
            InputField.onEndEdit.RemoveAllListeners();
            
            _oscQueryService?.Dispose();
        }
    }

}