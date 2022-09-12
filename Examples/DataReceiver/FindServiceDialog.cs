using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Makaretu.Dns;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples.DataReceiver
{
    public class FindServiceDialog : Dialog
    {
        private ListView _listView;
        private ServiceProfile? _selectedProfile;

        private OSCQueryService _service;

        public FindServiceDialog()
        {
            _service = new OSCQueryService( OSCQueryService.DefaultServerName + "1", OSCQueryService.DefaultPortHttp + 10, OSCQueryService.DefaultPortOsc + 10);
            
            Width = 45;
            Height = 10;
            X = Pos.Center();
            Y = Pos.Center();

            var connectButton = new Button("Connect", true);
            connectButton.Enabled = false;
            connectButton.Clicked += () =>
            {
                if (_selectedProfile != null)
                {
                    Application.Top.Add(new DataReceiver.ListServiceData(_selectedProfile));
                    Application.Top.Remove(this);
                }
            };
            AddButton(connectButton);

            _listView = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(2),
                Height = Dim.Fill(2),
            };
            _listView.SelectedItemChanged += args =>
            {
                _selectedProfile = _oscqServices?[args.Item];
                connectButton.Enabled = true;
            };
            Add(_listView);

            _service.OnProfileAdded += _ =>
            {
                RefreshListings();
            };

            Enter += _ => RefreshListings();
        }

        private List<ServiceProfile>? _oscqServices;

        public void RefreshListings()
        {
            _oscqServices = _service.GetOSCQueryServices().ToList();
            var foundServices = _oscqServices.Count > 0;
            Title = foundServices ? $"OSCQuery Services Found: {_oscqServices.Count}" : "Searching for OSCQuery Services...";
            
            _listView.Visible = foundServices;
            _listView.SetSource(_oscqServices.Select(service=>service.InstanceName.ToString()).ToList());

            SetNeedsDisplay();
        }
    }
}