using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBeagle.Output
{
    public interface IOutputPayload
    {
        public string Username { get; set; }
        public string Hostname { get; set; }
    }
}
