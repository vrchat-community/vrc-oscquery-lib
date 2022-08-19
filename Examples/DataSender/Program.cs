using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using NStack;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples
{
    class DataSender
    {

        static uint portOffset = 0;
        
        static void Main ()
        {
            Application.Init ();
            var menu = new MenuBar (new MenuBarItem [] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => { 
                        Application.RequestStop (); 
                    })
                }),
                new MenuBarItem("_New", new MenuItem[]
                {
                    new MenuItem("OSC_QueryService", "help", ()=>
                    {
                        AddOscQueryService();
                    })
                })
            });

            // Add both menu and win in a single call
            Application.Top.Add (menu);
            
            Application.Run ();
            Application.Shutdown ();
        }

        private static void AddOscQueryService()
        {
            var dialog = new Dialog("OSCQueryServiceCreator", 45, 8);

            int fieldWidth = 20;
            
            dialog.Add(new Label(0,1, "Service Name", false){Width = 20});
            dialog.Add(new Label(0,2, "TCP Port", false){Width = 20});
            dialog.Add(new Label(0,3, "OSC Port", false){Width = 20});
            
            var nameField = new TextField($"{OSCQueryService.DefaultServerName}-{portOffset}")
            {
                X = fieldWidth,
                Y = 1,
                Width = fieldWidth,
            };
            var tcpPortField = new TextField($"{OSCQueryService.DefaultPortHttp + portOffset}"){
                X = fieldWidth,
                Y = 2,
                Width = fieldWidth,
            };
            var oscField = new TextField($"{OSCQueryService.DefaultPortOsc + portOffset}"){
                X = fieldWidth,
                Y = 3,
                Width = fieldWidth,
            };
            
            dialog.Add(nameField,tcpPortField,oscField);
            
            var buttonConfirm = new Button("_Ok", true);
            var buttonCancel = new Button("_Cancel");
            dialog.AddButton(buttonConfirm);
            dialog.AddButton(buttonCancel);

            buttonConfirm.Clicked += () =>
            {
                var window = new OSCQueryServiceWindow(nameField.Text.ToString(), Int32.Parse(tcpPortField.Text.ToString()), Int32.Parse(oscField.Text.ToString()));
                portOffset++;
                Application.Top.Add(window);
                Application.Top.Remove(dialog);
            };

            buttonCancel.Clicked += () =>
            {
                Application.Top.Remove(dialog);
            };
            
            Application.Top.Add(dialog);
            Application.Top.BringSubviewToFront(dialog);
        }

        class OSCQueryServiceWindow : Window
        {
            private OSCQueryService _service;
            private MenuBar _menu;
            private MenuBarItem menuServicesItem;
            private MenuBarItem menuServicesItem2;
            private TextField oscItemField;

            public OSCQueryServiceWindow(string name, int tcpPort, int oscPort)
            {
                _service = new OSCQueryService(name, tcpPort, oscPort);
                Title = $"{name} TCP: {tcpPort} OSC: {oscPort}";
                Width = 50;
                Height = 20;

                var beeboo = new Bogus.DataSets.Hacker();

                for (int i = 0; i < 5; i++)
                {
                    var propertyName = $"{beeboo.Adjective()}-{beeboo.Noun()}";
                    var b = new Button($"{propertyName}: {i}")
                    {
                        Width = Dim.Fill(),
                        Height = 1,
                        X = 0,
                        Y = 1 + i,
                        AutoSize = false,
                        TextAlignment = TextAlignment.Left
                    };
                    var index = i;
                    b.Clicked += () => b.Text = $"{propertyName}: {new Random().Next(0,10)}";
                    Add(b);
                }
                
                menuServicesItem = new MenuBarItem("OSCQuery: 0", new []{new MenuItem("None Found", "", RefreshListings)});
                menuServicesItem2 = new MenuBarItem("OSC: 0", new []{new MenuItem("None Found", "", RefreshListings)});

                _menu = new MenuBar(new []
                {
                    menuServicesItem, menuServicesItem2
                });
                
                Add(_menu);
                
                _service.OnProfileAdded += _ =>
                {
                    RefreshListings();
                };

                Enter += _ => RefreshListings();
            }
            
            public void RefreshListings()
            {
                var oscqServices = _service.GetOSCQueryServices().Select(s => s.HostName.ToString()).ToList();
                var oscServices = _service.GetOSCServices().Select(s => s.HostName.ToString()).ToList();

                menuServicesItem.Title = $"OSCQuery: {oscqServices.Count}";
                menuServicesItem2.Title = $"OSC: {oscServices.Count}";

                var items1 = new List<MenuItem>();
                foreach (var oscqService in oscqServices)
                {
                    items1.Add(new MenuItem(oscqService, ustring.Empty, null));
                }
                menuServicesItem.Children = items1.ToArray();
                    
                var items2 = new List<MenuItem>();
                foreach (var oscService in oscServices)
                {
                    items2.Add(new MenuItem(oscService, ustring.Empty, null));
                }
                menuServicesItem2.Children = items2.ToArray();
                    
                this.SetNeedsDisplay();
            }
        }
    }
}