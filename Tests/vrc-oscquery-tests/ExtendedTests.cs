using System.Threading.Tasks;
namespace VRC.OSCQuery.Tests
{
    [TestFixture]
    public class ExtendedTests
    {
        [Test]
        public async Task OSCQueryService_OnDiscoveredServiceShutdown_EmitsOnOSCQueryServiceRemoved()
        {
            // Make and start sendService
            var sendServiceName = System.Guid.NewGuid().ToString();
            var sendService = new OSCQueryService
            (
                sendServiceName, 
                Extensions.GetAvailableTcpPort(),
                Extensions.GetAvailableUdpPort()
            );

            bool disconnected = false;
            
            // Make and start listenService
            var listenService = new OSCQueryService
            (
                System.Guid.NewGuid().ToString(), 
                Extensions.GetAvailableTcpPort(),
                Extensions.GetAvailableUdpPort()
            );

            listenService.serviceTimeoutInSeconds = 5;
            listenService.OnOscQueryServiceRemoved += profile =>
            {
                if (profile.name == sendServiceName)
                {
                    disconnected = true;
                }
            };
            listenService.OnOscQueryServiceAdded += async profile =>
            {
                if (profile.name == sendServiceName)
                {
                    sendService.Dispose();
                    // listenService.RefreshServices();

                    await Task.Delay(10000);
                    
                    Assert.True(disconnected);
                }
            };
            
            listenService.RefreshServices();
            await Task.Delay(10000);
            Assert.Fail("Timeout");
        }
    }
}