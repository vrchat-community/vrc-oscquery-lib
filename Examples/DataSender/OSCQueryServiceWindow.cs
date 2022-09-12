using System;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples
{
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
                _paramNames = new string[count];
                for (int i = 0; i < count; i++)
                {
                    var name = $"{wordSet.Adjective()}-{wordSet.Noun()}";
                    _paramNames[i] = name;
                    
                    _service.AddEndpoint<int>($"/{name}", Attributes.AccessValues.ReadOnly);
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
                    _service.SetValue($"/{_paramNames[i]}", value.ToString());
                }
            }

            private int[] _intParams;
            private string[] _paramNames;
        }
}