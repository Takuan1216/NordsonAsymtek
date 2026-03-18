using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.E84.Enum
{
    public enum enumE84Step 
    { 
        Ready = 0, 
        CsOn, 
        ValidOn, 
        TrReq, 
        BusyTp3, 
        BusyTp4, 
        ComptOn, 
        TimeoutTD, 
        TimeoutTp1, 
        TimeoutTp2, 
        TimeoutTp3, 
        TimeoutTp4, 
        TimeoutTp5, 
        SignalError,
        StageBusy,
        LightCurtainBusyOn,
        LightCurtain      
    };
    public enum enumE84Proc { Loading = 0, Unloading };
    public enum enumTpTime { TP1 = 0, TP2, TP3, TP4, TP5 };
    public enum enumE84OutPut { LReq = 0, UReq = 1, Va = 2, Ready = 3, Vs0 = 4, Vs1 = 5, Avbl = 6, Es = 7 }
    public enum enumE84Warning
    {
        TD0_TimeOut = 129,
        TP1_TimeOut = 130,
        TP2_TimeOut = 131,
        TP3_TimeOut = 132,
        TP4_TimeOut = 133,
        TP5_TimeOut = 134,
        SignalError = 135,
        StageIsBusy = 136,
        LightCurtain = 137,
        LightCurtainBusyOn = 138,

    }
}
