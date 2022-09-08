using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
            int targetPort = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryService("test-service", targetPort);
            var result = await new HttpClient().GetAsync($"http://localhost:{targetPort}");
            Assert.True(result.IsSuccessStatusCode);
            
            service.Dispose();
        }

        [Test]
        public async Task Service_WithRandomOSCPort_ReturnsPortInHostInfo()
        {
            int tcpPort = Extensions.GetAvailableTcpPort();
            int oscPort = Extensions.GetAvailableUdpPort();
            var service = new OSCQueryService("test-service", tcpPort, oscPort);
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}?{Attributes.HOST_INFO}");

            // Get HostInfo Json
            var hostInfoString = await response.Content.ReadAsStringAsync();
            var hostInfo = JsonConvert.DeserializeObject<HostInfo>(hostInfoString);
            Assert.That(hostInfo.oscPort, Is.EqualTo(oscPort));

            service.Dispose();
        }

        [Test]
        public async Task Service_WithAddedProperty_ReturnsValueForThatProperty()
        {
            var random = new Random();
            int tcpPort = random.Next(9000,9999);
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
            
            Assert.That(responseObject[Attributes.VALUE]!.Value<int>(), Is.EqualTo(randomInt));
            
            service.Dispose();
        }

        [Test]
        public async Task Service_WithMultiplePaths_ReturnsValuesForAllChildren()
        {
            var r = new Random();
            int tcpPort = Extensions.GetAvailableTcpPort();
            var udpPort = Extensions.GetAvailableUdpPort();
            var service = new OSCQueryService("TestService", tcpPort, udpPort);
            
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
            
            Assert.That(int.Parse(responseObject.Contents[name1].Value), Is.EqualTo(randomInt1));
            Assert.That(int.Parse(responseObject.Contents[name2].Value), Is.EqualTo(randomInt2));
            
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

        [Test]
        public async Task Service_WithRequestForFavicon_NoCrash()
        {
            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryService("TestService", port);
            
            var response = await new HttpClient().GetAsync($"http://localhost:{port}/favicon.ico");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

    }
}