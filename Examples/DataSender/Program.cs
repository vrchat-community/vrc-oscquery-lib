using System;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples
{
    class DataSender
    {
        static void Main ()
        {
            Console.SetWindowSize(50,20);
            Application.Init ();

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
            
            var tcpPortField = new TextField($"{wordSet.Port()}")
            { 
                X = fieldWidth, Y = 2, Width = fieldWidth
                
            };
            var oscField = new TextField($"{wordSet.Port()}"){
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

        class OSCQueryServiceWindow : Window
        {
            private OSCQueryService _service;
            private TextField oscItemField;

            public OSCQueryServiceWindow(string name, int tcpPort, int oscPort)
            {
                _service = new OSCQueryService(name, tcpPort, oscPort);
                Title = $"{name} TCP: {tcpPort} OSC: {oscPort}";
                Add(MakeIntParams(10));
            }

            public View MakeIntParams(int count)
            {
                var result = new FrameView("Params and Values - Press to Randomize")
                {
                    X = 1,
                    Y = 2,
                    Height = Dim.Fill(2),
                    Width = Dim.Fill(2),
                };

                var r = new Random();
                var wordSet = new Bogus.DataSets.Hacker();
                var GenerateIntProp = new Func<string, int, string>((propertyName, intValue) => $"/{propertyName} {intValue}");
                _intParams = new int[count];
                for (int i = 0; i < count; i++)
                {
                    var name = $"{wordSet.Adjective()}-{wordSet.Noun()}";
                    
                    var newValue = r.Next(0, 99);
                    SetIntParam(i, newValue);
                    
                    var b = new Button(GenerateIntProp(name, newValue))
                    {
                        Width = Dim.Fill(),
                        Height = 1,
                        X = 1,
                        Y = 1 + i,
                        AutoSize = false,
                        TextAlignment = TextAlignment.Left
                    };
                    var localIndex = i;
                    b.Clicked += () =>
                    {
                        int value = r.Next(0, 99);
                        SetIntParam(localIndex, value);
                        b.Text = GenerateIntProp(name, value);
                    };
                    result.Add(b);
                    _service.AddEndpoint<int>($"/{name}", Attributes.AccessValues.ReadOnly,  () => GetIntParam(localIndex).ToString());
                }

                return result;
            }

            public int GetIntParam(int i)
            {
                if (_intParams.Length > i)
                {
                    return _intParams[i];
                }
                else
                {
                    return -1;
                }
            }

            public void SetIntParam(int i, int value)
            {
                if (_intParams.Length > i)
                {
                    _intParams[i] = value;
                }
            }

            private int[] _intParams;
        }
        
    }
    public static class Extensions
    {
        public static string UpperCaseFirstChar(this string text) {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }

    }
}