using ChromeCast.Classes;
using ChromeCast.Device.Classes;
using ChromeCast.Device.Log.Interfaces;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Tmds.MDns;

namespace ChromeCast.Device.Application
{
    public class MdnsAdvertise
    {
        private readonly ILogger logger;
        private readonly string serviceType = "_googlecast._tcp";
        private const string serviceTypeEmbedded = "_googlezone._tcp";
        private readonly ushort port = 8009;
        private MulticastService mdns;
        private ServiceBrowser serviceBrowser;
        private ServiceBrowser serviceBrowserEmbedded;
        private readonly Device device;

        public MdnsAdvertise(ILogger loggerIn, Device deviceIn)
        {
            logger = loggerIn;
            device = deviceIn;
        }

        public void Advertise()
        {
            mdns = new MulticastService();
            mdns.QueryReceived += Mdns_QueryReceived;
            mdns.AnswerReceived += Mdns_AnswerReceived;
            mdns.MalformedMessage += Mdns_MalformedMessage;
            mdns.Start();

            // For future use:
            serviceBrowser = new ServiceBrowser();
            serviceBrowser.ServiceAdded += OnServiceAdded;
            serviceBrowser.ServiceRemoved += OnServiceRemoved;
            serviceBrowser.ServiceChanged += OnServiceChanged;
            serviceBrowser.StartBrowse(serviceType);

            serviceBrowserEmbedded = new ServiceBrowser();
            serviceBrowserEmbedded.ServiceAdded += OnServiceAdded;
            serviceBrowserEmbedded.ServiceRemoved += OnServiceRemoved;
            serviceBrowserEmbedded.ServiceChanged += OnServiceChanged;
            serviceBrowserEmbedded.StartBrowse(serviceTypeEmbedded);
        }

        private void Mdns_QueryReceived(object sender, MessageEventArgs e)
        {
            var addresses = MulticastService.GetIPAddresses()
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (!addresses.Contains(e.RemoteEndPoint.Address)) // Not a query from a local IP address
            {
                var msg = e.Message;
                if (msg.Questions.Any(q => q.Name.ToString().Contains(serviceType)))
                {
                    SendAnswer(addresses, msg, serviceType);
                    SendGroupAnswer(addresses, msg, serviceType);
                }
                else if (msg.Questions.Any(q => q.Name.ToString().Contains(serviceTypeEmbedded)))
                {
                    SendAnswer(addresses, msg, serviceTypeEmbedded);
                }
            }
        }

        private void SendAnswer(IEnumerable<IPAddress> addresses, Message msg, string service)
        {
            var instanceName = SystemCalls.SystemGuid();
            var res = msg.CreateResponse();
            foreach (var address in addresses)
            {
                res.Answers.Add(new ARecord
                {
                    Name = $"{service}.local",
                    Address = address
                });
            }
            res.Answers.Add(new TXTRecord
            {
                Name = $"SamDel-{instanceName.Replace("-", "")}.{service}.local",
                Strings = new List<string>()
                    {
                        $"id={instanceName.Replace("-", "")}",
                        $"cd={Guid.Empty.ToString().Replace("-", "").ToUpper()}",
                        $"rm=",
                        $"ve=05",
                        $"md=SamDel",
                        $"ic=/setup/icon.png",
                        $"fn={device.DeviceName}",
                        $"ca=2052",
                        $"st=0",
                        $"bs=0009B0700387",
                        $"nf=2",
                        $"rs="
                    }
            });
            res.Answers.Add(new SRVRecord
            {
                Name = $"SamDel-{instanceName.Replace("-", "")}.{service}.local",
                Port = port,
                Target = $"{serviceType}.local"
            });
            mdns.SendAnswer(res);
            //logger.Log($"SendAnswer: {res?.Answers?.FirstOrDefault()?.Name} {string.Join(";", addresses.ToList())} {device.DeviceName} {res?.Answers?.LastOrDefault()?.Name}");
        }

        private void SendGroupAnswer(IEnumerable<IPAddress> addresses, Message msg, string service)
        {
            foreach (var groupName in device.GroupNames)
            {
                var instanceName = Guid.NewGuid().ToString(); // TODO: persist group guid
                var res = msg.CreateResponse();
                foreach (var address in addresses)
                {
                    res.Answers.Add(new ARecord
                    {
                        Name = $"{service}.local",
                        Address = address
                    });
                }
                res.Answers.Add(new TXTRecord
                {
                    Name = $"SamDel-{instanceName.Replace("-", "")}.{service}.local",
                    Strings = new List<string>()
                    {
                        $"id={instanceName}",
                        $"cd={instanceName}",
                        $"rm=",
                        $"ve=05",
                        $"md=Google Cast Group",
                        $"ic=/setup/icon.png",
                        $"fn={groupName}",
                        $"ca=2052",
                        $"st=0",
                        $"bs=0009B0700387",
                        $"nf=2",
                        $"rs="
                    }
                });
                res.Answers.Add(new SRVRecord
                {
                    Name = $"SamDel-{instanceName.Replace("-", "")}.{service}.local",
                    Port = port,
                    Target = $"{serviceType}.local"
                });
                mdns.SendAnswer(res);
                //logger.Log($"SendGroupAnswer: {res?.Answers?.FirstOrDefault()?.Name} {string.Join(";", addresses.ToList())} {groupName} {res?.Answers?.LastOrDefault()?.Name}");
            }
        }

        private void Mdns_AnswerReceived(object sender, MessageEventArgs e)
        {
        }

        private void Mdns_MalformedMessage(object sender, byte[] e)
        {
        }

        private void OnServiceAdded(object sender, ServiceAnnouncementEventArgs e)
        {
            var addresses = MulticastService.GetIPAddresses()
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (!addresses.Contains(e.Announcement.Addresses?[0])) // Not a query from a local IP address
            {
                logger.Log($"Service added: {e.Announcement.Addresses?[0]} {e.Announcement.Txt.Where(x => x.StartsWith("fn=")).FirstOrDefault()?.ToString().Replace("fn=", "")} - {e.Announcement.Instance} {e.Announcement.Type}");
            }
        }

        private void OnServiceChanged(object sender, ServiceAnnouncementEventArgs e)
        {
        }

        private void OnServiceRemoved(object sender, ServiceAnnouncementEventArgs e)
        {
        }
    }
}
