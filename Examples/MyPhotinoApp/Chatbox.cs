using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using OscJack;

namespace VRC.OSCQuery.Examples.Photino
{
    public class Chatbox : IDisposable
    {
        // List of receivers to send to
        private List<OscClient> _senders = new();
        private string _serverName = "ChatboxServer";
        private bool _doRefresh = false;
        private const int RefreshServicesIntervalMS = 10000;

        // Constant strings
        private const string OSC_PATH_CHATBOX = "/chatbox";
        private const string OSC_PATH_CHATBOX_TYPING = "/chatbox/typing";
        private const string OSC_PATH_CHATBOX_INPUT = "/chatbox/input";
        
        // Service
        private static OSCQueryService _oscQueryService;

        public Action<OSCQueryServiceProfile> OnChatboxFound;
        public Action<string, object> OnViewUpdate; 
        
        public void Initialize(int port)
        {
            // Create a new OSCQueryService, advertise
            _oscQueryService = new OSCQueryService();
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
            OnChatboxFound += AddChatboxReceiver;
            _doRefresh = true;

            Task.Run(RefreshServices);
        }
        
        private async Task RefreshServices()
        {
            while (_doRefresh)
            {
                _oscQueryService.RefreshServices();
                await Task.Delay(RefreshServicesIntervalMS);
            }
        }

        private void AddChatboxReceiver(OSCQueryServiceProfile profile)
        {
            AddChatboxReceiver(profile.address, profile.port);
        }
        
        // Does the actual construction of the OSC Client, and advertises this service
        private void AddChatboxReceiver(IPAddress address, int port)
        {
            try
            {
                var receiver = new OscClient(address.ToString(), port);
                _senders.Add(receiver);
                _oscQueryService.AdvertiseOSCService(_serverName, port);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // Creates a new OSCClient for each new Chatbox-capable receiver found
        private async void OnOscQueryServiceFound(OSCQueryServiceProfile profile)
        {
            if (await ServiceSupportsChatbox(profile))
            {
                var hostInfo = await OSCQuery.Extensions.GetHostInfo(profile.address, profile.port);
                var oscService = new OSCQueryServiceProfile(profile.name, profile.address, hostInfo.oscPort,
                    OSCQueryServiceProfile.ServiceType.OSC);
                OnChatboxFound(oscService);
            }
        }
        
        // Checks for compatibility by looking for matching Chatbox root node
        private async Task<bool> ServiceSupportsChatbox(OSCQueryServiceProfile profile)
        {
            var tree = await OSCQuery.Extensions.GetOSCTree(profile.address, profile.port);
            return tree == null ? false : tree.GetNodeWithPath(OSC_PATH_CHATBOX) != null;
        }
        
        public string SendChatTyping(bool value)
        {
            foreach (var sender in _senders)
            {
                sender.Send(OSC_PATH_CHATBOX_TYPING, true);
            }

            return "";
        }
        
        public string SendChatMessage(string s)
        {
            // Send message to each sender
            foreach (var sender in _senders)
            {
                sender.Send(OSC_PATH_CHATBOX_INPUT, s, true);
            }
            return Program.WebResponse($"Sent {s}");
        }


        public void Dispose()
        {
            // Stop looking for services
            _doRefresh = false;
            foreach (OscClient sender in _senders)
            {
                sender.Dispose();
            }
            _oscQueryService.Dispose();
        }
    }
}