using SMBeagle.Enums;
using SMBeagle.HostDiscovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBeagle.ShareDiscovery
{
    class Share
    {
        public Host Host { get; set; }
        public string Name { get; set; }
        public ShareTypeEnum Type { get; set; }
        public Share(Host host, string name, ShareTypeEnum type)
        {
            Host = host;
            Name = name;
            Type = type;
        }
        public string uncPath
        {
            get
            {
                return $@"\\{Host.Address}\{Name}\".ToLower();
            }
        }
    }
}
