using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.Camera.Enum
{
    public enum enumStat1_Mode { Initializing, Remote, Maintenance, Recovery }
    public enum enumStat4_Move : int { InPos, Moving, Pause };

    public enum enumSignalTable : int
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
        OriginCompleted,
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
    public enum enumCommand
    {
        None,
        CNCT,
        INIT,
        STAT,
        EVNT,
        CLLC,
        GTLC,
        EXIS,
        RSTA,
        STIM,
        GTIM,
        RSET,
        STLV,
        GTLV,
        SVLV,
        STSN,
        GTSN,
        STET,
        GTET,
        SVET,
        STGN,
        GTGN,
        SVGN,
        SARE,
        RRCP,
    }


    public enum enumCustomError : int
    {
        Disconnect = 0,//不能亂改順序有固定 reference:enumAlignerError 0x20C0
        Status_Error,
        SendCommandFailure,
        SendCommandAckTimeout,
        InitialTimeout,
        InitialFailure,
        OriginTimeout,
        OriginFailure,
        MotionTimeout,
        MotionAbnormal,







    }
    public enum enumCustomWarning : int
    {

    }
}
