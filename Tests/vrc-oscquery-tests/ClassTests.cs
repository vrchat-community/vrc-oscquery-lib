using System.Net;
using NUnit.Framework;

namespace VRC.OSCQuery.Tests;

[TestFixture]
public class ClassTests
{
        # region OSCQueryServiceProfile Equality Tests
        [Test]
        public void OSCQueryServiceProfile_Equals_ReturnsTrueForEqualProfiles()
        {
            var name = Guid.NewGuid().ToString();
            var address = IPAddress.Loopback;
            var port = new Random().Next();
            var serviceType = OSCQueryServiceProfile.ServiceType.OSC;
            var profile1 = new OSCQueryServiceProfile(name, address, port, serviceType);
            var profile2 = new OSCQueryServiceProfile(name, address, port, serviceType);
            Assert.That(profile1.Equals(profile2));
        }
        
        [Test]
        public void OSCQueryServiceProfile_Equals_ReturnsFalseForDifferentName()
        {
            var name1 = Guid.NewGuid().ToString();
            var name2 = Guid.NewGuid().ToString();
            var address = IPAddress.Loopback;
            var port = new Random().Next();
            var serviceType = OSCQueryServiceProfile.ServiceType.OSC;
            var profile1 = new OSCQueryServiceProfile(name1, address, port, serviceType);
            var profile2 = new OSCQueryServiceProfile(name2, address, port, serviceType);
            Assert.False(profile1.Equals(profile2));
        }
        
        [Test]
        public void OSCQueryServiceProfile_Equals_ReturnsFalseForDifferentAddress()
        {
            var name = Guid.NewGuid().ToString();
            var address1 = IPAddress.Loopback;
            var address2 = IPAddress.Any;
            var port = new Random().Next();
            var serviceType = OSCQueryServiceProfile.ServiceType.OSC;
            var profile1 = new OSCQueryServiceProfile(name, address1, port, serviceType);
            var profile2 = new OSCQueryServiceProfile(name, address2, port, serviceType);
            Assert.False(profile1.Equals(profile2));
        }
        
        [Test]
        public void OSCQueryServiceProfile_Equals_ReturnsFalseForDifferentPort()
        {
            var name = Guid.NewGuid().ToString();
            var address = IPAddress.Loopback;
            var port1 = new Random().Next();
            var port2 = new Random().Next();
            var serviceType = OSCQueryServiceProfile.ServiceType.OSC;
            var profile1 = new OSCQueryServiceProfile(name, address, port1, serviceType);
            var profile2 = new OSCQueryServiceProfile(name, address, port2, serviceType);
            Assert.False(profile1.Equals(profile2));
        }
        
        [Test]
        public void OSCQueryServiceProfile_Equals_ReturnsFalseForDifferentServiceType()
        {
            var name = Guid.NewGuid().ToString();
            var address = IPAddress.Loopback;
            var port = new Random().Next();
            var serviceType1 = OSCQueryServiceProfile.ServiceType.OSC;
            var serviceType2 = OSCQueryServiceProfile.ServiceType.OSCQuery;
            var profile1 = new OSCQueryServiceProfile(name, address, port, serviceType1);
            var profile2 = new OSCQueryServiceProfile(name, address, port, serviceType2);
            Assert.False(profile1.Equals(profile2));
        }
        
        [Test]
        public void OSCQueryServiceProfile_Equals_ReturnsFalseForNull()
        {
            var name = Guid.NewGuid().ToString();
            var address = IPAddress.Loopback;
            var port = new Random().Next();
            var serviceType = OSCQueryServiceProfile.ServiceType.OSC;
            var profile1 = new OSCQueryServiceProfile(name, address, port, serviceType);
            Assert.False(profile1.Equals(null));
        }

        #endregion
}