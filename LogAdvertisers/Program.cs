using VRC.OSCQuery;

namespace VRC.OSCquery.Examples;

class LogAdvertisers
{
    static void Main()
    {
        var service = new OSCQueryServiceBuilder().WithDefaults().Build();
        var localIp = service.HostIP;
        var localPort = service.TcpPort;
        var name = service.ServerName;
        
        service.OnOscQueryServiceAdded += async (serviceProfile) =>
        {
            System.Console.WriteLine($"OSCQuery Service Found: {serviceProfile.name} {serviceProfile.address}:{serviceProfile.port}");
            if (serviceProfile.name != name)
            {
                System.Console.WriteLine($"OSCQuery Service Found: {serviceProfile.name} {serviceProfile.address}:{serviceProfile.port}");
                // Get the service's tree
                Console.WriteLine($"Requesting tree from {serviceProfile.name}");
                var tree = await OSCQuery.Extensions.GetOSCTree(serviceProfile.address, serviceProfile.port);
                Console.WriteLine($"{serviceProfile.name}:\n {tree}");
            }
        };
        
        service.OnOscServiceAdded += (serviceProfile) =>
        {
            System.Console.WriteLine($"OSC Service Found: {serviceProfile.name} {serviceProfile.address}:{serviceProfile.port}");
        };
        
        // Exit on user keypress
        System.Console.WriteLine("Press any key to exit.");
        System.Console.ReadKey();
        
        service.Dispose();
    }
}