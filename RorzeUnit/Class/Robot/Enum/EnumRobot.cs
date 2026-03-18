using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Robot.Enum
{
    public enum enumRobotMode { Initializing, Remote, Maintenance, Recovery, TeachingPendent }

    public enum enumRobotArms { UpperArm, LowerArm, BothArms, Empty }

    public enum enumRBAxis { XAX1 = 0, ZAX1, ROT1, ARM1, ARM2 };

    public enum enumRobotAxis { Arm1, Arm2, Rot, Zax, Xax, ExtX };

    public enum enumRobotStatus : int { InPos, Moving, Pause };
    public enum enumRobotSpeedMode : int { Normal = 0, Maintenace = 1 };

    public enum enumRobotAction : int { Standby = 0, Load = 1, Unlaod = 2, Flip =3}

    public enum RobotPos
    {
        NotORGN = -1,
        Home = 0,
        LoadPort1 = 1,
        LoadPort2,
        LoadPort3,
        LoadPort4,
        LoadPort5,
        LoadPort6,
        LoadPort7,
        LoadPort8,
        BarCodeReader,
        AlignerA,
        AlignerB,
        BufferA,
        BufferB,

        Equipment1, Equipment2, Equipment3, Equipment4
    }

    public enum enumRobotDataType : int { DTRB, DTUL, DEQU, DRCI, DRCS, DMNT, DMPR, DCFG, DAPM };
    public enum enumInit { INIT = 0, Total };
    public enum enumOrgn { ORGN = 0, Total };
    public enum enumProc { RSTA = 0, STOP, PAUS, SSPD, EXCT, Total };
    public enum enumMove { ORGN = 0, HOME, EXTD, LOAD, UNLD, TRNS, EXCH, CLMP, UCLM, WMAP, STEP, Total };

    public enum enumArmFunction : int { NONE = 0, NORMAL = 1, I = 2, FRAME = 3 }
    public enum enumRobotCommand
    {
        /// <summary>
        /// 原點搜尋, [ORGN]
        /// </summary>
        Orgn,
        /// <summary>
        /// 移至Stage standby位置, [HOME]
        /// </summary>
        Home,
        /// <summary>
        /// 伸手臂, [EXTD]
        /// </summary>
        ExtendingArm,
        /// <summary>
        /// 取片, [LOAD]
        /// </summary>
        TakeWafer,
        /// <summary>
        /// 放片, [UNLD]
        /// </summary>
        PutWafer,
        /// <summary>
        /// 傳送, [TRNS]
        /// </summary>
        TransferWafer,
        /// <summary>
        /// wafer transfer2, [UTRN]
        /// </summary>
        TransferWafer2,
        /// <summary>
        /// 交換片, [EXCH]
        /// </summary>
        ExchangeArm,
        /// <summary>
        /// 手臂真空開啟, [CLMP]
        /// </summary>
        VacuumOn,
        /// <summary>
        /// 手臂真空關閉, [UCLM]
        /// </summary>
        VacuumOff,
        /// <summary>
        /// 掃片, [WMAP]
        /// </summary>
        Mapping,
        /// <summary>
        ///Wafer Carry Out, [MGET]
        /// </summary>
        CarryOutWafer,
        /// <summary>
        ///Wafer Carry Out(the fist half), [MGT1]
        /// </summary>
        CarryOutWafer1,
        /// <summary>
        ///Wafer Carry Out(the second half), [MGT2]
        /// </summary>
        CarryOutWafer2,
        /// <summary>
        ///Wafer Carry In, [MPUT]
        /// </summary>
        CarryInWafer,
        /// <summary>
        ///Wafer Carry In(the fist half), [MPT1]
        /// </summary>
        CarryInWafer1,
        /// <summary>
        ///Wafer Carry In(the second half), [MPT2]
        /// </summary>
        CarryInWafer2,
        /// <summary>
        ///插槽中的晶圓存在檢查, [WCHK]
        /// </summary>
        CheckWaferInSlot,
        /// <summary>
        ///X方向補償, [ALEX]
        /// </summary>
        ALEX,
        /// <summary>
        ///執行補償操作後, [ALLD]
        /// </summary>
        ALLD,
        /// <summary>
        ///執行補償操作後, [ALUL]
        /// </summary>
        ALUL,
        /// <summary>
        ///執行補償操作後, [ALGT]
        /// </summary>
        ALGT,
        /// <summary>
        ///執行補償操作後, [ALEA]
        /// </summary>
        ALEA,
        /// <summary>
        ///執行補償操作後, [ALMV]
        /// </summary>
        ALMV,
        /// <summary>
        ///執行補償操作後, [ZMOV]
        /// </summary>
        ZMOV,
        /// <summary>
        ///執行補償操作後, [MSSC]
        /// </summary>
        MSSC,
        /// <summary>
        ///執行補償操作後, [EXCC]
        /// </summary>
        EXCC,
        /// <summary>
        ///執行補償操作後, [FLIP]
        /// </summary>
        Flip,
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
        SetMode,
        /// <summary>
        /// 記憶體資料存到硬碟, [WTDT]
        /// </summary>
        StoreDate,
        /// <summary>
        /// 讀取資料, [RTDT]
        /// </summary>
        ReadData,
        /// <summary>
        /// 傳送資料, [TRDT]
        /// </summary>
        TransferDate,
        /// <summary>
        /// 速度設定, [SSPD]
        /// </summary>
        SetSpeed,
        /// <summary>
        /// 設置當前位置, [SPOS]
        /// </summary>
        SetPos,
        /// <summary>
        /// 設定扭力, [TORQ]
        /// </summary>
        SetTorque,
        /// <summary>
        /// 電機勵磁控制, [EXCT]
        /// </summary>
        ExcitationControl,
        /// <summary>
        /// 電機勵磁控制, [BRAK]
        /// </summary>
        BRAK,
        /// <summary>
        /// 電機勵磁控制, [SVAC]
        /// </summary>
        SVAC,
        /// <summary>
        /// 電機勵磁控制, [SPOT]
        /// </summary>
        SPOT,
        /// <summary>
        /// 問機況, [STAT]
        /// </summary>
        GetStatus,
        /// <summary>
        /// 問IO, [GPIO]
        /// </summary>
        GetIO,
        /// <summary>
        /// 問掃片結果, [GMAP]
        /// </summary>
        GetMappingData,
        /// <summary>
        /// 問Mapping的Wafer資料, [RAC2]
        /// </summary>
        GetRAC2,
        /// <summary>
        /// 問版本, [GVER]
        /// </summary>
        GetVersion,
        /// <summary>
        /// 取得log, [GLOG]
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
        /// 問系統時間, [GPOS]
        /// </summary>
        GetPos,
        /// <summary>
        /// 問系統時間, [GWID]
        /// </summary>
        GetWaferID,
        /// <summary>
        /// , [TDST]
        /// </summary>
        TDST,
        /// <summary>
        /// , [MOVT]
        /// </summary>
        MOVT,
        /// <summary>
        /// , [GVAC]
        /// </summary>
        GVAC,
        /// <summary>
        /// , [EXST]
        /// </summary>
        EXST,
        /// <summary>
        /// , [GCLM]
        /// </summary>
        GCLM,
        /// <summary>
        /// , [GCHK]
        /// </summary>
        GCHK,
        /// <summary>
        /// , [WAIT]
        /// </summary>
        WAIT,
        /// <summary>
        /// , [TDSA]
        /// </summary>
        TDSA,
        /// <summary>
        /// , [GAEX]
        /// </summary>
        GAEX,
        /// <summary>
        /// , [GALD]
        /// </summary>
        GALD,
        /// <summary>
        /// , [SDRV]
        /// </summary>
        SDRV,
        /// <summary>
        /// , [DTRB.STDT]
        /// </summary>
        DtrbSTDT,
        /// <summary>
        /// , [DTUL.STDT]
        /// </summary>
        DtulSTDT,
        /// <summary>
        /// , [DEQU.STDT]
        /// </summary>
        DequSTDT,
        /// <summary>
        /// , [DRCI.STDT]
        /// </summary>
        DrciSTDT,
        /// <summary>
        /// , [DRCS.STDT]
        /// </summary>
        DrcsSTDT,
        /// <summary>
        /// , [DMNT.STDT]
        /// </summary>
        DmntSTDT,
        /// <summary>
        /// , [DMPR.STDT]
        /// </summary>
        DmprSTDT,
        /// <summary>
        /// , [DMPR.STDT]
        /// </summary>
        DcfgSTDT,
        /// <summary>
        /// , [DTRB.GTDT]
        /// </summary>
        DtrbGTDT,
        /// <summary>
        /// , [DTUL.GTDT]
        /// </summary>
        DtulGTDT,
        /// <summary>
        /// , [DEQU.GTDT]
        /// </summary>
        DequGTDT,
        /// <summary>
        /// , [DRCI.GTDT]
        /// </summary>
        DrciGTDT,
        /// <summary>
        /// , [DRCS.GTDT]
        /// </summary>
        DrcsGTDT,
        /// <summary>
        /// , [DMNT.GTDT]
        /// </summary>
        DmntGTDT,
        /// <summary>
        /// , [DMPR.GTDT]
        /// </summary>
        DmprGTDT,
        /// <summary>
        /// , [DMPR.GTDT]
        /// </summary>
        DcfgGTDT,


        DapmGTDT,
        DapmSTDT,

        ABSC,

        /// <summary>
        /// , [XAX1.STEP]
        /// </summary>
        Xax1Step,
        /// <summary>
        /// , [ZAX1.STEP]
        /// </summary>
        Zax1Step,
        /// <summary>
        /// , [ROT1.STEP]
        /// </summary>
        Rot1Step,
        /// <summary>
        /// , [ARM1.STEP]
        /// </summary>
        Arm1Step,
        /// <summary>
        /// , [ARM2.STEP]
        /// </summary>
        Arm2Step,

        /// <summary>
        /// , [XAX1.GPOS]
        /// </summary>
        Xax1Gpos,
        /// <summary>
        /// , [ZAX1.GPOS]
        /// </summary>
        Zax1Gpos,
        /// <summary>
        /// , [ROT1.GPOS]
        /// </summary>
        Rot1Gpos,
        /// <summary>
        /// , [ARM1.GPOS]
        /// </summary>
        Arm1Gpos,
        /// <summary>
        /// , [ARM2.GPOS]
        /// </summary>
        Arm2Gpos,

        Xax1Extd,
        Zax1Extd,
        Rot1Extd,
        Arm1Extd,
        Arm2Extd,
        Arm1Clmp,
        Arm2Clmp,
        Arm1Uclm,
        Arm2Uclm,
        // 控制PIN伸縮,0是縮
        MTPN,//2024.4.10針對TRB2新增
        // 詢問PIN的狀態 eTRB2.GTPN:xxxx/xxxx
        GTPN,//2024.4.10針對TRB2新增

        ClientConnected,
        Max
    }
    public enum enumRobotSignalTable : int
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
        Max
    }
    public enum enumDEQU_15_waferSearch : int
    {
        None = 0,
        UpperFinger, LowerFinger,
        UpperWrist, LowerWrist,
        UpperLowerFinger, UpperLowerWrist,
        UpperFingerLowerWrist,
        UpperWristLowerWrist
    }


}
