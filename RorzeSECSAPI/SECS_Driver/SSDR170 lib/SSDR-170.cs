using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rorze.SecsDriver
{
    public static class SSDR_170
    {

        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct SECURITY_ATTRIBUTES
        {
            public int length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        public static int S2_L = ((1 << 2) | 0);
        public static int S2_LS2_STRING = ((4 << 2) | 0);
        public static int S2_U4 = ((6 << 2) | 2);
        public static int S2_U2 = ((6 << 2) | 1);
        public static int S2_U1 = ((6 << 2) | 0);
        public static int S2_B = ((2 << 2) | 0);
        public static int S2_BOOLEAN = ((3 << 2) | 0);
        public static int S2_F4 = ((8 << 2) | 2);
        public static int S2_F8= ((8 << 2) | 3);
        public static int S2_I2 = ((7 << 2) | 1);
        //[DllImport("sdr170.dll")]
        //public static extern IntPtr SdrItemOutput(SDRMSG pmessage, int type, char[] pbuffer, long count);
        //===Start Secs
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrConfigure(string path, string args, string file, int timeout, string logCmd);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrStart(int buffers, int timeout, string logCmd, string sdrlName);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrPortEnable(ushort DeviceID);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrIdEnable(ushort DeviceID);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrIdSemSet( ushort DeviceID, IntPtr semid);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrPortSemSet(ushort DeviceID, IntPtr semid);

        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrIdPoll(int DeviceID, ref ushort pticket, ref SDRMSG pmsg);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrConnectionStatusGet(ushort id);
        //===

        [DllImport("sdr.dll")]
        public static extern int SdrItemInitO(ref SDRMSG pmsg);
        [DllImport("sdr.dll")]
        public static extern int SdrItemSize(int type);

        //=== Stop Secs
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrPortSeparate(ushort id);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrIdDisable(ushort id);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrPortDisable(ushort model_id);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrStop(string logCmd);
        //====

        [DllImport("sdr.dll")]
        public static extern int SdrItemGeneric(int type);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutput(ref SDRMSG pmsg, Int32 type, object value, int count);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutput(ref SDRMSG pmsg, Int32 type,  string Value, int count);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutput(ref SDRMSG pmsg, Int32 type,ref int Value, int count);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutput(ref SDRMSG pmsg, Int32 type, ref long Value, int count);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutput(ref SDRMSG pmsg, Int32 type, ref double Value, int count);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutputNLB(ref SDRMSG pmessage, int type, ref int Value, int count, int nlb);
        [DllImport("sdr.dll")]
        public static extern int SdrItemOutputNLB(ref SDRMSG pmessage, int type, ref object value, int count, int nlb);
        [DllImport("sdr.dll")]
        public static extern int SdrRequest(ushort id, ref SDRMSG pmessage, ref ushort pticket);
        [DllImport("sdr.dll")]
        public static extern int SdrResponse( ushort pticket, ref SDRMSG pmessage );
        [DllImport("sdr.dll")]
        public static extern long SdrItemInitI(ref SDRMSG pmsg);

        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type,  IntPtr Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type, ref int Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type, ref bool Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type, ref double Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type, ref float Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type, ref char[] Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type,ref string Value, int count);
        [DllImport("sdr.dll")]
        public static extern Int32 SdrItemInput(ref SDRMSG pmessage, Int32 type, ref byte[] Value, int count);

        [DllImport("sdr.dll")]
        public static extern int SdrMessageGet( ushort pticket, ref SDRMSG pmessage);
        [DllImport("sdr.dll")]
        public static extern int SdrLNote(string Name);
        [DllImport("sdr.dll")]
        public static extern int SdrLogOn();
        [DllImport("sdr.dll")]
        public static extern int SdrLogOff();
        [DllImport("sdr.dll")]
        public static extern int SdrTicketDrop(ushort pticket);
        [DllImport("sdr.dll")]
        public static extern int SdrAutoDropTimeSet(ushort id, int TimeOut);
        [DllImport("sdr.dll")]
        public static extern int SdrAutoDropTimeGet(ushort id, int TimeOut);
        [DllImport("C:\\Sdr\\sdr170.dll")]
        public static extern int SdrTicketPoll(ushort pticket, ref SDRMSG pmsg);
       
        // [DllImport("gwgem.dll")]
        // public static extern int GemStartInit(string gcpFile, string daemon, string ext, string msg, int timeout, string logCmd);


        // [DllImport("gwgem.dll")]
        //public static extern int GemInit(string envpath);
        //[DllImport("gwgem.dll")]
        //public static extern long GemGetVar(long id, int itemtype, ref byte[] CMD, long itemct);
        //[DllImport("gwgem.dll")]
        //public static extern int GemEnable();
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr OpenEvent(UInt32 dwDesiredAccess,
        bool bInheritHandle, String lpName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes,
                bool bManualReset, bool bInitialState, string lpName);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetEvent(IntPtr hEvent);
        [DllImport("kernel32.dll")]
         public static extern uint WaitForSingleObject(IntPtr lpHandles ,uint dwMilliseconds);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ResetEvent(IntPtr hEvent);
        [DllImport("Kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("shell32.dll")]
        public static extern int ShellExecute(string hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);
        //  public static extern uint WaitForSingleObject(EventWaitHandle lpHandles, uint dwMilliseconds);
        //public struct SDRMSG
        //{
        //    public int stream ;     /* Stream Number */
        //    public int function;   /* Function Number */
        //    public int wbit;       /* W-bit Setting */
        //    public long length;     /* Message or Buffer Length */
        //    public string buffer;     /* Pointer to Message Text Buffer */

        //    public long error;          /* Error Code */
        //    public int next;           /* Next Data Item Type */
        //    public string txtp;   /* Message Text Pointer */
        //    public long txtc;           /* Message Text Count */
        //    public string wtxtp;  /* Working txtp */
        //    public long wtxtc;          /* Working txtc */
        //}

        [StructLayout(LayoutKind.Sequential)]
        public  struct SDRMSG
        {
           // [MarshalAs(UnmanagedType.U4)]
            public uint stream ;     /* Stream Number */
           // [MarshalAs(UnmanagedType.U4)]
            public uint function;   /* Function Number */
          //  [MarshalAs(UnmanagedType.U4)]
            public uint wbit;       /* W-bit Setting */
           // [MarshalAs(UnmanagedType.U8)]
            public int length;     /* Message or Buffer Length */

           // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3000)]
            public IntPtr buffer;     /* Pointer to Message Text Buffer */
          //  [MarshalAs(UnmanagedType.U8)]
            public int error;          /* Error Code */
          //  [MarshalAs(UnmanagedType.U4)]
            public int next;           /* Next Data Item Type */
           // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3000)]                          // [MarshalAs(UnmanagedType.ByValArray)]
            public IntPtr txtp;   /* Message Text Pointer */
          //  [MarshalAs(UnmanagedType.U8)]
            public int txtc;           /* Message Text Count */
         // [MarshalAs(UnmanagedType.ByValArray,SizeConst =3000)]
            public IntPtr wtxtp;  /* Working txtp */
          //  [MarshalAs(UnmanagedType.U8)]
            public int wtxtc;          /* Working txt*/
        }
    }
}
