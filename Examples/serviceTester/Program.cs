using Makaretu.Dns;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using VRC.OSCQuery;

namespace Browser
{
    class Program
    {
        static ILogger _programLogger;
        
        public static void Main(string[] args)
        {
            var r = new Random();
            var name = Guid.NewGuid().ToString();
            var httpPort = r.Next(5000, 9999);
            var oscPort = r.Next(5000, 9999);

            var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            _programLogger = loggerFactory.CreateLogger<Program>();
            _programLogger.LogInformation($"Started {name} at http: {httpPort} osc: {oscPort}");

            var libLogger = loggerFactory.CreateLogger<OSCQueryService>();
            var q = new OSCQueryService(name, httpPort, oscPort, libLogger );

            q.AddEndpoint<int>("test", Attributes.AccessValues.ReadOnly, "/test", () => 69);
            q.OnProfileAdded += OnProfileAdded;
            
            Console.ReadKey();
            q.Dispose();
        }

        private static void OnProfileAdded(ServiceProfile obj)
        {
            var srv = obj.Resources.OfType<SRVRecord>().First();
            _programLogger.LogInformation($"Found {obj.FullyQualifiedName} on {srv.Port}");
        }
    }
}