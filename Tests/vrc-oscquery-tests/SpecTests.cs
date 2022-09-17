using System;
using System.Net;
using System.Net.Http;
using System.Threading;
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
            // Get HostInfo Json
            var hostInfo = await Extensions.GetHostInfo(IPAddress.Loopback, tcpPort);
            Assert.That(hostInfo.oscPort, Is.EqualTo(oscPort));

            service.Dispose();
        }

        [Test]
        public async Task Service_WithAddedIntProperty_ReturnsValueForThatProperty()
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
                randomInt.ToString()
            );
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}{path}");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.That(responseObject[Attributes.VALUE]!.Value<int>(), Is.EqualTo(randomInt));
            
            service.Dispose();
        }
        
        [Test]
        public async Task Service_WithAddedBoolProperty_ReturnsValueForThatProperty()
        {
            var random = new Random();
            int tcpPort = random.Next(9000,9999);
            var service = new OSCQueryService("TestService", tcpPort);
            
            string name = Guid.NewGuid().ToString();
            string path = $"/{name}";
            service.AddEndpoint<bool>(
                path, 
                Attributes.AccessValues.ReadOnly,
                false.ToString()
            );
            service.SetValue(path, "true");
            
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}{path}");
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.That(responseObject[Attributes.VALUE]!.Value<bool>(), Is.EqualTo(true));
            
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
                randomInt1.ToString()
            );

            service.AddEndpoint<int>(
                path2, 
                Attributes.AccessValues.ReadOnly,
                randomInt2.ToString()
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
        public void GetOSCTree_ReturnsExpectedValues()
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
                randomInt1.ToString()
            );

            service.AddEndpoint<int>(
                path2, 
                Attributes.AccessValues.ReadOnly,
                randomInt2.ToString()
            );

            var tree = Task.Run(() => Extensions.GetOSCTree(IPAddress.Loopback, tcpPort)).GetAwaiter().GetResult();
            Assert.NotNull(tree);

            var node1 = tree.GetNodeWithPath(path1);
            var node2 = tree.GetNodeWithPath(path2);
            
            Assert.That(node1.Name, Is.EqualTo(name1));
            Assert.That(node1.Value, Is.EqualTo(randomInt1.ToString()));
            
            Assert.That(node2.Name, Is.EqualTo(name2));
            Assert.That(node2.Value, Is.EqualTo(randomInt2.ToString()));
            
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
        
        [Test]
        public async Task Service_After404_CanReturnParameterValue()
        {
            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryService("TestService", port);
            
            var response = await new HttpClient().GetAsync($"http://localhost:{port}/whatever");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            
            // Add random int param
            string name = Guid.NewGuid().ToString();
            string path = $"/{name}";
            int value = new Random().Next();
            service.AddEndpoint<int>(
                path, 
                Attributes.AccessValues.ReadOnly,
                value.ToString()
            );

            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            response = await new HttpClient().GetAsync($"http://localhost:{port}{path}", tokenSource.Token);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.That(responseObject[Attributes.VALUE]!.Value<int>(), Is.EqualTo(value));
            
            service.Dispose();
        }

    }
}