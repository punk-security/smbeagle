using SMBeagle.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace SMBeagle.NetworkDiscovery
{
    class Network
    {
        static Dictionary<string, IPNetwork2> _PrivateNetworks = new Dictionary<string, IPNetwork2>
            {
                { "IPV4_RFC1918_1", IPNetwork2.Parse("10.0.0.0/8") },
                { "IPV4_RFC1918_2", IPNetwork2.Parse("172.16.0.0/12") },
                { "IPV4_RFC1918_3", IPNetwork2.Parse("192.168.0.0/16") },
                { "IPV4_CGRADE_NAT", IPNetwork2.Parse("100.64.0.0/10") }, //Might get rid of this one as not really used?
                { "IPV4_LINK-LOCAL", IPNetwork2.Parse("169.254.0.0/16") },
                { "IPV6_RFC4193", IPNetwork2.Parse("fd00::/8") },
                { "IPV6_LINK-LOCAL", IPNetwork2.Parse("fe80::/10") },
            };
        private IPNetwork2 _Net { get; set; }
        public bool IsPrivate { get { return _IsPrivate; } }
        private bool _IsPrivate { get; set; } = false;
        public int IPVersion { get
            {
                if (Address.Contains(":"))
                    return 6;
                else
                    return 4;
            }
        }
        public string Address
        {
            get
            {
                return _Net.Network.ToString();
            }
        }
        public string Mask
        {
            get
            {
                return _Net.Netmask.ToString();
            }
        }
        public string Cidr
        {
            get
            {
                return _Net.Cidr.ToString();
            }
        }

        public string Value
        {
            get
            {
                return _Net.Value;
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public List<string> AddressList {
            get
            {
                List<string> addresses = new List<string>();
                foreach (IPAddress ip in _Net.ListIPAddress())
                {
                    addresses.Add(ip.ToString());
                }
                return addresses;

            }
            }

        public NetworkDiscoverySourceEnum Source { get; }
        public Network(string cidr, NetworkDiscoverySourceEnum source)
        {
            //TODO: Validate its a cidr?
            Source = source;
            _Net = IPNetwork2.Parse(cidr);
            foreach (string name in _PrivateNetworks.Keys)
            {
                if (_PrivateNetworks[name].Contains(_Net))
                    {
                    _IsPrivate = true;
                }
            }
        }
        public static bool IsPrivateAddress(string address)
        {
            foreach (string name in _PrivateNetworks.Keys)
            {
                IPAddress ip = IPAddress.Parse(address);
                if (_PrivateNetworks[name].Contains(ip))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsNetwork(Network network)
        {
            if (IPVersion != network.IPVersion)
                return false;
            IPNetwork2 childNet = IPNetwork2.Parse(network.Value);
            return _Net.Contains(childNet);
        }
    }
}
