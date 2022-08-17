using System;
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
            int tcpPort = 8080;
            var service = new OSCQueryService()
            {
                httpPort = tcpPort
            };
            
            int randomInt = new Random().Next();
            
            string name = Guid.NewGuid().ToString();
            string path = $"/{name}";
            service.AddEndpoint<int>(
                name, 
                Attributes.AccessValues.ReadOnly, 
                path, 
                () => randomInt
                );
            var response = await new HttpClient().GetAsync($"http://localhost:{tcpPort}{path}");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseString);
            
            Assert.AreEqual(randomInt, responseObject[Attributes.VALUE].Value<int>());
            
            service.Dispose();
        }

        private int GetRandomPort()
        {
            return new Random().Next(1024, 49151);
        }

    }
}