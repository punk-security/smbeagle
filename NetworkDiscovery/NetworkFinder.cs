using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using SMBeagle.Enums;

namespace SMBeagle.NetworkDiscovery
{
    class NetworkFinder
    {
        public List<Network> PrivateNetworks { 
            get 
            {
                return _Networks.Where(item => item.IsPrivate == true).ToList();
            }
}
        List<Network> _Networks = new List<Network>();
        List<string> _Addresses = new List<string>();
        List<string> _LocalAddresses = new List<string>();
        public List<Network> Networks { get { return _Networks; } }
        public List<string> PrivateAddresses
        {
            get
            {
                return _Addresses.Where(item => Network.IsPrivateAddress(item) == true).ToList();
            }
        }
        public List<string> Addresses { get { return _Addresses; } }
        public List<string> LocalAddresses { get { return _LocalAddresses; } }
        public NetworkFinder()
        {
        }

        public void AddAddress(string address)
        {
            // Remove ScopeID on IPv6
            if (address.Contains("%"))
                address = address.Substring(0, address.IndexOf("%"));
            // Dont store loopback
            if (address.Length > 3 && address.Substring(0, 4) == "127.")
                return;
            if (address == "::1")
                return;
            // Dont store if it already exists
            if (_Addresses.Contains(address))
                return;
            // Store
            _Addresses.Add(address);
        }
        public void AddNetwork(string network, NetworkDiscoverySourceEnum source)
        {
            Network net = new Network(network, source);
            AddNetwork(net);
        }
        public void AddNetwork(Network network)
        {
            if (network.Value == "::/64" || network.Address == "127.0.0.0")
                return;
            // return without storing if this network already exists
            if (_Networks.Any(item => item.Value == network.Value))
                return;
            // return without storing if network is child of another already tracked
            if (_Networks.Any(item => item.ContainsNetwork(network)))
                return;
            // remove childnet if this network fully contains it
            // todo: are there edge cases where we want to keep child nets?
            List<Network> iter = new List<Network>(_Networks);
            foreach ( Network net in iter)
            {
                if (network.ContainsNetwork(net))
                    _Networks.Remove(net);
            }
            _Networks.Add(network);
        }
        public void DiscoverNetworks()
        {
            DiscoverNetworksViaSockets();
            DiscoverNetworksViaClientConfiguration();
        }

        public void DiscoverNetworksViaSockets()
        {
            List<string> addresses = DiscoverAddressesViaSockets();
            List<Network> networks = new List<Network>();
            foreach (string address in addresses)
            {
                // Expect a IPv4 net to be /24 and a IPv6 to be /64
                Network net = new Network(ConvertAddressToNetwork(address), NetworkDiscoverySourceEnum.NETSTAT);
                AddNetwork(net);
            }
        }

        private string ConvertAddressToNetwork(string address, string subnetmask = null, int cidr = 0)
        {
            // Remove ScopeID on IPv6
            if (address.Contains("%"))
                address = address.Substring(0,address.IndexOf("%"));
            if (subnetmask != null && subnetmask != "0.0.0.0")
            {
                cidr = IPNetwork.ToCidr(IPAddress.Parse(subnetmask));
            }
            // Set cidr
            if (cidr == 0)
                cidr = address.Contains(":") ? 64 : 24;
            return string.Format("{0}/{1}", address, cidr);
        }

        public List<string> DiscoverAddressesViaSockets()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            var addresses = new List<string>();
            foreach (var connection in connections)
            {
                AddAddress(connection.RemoteEndPoint.Address.ToString());
                addresses.Add(connection.RemoteEndPoint.Address.ToString());
                addresses.Add(connection.LocalEndPoint.Address.ToString());
            }
            return new HashSet<string>(addresses).ToList();
        }

        public List<string> DiscoverNetworksViaClientConfiguration()
        {
            foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                List<UnicastIPAddressInformation> addresses = iface.GetIPProperties().UnicastAddresses.ToList();
                foreach (UnicastIPAddressInformation address in addresses)
                {
                    AddLocalAddress(address.Address.ToString());
                    // Convert to network and attempt to store
                    string net = ConvertAddressToNetwork(address.Address.ToString(), address.IPv4Mask.ToString());
                    AddNetwork(net, NetworkDiscoverySourceEnum.LOCAL);
                }
            }
            return new List<string>();
        }

        public void AddLocalAddress(string address)
        {
            if (address.Contains("%"))
                address = address.Substring(0, address.IndexOf("%"));
            if (_LocalAddresses.Contains(address))
                return;
            _LocalAddresses.Add(address);
        }

    }
}
