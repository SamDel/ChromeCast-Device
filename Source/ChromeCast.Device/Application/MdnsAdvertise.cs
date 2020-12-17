using ChromeCast.Device.Log.Interfaces;
using Makaretu.Dns;
using System;

namespace ChromeCast.Device.Application
{
    public class MdnsAdvertise
    {
        private readonly ILogger logger;
        private readonly string deviceName;
        private readonly string serviceType = "_googlecast._tcp";
        private readonly ushort port = 8009;

        public MdnsAdvertise(ILogger loggerIn, string deviceNameIn)
        {
            logger = loggerIn;
            deviceName = deviceNameIn;
        }

        public void Advertise()
        {
            var mdns = new MulticastService();
            var sd = new ServiceDiscovery(mdns);
            var instanceName = Guid.NewGuid().ToString();
            var serviceProfile = new ServiceProfile(instanceName, serviceType, port)
            {
                InstanceName = $"{deviceName}-{instanceName.Replace("-", "")}"
            };
            serviceProfile.AddProperty("id", instanceName.Replace("-", ""));
            serviceProfile.AddProperty("cd", Guid.NewGuid().ToString().Replace("-", "").ToUpper());
            serviceProfile.AddProperty("rm", string.Empty);
            serviceProfile.AddProperty("ve", "00");
            serviceProfile.AddProperty("md", "SamDel");
            serviceProfile.AddProperty("ic", "");
            serviceProfile.AddProperty("fn", deviceName);
            serviceProfile.AddProperty("ca", "0000");
            serviceProfile.AddProperty("st", "0");
            serviceProfile.AddProperty("bs", "000000000000");
            serviceProfile.AddProperty("nf", "0");
            serviceProfile.AddProperty("rs", "");
            sd.Advertise(serviceProfile);

            mdns.Start();
            logger.Log($"MdnsAdvertise: Advertising {instanceName}-{serviceType}-{port}...");
        }
    }
}
