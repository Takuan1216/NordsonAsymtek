using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rorze.Secs
{
    public interface ISECS
    {
        bool InitSecs();
        bool InitSecsForEQP();
        //  bool SecsStart();
        bool SecsReStart();
        bool SecsStop();


        // Secs Message Action 
        int DataIteminitin(ref object msg, SecsMessageObject1Args secsobject);
        int DataItemIn(SecsFormateType formate, ref object msg, ref object data);
        int DataIteminitoutResponse(ref object msg);
        int DataIteminitoutResponse(ref object msg, SecsMessageObject1Args secsobject);
        int DataIteminitoutRequest(QsStream Station, QsFunction Function, bool UseWbit, ref object msg);

        int DataIteminitoutResponse(QsStream Station, QsFunction Function, ref object msg, SecsMessageObject1Args secsobject);
        int DataItemOut(SecsFormateType formate, ref object msg, object data, params int[] Listcount);
        void SendMessage(QsStream Station, QsFunction Function, object SecsMessage, SecsMessageObject1Args Secsobject = null);


        //Secs  Attributes
        bool GetInternalConnected();
        bool GetSecsStarted();

        SECSMODE GetSecsMode();
        string GetLocalIP();
        int GetLocalPort();
        int GetDeviceID();
        int GetT3TimeOut();
        int GetT5TimeOut();
        int GetT6TimeOut();
        int GetT7TimeOut();
        int GetT8TimeOut();


        void SetLocalIP(string IP);
        void SetLocalPort(int PortID);
        void SetDeviceID(int ID);
        void SetT3TimeOut(int T3);
        void SetT5TimeOut(int T5);
        void SetT6TimeOut(int T6);
        void SetT7TimeOut(int T7);
        void SetT8TimeOut(int T8);
        void SaveConfig();

        void SetSECSMODE(SECSMODE mode);
        ConnState GetSecsConnState();
        event SecsMessageObject1Args.SecsMessageHandle ReceiveSecsMessage;
        event SecsInternalArgs.SecsInternalHandle InternalStatusChange;
    }

    public class SecsMessageObject1Args : EventArgs
    {
        public delegate void SecsMessageHandle(SecsMessageObject1Args secsMsg);

        QsStream _station;
        QsFunction _function;
        object _msg; // Secs message formate 
        int _wbit;
        public ushort _Tick;
        object _head; 
        public SecsMessageObject1Args(QsStream station, QsFunction function, object head, object Msg, int wbit, ushort tick)
        {
            _station = station;
            _function = function;
            _head = head;
            _msg = Msg;
            _wbit = wbit;
            _Tick = tick;
        }
        public QsStream Station { get { return _station; } set { _station = value; } }
        public QsFunction Function { get { return _function; } set { _function = value; } }
        public object GetSecsMessage { get { return _msg; } }

        public object GetSECSHead { get { return _head; } }
        public int WBit { get { return _wbit; } set { if (value > 0) _wbit = 1; else if (value <= 0) _wbit = 0; } }
    }

    public enum GEMControlStats
    { OFFLINE = 0, EQUIPMENTOFFLINE = 1, ATTEMTPONLINE = 2, HOSTOFFLINE = 3, ONLINELOCAL = 4, ONLINEREMOTE = 5 }
    public enum GEMProcessStats
    {
        Init = 0,
        Idle = 1,
        FOUPClamp = 2,
        ComMID = 3,
        FOUPDocking = 4,
        FOUPReady = 5,
        FunctionSetup = 6,
        FunctionSetupFail = 7,
        Executing = 8,
        Finish = 9,
        TagWriteData = 10,
        Abort = 11,
        Pause = 12,
        Resume = 13,
        Stop = 14,
        FOUPUnDock = 15,
        FOUPUnClamp = 16,
        PodReadyToMoveOut = 17,
    }
    public enum GEMCommStats
    { DISABLE = 0, NOTCOMMUNICATION = 1, COMMUNICATION = 2 }
    public enum SECSMODE
    {
        HSMS_MODE = 0, // TCP/IP
        SECS_MODE = 1  // RS232
    }
    public enum SECSDriver
    {
        SDR = 0,
        ITRI = 1,
    }

    public enum QsStream : int
    {
        S1 = 1, S2 = 2, S3 = 3, S4 = 4, S5 = 5, S6 = 6, S7 = 7, S8 = 8, S9 = 9, S10 = 10,
        S11 = 11, S12 = 12, S13 = 13, S14 = 14, S15 = 15, S16 = 16, S17 = 17, S18 = 18
    }
    public enum QsFunction : int
    {
        F0 = 0, F1 = 1, F2 = 2, F3 = 3, F4 = 4, F5 = 5, F6 = 6, F7 = 7, F8 = 8, F9 = 9, F10 = 10,
        F11 = 11, F12 = 12, F13 = 13, F14 = 14, F15 = 15, F16 = 16, F17 = 17, F18 = 18, F19 = 19, F20 = 20,
        F21 = 21, F22 = 22, F23 = 23, F24 = 24, F25 = 25, F26 = 26, F27 = 27, F28 = 28, F29 = 29, F30 = 30,
        F31 = 31, F32 = 32, F33 = 33, F34 = 34, F35 = 35, F36 = 36, F37 = 37, F38 = 38, F39 = 39, F40 = 40,
        F41 = 41, F42 = 42, F43 = 43, F44 = 44, F45 = 45, F46 = 46, F47 = 47, F48 = 48, F49 = 49, F50 = 50,
        F51 = 51, F52 = 52, F53 = 53, F54 = 54, F55 = 55, F56 = 56, F57 = 57, F58 = 58, F59 = 59, F60 = 60,
        F61 = 61, F62 = 62, F63 = 63, F64 = 64, F65 = 65, F66 = 66, F67 = 67, F68 = 68, F69 = 69, F70 = 70,
        F71 = 71, F72 = 72, F73 = 73, F74 = 74, F75 = 75, F76 = 76, F77 = 77, F78 = 78, F79 = 79, F80 = 80,
    }
    public enum SecsFormateType
    { L, U1, U2, U4, A, B, Bool, F4, F8 ,I2}
    public enum ConnState
    { SCS_NCONN = 1, SCS_SCONN, SCS_ABDIS, SCS_INDIS, SCS_ABDIS_SCS_INDIS };

    public enum BWSAction { WaferIn = 0, WaferOut, SorterFunc }
    public enum ToolProcessMode { OfflineMode = 0, OnlineMode }
    public struct SECSConneetConfig
    {
        public SECSDriver Driver;
        public string LocalIP;
        public int LocalPort;
        public SECSMODE Mode;
        public int DDEVICEID;
        public int T3;
        public int T5;
        public int T6;
        public int T7;
        public int T8;

    }
    public struct SECSParameterConfig
    {
        public bool S10Wbit;
        public bool S5Wbit;
        public bool S6Wbit;
        public bool PJSlotmapBypass;
        public bool CarrireIDByHost;
        public bool IsSimulation;
        public int CanExcuteJob;

        public GEMControlStats OnlineSubStats;
        public SettingStreamFunction ConnectFunction;
        public SettingStreamFunction AlarmFunction;
        public ToolProcessMode ProcessMode;
        public SECSDriver GemDriver;
        public bool EnableSECS;

    }
    public class SettingStreamFunction
    {
        QsStream _Stream;
        QsFunction _Function;
        public SettingStreamFunction(QsStream Stream, QsFunction Function)
        {
            _Stream = Stream;
            _Function = Function;
        }
        public QsStream Stream { get { return _Stream; } set { _Stream = value; } }
        public QsFunction Function { get { return _Function; } set { _Function = value; } }
    }
    public class SecsInternalArgs
    {
        public delegate void SecsInternalHandle(SecsInternalArgs Internal);
        bool _InternalConnect = false;
        public SecsInternalArgs(ConnState mode)
        {
            _InternalConnect = (mode == ConnState.SCS_SCONN) ? true : false;
        }

        public bool GetInternalConnect { get { return _InternalConnect; } }
    }
    public class StreamFuntionException : Exception
    {
        public int nAck { get; set; }
        public int nErrorCode { get; set; }


        public StreamFuntionException(int Ack, int ErrorCode, string ErrorStr)
            : base(ErrorStr)
        {
            nAck = Ack;
            nErrorCode = ErrorCode;
        }
    }
}
