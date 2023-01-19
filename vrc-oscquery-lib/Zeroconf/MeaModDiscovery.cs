using System;
using System.Collections.Generic;
using System.Linq;
using MeaMod.DNS.Model;
using MeaMod.DNS.Multicast;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VRC.OSCQuery
{
    public class MeaModDiscovery : IDiscovery
    {
        private ServiceDiscovery _discovery;
        private MulticastService _mdns;
        private static ILogger<OSCQueryService> Logger;
        
        // Store discovered services
        private readonly HashSet<OSCQueryServiceProfile> _oscQueryServices = new HashSet<OSCQueryServiceProfile>();
        private readonly HashSet<OSCQueryServiceProfile> _oscServices = new HashSet<OSCQueryServiceProfile>();
        
        public HashSet<OSCQueryServiceProfile> GetOSCQueryServices() => _oscQueryServices;
        public HashSet<OSCQueryServiceProfile> GetOSCServices() => _oscServices;

        public void Dispose()
        {
            _discovery?.Dispose();
            _mdns?.Stop();
        }

        public MeaModDiscovery(ILogger<OSCQueryService> logger = null)
        {
            Logger = logger ?? new NullLogger<OSCQueryService>();
            
            _mdns = new MulticastService();
            _mdns.UseIpv6 = false;
            _mdns.IgnoreDuplicateMessages = true;
            
            _discovery = new ServiceDiscovery(_mdns);
            
            // Query for OSC and OSCQuery services on every network interface
            _mdns.NetworkInterfaceDiscovered += (s, e) =>
            {
                RefreshServices();
            };
            
            // Callback invoked when the above query is answered
            _mdns.AnswerReceived += OnRemoteServiceInfo;
            _mdns.Start();
        }
        
        public void RefreshServices()
        {
            _mdns.SendQuery(OSCQueryService._localOscUdpServiceName);
            _mdns.SendQuery(OSCQueryService._localOscJsonServiceName);
        }
        
        public event Action<OSCQueryServiceProfile> OnOscServiceAdded;
        public event Action<OSCQueryServiceProfile> OnOscQueryServiceAdded;
        
        private Dictionary<OSCQueryServiceProfile, ServiceProfile> _profiles = new Dictionary<OSCQueryServiceProfile, ServiceProfile>();
        public void Advertise(OSCQueryServiceProfile profile)
        {
            var meaProfile = new ServiceProfile(profile.name, profile.GetServiceTypeString(), (ushort)profile.port, new[] { profile.address });
            _discovery.Advertise(meaProfile);
            _profiles.Add(profile, meaProfile);
            
            Logger.LogInformation($"Advertising Service {profile.name} of type {profile.serviceType} on {profile.port}");
        }

        public void Unadvertise(OSCQueryServiceProfile profile)
        {
            if (_profiles.ContainsKey(profile))
            {
                _discovery.Unadvertise(_profiles[profile]);
                _profiles.Remove(profile);
            }
            Logger.LogInformation($"Unadvertising Service {profile.name} of type {profile.serviceType} on {profile.port}");
        }

        /// <summary>
        /// Callback invoked when an mdns Service provides information about itself 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs">Event Data with info from queried Service</param>
        private void OnRemoteServiceInfo(object sender, MessageEventArgs eventArgs)
        {
            var response = eventArgs.Message;
            
            try
            {
                // Check whether this service matches OSCJSON or OSC services for which we're looking
                var hasMatch = response.Answers.Any(record => OSCQueryService.MatchedNames.Contains(record?.CanonicalName));
                if (!hasMatch)
                {
                    return;
                }

                foreach (ResourceRecord answer in response.Answers.Where(a=>OSCQueryService.MatchedNames.Contains(a.CanonicalName)))
                {
                    try
                    {
                        var srvRecord = response.AdditionalRecords.OfType<SRVRecord>().First();
                        // Get the name and SRV Record of the service
                        var name = answer.CanonicalName;
                        AddMatchedService(response, srvRecord);
                    }
                    catch (Exception)
                    {
                        Logger.LogInformation($"No good SRV Record found in {response.Id}");
                    }
                }
            }
            catch (Exception e)
            {
                // Using a non-error log level because we may have just found a non-matching service
                Logger.LogInformation($"Could not parse answer from {eventArgs.RemoteEndPoint}: {e.Message}");
            }
        }

        private void AddMatchedService(Message response, SRVRecord srvRecord)
        {
            // Get the rest of the items we need to track this service
            var port = srvRecord.Port;
            var domainName = srvRecord.Name.Labels;
            var instanceName = domainName[0];

            var serviceName = string.Join(".", domainName.Skip(1));
            var ips = response.AdditionalRecords.OfType<ARecord>().Select(r => r.Address);
                
            var ipAddressList = ips.ToList();
            var profile = new ServiceProfile(instanceName, serviceName, srvRecord.Port, ipAddressList);

            // If this is an OSC service, add it to the OSC collection
            if (string.Compare(serviceName, OSCQueryService._localOscUdpServiceName, StringComparison.Ordinal) == 0 && !_profiles.ContainsValue(profile))
            {
                // Make sure there's not already a service with the same name
                if (_oscServices.All(p => p.name != profile.InstanceName))
                {
                    var p = new OSCQueryServiceProfile(instanceName, ipAddressList.First(), port, OSCQueryServiceProfile.ServiceType.OSC);
                    _oscServices.Add(p);
                    OnOscServiceAdded?.Invoke(p);
                    Logger.LogInformation($"Found OSC Service match {serviceName} on port {port}");
                }
            }
            // If this is an OSCQuery service, add it to the OSCQuery collection
            else if (string.Compare(serviceName, OSCQueryService._localOscJsonServiceName, StringComparison.Ordinal) == 0 && !_profiles.ContainsValue(profile))
            {
                // Make sure there's not already a service with the same name
                if (_oscQueryServices.All(p => !p.name.Equals(profile.InstanceName)))
                {
                    var p = new OSCQueryServiceProfile(instanceName, ipAddressList.First(), port, OSCQueryServiceProfile.ServiceType.OSCQuery);
                    _oscQueryServices.Add(p);
                    OnOscQueryServiceAdded?.Invoke(p);
                    Logger.LogInformation($"Found OSCQuery Service match {serviceName} on port {port}");
                }
            }
        }
    }
    
    
}