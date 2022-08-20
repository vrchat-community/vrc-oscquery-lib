using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VRC.OSCQuery.Tests
{
    public class SpecTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task OSCQueryService_OnRandomPort_ReturnsStatusCodeAtRoot()
        {
            int targetPort = GetRandomPort();
            var service = new OSCQueryService("test-service", targetPort);
            var result = await new HttpClient().GetAsync($"http://localhost:{targetPort}");
            Assert.True(result.IsSuccessStatusCode);
            
            service.Dispose();
        }

        [Test]
        public async Task Service_WithRandomOSCPort_ReturnsPortInHostInfo()
        {
            int tcpPort = GetRandomPort();
            int oscPort = GetRandomPort(); // Could technically conflict with above
            var service = new OSCQueryService("test-service", tcpPort, oscPort);
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}?{Attributes.HOST_INFO}");

            // Get HostInfo Json
            var hostInfoString = await response.Content.ReadAsStringAsync();
            var hostInfo = JsonConvert.DeserializeObject<HostInfo>(hostInfoString);
            Assert.AreEqual(oscPort, hostInfo.oscPort);

            service.Dispose();
        }

        [Test]
        public async Task Service_WithAddedProperty_ReturnsValueForThatProperty()
        {
            var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            var libLogger = loggerFactory.CreateLogger<OSCQueryService>();
            
            int tcpPort = 8080;
            var service = new OSCQueryService("TestService", tcpPort);
            int randomInt = new Random().Next();
            
            string name = Guid.NewGuid().ToString();
            string path = $"/{name}";
            service.AddEndpoint<int>(
                path, 
                Attributes.AccessValues.ReadOnly, 
                () => randomInt.ToString()
                );
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}{path}");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.AreEqual(randomInt, responseObject[Attributes.VALUE].Value<int>());
            
            service.Dispose();
        }

        [Test]
        public async Task Service_WithMultiplePaths_ReturnsValuesForAllChildren()
        {
            var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            var libLogger = loggerFactory.CreateLogger<OSCQueryService>();
            
            var r = new Random();
            int tcpPort = r.Next(9000,9999);
            var service = new OSCQueryService("TestService", tcpPort, OSCQueryService.DefaultPortOsc, libLogger);
            
            int randomInt1 = r.Next();
            int randomInt2 = r.Next();
            
            string name1 = Guid.NewGuid().ToString();
            string name2 = Guid.NewGuid().ToString();
            
            string path1 = $"/{name1}";
            string path2 = $"/{name2}";
            
            service.AddEndpoint<int>(
                path1, 
                Attributes.AccessValues.ReadOnly, 
                () => randomInt1.ToString()
            );
            
            service.AddEndpoint<int>(
                path2, 
                Attributes.AccessValues.ReadOnly, 
                () => randomInt2.ToString()
            );
            
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}/");

            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<OSCQueryNode>(responseString);
            
            Assert.AreEqual(randomInt1, int.Parse(responseObject.Contents[name1].Value));
            Assert.AreEqual(randomInt2, int.Parse(responseObject.Contents[name2].Value));

            service.Dispose();
        }

        [Test]
        public async Task Service_AfterAddingGrandChildNode_HasNodesForEachAncestor()
        {
            var service = new OSCQueryService();

            string fullPath = "/foo/bar/baz";

            service.AddEndpoint<int>(fullPath, Attributes.AccessValues.ReadOnly);
            
            var response = await new HttpClient().GetAsync($"http://localhost:{OSCQueryService.DefaultPortHttp}/");

            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OSCQueryNode>(responseString);
            
            Assert.NotNull(result.Contents["foo"].Contents["bar"].Contents["baz"]);
            
            Assert.Pass();
        }

        private int GetRandomPort()
        {
            return new Random().Next(1024, 49151);
        }

    }
}