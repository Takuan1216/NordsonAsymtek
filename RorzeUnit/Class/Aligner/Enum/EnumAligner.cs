using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Aligner.Enum
{
    public enum eAxis { XAX1 = 0, YAX1 = 1, ZAX1 = 2, ROT1 = 3 };
    public enum eInit { INIT = 0, Total };
    public enum eOrgn { ORGN = 0, Total };
    public enum eProc { RSTA = 0, STOP, PAUS, SSPD, Total };
    public enum eMove { ORGN = 0, HOME, EXTD, ALGN, CALN, READ, CALM, UCLM, STEP, Total };
    public enum eClampWorkVacuumStatus { WorksExist,WorksNoExist,VacuumError };

    public enum enumAlignerMode
    {
        Initializing,
        Remote,
        Maintenance,
        Recovery
    }
    public enum enumAlignerStatus : int { InPos, Moving, Pause };

    public enum enumAlignerSignalTable : int
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
    public enum enumAlignerCommand
    {
        /// <summary>
        /// 原點搜尋, [ORGN]
        /// </summary>
        Orgn,
        /// <summary>
        /// 回Home位置, [HOME]
        /// </summary>
        Home,
        /// <summary>
        /// Dock Foup [CLMP]
        /// </summary>
        Clamp,
        /// <summary>
        /// UnDock Foup, [UCLM]
        /// </summary>
        UnClamp,
        /// <summary>
        /// Wafer校正, [ALGN]
        /// </summary>
        Alignment,
        /// <summary>
        /// 鏡頭校正, [CALN]
        /// </summary>
        CameraAlignment,
        /// <summary>
        /// R軸轉動 [EXTD]
        /// </summary>
        RotationExtd,
        /// <summary>
        /// 讀CarrierID [READ]
        /// </summary>
        ReadID,
        /// <summary>
        /// 事件自動回報設定, [EVNT]
        /// </summary>
        Event,
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
        /// 動作暫停, [STEP]
        /// </summary>
        RotationStep,
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
        /// 速度設定, [SSPD]
        /// </summary>
        Speed,
        /// <summary>
        /// 問機況, [STAT]
        /// </summary>
        Status,
        /// <summary>
        /// 問IO, [GPIO]
        /// </summary>
        GetIO,
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
        /// 問encoder [XAX1.GPOS]
        /// </summary>
        GetXAxisPos,
        /// <summary>
        /// 問encoder [YAX1.GPOS]
        /// </summary>
        GetYAxisPos,
        /// <summary>
        /// 問encoder [ZAX1.GPOS]
        /// </summary>
        GetZAxisPos,
        /// <summary>
        /// 問encoder [ROT1.GPOS]
        /// </summary>
        GetRAxisPos,
        /// <summary>
        /// 取得CarrierType, [GWID]
        /// </summary>
        GetType,
        /// <summary>
        ///記錄刻痕停止位置, [TDST]
        /// </summary>
        NotchStopPos,
        /// <summary>
        /// 取得Sensor數值 [GTAD]
        /// </summary>
        GetSensorValue,
        /// <summary>
        /// 取得真空壓力 [GPRS]
        /// </summary>
        GetVacuumValue,
        /// <summary>
        /// 取得Size [GSIZ]
        /// </summary>
        GetSize,
        /// <summary>
        /// 設定Size [SSIZ]
        /// </summary>
        SetSize,
        /// <summary>
        /// ID Read Results [GTID]
        /// </summary>
        GetID,
        /// <summary>
        /// Work Position Information Acuquisition [GTMP]
        /// </summary>
        GetMP,
        /// <summary>
        /// 讀DPRM [DPRM.GTDT]
        /// </summary>
        GetDPRM,
        /// <summary>
        /// 讀DPRM [DCST.GTDT]
        /// </summary>
        GetDCST,
        /// <summary>
        /// 讀DPRM [DCST.STDT]
        /// </summary>
        SetDCST,
        ClientConnected,
        /// <summary>
        /// 設定Output bit on/off [SPOT]
        /// </summary>
        SetOutputBit,
        /// <summary>
        /// 馬達機磁 [EXCT]
        /// </summary>
        Exct,
        RAxisAbsolute,
        RAxisRelative,
        Max
    }

}
