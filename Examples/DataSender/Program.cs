using System;
using System.Text.RegularExpressions;
using Common.Logging;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples
{
    class DataSender
    {
        static void Main ()
        {
            Console.SetWindowSize(50,20);
            Application.Init ();

            LogManager.Adapter = new StatusLoggerFactoryAdapter();
            // LogManager.GetLogger<DataSender>().Info($"Starting DataSender");
            AddOscQueryService();

            Application.Run ();
            Application.Shutdown ();
        }

        private static void AddOscQueryService()
        {
            var dialog = new Dialog("OSCQueryServiceCreator", 45, 8);

            dialog.Add(new Label(0,1, "Service Name", false){Width = 20});
            dialog.Add(new Label(0,2, "TCP Port", false){Width = 20});
            dialog.Add(new Label(0,3, "OSC Port", false){Width = 20});

            var wordSet = new Bogus.DataSets.Internet();
            var w = new Bogus.DataSets.Hacker();
            
            // Construct Input Fields
            int fieldWidth = 20;
            var nameField = new TextField($"{w.Adjective().UpperCaseFirstChar()}-{w.Noun().UpperCaseFirstChar()}-{w.Abbreviation()}")
            {
                X = fieldWidth, Y = 1, Width = fieldWidth
            };

            var startPort = VRC.OSCQuery.Extensions.GetAvailableTcpPort();
            var tcpPortField = new TextField($"{startPort}")
            { 
                X = fieldWidth, Y = 2, Width = fieldWidth
                
            };
            var oscField = new TextField($"{startPort}"){
                X = fieldWidth, Y = 3, Width = fieldWidth,
            };
            
            dialog.Add(nameField,tcpPortField,oscField);

            var buttonConfirm = new Button("_Ok", true);
            dialog.AddButton(buttonConfirm);

            buttonConfirm.Clicked += () =>
            {
                var window = new OSCQueryServiceWindow(nameField.Text.ToString(), Int32.Parse(tcpPortField.Text.ToString()), Int32.Parse(oscField.Text.ToString()));
                Application.Top.Add(window);
                Application.Top.Remove(dialog);
            };

            Application.Top.Add(dialog);
            Application.Top.BringSubviewToFront(dialog);
        }

    }
    public static class Extensions
    {
        public static string UpperCaseFirstChar(this string text) {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }

    }
}