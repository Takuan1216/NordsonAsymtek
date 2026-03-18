using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RorzeUnit.Class.E84;
using static RorzeUnit.Class.E84.SB058_E84;
using RorzeUnit.Class.E84.Enum;
using RorzeUnit.Class.E84.Event;
using RorzeUnit.Event;
using RorzeComm;

namespace RorzeUnit.Interface
{
    public interface I_E84
    {
        enumE84Step E84Step { get; set; }
        enumE84Proc E84_Proc { get; set; }
        bool ResetFlag { get; set; }
        DateTime[] TmrTP { get; set; }
        DateTime TmrTD { get; set; }

        int[] SetTpTime { set; }
        int BodyNo { get; }

        bool GetAutoMode { get; }
        bool SetAutoMode(bool bOn);

        bool Disable { get; }
        int HCLID { get; }

        bool isValidOn { get; }
        bool isCs0On { get; }
        bool isCs1On { get; }
        bool isAvblOn { get; }
        bool isTrReqOn { get; }
        bool isBusyOn { get; }
        bool isComptOn { get; }
        bool isContOn { get; }

        bool isSetLReq { get; }
        bool isSetUReq { get; }
        bool isSetVa { get; }
        bool isSetReady { get; }
        bool isSetVs0 { get; }
        bool isSetVs1 { get; }
        bool isSetAvbl { get; }
        bool isSetEs { get; }

        void SetLReq(bool bOn);
        void SetUReq(bool bOn);
        void SetVa(bool bOn);
        void SetReady(bool bOn);

        void SetVs0(bool bOn);
        void SetVs1(bool bOn);
        void SetAvbl(bool bOn);
        void SetEs(bool bOn);

        bool ResetError();

        void ClearSignal();

        bool isTimeoutTD();
        bool isTimeoutTP1();
        bool isTimeoutTP2();
        bool isTimeoutTP3();
        bool isTimeoutTP4();
        bool isTimeoutTP5();

        //event=============================================
        event E84ModeChangeEventHandler OnAceessModeChange;
        event OccurErrorEventHandler OnOccurError;
        event OccurErrorEventHandler OnOccurErrorRest;
        event EventHandler OnOccurE84InIOChange;
        event AutoProcessingEventHandler DoAutoProcessing;
        //=============================================
        dlgb_v dlgAreaTrigger { get; set; }
        bool AreaTrigger { get; }
    }
}
