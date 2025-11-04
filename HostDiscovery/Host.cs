using SMBeagle.ShareDiscovery;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SMBeagle.HostDiscovery
{
    class Host
    {
        public static int PORT_MAX_WAIT_MS = 1000;
        public string Address { get; set; }
        public bool SMBAvailable { get { return _SMBAvailable; } }
        private bool _SMBAvailable { get; set; }
        public List<Share> Shares { get; set; } = new();
        #nullable enable
        public ISMBClient? Client { get; set; } = null;
        #nullable disable
        public int ShareCount { get { return Shares.Count; } }
        public Host(string address)
        {
            Address = address;
        }

        public void TestSMB()
        {
            _SMBAvailable = HostRespondsToTCP445();
        }

        bool HostRespondsToTCP445()
        {
            using TcpClient t = new();

            try
            {
                if (t.ConnectAsync(Address, 445).Wait(PORT_MAX_WAIT_MS))
                    return true; // We connected

                return false; // We timedout
            }
            catch
            {
                return false; // We hit an error
            }
        }

        public override string ToString()
        {
            return Address;
        }


    }
}
