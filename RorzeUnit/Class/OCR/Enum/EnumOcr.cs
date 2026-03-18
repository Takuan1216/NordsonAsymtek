using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.OCR.Enum
{
    public enum enumName { A1 = 0, A2 = 1, B1 = 2, B2 = 3 };

    public enum enumOcrSignalTable : int
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

    public enum enumOcrMode : int { Initializing, Remote, Maintenance }

    public enum enumMoveStatus : int { InPos, Moving, Pause };

    public enum enumOcrCommand
    {
        CNCT,
        INIT,
        STAT,
        RSTA,
        EVNT,
        GVER,
        STIM,
        GTIM,

        SARE,
        LORE,
        GRFN,
        SEST,

        GIMG,
        LIMG,

        SPAR,
        GPAR,

        EXEC,
        AUTN,
        DATA,

        IPST

    }

    public enum enumCustomErr : int
    {
        AckTimeout,
        SendCommandFailure,
        MotionTimeout,
        MotionAbnormal,
    }
}
