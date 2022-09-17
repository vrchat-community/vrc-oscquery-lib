using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Common.Logging;
using UnityEngine;
using UnityEngine.UI;
using OscCore;

#pragma warning disable 4014

namespace VRC.OSCQuery.Examples.OSCQueryExplorerUnity
{
    public class ChatboxCanvas : MonoBehaviour
    {
        public Text HeaderText;
        public InputField InputField;
        public bool connectToDefaultVRCEndpoint;

        private OSCQueryService _oscQueryService;
        private List<OscClient> _receivers = new List<OscClient>();

        void Start()
        {
            LogManager.Adapter = new UnityLoggerFactoryAdapter(LogLevel.All, true, true, true, "HH:mm:ss");
            StartService();

            InputField.onValueChanged.AddListener(OnValueChanged);
            InputField.onEndEdit.AddListener(OnEndEdit);

            if (connectToDefaultVRCEndpoint)
            {
                AddChatboxReceiver(IPAddress.Loopback, 9000);
            }
        }

        private async void OnOscQueryServiceFound(OSCQueryServiceProfile profile)
        {
            Debug.Log($"found oscqueryservice {profile.name}");
            if (await ServiceSupportsChatbox(profile))
            {
                var hostInfo = await OSCQuery.Extensions.GetHostInfo(profile.address, profile.port);
                AddChatboxReceiver(profile.address, hostInfo.oscPort);
            }
        }

        private void AddChatboxReceiver(IPAddress address, int port)
        {
            var receiver = new OscClient(address.ToString(), port);
            _receivers.Add(receiver);
            _oscQueryService.BeAnOscServer(_serverName, port);
        }

        private async Task<bool> ServiceSupportsChatbox(OSCQueryServiceProfile profile)
        {
            Debug.Log($"Checking fro chatbox in {profile.name}");
            var tree = await OSCQuery.Extensions.GetOSCTree(profile.address, profile.port);
            Debug.Log($"Got tree with {tree.Contents.Count} nodes");
            return tree.GetNodeWithPath("/chatbox") != null;
        }

        private void OnValueChanged(string value)
        {
            foreach (var receiver in _receivers)
            {
                receiver.Send("/chatbox/typing", true);
            }
        }

        private void OnEndEdit(string value)
        {
            foreach (var receiver in _receivers)
            {
                string address = "/chatbox/input";
                receiver.Send(address, value, true);
            }
        }

        private string _serverName;
        
        private void StartService()
        {
            var w = new Bogus.DataSets.Hacker();
            var w2 = new Bogus.DataSets.Lorem();
            _serverName = $"{w.IngVerb().UpperCaseFirstChar()}-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

            var port = VRC.OSCQuery.Extensions.GetAvailableTcpPort();
            _oscQueryService = new OSCQueryService(_serverName);
            _oscQueryService.BeAnOSCQueryServer(_serverName, port);
            
            _oscQueryService.OnOscQueryServiceAdded += OnOscQueryServiceFound;

            HeaderText.text = $"{_serverName} advertising on {port}";
            
            // Look for existing Chatbox-capable service
            foreach (var profile in _oscQueryService.GetOSCQueryServices())
            {
               OnOscQueryServiceFound(profile);
            }
            
            _oscQueryService.RefreshServices();
        }

        private void OnDestroy()
        {
            // _receiver.Dispose();
            _oscQueryService.Dispose();
        }
    }

}