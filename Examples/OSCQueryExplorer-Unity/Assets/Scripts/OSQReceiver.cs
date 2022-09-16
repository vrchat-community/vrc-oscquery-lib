using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Logging;
using UnityEngine;
using UnityEngine.UI;
using BlobHandles;
using OscCore;

#pragma warning disable 4014

namespace VRC.OSCQuery.Examples.OSCQueryExplorerUnity
{
    public class OSQReceiver : MonoBehaviour
    {
        public Text HeaderText;
        public Text InfoText;
        private OSCQueryService _oscQuery;
        private OscServer _receiver;
        private Dictionary<BlobString, string> _messagesValues = new Dictionary<BlobString, string>();
        private bool _messagesDirty;
        public int maxMessages = 10;
        
        void Start()
        {
            LogManager.Adapter = new UnityLoggerFactoryAdapter(LogLevel.All, true, true, true, "HH:mm:ss");
            StartService();
        }

        private void StartService()
        {
            var w = new Bogus.DataSets.Hacker();
            var w2 = new Bogus.DataSets.Lorem();
            var serverName = $"{w.IngVerb().UpperCaseFirstChar()}-{w2.Word().UpperCaseFirstChar()}-{w.Abbreviation()}";

            var port = VRC.OSCQuery.Extensions.GetAvailableTcpPort();
            _receiver = OscServer.GetOrCreate(port);
            _receiver.AddMonitorCallback(OnMessageReceived);
            _oscQuery = new OSCQueryService(
                serverName, 
                port,
                port
            );
            
            HeaderText.text = $"{serverName} listening on {port}";
        }

        private void OnMessageReceived(BlobString address, OscMessageValues values)
        {
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

        private void OnDestroy()
        {
            _receiver.Dispose();
            _oscQuery.Dispose();
        }
    }
    
    public static class Extensions
    {
        public static string UpperCaseFirstChar(this string text) {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }

    }

}