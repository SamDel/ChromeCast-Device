using ChromeCast.Device.Log.Interfaces;
using Makaretu.Dns;
using System;
using System.Linq;
using System.Net.Sockets;
using Tmds.MDns;

namespace ChromeCast.Device.Application
{
    public class MdnsAdvertise
    {
        private readonly ILogger logger;
        private readonly string deviceName;
        private readonly string serviceType = "_googlecast._tcp";
        private const string serviceTypeEmbedded = "_googlezone._tcp";
        private readonly ushort port = 8009;
        private MulticastService mdns;

        public MdnsAdvertise(ILogger loggerIn, string deviceNameIn)
        {
            logger = loggerIn;
            deviceName = deviceNameIn;
        }

        public void Advertise()
        {
            mdns = new MulticastService();
            mdns.QueryReceived += Mdns_QueryReceived;
            mdns.AnswerReceived += Mdns_AnswerReceived;

            var serviceDiscovery = new ServiceDiscovery(mdns);
            serviceDiscovery.ServiceDiscovered += ServiceDiscovery_ServiceDiscovered;
            serviceDiscovery.ServiceInstanceDiscovered += ServiceDiscovery_ServiceInstanceDiscovered;

            var instanceName = string.Empty;// SystemCalls.SystemGuid();
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
            serviceProfile.AddProperty("fn", string.Empty); // deviceName);
            serviceProfile.AddProperty("ca", "0000");
            serviceProfile.AddProperty("st", "0");
            serviceProfile.AddProperty("bs", "000000000000");
            serviceProfile.AddProperty("nf", "0");
            serviceProfile.AddProperty("rs", "");
            serviceDiscovery.Advertise(serviceProfile);

            mdns.Start();
            logger.Log($"MdnsAdvertise: Advertising {instanceName}-{serviceType}-{port}...");

            ServiceBrowser serviceBrowser = new ServiceBrowser();
            serviceBrowser.ServiceAdded += OnServiceAdded;
            serviceBrowser.ServiceRemoved += OnServiceRemoved;
            serviceBrowser.ServiceChanged += OnServiceChanged;
            serviceBrowser.StartBrowse(serviceType);

            ServiceBrowser serviceBrowserEmbedded = new ServiceBrowser();
            serviceBrowserEmbedded.ServiceAdded += OnServiceAdded;
            serviceBrowserEmbedded.ServiceRemoved += OnServiceRemoved;
            serviceBrowserEmbedded.ServiceChanged += OnServiceChanged;
            serviceBrowserEmbedded.StartBrowse(serviceTypeEmbedded);
        }

        private void Mdns_QueryReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (msg.Questions.Any(q => q.Name.ToString().Contains(serviceType)))
            {
                var res = msg.CreateResponse();
                var addresses = MulticastService.GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                foreach (var address in addresses)
                {
                    res.Answers.Add(new ARecord
                    {
                        Name = serviceType,
                        Address = address
                    });
                }
                mdns.SendAnswer(res);
            }
        }

        private void OnServiceChanged(object sender, ServiceAnnouncementEventArgs e)
        {
        }

        private void OnServiceRemoved(object sender, ServiceAnnouncementEventArgs e)
        {
        }

        private void OnServiceAdded(object sender, ServiceAnnouncementEventArgs e)
        {
        }

        private void ServiceDiscovery_ServiceInstanceDiscovered(object sender, ServiceInstanceDiscoveryEventArgs e)
        {
        }

        private void ServiceDiscovery_ServiceDiscovered(object sender, DomainName e)
        {
        }

        private void Mdns_AnswerReceived(object sender, MessageEventArgs e)
        {
        }
    }
}
