using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Loadport.Enum
{
    public enum enumLoadPortMode : int { Initializing, Remote, Maintenance, Recovery }

    public enum enumLoadPortStatus : int { InPos, Moving, Pause };

    public enum enumLoadPortPos : int { Unknow = 0, Home = 1, Dock = 2, NotORGN = 99 };

    public enum enumLoadPortSignalTable : int
    {
        /// <summary>
        /// 自動模式中 STAT.S1.1 = 1(Remote)
        /// </summary>
        Remote,
        /// <summary>
        /// 手動模式中 STAT.S1.1 = 3(Recovery)
        /// </summary>
        Recovery,
        /// <summary>
        /// 原點復歸完成 STAT.S1.2 = 1(Origin return completion)
        /// </summary>
        OPRCompleted,
        /// <summary>
        /// 命令處理完成 STAT.S1.3 = 0(Stop)
        /// </summary>
        ProcessCompleted,
        /// <summary>
        /// 運動到位 STAT.S1.4 = 0(Stop)
        /// </summary>
        MotionCompleted,
        /// <summary>
        /// Client端第一次連線應答 [CNCT]
        /// </summary>
        Connected,
        StepCompleted,
        Max
    }

    public enum enumLoadPortCommand
    {
        /// <summary>
        /// 原點搜尋, [ORGN]
        /// </summary>
        Orgn,
        /// <summary>
        /// Dock Foup [CLMP]
        /// </summary>
        Clamp,
        /// <summary>
        /// UnDock Foup, [UCLM]
        /// </summary>
        UnClamp,
        /// <summary>
        /// 掃片, [WMAP]
        /// </summary>
        Mapping,
        /// <summary>
        /// E84Load, [LOAD]
        /// </summary>
        E84Load,
        /// <summary>
        /// E84UnLoad, [UNLD]
        /// </summary>
        E84UnLoad,
        /// <summary>
        /// 事件自動回報設定, [EVNT]
        /// </summary>
        SetEvent,
        /// <summary>
        /// Reset, [RSTA]
        /// </summary>
        Reset,
        /// <summary>
        /// 控制器初始化, [INIT]
        /// </summary>
        Initialize,
        /// <summary>
        /// 軟急停, [STOP]
        /// </summary>
        Stop,
        /// <summary>
        /// 動作暫停, [PAUS]
        /// </summary>
        Pause,
        /// <summary>
        /// 模式切換, [MODE]
        /// </summary>
        Mode,
        /// <summary>
        /// 記憶體資料存到硬碟, [WTDT]
        /// </summary>
        Wtdt,
        /// <summary>
        /// 取得Data, [RTDT]
        /// </summary>
        GetData,
        /// <summary>
        /// 記憶體資料存到控制器, [TRDT]
        /// </summary>
        TransferData,
        /// <summary>
        /// 速度設定, [SSPD]
        /// </summary>
        Speed,
        /// <summary>
        /// 設定IO, [SPOT]
        /// </summary>
        SetIO,
        /// <summary>
        /// 問機況, [STAT]
        /// </summary>
        Status,
        /// <summary>
        /// 問IO, [GPIO]
        /// </summary>
        GetIO,
        /// <summary>
        /// 問掃片結果, [GetRAC2]
        /// </summary>
        GetRAC2,
        /// <summary>
        /// 問掃片結果, [GMAP]
        /// </summary>
        GetMappingData,
        /// <summary>
        /// 問版本, [GVER]
        /// </summary>
        GetVersion,
        /// <summary>
        /// 儲存log, [GLOG]
        /// </summary>
        GetLog,
        /// <summary>
        /// 設定系統時間, [STIM]
        /// </summary>
        SetDateTime,
        /// <summary>
        /// 問系統時間, [GTIM]
        /// </summary>
        GetDateTime,
        /// <summary>
        /// 問encoder [GPOS]
        /// </summary>
        GetPos,
        /// <summary>
        /// 取得CarrierType, [GWID]
        /// </summary>
        GetType,
        /// <summary>
        /// 設定CarrierType, [SWID]
        /// </summary>
        SetType,
        /// <summary>
        /// CheckSum [GSUM]
        /// </summary>
        CheckSum,
        ZaxStep,
        ZaxHome,
        YaxStep,
        YaxHome,
        RotHome,
        /// <summary>
        /// 讀CarrierID [READ]
        /// </summary>
        ReadID,
        // <summary>
        /// 寫CarrierID [WRIT]
        /// </summary>
        WriteID,
        // <summary>
        /// 讀DPRM [DPRM.GTDT]
        /// </summary>
        GetDPRM,
        // <summary>
        /// 讀DPRM [DPRM.STDT]
        /// </summary>
        SetDPRM,
        /// 讀DMPR [DMPR.GTDT]
        /// </summary>
        GetDMPR,
        // <summary>
        /// 讀DPRM [DMPR.STDT]
        /// </summary>
        SetDMPR,
        // <summary>
        /// 讀DPRM [DCST.GTDT]
        /// </summary>
        GetDCST,
        // <summary>
        /// 讀DPRM [DCST.STDT]
        /// </summary>
        SetDCST,
        ClientConnected,
        /// <summary>
        /// 讀RFID CarrierID [READ(1,2)]
        /// </summary>
        Read,
        Max
    }

    public enum enumStateMachine : int
    {
        PS_Unknown = 0,
        PS_Disable,
        PS_ReadyToLoad,
        PS_FoupOn,
        PS_Arrived,
        PS_Clamped,
        PS_Docking,
        PS_Docked,
        PS_FuncSetup,
        PS_FuncSetupNG,
        PS_Process,
        PS_Complete,
        PS_Abort,
        PS_Stop,
        PS_UnDocking,
        PS_UnDocked,
        PS_ReadyToUnload,
        PS_UnClamped,
        PS_Removed,
        PS_Error
    };

    public enum enumFoupType : int
    {
        FUP1 = 0,
        FUP2 = 1,
        FUP3 = 2,
        FUP4 = 3,
        FUP5 = 4,
        FUP6 = 5,
        FUP7 = 6,
        FSB1 = 7,
        FSB2 = 8,
        FSB3 = 9,
        FSB4 = 10,
        FSB5 = 11,
        OCP1 = 12,
        OCP2 = 13,
        OCP3 = 14,
        FPO1 = 15,
    }

    public enum enumWaferStatus : int
    {
        none = 0,
        Normal = 1,
        Thick = 2,
        CrossWafer = 3,
        FrontBow = 4,
        Double = 7,
        Thin = 8,
    }

    public enum CarrierIDStats
    {
        Create = -2,
        NoStatus = -1,
        IDNotRead = 0,
        IDRead = 1,
        IDReadFail = 2,
        IDVerificationok = 3,
        IDVerificationFail = 4,
    }

    public enum CarrierSlotMapStats
    {
        NoStatus = -1,
        NotSlotMap = 0,
        SlotMappingOK = 1,
        SlotMappingVerificationok = 2,
        SlotMappingVerificationFail = 3,

    }
    public enum CarrierAccessStats
    {
        NoStatus = 0,
        NotAccess,
        Accessing,
        CarrierComplete,
        CarrierStop,

    }
    public enum E84PortStates { OUTOFSERVICE = 0, ReadytoLoad = 2, TransferBlock = 1, ReadytoUnload = 3 }



}
