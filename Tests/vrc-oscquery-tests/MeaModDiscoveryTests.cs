﻿using System.Net;

namespace VRC.OSCQuery.Tests;

public class MeaModDiscoveryTests
{
    private const int DELAY_TIME_MS = 1000;
    
    [Test]
    public async Task Discovery_WhenOSCQueryServiceAdvertised_ContainsService()
    {
        // Set up Advertising and Discovery
        var advertiser = new MeaModDiscovery();
        var discoverer = new MeaModDiscovery();
        var oscQueryServiceProfile = new OSCQueryServiceProfile
        (
            Guid.NewGuid().ToString(),
            IPAddress.Loopback,
            Extensions.GetAvailableTcpPort(),
            OSCQueryServiceProfile.ServiceType.OSCQuery
        );

        // Advertise the service, Refresh the services, and wait for the service to be discovered
        advertiser.Advertise(oscQueryServiceProfile);
        await Task.Delay(DELAY_TIME_MS); // Wait for the service to be advertised
        discoverer.RefreshServices();
        await Task.Delay(DELAY_TIME_MS); // Wait for the service to be discovered

        // Assert that the service was discovered
        var discoveredServices = discoverer.GetOSCQueryServices();
        Assert.Contains(oscQueryServiceProfile, discoveredServices.ToList());

        // Stop advertising the service
        advertiser.Unadvertise(oscQueryServiceProfile);
    }
    
    [Test]
    public async Task Discovery_WhenOSCQueryServiceUnadvertised_DoesNotContainService()
    {
        // Set up Advertising and Discovery
        var advertiser = new MeaModDiscovery();
        var discoverer = new MeaModDiscovery();
        var oscQueryServiceProfile = new OSCQueryServiceProfile
        (
            Guid.NewGuid().ToString(),
            IPAddress.Loopback,
            Extensions.GetAvailableTcpPort(),
            OSCQueryServiceProfile.ServiceType.OSCQuery
        );

        // Advertise the service, Refresh the services, and wait for the service to be discovered
        advertiser.Advertise(oscQueryServiceProfile);
        await Task.Delay(DELAY_TIME_MS); // Wait for the service to be advertised
        discoverer.RefreshServices();
        await Task.Delay(DELAY_TIME_MS); // Wait for the service to be discovered

        // Assert that the service was discovered
        var discoveredServices = discoverer.GetOSCQueryServices();
        Assert.Contains(oscQueryServiceProfile, discoveredServices.ToList());

        // Stop advertising the service, Refresh the services, and wait for the service to be disconnected
        advertiser.Unadvertise(oscQueryServiceProfile);
        discoverer.RefreshServices();
        await Task.Delay(DELAY_TIME_MS); // Wait for the service to be disconnected
        
        // Assert that the service is no longer in the list
        discoveredServices = discoverer.GetOSCQueryServices();
        Assert.IsFalse(discoveredServices.Contains(oscQueryServiceProfile));
    }
}