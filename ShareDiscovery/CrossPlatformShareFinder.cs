using SMBeagle.HostDiscovery;
using SMBeagle.Output;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Net;

namespace SMBeagle.ShareDiscovery
{
    class CrossPlatformShareFinder
    {
        private static bool AttemptClientLogin(ISMBClient client, Host host, string domain, string username, string password)
        {
            if (client.Connect(IPAddress.Parse(host.Address), SMBTransportType.DirectTCPTransport) &&
            client.Login(domain, username, password) == NTStatus.STATUS_SUCCESS)
            {
                return true;
            }
            if (client.Connect(IPAddress.Parse(host.Address), SMBTransportType.NetBiosOverTCP) &&
                client.Login(domain, username, password) == NTStatus.STATUS_SUCCESS)
            {
                return true;
            }
            return false;
        }
        public static bool GetClient(Host host, string domain, string username, string password)
        {
            ISMBClient client;
            client = new SMB2Client();
            if (AttemptClientLogin(client, host, domain, username, password))
            {
                //SMB2
                host.Client = client;
                return true;
            }
            client = new SMB1Client();
            if (AttemptClientLogin(client, host, domain, username, password))
            {
                //SMB1
                host.Client = client;
                return true;
            }
            return false;
        }

        private static List<String> GetDeviceShares(Host host)
        {
            NTStatus returnCode;
            List<string> shares = host.Client.ListShares(out returnCode);
            if (returnCode == NTStatus.STATUS_SUCCESS)
                return shares;
            else
            {
                OutputHelper.WriteLine($"Could not list shares from device - ERROR CODE: '{ returnCode }' for host '{host}'");
                return new List<String>();
            }
        }

        public static void DiscoverDeviceShares(Host host)
        {
            if (host.Client == null)
            {
                OutputHelper.WriteLine($"Error: No SMBClient connection established for this host '{host}'");
                return;
            }
            List<string> shares = GetDeviceShares(host);
            foreach (String s in shares)
            {
                Share share = new Share(host, s, Enums.ShareTypeEnum.DISK);
                host.Shares.Add(share);
            }
        }
    }
}
