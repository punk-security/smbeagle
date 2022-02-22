using SMBeagle.HostDiscovery;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SMBeagle.ShareDiscovery
{
    class WindowsShareFinder
    {
        // https://github.com/mitchmoser/SharpShares/blob/master/SharpShares/Enums/Shares.cs
        [DllImport("Netapi32.dll", SetLastError = true)]
        public static extern int NetWkstaGetInfo(string servername, int level, out IntPtr bufptr);

        [DllImport("Netapi32.dll", SetLastError = true)]
        static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetShareEnum(
            StringBuilder ServerName,
            int level,
            ref IntPtr bufPtr,
            uint prefmaxlen,
            ref int entriesread,
            ref int totalentries,
            ref int resume_handle
        );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHARE_INFO_1
        {
            public string shi1_netname;
            public uint shi1_type;
            public string shi1_remark;
            public SHARE_INFO_1(string sharename, uint sharetype, string remark)
            {
                this.shi1_netname = sharename;
                this.shi1_type = sharetype;
                this.shi1_remark = remark;
            }
            public override string ToString()
            {
                return shi1_netname;
            }
        }

        private enum NetError : uint
        {
            NERR_Success = 0,
            NERR_BASE = 2100,
            NERR_UnknownDevDir = (NERR_BASE + 16),
            NERR_DuplicateShare = (NERR_BASE + 18),
            NERR_BufTooSmall = (NERR_BASE + 23),
        }

        private enum SHARE_TYPE : uint
        {
            STYPE_DISKTREE = 0x00000000,
            STYPE_PRINTQ = 0x00000001,
            STYPE_DEVICE = 0x00000002,
            STYPE_IPC = 0x00000003,
            STYPE_CLUSTER_FS = 0x02000000,
            STYPE_CLUSTER_SOFS = 0x04000000,
            STYPE_CLUSTER_DFS = 0x08000000,
            STYPE_SPECIAL = 0x80000000,
        }

        const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;

        public static List<SHARE_INFO_1> EnumNetShares(Host host)
        {
            List<SHARE_INFO_1> ShareInfos = new List<SHARE_INFO_1>();
            int entriesread = 0, totalentries = 0, resume_handle = 0;
            int nStructSize = Marshal.SizeOf(typeof(SHARE_INFO_1));
            IntPtr bufPtr = IntPtr.Zero;
            StringBuilder server = new StringBuilder(host.Address);
            int ret = NetShareEnum(server, 1, ref bufPtr, MAX_PREFERRED_LENGTH, ref entriesread, ref totalentries, ref resume_handle);
            if (ret == (int)NetError.NERR_Success)
            {
                IntPtr currentPtr = bufPtr;
                for (int i = 0; i < entriesread; i++)
                {
                    SHARE_INFO_1 shi1 = (SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(SHARE_INFO_1));
                    ShareInfos.Add(shi1);
                    currentPtr += nStructSize;
                }
                NetApiBufferFree(bufPtr);
            }
            return ShareInfos;
        }

        public static void DiscoverDeviceShares(Host host)
        {
            List<SHARE_INFO_1> shareInfos = EnumNetShares(host);
            foreach (SHARE_INFO_1 si in shareInfos)
            {
                Share share = ConvertShareInfoToShare(host, si);
                if (share != null)
                    host.Shares.Add(share);
            }
        }

        private static Share ConvertShareInfoToShare(Host host, SHARE_INFO_1 shareInfo)
        {
            switch(shareInfo.shi1_type)
            {
                case (uint)SHARE_TYPE.STYPE_CLUSTER_DFS:
                    return new Share(host, shareInfo.shi1_netname, Enums.ShareTypeEnum.DFS_SHARE);
                case (uint)SHARE_TYPE.STYPE_CLUSTER_FS:
                    return new Share(host, shareInfo.shi1_netname, Enums.ShareTypeEnum.CLUSTER_SHARE);
                case (uint)SHARE_TYPE.STYPE_CLUSTER_SOFS:
                    return new Share(host, shareInfo.shi1_netname, Enums.ShareTypeEnum.SCALE_OUT_CLUSTER_SHARE);
                default: 
                    return new Share(host, shareInfo.shi1_netname, Enums.ShareTypeEnum.DISK);
            }
        }
    }
}
