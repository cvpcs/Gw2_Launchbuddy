﻿    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.Text;
    using System.IO;

namespace Gw2_Launchbuddy

    {

        public class Win32API
        {
        [DllImport(@"urlmon.dll", CharSet = CharSet.Auto)] private extern static System.UInt32 FindMimeFromData(
        System.UInt32 pBC,
        [MarshalAs(UnmanagedType.LPStr)] System.String pwzUrl,
        [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
        System.UInt32 cbSize,
        [MarshalAs(UnmanagedType.LPStr)] System.String pwzMimeProposed,
        System.UInt32 dwMimeFlags,
        out System.UInt32 ppwzMimeOut,
        System.UInt32 dwReserverd
        );




        [DllImport("ntdll.dll")]
            public static extern int NtQueryObject(IntPtr ObjectHandle, int
                ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength,
                ref int returnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

            [DllImport("ntdll.dll")]
            public static extern uint NtQuerySystemInformation(int
                SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength,
                ref int returnLength);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr OpenMutex(UInt32 desiredAccess, bool inheritHandle, string name);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll")]
            public static extern int CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
               ushort hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
               uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentProcess();

            public enum ObjectInformationClass : int
            {
                ObjectBasicInformation = 0,
                ObjectNameInformation = 1,
                ObjectTypeInformation = 2,
                ObjectAllTypesInformation = 3,
                ObjectHandleInformation = 4
            }

            [Flags]
            public enum ProcessAccessFlags : uint
            {
                All = 0x001F0FFF,
                Terminate = 0x00000001,
                CreateThread = 0x00000002,
                VMOperation = 0x00000008,
                VMRead = 0x00000010,
                VMWrite = 0x00000020,
                DupHandle = 0x00000040,
                SetInformation = 0x00000200,
                QueryInformation = 0x00000400,
                Synchronize = 0x00100000
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_BASIC_INFORMATION
            { // Information Class 0
                public int Attributes;
                public int GrantedAccess;
                public int HandleCount;
                public int PointerCount;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int NameInformationLength;
                public int TypeInformationLength;
                public int SecurityDescriptorLength;
                public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_TYPE_INFORMATION
            { // Information Class 2
                public UNICODE_STRING Name;
                public int ObjectCount;
                public int HandleCount;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int Reserved4;
                public int PeakObjectCount;
                public int PeakHandleCount;
                public int Reserved5;
                public int Reserved6;
                public int Reserved7;
                public int Reserved8;
                public int InvalidAttributes;
                public GENERIC_MAPPING GenericMapping;
                public int ValidAccess;
                public byte Unknown;
                public byte MaintainHandleDatabase;
                public int PoolType;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_NAME_INFORMATION
            { // Information Class 1
                public UNICODE_STRING Name;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct UNICODE_STRING
            {
                public ushort Length;
                public ushort MaximumLength;
                public IntPtr Buffer;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct GENERIC_MAPPING
            {
                public int GenericRead;
                public int GenericWrite;
                public int GenericExecute;
                public int GenericAll;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SYSTEM_HANDLE_INFORMATION
            { // Information Class 16
                public int ProcessID;
                public byte ObjectTypeNumber;
                public byte Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
                public ushort Handle;
                public int Object_Pointer;
                public UInt32 GrantedAccess;
            }

            public const int MAX_PATH = 260;
            public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
            public const int DUPLICATE_SAME_ACCESS = 0x2;
            public const int DUPLICATE_CLOSE_SOURCE = 0x1;



        public static string getMimeFromFile(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename + " not found");

            byte[] buffer = new byte[256];
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                if (fs.Length >= 256)
                    fs.Read(buffer, 0, 256);
                else
                    fs.Read(buffer, 0, (int)fs.Length);
            }
            try
            {
                System.UInt32 mimetype;
                FindMimeFromData(0, null, buffer, 256, null, 0, out mimetype, 0);
                System.IntPtr mimeTypePtr = new IntPtr(mimetype);
                string mime = Marshal.PtrToStringUni(mimeTypePtr);
                Marshal.FreeCoTaskMem(mimeTypePtr);
                return mime;
            }
            catch (Exception e)
            {
                return "unknown/unknown";
            }
        }
    }

        public class Win32Processes
        {
            const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
            const uint STATUS_INFO_LENGTH_MISMATCH = 0xc0000004;

            public static string getObjectTypeName(Win32API.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
            {
                IntPtr m_ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
                IntPtr ipHandle = IntPtr.Zero;
                var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
                IntPtr ipBasic = IntPtr.Zero;
                var objObjectType = new Win32API.OBJECT_TYPE_INFORMATION();
                IntPtr ipObjectType = IntPtr.Zero;
                IntPtr ipObjectName = IntPtr.Zero;
                string strObjectTypeName = "";
                int nLength = 0;
                int nReturn = 0;
                IntPtr ipTemp = IntPtr.Zero;
            

                if (!Win32API.DuplicateHandle(m_ipProcessHwnd, shHandle.Handle,
                                              Win32API.GetCurrentProcess(), out ipHandle,
                                              0, false, Win32API.DUPLICATE_SAME_ACCESS))
                    return null;


                    
                ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
                Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation,
                                       ipBasic, Marshal.SizeOf(objBasic), ref nLength);
                objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
                Marshal.FreeHGlobal(ipBasic);

                ipObjectType = Marshal.AllocHGlobal(objBasic.TypeInformationLength);
                nLength = objBasic.TypeInformationLength;
                while ((uint)(nReturn = Win32API.NtQueryObject(
                    ipHandle, (int)Win32API.ObjectInformationClass.ObjectTypeInformation, ipObjectType,
                      nLength, ref nLength)) ==
                    Win32API.STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(ipObjectType);
                    ipObjectType = Marshal.AllocHGlobal(nLength);
                }

                objObjectType = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(ipObjectType, objObjectType.GetType());
                if (Is64Bits())
                {
                    ipTemp = new IntPtr(Convert.ToInt64(objObjectType.Name.Buffer.ToString(), 10) >> 32);
                }
                else
                {
                    ipTemp = objObjectType.Name.Buffer;
                }

            strObjectTypeName = Marshal.PtrToStringUni(ipTemp, objObjectType.Name.Length >> 1);



            Marshal.FreeHGlobal(ipObjectType);
                Win32API.CloseHandle(ipHandle);
                return strObjectTypeName;
            }


            public static string getObjectName(Win32API.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
            {
                IntPtr m_ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
                IntPtr ipHandle = IntPtr.Zero;
                var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
                IntPtr ipBasic = IntPtr.Zero;
                IntPtr ipObjectType = IntPtr.Zero;
                var objObjectName = new Win32API.OBJECT_NAME_INFORMATION();
                IntPtr ipObjectName = IntPtr.Zero;
                string strObjectName = "";
                int nLength = 0;
                int nReturn = 0;
                IntPtr ipTemp = IntPtr.Zero;
            
                if (!Win32API.DuplicateHandle(m_ipProcessHwnd, shHandle.Handle, Win32API.GetCurrentProcess(),
                                              out ipHandle, 0, false, Win32API.DUPLICATE_SAME_ACCESS))
                    return null;
                
                    
                ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
                Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation,
                                       ipBasic, Marshal.SizeOf(objBasic), ref nLength);
                objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
                Marshal.FreeHGlobal(ipBasic);


                nLength = objBasic.NameInformationLength;

                ipObjectName = Marshal.AllocHGlobal(nLength);
                while ((uint)(nReturn = Win32API.NtQueryObject(
                         ipHandle, (int)Win32API.ObjectInformationClass.ObjectNameInformation,
                         ipObjectName, nLength, ref nLength))
                       == Win32API.STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(ipObjectName);
                    ipObjectName = Marshal.AllocHGlobal(nLength);
                }
                objObjectName = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(ipObjectName, objObjectName.GetType());

                if (Is64Bits())
                {
                    ipTemp = new IntPtr(Convert.ToInt64(objObjectName.Name.Buffer.ToString(), 10) >> 32);
                }
                else
                {
                    ipTemp = objObjectName.Name.Buffer;
                }

                if (ipTemp != IntPtr.Zero)
                {

                    byte[] baTemp2 = new byte[nLength];
                    try
                    {
                        Marshal.Copy(ipTemp, baTemp2, 0, nLength);

                        strObjectName = Marshal.PtrToStringUni(Is64Bits() ?
                                                               new IntPtr(ipTemp.ToInt64()) :
                                                               new IntPtr(ipTemp.ToInt32()));
                        return strObjectName;
                    }
                    catch (AccessViolationException)
                    {
                        return null;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ipObjectName);
                        Win32API.CloseHandle(ipHandle);
                    }
                }
                return null;
            }


        public static List<Win32API.SYSTEM_HANDLE_INFORMATION>
    GetHandles(Process process = null, string IN_strObjectTypeName = null, string IN_strObjectName = null)
        {
            uint nStatus;
            int nHandleInfoSize = 0x10000;
            IntPtr ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
            int nLength = 0;
            IntPtr ipHandle = IntPtr.Zero;

            while ((nStatus = Win32API.NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer,
                                                                nHandleInfoSize, ref nLength)) ==
                    STATUS_INFO_LENGTH_MISMATCH)
            {
                nHandleInfoSize = nLength;
                Marshal.FreeHGlobal(ipHandlePointer);
                ipHandlePointer = Marshal.AllocHGlobal(nLength);
            }

            byte[] baTemp = new byte[nLength];
            Marshal.Copy(ipHandlePointer, baTemp, 0, nLength);

            long lHandleCount = 0;
            if (Is64Bits())
            {
                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
            }
            else
            {
                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
            }

            Win32API.SYSTEM_HANDLE_INFORMATION shHandle;
            List<Win32API.SYSTEM_HANDLE_INFORMATION> lstHandles = new List<Win32API.SYSTEM_HANDLE_INFORMATION>();

            for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
            {
                shHandle = new Win32API.SYSTEM_HANDLE_INFORMATION();
                if (Is64Bits())
                {
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                }
                else
                {
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                }

                if (process != null)
                {
                    if (shHandle.ProcessID != process.Id) continue;
                }

                string strObjectTypeName = "";
                if (IN_strObjectTypeName != null)
                {
                    strObjectTypeName = getObjectTypeName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                    if (strObjectTypeName != IN_strObjectTypeName) continue;
                }
                
                string strObjectName = IN_strObjectName;

                if (IN_strObjectName != null)
                {
                    strObjectName = getObjectName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                    if (strObjectName != IN_strObjectName) continue;

                }
                

                string strObjectTypeName2 = getObjectTypeName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                string strObjectName2 = getObjectName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                Console.WriteLine(shHandle.ProcessID.ToString() + "\n" + strObjectTypeName2.ToString() + "\n" + strObjectName2.ToString());

                lstHandles.Add(shHandle);
            }
            return lstHandles;
        }

        public static bool Is64Bits()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
        }



        /*

        public static List<Win32API.SYSTEM_HANDLE_INFORMATION>
            GetHandles(Process process = null, string IN_strObjectTypeName = null, string IN_strObjectName = null)
            {
                uint nStatus;
                int nHandleInfoSize = 0x10000;
                IntPtr ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
                int nLength = 0;
                IntPtr ipHandle = IntPtr.Zero;

                while ((nStatus = Win32API.NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer,
                                                                    nHandleInfoSize, ref nLength)) ==
                        STATUS_INFO_LENGTH_MISMATCH)
                {
                    nHandleInfoSize = nLength;
                    Marshal.FreeHGlobal(ipHandlePointer);
                    ipHandlePointer = Marshal.AllocHGlobal(nLength);
                }

                byte[] baTemp = new byte[nLength];
                Marshal.Copy(ipHandlePointer, baTemp, 0, nLength);

                long lHandleCount = 0;
                if (Is64Bits())
                {
                    lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                    ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
                }
                else
                {
                    lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                    ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
                }

                Win32API.SYSTEM_HANDLE_INFORMATION shHandle;
                List<Win32API.SYSTEM_HANDLE_INFORMATION> lstHandles = new List<Win32API.SYSTEM_HANDLE_INFORMATION>();

                for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
                {
                    shHandle = new Win32API.SYSTEM_HANDLE_INFORMATION();
                    if (Is64Bits())
                    {
                        shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                        ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                    }
                    else
                    {
                        ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                        shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    }

                    if (process != null)
                    {
                        if (shHandle.ProcessID != process.Id) continue;
                    }

                    string strObjectTypeName = "";
                    if (IN_strObjectTypeName != null)
                    {
                        strObjectTypeName = getObjectTypeName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                        if (strObjectTypeName != IN_strObjectTypeName) continue;
                    }

                    string strObjectName = "";
                    if (IN_strObjectName != null)
                    {
                        strObjectName = getObjectName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                        if (strObjectName != IN_strObjectName) continue;
                    }

                    string strObjectTypeName2 = getObjectTypeName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                    string strObjectName2 = getObjectName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                    Console.WriteLine(shHandle.ProcessID.ToString() + "\n" +strObjectTypeName2.ToString() + "\n" + strObjectName2.ToString());

                    lstHandles.Add(shHandle);
                }
                return lstHandles;
            }

            public static bool Is64Bits()
            {
                return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
            }
            */
    }



    class MutexCloser
    {
        public bool CloseMutex(int ProcessId, string MutexName)
        {

            try
            {
                Process process = Process.GetProcessById(ProcessId);
                var handles_new = Win32API.getMimeFromFile("gw2.exe");
                var handles = Win32Processes.GetHandles(process, "Mutant", "\\Sessions\\1\\BaseNamedObjects\\" + MutexName);
                if (handles.Count == 0) throw new System.ArgumentException("NoMutex", "original");
                foreach (var handle in handles)
                {
                    IntPtr ipHandle = IntPtr.Zero;
                    Win32API.DuplicateHandle(Process.GetProcessById(handle.ProcessID).Handle, handle.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_CLOSE_SOURCE);
                    Win32API.CloseHandle(ipHandle);
                    Console.WriteLine("Mutex was killed");
                    return true;
                }
            }
            catch (Exception err)
            {
                
                Console.WriteLine(err.Message);
                return false;
            }
            return false;
        }
    }
    }

