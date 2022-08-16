using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Makaretu.Dns;
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

        private int GetRandomPort()
        {
            return new Random().Next(1024, 49151);
        }

    }
}