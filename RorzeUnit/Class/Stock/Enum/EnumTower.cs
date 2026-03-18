using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.Stock.Enum
{
    public enum enumTowerMode : int { Initializing, Remote, Maintenance, Recovery }

    public enum enumTowerStatus : int { InPos, Moving, Pause };

    public enum enumTowerSignalTable : int
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

    public enum enumTowerCommand
    {

        Orgn,
        Home,
        Extend,
        Load,
        Unload,
        Clamp,
        UnClamp,
        Mapping,


        E84Load,

        E84UnLoad,

        SetEvent,

        Reset,

        Initialize,

        Stop,

        Pause,

        Mode,

        Wtdt,

        GetData,

        TransferData,

        Speed,

        SetIO,

        Status,

        GetIO,

        GetMappingData,

        GetVersion,

        GetLog,

        SetDateTime,

        GetDateTime,

        GetPos,
        EPOS,//ENCODLE
        GetType,

        SetType,

        CheckSum,
        ZaxStep,
        ZaxHome,
        YaxStep,
        YaxHome,
        RotHome,

        ReadID,

        WriteID,

        GetDPRM,

        SetDPRM,

        GetDMPR,

        SetDMPR,

        GetDCST,

        SetDCST,
        ClientConnected,
        Max
    }

    public enum enumStateMachine : int
    {
        PS_Unknown,
        PS_Disable,
        PS_Ready,
        //PS_ReadyToLoad,
        //PS_FoupOn,
        //PS_Arrived,
        //PS_Clamped,
        //PS_Docking,
        //PS_Docked,
        //PS_FuncSetup,
        //PS_FuncSetupNG,
        PS_Process,
        PS_Complete,
        PS_Abort,
        PS_Stop,
        //PS_UnDocking,
        //PS_UnDocked,
        //PS_ReadyToUnload,
        //PS_UnClamped,
        //PS_Removed,
        PS_Error
    };
    //======================================================================
    public enum enumTowerCustomError : int
    {
        Status_Error = 0,
        SendCommandFailure = 1,
        OriginPosReturnFailure = 2,
        OriginPosReturnTimeout = 3,
        MovingHomePosFailure = 4,
        AckTimeout = 5,
        ExtendingArmFailure = 6,
        TakeWaferFailure = 7,
        PutWaferFailure = 8,
        WaferExchangeFailure = 9,
        TurnOnVacuumFailure = 10,
        TurnOffVacuumFailure = 11,
        MappingFailure = 12,
        GetMappingDataFailure = 13,
        GetStatusFailure = 14,
        ResetFailure = 15,
        InitialFailure = 16,
        StopMotionTimeout = 17,
        StopMotionFailure = 18,
        PauseMotionTimeout = 19,
        PauseMotionFailure = 20,
        SetSpeedFailure = 21,
        SingleAxisAckTimeout = 22,
        SingleAxisOPRFailure = 23,
        SingleAxisAbsoluteMovingFailure = 24,
        SingleAxisJogMovingTimeout = 25,
        SingleAxisJogMovingFailure = 26,
        SingleAxisGetPosFailure = 27,
        MotionTimeout = 28,
        ModeTimeout = 29,
        EncoderBatteryError = 30,
        RobotNoInManualMode = 31,
        XAxisMotionTimeout = 32,
        NotFoundAvailableArm = 33,
        UploadTeachingDataFailure = 34,
        DownloadTeachingDataFailure = 35,
        ModeFailure = 36,
        RejectManualFuncWhenAuto = 37,
        GetMaintenanceDataFailure = 38,
        SetParameterFailure = 39,
        ProgramError = 40,
        RobotError = 41,
        RobotIsBusy = 42,
        ManualMode = 43,
        SettingWithoutWaferOut = 44,
        SettingWithoutWaferIn = 45,
        InterlockStop = 46,
        InitialRejectWhenAuto = 47,
        MotionAbnormal = 48,
        WaferAndDataNotMatch = 49,
        ProcessFlagTimeout = 50,
        ProcessFlagAbnormal = 51,

        E84Auto_FOUPManualRemove = 52,
        E84Auto_FOUPManualDetect = 53,

        Max = 64,
    }

    public enum enumTowerWarning : int
    {

        N2_Source_Pressure,
        XCDA_Source_Pressure,
        Adj_Pre_1,
        Adj_Pre_2,
        Adj_Pre_3,
        Adj_Pre_4,
        IonizerError,

        T1_CAS1_Purge_P1warning,
        T1_CAS1_Purge_P2warning,
        T2_CAS1_Purge_P1warning,
        T2_CAS1_Purge_P2warning,
        T3_CAS1_Purge_P1warning,
        T3_CAS1_Purge_P2warning,
        T4_CAS1_Purge_P1warning,
        T4_CAS1_Purge_P2warning,

        T1_CAS2_Purge_P1warning,
        T1_CAS2_Purge_P2warning,
        T2_CAS2_Purge_P1warning,
        T2_CAS2_Purge_P2warning,
        T3_CAS2_Purge_P1warning,
        T3_CAS2_Purge_P2warning,
        T4_CAS2_Purge_P1warning,
        T4_CAS2_Purge_P2warning,

        T1_CAS3_Purge_P1warning,
        T1_CAS3_Purge_P2warning,
        T2_CAS3_Purge_P1warning,
        T2_CAS3_Purge_P2warning,
        T3_CAS3_Purge_P1warning,
        T3_CAS3_Purge_P2warning,
        T4_CAS3_Purge_P1warning,
        T4_CAS3_Purge_P2warning,

        T1_CAS4_Purge_P1warning,
        T1_CAS4_Purge_P2warning,
        T2_CAS4_Purge_P1warning,
        T2_CAS4_Purge_P2warning,
        T3_CAS4_Purge_P1warning,
        T3_CAS4_Purge_P2warning,
        T4_CAS4_Purge_P1warning,
        T4_CAS4_Purge_P2warning,

        T1_CAS5_Purge_P1warning,
        T1_CAS5_Purge_P2warning,
        T2_CAS5_Purge_P1warning,
        T2_CAS5_Purge_P2warning,
        T3_CAS5_Purge_P1warning,
        T3_CAS5_Purge_P2warning,
        T4_CAS5_Purge_P1warning,
        T4_CAS5_Purge_P2warning,

        T1_CAS6_Purge_P1warning,
        T1_CAS6_Purge_P2warning,
        T2_CAS6_Purge_P1warning,
        T2_CAS6_Purge_P2warning,
        T3_CAS6_Purge_P1warning,
        T3_CAS6_Purge_P2warning,
        T4_CAS6_Purge_P1warning,
        T4_CAS6_Purge_P2warning,

        T1_CAS7_Purge_P1warning,
        T1_CAS7_Purge_P2warning,
        T2_CAS7_Purge_P1warning,
        T2_CAS7_Purge_P2warning,
        T3_CAS7_Purge_P1warning,
        T3_CAS7_Purge_P2warning,
        T4_CAS7_Purge_P1warning,
        T4_CAS7_Purge_P2warning,

        T1_CAS8_Purge_P1warning,
        T1_CAS8_Purge_P2warning,
        T2_CAS8_Purge_P1warning,
        T2_CAS8_Purge_P2warning,
        T3_CAS8_Purge_P1warning,
        T3_CAS8_Purge_P2warning,
        T4_CAS8_Purge_P1warning,
        T4_CAS8_Purge_P2warning,
    }

}
