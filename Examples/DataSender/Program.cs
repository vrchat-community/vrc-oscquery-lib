using System;
using System.Collections.Generic;
using System.Linq;
using NStack;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples
{
    class DataSender
    {
        static void Main ()
        {
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
            
            // Construct Input Fields
            int fieldWidth = 20;
            var nameField = new TextField($"{OSCQueryService.DefaultServerName}")
            {
                X = fieldWidth, Y = 1, Width = fieldWidth
            };
            
            var tcpPortField = new TextField($"{OSCQueryService.DefaultPortHttp}")
            { 
                X = fieldWidth, Y = 2, Width = fieldWidth
                
            };
            var oscField = new TextField($"{OSCQueryService.DefaultPortOsc}"){
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
                // Border.BorderStyle = BorderStyle.None;

                Add(MakeIntParams(5));
                Add(MakeStringParams(5));

                _service.OnProfileAdded += _ =>
                {
                    RefreshListings();
                };

                Enter += _ => RefreshListings();
            }

            public View MakeIntParams(int count)
            {
                var result = new Window("Outgoing OSC Int Params")
                {
                    X = 0,
                    Y = 1,
                    Height = Dim.Fill(2),
                    Width = Dim.Percent(50f),
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

            public View MakeStringParams(int count)
            {
                var result = new Window("Outgoing OSC String Params")
                {
                    X = Pos.Percent(50f),
                    Y = 1,
                    Height = Dim.Fill(2),
                    Width = Dim.Percent(50)
                };
                
                var wordSet = new Bogus.DataSets.Hacker();
                var GenerateStringProp = new Func<string, string>((propertyName) => $"/{propertyName} {wordSet.IngVerb()} {wordSet.Noun()}");
                
                for (int i = 0; i < count; i++)
                {
                    var name = $"{wordSet.Adjective()}-{wordSet.Noun()}";
                    var b = new Button(GenerateStringProp(name))
                    {
                        Width = Dim.Fill(),
                        Height = 1,
                        X = 0,
                        Y = 1 + i,
                        AutoSize = false,
                        TextAlignment = TextAlignment.Left
                    };
                    b.Clicked += () => b.Text = GenerateStringProp(name);
                    result.Add(b);
                }

                return result;
            }
            
            public void RefreshListings()
            {
                var oscqServices = _service.GetOSCQueryServices().Select(s => s.HostName.ToString()).ToList();
                var oscServices = _service.GetOSCServices().Select(s => s.HostName.ToString()).ToList();

                var items1 = new List<MenuItem>();
                foreach (var oscqService in oscqServices)
                {
                    items1.Add(new MenuItem(oscqService, ustring.Empty, null));
                }
                    
                var items2 = new List<MenuItem>();
                foreach (var oscService in oscServices)
                {
                    items2.Add(new MenuItem(oscService, ustring.Empty, null));
                }
                    
                SetNeedsDisplay();
            }
        }
    }
}