using SMBeagle.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBeagle.ShareDiscovery
{
    class Share
    {
        public string Name { get; set; }
        public ShareTypeEnum Type { get; set; }
        public Share(string name, ShareTypeEnum type)
        {
            Name = name;
            Type = type;
        }
    }
}
