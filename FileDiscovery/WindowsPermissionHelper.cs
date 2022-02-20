using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace SMBeagle.FileDiscovery
{
    [SupportedOSPlatform("windows")]
    class WindowsPermissionHelper
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern uint GetEffectiveRightsFromAcl(IntPtr pDacl, ref TRUSTEE pTrustee, ref ACCESS_MASK pAccessRights);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        struct TRUSTEE
        {
            IntPtr pMultipleTrustee; // must be null
            public int MultipleTrusteeOperation;
            public TRUSTEE_FORM TrusteeForm;
            public TRUSTEE_TYPE TrusteeType;
            [MarshalAs(UnmanagedType.LPStr)]
            public string ptstrName;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AUTHZ_ACCESS_REQUEST
        {
            public int DesiredAccess;
            public byte[] PrincipalSelfSid;
            public OBJECT_TYPE_LIST[] ObjectTypeList;
            public int ObjectTypeListLength;
            public IntPtr OptionalArguments;
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_TYPE_LIST
        {
            OBJECT_TYPE_LEVEL Level;
            int Sbz;
            IntPtr ObjectType;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct AUTHZ_ACCESS_REPLY
        {
            public int ResultListLength;
            public IntPtr GrantedAccessMask;
            public IntPtr SaclEvaluationResults;
            public IntPtr Error;
        };

        public enum OBJECT_TYPE_LEVEL : int
        {
            ACCESS_OBJECT_GUID = 0,
            ACCESS_PROPERTY_SET_GUID = 1,
            ACCESS_PROPERTY_GUID = 2,
            ACCESS_MAX_LEVEL = 4
        };
        enum TRUSTEE_FORM
        {
            TRUSTEE_IS_SID,
            TRUSTEE_IS_NAME,
            TRUSTEE_BAD_FORM,
            TRUSTEE_IS_OBJECTS_AND_SID,
            TRUSTEE_IS_OBJECTS_AND_NAME
        }

        enum AUTHZ_RM_FLAG : uint
        {
            AUTHZ_RM_FLAG_NO_AUDIT = 1,
            AUTHZ_RM_FLAG_INITIALIZE_UNDER_IMPERSONATION = 2,
            AUTHZ_RM_FLAG_NO_CENTRAL_ACCESS_POLICIES = 4,
        }

        enum TRUSTEE_TYPE
        {
            TRUSTEE_IS_UNKNOWN,
            TRUSTEE_IS_USER,
            TRUSTEE_IS_GROUP,
            TRUSTEE_IS_DOMAIN,
            TRUSTEE_IS_ALIAS,
            TRUSTEE_IS_WELL_KNOWN_GROUP,
            TRUSTEE_IS_DELETED,
            TRUSTEE_IS_INVALID,
            TRUSTEE_IS_COMPUTER
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        static extern uint GetNamedSecurityInfo(
            string pObjectName,
            SE_OBJECT_TYPE ObjectType,
            SECURITY_INFORMATION SecurityInfo,
            out IntPtr pSidOwner,
            out IntPtr pSidGroup,
            out IntPtr pDacl,
            out IntPtr pSacl,
            out IntPtr pSecurityDescriptor);
        [DllImport("authz.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzInitializeContextFromSid", CharSet = CharSet.Unicode)]
        static extern public bool AuthzInitializeContextFromSid(
                                               int Flags,
                                               IntPtr UserSid,
                                               IntPtr AuthzResourceManager,
                                               IntPtr pExpirationTime,
                                               LUID Identitifier,
                                               IntPtr DynamicGroupArgs,
                                               out IntPtr pAuthzClientContext
                                               );



        [DllImport("authz.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzInitializeResourceManager", CharSet = CharSet.Unicode)]
        static extern public bool AuthzInitializeResourceManager(
                                        int flags,
                                        IntPtr pfnAccessCheck,
                                        IntPtr pfnComputeDynamicGroups,
                                        IntPtr pfnFreeDynamicGroups,
                                        string name,
                                        out IntPtr rm
                                        );
        [DllImport("authz.dll", EntryPoint = "AuthzAccessCheck", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool AuthzAccessCheck(int flags,
                                                    IntPtr hAuthzClientContext,
                                                     ref AUTHZ_ACCESS_REQUEST pRequest,
                                                     IntPtr AuditEvent,
                                                     IntPtr pSecurityDescriptor,
                                                    byte[] OptionalSecurityDescriptorArray,
                                                    int OptionalSecurityDescriptorCount,
                                                    ref AUTHZ_ACCESS_REPLY pReply,
                                                    out IntPtr phAccessCheckResults);


        [DllImport("authz.dll", EntryPoint = "AuthzFreeResourceManager", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool AuthzFreeResourceManager( IntPtr hAuthzResourceManager );


        enum ACCESS_MASK : uint
        {
            FILE_TRAVERSE = 0x20,
            FILE_LIST_DIRECTORY = 0x1,
            FILE_READ_DATA = 0x1,
            FILE_READ_ATTRIBUTES = 0x80,
            FILE_READ_EA = 0x8,
            FILE_ADD_FILE = 0x2,
            FILE_WRITE_DATA = 0x2,
            FILE_ADD_SUBDIRECTORY = 0x4,
            FILE_APPEND_DATA = 0x4,
            FILE_WRITE_ATTRIBUTES = 0x100,
            FILE_WRITE_EA = 0x10,
            FILE_DELETE_CHILD = 0x40,
            DELETE = 0x10000,
            READ_CONTROL = 0x20000,
            WRITE_DAC = 0x40000,
            WRITE_OWNER = 0x80000,


            ////////FILE_EXECUTE =0x20,   
        }

        [Flags]
        enum SECURITY_INFORMATION : uint
        {
            OWNER_SECURITY_INFORMATION = 0x00000001,
            GROUP_SECURITY_INFORMATION = 0x00000002,
            DACL_SECURITY_INFORMATION = 0x00000004,
            SACL_SECURITY_INFORMATION = 0x00000008,
            UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
            UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
            PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
            PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000
        }

        enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE = 0,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }

        public static IntPtr GetUserSid()
        {
            string username = WindowsIdentity.GetCurrent().Name;
            NTAccount ac = new NTAccount(username);
            SecurityIdentifier sid = (SecurityIdentifier)ac.Translate(typeof(SecurityIdentifier));
            byte[] bytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(bytes, 0);
            String _psUserSid = "";
            foreach (byte si in bytes)
            {
                _psUserSid += si;
            }
            IntPtr UserSid = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, UserSid, bytes.Length);
            return UserSid;
        }

        public static IntPtr GetpClientContext()
        {
            IntPtr sid = WindowsPermissionHelper.GetUserSid();
            IntPtr hManager = IntPtr.Zero;
            AuthzInitializeResourceManager(1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, null, out hManager);
            IntPtr pClientContext = IntPtr.Zero;
            LUID unusedSid = new LUID();
            AuthzInitializeContextFromSid(0, sid, hManager, IntPtr.Zero, unusedSid, IntPtr.Zero, out pClientContext);
            return pClientContext;
        }

    public static ACL ResolvePermissions(string path, IntPtr pClientContext)
    {

        ACL acl = new ACL() { Readable = false, Writeable = false, Deletable = false };
        IntPtr pSidOwner, pSidGroup, pDacl, pSacl, pSecurityDescriptor;

        uint ret = GetNamedSecurityInfo(path,
            SE_OBJECT_TYPE.SE_FILE_OBJECT,
            SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION,
            out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);



            AUTHZ_ACCESS_REQUEST request = new AUTHZ_ACCESS_REQUEST();
            request.DesiredAccess = 0x02000000;
            request.PrincipalSelfSid = null;
            request.ObjectTypeList = null;
            request.ObjectTypeListLength = 0;
            request.OptionalArguments = IntPtr.Zero;

            AUTHZ_ACCESS_REPLY reply = new AUTHZ_ACCESS_REPLY();
            reply.GrantedAccessMask = IntPtr.Zero;
            reply.ResultListLength = 0;
            reply.SaclEvaluationResults = IntPtr.Zero;
            IntPtr AccessReply = IntPtr.Zero;
            reply.Error = Marshal.AllocHGlobal(1020);
            reply.GrantedAccessMask = Marshal.AllocHGlobal(sizeof(uint));
            reply.ResultListLength = 1;
            if (AuthzAccessCheck(0, pClientContext, ref request, IntPtr.Zero, pSecurityDescriptor, null, 0, ref reply, out AccessReply))
            {
                int granted_access = Marshal.ReadInt32(reply.GrantedAccessMask);
                ACCESS_MASK mask = (ACCESS_MASK)granted_access;
                if ((mask & ACCESS_MASK.DELETE) > 0)
                    acl.Deletable = true;
                if ((mask & ACCESS_MASK.FILE_READ_DATA) > 0)
                    acl.Readable = true;
                if ((mask & ACCESS_MASK.FILE_WRITE_DATA) > 0)
                    acl.Writeable = true;
            }
            //FreePointerC(pClientContext);
            FreePointerH(AccessReply);
            FreePointerH(reply.GrantedAccessMask);
            FreePointerH(reply.SaclEvaluationResults);
            FreePointerH(reply.Error);
        ////FreePointerC(pSidOwner);
        ////FreePointerC(pSidGroup);
        ////FreePointerC(pDacl);
        FreePointerC(pSacl);

        FreePointerC(pSecurityDescriptor);
        return acl;
    }


    public static ACL ResolvePermissionsSlow(string path)
    {
        ACL acl = new();
        try
        {
            new FileStream(path, FileMode.Open, FileAccess.Read).Dispose();
            acl.Readable = true;
        }
        catch { }
        try
        {
            new FileStream(path, FileMode.Open, FileAccess.Write).Dispose();
            acl.Writeable = true;
        }
        catch { }
        return acl;
    }

    static void FreePointerH(IntPtr pointer)
    {
        if (pointer != IntPtr.Zero)
            Marshal.FreeHGlobal(pointer);
    }

    static void FreePointerC(IntPtr pointer )
    {
        if (pointer != IntPtr.Zero)
            Marshal.FreeCoTaskMem(pointer);
    }

    }
}