using PhotinoNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace VRC.OSCQuery.Examples.Photino
{
    class Program
    {

        private static Chatbox _chatbox;
        private static int _port;

        private static Dictionary<string, Func<string, string>> _commands = new();

        [STAThread]
        static void Main(string[] args)
        {
            // Window title declared here for visibility
            string windowTitle = "OSCQuery Explorer";

            _port = 8090;

            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                    .SetTitle(windowTitle)
                    // Resize to a percentage of the main monitor work area
                    .SetUseOsDefaultSize(false)
                    .SetSize(new Size(600, 400))
                    // Center window in the middle of the screen
                    .Center()
                    .RegisterWebMessageReceivedHandler((object sender, string message) =>
                    {
                        var window = (PhotinoWindow)sender;

                        try
                        {
                            var commandMessage = JsonConvert.DeserializeObject<WebCommandMessage>(message);
                            if (_commands.ContainsKey(commandMessage.command))
                            {
                                window.SendWebMessage(_commands[commandMessage.command].Invoke(commandMessage.data));
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Could not parse command from {message} because {e.Message}");
                            return;
                        }
                    })
                    .Load("wwwroot/index.html")
                // .Load(new Uri($"http://localhost:{port}"))
                ;

            _commands.Add("startChatbox", s => StartChatbox(window));

            window.WaitForClose(); // Starts the application event loop
            if (_chatbox != null)
            {
                _chatbox.Dispose();
            }
        }

        private static string StartChatbox(PhotinoWindow window)
        {
            if (_chatbox != null) return null;

            window.SetTitle("VRC-Chatbox");

            // Starts an OSCQuery Server with Bogus Name, listens for Chatbox Services
            _chatbox = new Chatbox();
            _chatbox.OnChatboxFound += profile => window.SendWebMessage
            (
                WebCommandMessage.Create("connectToServer", profile).ToString()
            );
            _chatbox.Initialize(_port);
            _commands.Add("sendChatMessage", s => _chatbox.SendChatMessage(s));
            _commands.Add("sendChatTyping", s => _chatbox.SendChatTyping(true));

            return WebResponse("Started Chatbox");
        }

        public static string WebResponse(string message)
        {
            return new WebCommandMessage("response", message).ToString();
        }

        public class WebCommandMessage
        {
            public string command;
            public string data;

            public WebCommandMessage(string command, string data)
            {
                this.command = command;
                this.data = data;
            }
            
            public static WebCommandMessage Create<T>(string command, T data)
            {
                string dataString = JsonConvert.SerializeObject(data);
                return new WebCommandMessage(command, dataString);
            }

            public new string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
    }
}
