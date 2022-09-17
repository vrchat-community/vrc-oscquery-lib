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
        
        private OSCQueryService _oscQuery;
        private OscServer _receiver;
        
        void Start()
        {
            LogManager.Adapter = new UnityLoggerFactoryAdapter(LogLevel.All, true, true, true, "HH:mm:ss");
            StartService();
            
            _oscQuery.OnOscServiceAdded += OnOSCFound;
            _oscQuery.OnOscQueryServiceAdded += OnOSCQueryFound;
            
            InputField.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnOSCQueryFound(OSCServiceProfile profile)
        {
            Debug.Log($"Found OSCQuery Service {profile.name} on {profile.address}:{profile.port}");
        }

        private void OnOSCFound(OSCServiceProfile profile)
        {
            Debug.Log($"Found OSC Service {profile.name} on {profile.address}:{profile.port}");
        }

        private void OnValueChanged(string value)
        {
            Debug.Log($"Field now reads {value}");
        }

        private void StartService()
        {
            var w = new Bogus.DataSets.Hacker();
            var w2 = new Bogus.DataSets.Lorem();
            var serverName = $"{w.IngVerb().UpperCaseFirstChar()}-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

            var port = VRC.OSCQuery.Extensions.GetAvailableTcpPort();
            _receiver = OscServer.GetOrCreate(port);
            _oscQuery = new OSCQueryService(
                serverName, 
                port,
                port
            );
            
            HeaderText.text = $"{serverName} advertising on {port}";
        }

        private void OnDestroy()
        {
            _receiver.Dispose();
            _oscQuery.Dispose();
        }
    }

}