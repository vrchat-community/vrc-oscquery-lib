using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Makaretu.Dns;
using Newtonsoft.Json;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples.DataReceiver
{
    class DataReceiver
    {

        private static OSCQueryService _service;
        
        static void Main ()
        {
            Application.Init ();
            
            // Add both menu and win in a single call
            Application.Top.Add (new FindServiceDialog(_service));
            
            Application.Run ();
            Application.Shutdown ();
        }

        public class ListServiceData : Window
        {
            private int _tcpPort;
            private TextView _textView;
            private ServiceProfile _profile;
            private SRVRecord _srvRecord;
            
            public ListServiceData(ServiceProfile profile)
            {
                _profile = profile;
                _srvRecord = profile.Resources.OfType<SRVRecord>().First();
                _tcpPort = _srvRecord.Port;

                _textView = new TextView()
                {
                    Width = Dim.Fill(2),
                    Height = Dim.Fill(2)
                };

                Title = $"{_profile.InstanceName} on {_srvRecord.Port}";
                
                Add(_textView);
                RefreshData();
            }

            private async Task RefreshData()
            {
                var response = await new HttpClient().GetAsync($"http://localhost:{_tcpPort}/");

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }
                
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OSCQueryRootNode>(responseString);

                var sb = new StringBuilder();
                foreach (var pair in result.Contents)
                {
                    sb.AppendLine($"{pair.Key}: {pair.Value.Value}");
                }

                _textView.Text = sb.ToString();
                await Task.Delay(500); // poll every half second
                RefreshData();
            } 
        }
    }
}