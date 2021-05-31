using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBeagle.Enums
{
    public enum DirectoryTypeEnum
    {
        UNKNOWN = 0,
        SMB = 1,
        LOCAL_REMOVEABLE = 2,
        LOCAL_FIXED = 4,
        LOCAL_NETWORK = 8,
        LOCAL_CDROM = 16,
    }
}
