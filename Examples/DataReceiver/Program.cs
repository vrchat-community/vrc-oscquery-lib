using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples.DataReceiver
{
    class DataReceiver
    {
        
        static void Main ()
        {
#pragma warning disable CA1416
            Console.SetWindowSize(50,15);
#pragma warning restore CA1416
            Application.Init ();

            Application.Top.Add (new FindServiceDialog(new StatusLogger()));
            
            Application.Run ();
            Application.Shutdown ();
        }

        public class ListServiceData : Window
        {
            private int _tcpPort;
            private TextView _textView;
            private OSCQueryServiceProfile? _profile;
            
            public ListServiceData(OSCQueryServiceProfile profile)
            {
                _profile = profile;
                _tcpPort = profile.port;

                _textView = new TextView()
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(2),
                    Height = Dim.Fill(2),
                    ReadOnly = true,
                };

                Title = $"{_profile?.name} on {_profile.port}";
                
                Add(_textView);
#pragma warning disable 4014
                RefreshData();
#pragma warning restore 4014
            }

            private async Task RefreshData()
            {
                var response = await new HttpClient().GetAsync($"http://localhost:{_tcpPort}/");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<OSCQueryRootNode>(responseString);

                    var sb = new StringBuilder();
                    foreach (var pair in result.Contents)
                    {
                        sb.AppendLine($"{pair.Key}: {pair.Value.Value[0].ToString()}");
                    }

                    _textView.Text = sb.ToString();
                }
                
                await Task.Delay(500); // poll every half second
#pragma warning disable 4014
                RefreshData();
#pragma warning restore 4014
            } 
        }
    }
}