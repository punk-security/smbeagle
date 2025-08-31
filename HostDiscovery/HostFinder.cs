using SMBeagle.NetworkDiscovery;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SMBeagle.HostDiscovery
{
    class HostFinder
    {
        private List<Host> _Hosts = new List<Host>();
        public List<Host> ReachableHosts { get { return _Hosts.Where(item => item.SMBAvailable).ToList(); } }
        public List<Host> HostsWithShares { get { return ReachableHosts.Where(item => item.ShareCount > 0).ToList(); } }
        private List<string> _Candidates = new List<string>();
        public HostFinder(List<string> knownHostAddresses, List<Network> knownNetworks, List<string> blacklistedAddresses )
        {
            BuildCandidateList(knownHostAddresses, knownNetworks);
            foreach (string candidate in _Candidates)
            {
                if (blacklistedAddresses.Contains(candidate))
                    continue; // Dont store blacklisted addresses
                AddHost(candidate);
            }
            InititiateSMBTestsForHosts();
        }

        private void InititiateSMBTestsForHosts(int chunkSize = 500)
        {
            for (int x = 0; x <= _Hosts.Count / chunkSize; x++)
            {
                foreach (Host host in _Hosts.Skip(x * chunkSize).Take(chunkSize))
                {
                    Thread t = new(() => host.TestSMB());
                    t.Start();
                }
                // Wait for max scan time
                Thread.Sleep(Host.PORT_MAX_WAIT_MS);
            }
        }

        public void AddHost(Host host)
        {
            if (_Hosts.Any(item => item.Address == host.Address))
                return;
            _Hosts.Add(host);
        }

        public void AddHost(string address)
        {
            AddHost(new Host(address));
        }

        public void BuildCandidateList(List<string> knownHostAddresses, List<Network> knownNetworks)
        {
            List<string> candidates = new List<string>();
            foreach (Network network in knownNetworks.Where(item => item.IPVersion == 4))
            {
                foreach (string address in network.AddressList)
                    candidates.Add(address);
            }
            foreach (string address in knownHostAddresses)
            {
                candidates.Add(address);
            }
            _Candidates = new HashSet<string>(candidates).ToList();
        }


    }
}
