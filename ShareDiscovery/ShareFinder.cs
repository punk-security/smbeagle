using SMBeagle.HostDiscovery;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SMBeagle.ShareDiscovery
{
    class ShareFinder
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

        public static List<SHARE_INFO_1> EnumNetShares(string address)
        {
            List<SHARE_INFO_1> ShareInfos = new List<SHARE_INFO_1>();
            int entriesread = 0, totalentries = 0, resume_handle = 0;
            int nStructSize = Marshal.SizeOf(typeof(SHARE_INFO_1));
            IntPtr bufPtr = IntPtr.Zero;
            StringBuilder server = new StringBuilder(address);
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

        public static List<Share> EnumerateSharesViaDirectSmb(string address, string domain = "",string username = "", string password = "")
        {
            bool isConnected;
            ISMBClient client;
            try
            {
                client = new SMB2Client();
                isConnected = client.Connect(address, SMBTransportType.DirectTCPTransport);
            }
            catch
            {
                try
                {
                    client = new SMB1Client();
                    isConnected = client.Connect(address, SMBTransportType.DirectTCPTransport);
                }
                catch
                {
                    return new();
                }
            }
            if (isConnected)
            {
                NTStatus status = client.Login(domain, username, password);
                List<string> shares = new();
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    shares = client.ListShares(out status);
                }
                client.Disconnect();
                return shares.Select(share => new Share(share, Enums.ShareTypeEnum.DISK)).ToList();
            }
            return new();
        }

        public static void DiscoverDeviceSharesWindows(Host host)
        {
            EnumNetShares(host.Address)
                    .ForEach(si => host.Shares.Add(ConvertShareInfoToShare(si)));
        }

        public static void DiscoverDeviceSharesNative(Host host)
        {
            EnumerateSharesViaDirectSmb(host.Address).ForEach(share => host.Shares.Add(share));
        }

        private static Share ConvertShareInfoToShare(SHARE_INFO_1 shareInfo)
        {
            switch(shareInfo.shi1_type)
            {
                case (uint)SHARE_TYPE.STYPE_CLUSTER_DFS:
                    return new Share(shareInfo.shi1_netname, Enums.ShareTypeEnum.DFS_SHARE);
                case (uint)SHARE_TYPE.STYPE_CLUSTER_FS:
                    return new Share(shareInfo.shi1_netname, Enums.ShareTypeEnum.CLUSTER_SHARE);
                case (uint)SHARE_TYPE.STYPE_CLUSTER_SOFS:
                    return new Share(shareInfo.shi1_netname, Enums.ShareTypeEnum.SCALE_OUT_CLUSTER_SHARE);
                default:
                    return new Share(shareInfo.shi1_netname, Enums.ShareTypeEnum.DISK);
            }


        }
    }
}
