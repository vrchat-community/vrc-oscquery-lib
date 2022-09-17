using System;
using System.Collections.Generic;
using Common.Logging;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text;
using MeaMod.DNS.Model;
using MeaMod.DNS.Multicast;
using Newtonsoft.Json;
#pragma warning disable 4014

namespace VRC.OSCQuery.Examples.OSCQueryExplorerUnity
{
    [RequireComponent(typeof(Canvas))]
    public class OSQCanvas : MonoBehaviour
    {
        public Button RefreshButton;
        public GameObject ServerButtonPrefab;
        public Transform ServerButtonContainer;
        public Text InfoText;
        private Canvas _canvas;
        private OSCQueryService _oscQuery;

        private List<ServiceProfile> _profiles = new List<ServiceProfile>();
    
        void Start()
        {
            LogManager.Adapter = new UnityLoggerFactoryAdapter(LogLevel.All, true, true, true, "HH:mm:ss");
            
            StartService();
            RefreshButton.onClick.AddListener(RefreshServices);
            
            _canvas = GetComponent<Canvas>();
        }

        private void StartService()
        {
            var port = VRC.OSCQuery.Extensions.GetAvailableTcpPort();
            _oscQuery = new OSCQueryService(
                "OSCQueryExplorer-Unity", 
                port,
                port
            );
            _oscQuery.OnProfileAdded += profile => RefreshServices();
        }

        private void ClearServices()
        {
            foreach (Transform child in ServerButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private async void RefreshServices()
        {
            // Wait for main thread
            await UniTask.SwitchToMainThread();
            
            // Don't allow edit-time refreshes
            if (!Application.isPlaying) return;

            // Clear all existing buttons
            ClearServices();
            
            // Add each one anew
            foreach (var profile in _oscQuery.GetOSCQueryServices())
            {
                // Create button
                var serverButtonGameObject = Instantiate(ServerButtonPrefab, ServerButtonContainer, false);
            
                // Set Label
                var label = serverButtonGameObject.GetComponentInChildren<Text>();
                label.text = $"{profile.name}";
            
                // Listen for click
                var button = serverButtonGameObject.GetComponent<Button>();
                button.onClick.AddListener(()=>ShowInfoFor(profile));
            }
        }

        private async UniTask ShowInfoFor(OSCQueryServiceProfile profile)
        {
            try
            {
                await RefreshData(profile.port, InfoText);
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't find SRVRecord in {profile.name}: {e.Message}");
                return;
            }
        }
        
        private async UniTask RefreshData(int _tcpPort, Text _textView)
        {
            var response = await new HttpClient().GetAsync($"http://localhost:{_tcpPort}/");

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OSCQueryRootNode>(responseString);

                _textView.text = GetStringFromContents(result.Contents);
            }
        }

        private string GetStringFromContents(Dictionary<string, OSCQueryNode> contents, string prefix = "")
        {
            var sb = new StringBuilder();
            foreach (var pair in contents)
            {
                sb.AppendLine($"{prefix}{pair.Key} {pair.Value.Value}");
                if (pair.Value.Contents != null)
                {
                    sb.Append(GetStringFromContents(pair.Value.Contents, $"    {prefix}"));
                }
                
                // Add extra line after top-level items
                if (prefix == "")
                {
                    sb.AppendLine("");
                }
            }

            return sb.ToString();
        }

        private void OnDestroy()
        {
            _oscQuery.Dispose();
            ClearServices();
        }
    }

}