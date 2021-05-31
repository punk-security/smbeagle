using SMBeagle.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace SMBeagle.NetworkDiscovery
{
    class Network
    {
        static Dictionary<string, IPNetwork> _PrivateNetworks = new Dictionary<string, IPNetwork>
            {
                { "IPV4_RFC1918_1", IPNetwork.IANA_ABLK_RESERVED1 },
                { "IPV4_RFC1918_2", IPNetwork.IANA_BBLK_RESERVED1 },
                { "IPV4_RFC1918_3", IPNetwork.IANA_CBLK_RESERVED1 },
                { "IPV4_CGRADE_NAT", IPNetwork.Parse("100.64.0.0/10") }, //Might get rid of this one as not really used?
                { "IPV4_LINK-LOCAL", IPNetwork.Parse("169.254.0.0/16") },
                { "IPV6_RFC4193", IPNetwork.Parse("fd00::/8") },
                { "IPV6_LINK-LOCAL", IPNetwork.Parse("fe80::/10") },
            };
        private IPNetwork _Net { get; set; }
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
            _Net = IPNetwork.Parse(cidr);
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
            IPNetwork childNet = IPNetwork.Parse(network.Value);
            return _Net.Contains(childNet);
        }
    }
}
