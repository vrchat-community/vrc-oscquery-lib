using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VRC.OSCQuery.Tests
{
    [TestFixture]
    public class SpecTests
    {
         [Test]
        public async Task OSCQueryServiceFluent_FromFluentBuilderWithTcpPort_ReturnsSamePort()
        {
            int port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .Build();
            
            Assert.That(port, Is.EqualTo(service.TcpPort));
            
            service.Dispose();
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_OnRandomPort_ReturnsStatusCodeAtRoot()
        {
            int port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();
            
            var result = await new HttpClient().GetAsync($"http://localhost:{port}");
            Assert.True(result.IsSuccessStatusCode);
            
            service.Dispose();
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_WithRandomOSCPort_ReturnsPortInHostInfo()
        {
            int port = Extensions.GetAvailableTcpPort();
            int oscPort = Extensions.GetAvailableUdpPort();
            
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .WithUdpPort(oscPort)
                .StartHttpServer()
                .Build();
            
            // Get HostInfo via HTTP
            var hostInfo = await Extensions.GetHostInfo(IPAddress.Loopback, port);
            Assert.That(hostInfo.oscPort, Is.EqualTo(oscPort));

            service.Dispose();
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_WithAddedIntProperty_ReturnsValueForThatProperty()
        {
            int port = Extensions.GetAvailableTcpPort();

            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();

            int randomInt = new Random().Next();
            
            string name = Guid.NewGuid().ToString();
            string path = $"/{name}";
            service.AddEndpoint<int>(
                path, 
                Attributes.AccessValues.ReadOnly,
                randomInt.ToString()
            );
            var response = await new HttpClient().GetAsync($"http://localhost:{port}{path}");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.That(responseObject[Attributes.VALUE]!.Value<int>(), Is.EqualTo(randomInt));
            
            service.Dispose();
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_WithAddedBoolProperty_ReturnsValueForThatProperty()
        {
            var random = new Random();
            int port = Extensions.GetAvailableTcpPort();

            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();

            string name = Guid.NewGuid().ToString();
            string path = $"/{name}";
            service.AddEndpoint<int>(
                path, 
                Attributes.AccessValues.ReadOnly,
                false.ToString()
            );
            service.SetValue(path, "true");
            
            var response = await new HttpClient().GetAsync($"http://localhost:{port}{path}");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.That(responseObject[Attributes.VALUE]!.Value<bool>(), Is.EqualTo(true));
            
            service.Dispose();
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_WithMultiplePaths_ReturnsValuesForAllChildren()
        {
            var r = new Random();
            int port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();
            
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
            
            var response = await new HttpClient().GetAsync($"http://localhost:{port}/");

            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<OSCQueryNode>(responseString);
            
            Assert.That(int.Parse(responseObject.Contents[name1].Value), Is.EqualTo(randomInt1));
            Assert.That(int.Parse(responseObject.Contents[name2].Value), Is.EqualTo(randomInt2));
            
            service.Dispose();
        }
        
        [Test]
        public void OSCQueryServiceFluent_GetOSCTree_ReturnsExpectedValues()
        {
            var r = new Random();
            int tcpPort = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(tcpPort)
                .StartHttpServer()
                .Build();
            
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
        public async Task OSCQueryServiceFluent_AfterAddingGrandChildNode_HasNodesForEachAncestor()
        {

            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();

            string fullPath = "/foo/bar/baz";

            service.AddEndpoint<int>(fullPath, Attributes.AccessValues.ReadOnly);
            
            var response = await new HttpClient().GetAsync($"http://localhost:{port}/");

            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OSCQueryNode>(responseString);
            
            Assert.NotNull(result.Contents["foo"].Contents["bar"].Contents["baz"]);
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_WithRequestForFavicon_ReturnsSuccess()
        {
            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();
            
            var response = await new HttpClient().GetAsync($"http://localhost:{port}/favicon.ico");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        
        [Test]
        public async Task OSCQueryServiceFluent_After404_CanReturnParameterValue()
        {
            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();
            
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
        
        [Test]
        public void OSCQueryServiceFluent_GivenInvalidPathToAdd_ReturnsFalse()
        {
            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();
            var result = service.AddEndpoint<bool>("invalid", Attributes.AccessValues.ReadWrite);
            Assert.False(result);
        }
        
        [Test]
        public void OSCQueryServiceFluent_RootNode_HasFullPathWithSlash()
        {
            var port = Extensions.GetAvailableTcpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .StartHttpServer()
                .Build();
            
            var tree = Task.Run(() => Extensions.GetOSCTree(IPAddress.Loopback, port)).GetAwaiter().GetResult();
            Assert.NotNull(tree);
            string rootPath = "/";
            var rootNode = tree.GetNodeWithPath(rootPath);
            Assert.That(rootPath, Is.EqualTo(rootNode.FullPath));
        }
        
        [Test]
        public void OSCQueryServiceFluent_WithUdpPort_ReturnsSamePort()
        {
            var port = Extensions.GetAvailableTcpPort();
            var oscPort = Extensions.GetAvailableUdpPort();
            var service = new OSCQueryServiceBuilder()
                .WithTcpPort(port)
                .WithUdpPort(oscPort)
                .StartHttpServer()
                .Build();
            
            Assert.That(oscPort, Is.EqualTo(service.OscPort));
        }

    }
}