using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.RC500.RCEnum
{
    public enum enumRC5X0Mode { Initializing, Remote, Maintenance, Recovery }
    public enum enumRC5X0Status : int { InPos, Moving, Pause };
    public enum enumRC550Axis : int { AXS1 = 0, AXS2, AXS3, AXS4, AXS5, AXS6, None };
    public enum enumRC500SignalTable : int
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
    public enum enumRC5X0Command_IO
    {
        /// <summary>
        /// 事件自動回報設定, [EVNT]
        /// </summary>
        EVNT,
        /// <summary>
        /// Reset, [RSTA]
        /// </summary>
        RSTA,
        /// <summary>
        /// 控制器初始化, [INIT]
        /// </summary>
        INIT,
        MOVE,
        STOP,
        /// <summary>
        /// 記憶體資料存到硬碟, [WTDT]
        /// </summary>
        WTDT,
        RTDT,
        /// <summary>
        /// 問機況, [STAT]
        /// </summary>
        STAT,
        GPIO,
        SPOT,
        SPTM,
        /// <summary>
        /// 問版本, [GVER]
        /// </summary>
        GVER,
        GLOG,
        /// <summary>
        /// 設定系統時間, [STIM]
        /// </summary>
        STIM,
        /// <summary>
        /// 問系統時間, [GTIM]
        /// </summary>
        GTIM,
        GREV,
        GPRS,
        GDIO,
        SDOU,
        SDOB,
        /// <summary>
        /// Client連線成功, [CNCT]
        /// Rorze to Host only
        /// </summary>
        CNCT,
        Max,
    }
    public enum enumRC5X0Command_Motion//RC550 軸卡
    {
        /// <summary>
        /// 原點搜尋, [ORGN]
        /// </summary>
        ORGN,
        /// <summary>
        /// 移動原點, [HOME]
        /// </summary>
        HOME,
        /// <summary>
        /// 絕對移動, [MABS]
        /// </summary>
        MABS,
        /// <summary>
        /// 相對移動, [MREL]
        /// </summary>
        MREL,
        /// <summary>
        /// 手臂真空開啟, [CLMP]
        /// </summary>
        CLMP,
        /// <summary>
        /// 手臂真空關閉, [UCLM]
        /// </summary>
        UCLM,
        /// <summary>
        /// 事件自動回報設定, [EVNT]
        /// </summary>
        EVNT,
        /// <summary>
        /// Reset, [RSTA]
        /// </summary>
        RSTA,
        /// <summary>
        /// 控制器初始化, [INIT]
        /// </summary>
        INIT,
        /// <summary>
        /// 軟急停, [STOP]
        /// </summary>
        STOP,
        /// <summary>
        /// 動作暫停, [PAUS]
        /// </summary>
        PAUS,
        /// <summary>
        /// 模式切換, [MODE]
        /// </summary>
        MODE,
        /// <summary>
        /// 記憶體資料存到硬碟, [WTDT]
        /// </summary>
        WTDT,
        /// <summary>
        /// 記憶體資料從硬碟讀出, [RTDT]
        /// </summary>
        RTDT,
        /// <summary>
        /// 速度設定, [SSPD]
        /// </summary>
        SSPD,
        /// <summary>
        /// 馬達機磁, [EXCT]
        /// </summary>
        EXCT,
        /// <summary>
        /// 設定馬達扭矩, [TORQ]
        /// </summary>
        TORQ,
        /// <summary>
        /// 問機況, [STAT]
        /// </summary>
        STAT,
        /// <summary>
        /// 問IO, [GPIO]
        /// </summary>
        GPIO,
        /// <summary>
        /// 問版本, [GVER]
        /// </summary>
        GVER,
        /// <summary>
        /// 沒用過, [GLOG]
        /// </summary>
        GLOG,
        /// <summary>
        /// 設定系統時間, [STIM]
        /// </summary>
        STIM,
        /// <summary>
        /// 問系統時間, [GTIM]
        /// </summary>
        GTIM,
        /// <summary>
        /// 問位置, [GPOS]
        /// </summary>
        GPOS,
        /// <summary>
        /// 沒用過, [TDST]
        /// </summary>
        TDST,
        /// <summary>
        /// 沒用過, [GTDT]
        /// </summary>
        GTDT,
        /// <summary>
        /// 能問到encode, [GPSX]
        /// </summary>
        GPSX,
        /// <summary>
        /// IO輸出,[SPOT]
        /// </summary>
        SPOT,
        /// <summary>
        /// 沒用過, [GTAD]
        /// </summary>
        GTAD,
        /// <summary>
        /// Client連線成功, [CNCT]
        /// Rorze to Host only
        /// </summary>
        CNCT,
        /// <summary>
        /// 相對移動, [STEP]
        /// </summary>
        STEP,
        /// <summary>
        /// Mapping
        /// </summary>
        SMAP,
        /// <summary>
        /// Mapping
        /// </summary>
        GMAP,
        /// <summary>
        /// Mapping
        /// </summary>
        GetDMPR,
        SetDMPR,
        Max,
    }
}
