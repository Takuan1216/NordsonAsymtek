using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Event;
using System;
using System.Collections.Generic;

namespace RorzeUnit.Interface
{
    public interface I_RC5X0_IO
    {
        bool Simulate { get; }
        bool Connected { get; }
        int BodyNo { get; }
        bool Disable { get; }
        string VersionData { get; }
        bool IsError { get; }
        int[] GetFanGrev { get; }
        int[] GetSenGprs { get; }
        //--------------------------------------------------
        event EventHandler OnInitializationComplete;
        event EventHandler OnInitializationFail;

        event OccurErrorEventHandler OnOccurStatErr;
        event OccurErrorEventHandler OnOccurCancel;
        event OccurErrorEventHandler OnOccurCustomErr;
        event OccurErrorEventHandler OnOccurErrorRest;

        event NotifyGDIOEventHandler OnNotifyEvntGDIO;
        event EventHandler<bool> OnNotifyEvntCNCT;
        event EventHandler<int[]> OnOccurGPRS;     //壓差計
        //--------------------------------------------------
        void Open();
        void StopPollingThread();
        //--------------------------------------------------
        void INIT();
        void RSTA();
        //--------------------------------------------------
        void InitW();
        void EvntW();
        void MoveW(int n);
        void StopW(int n);
        void RstaW();
        void SdobW(int nID, int nBit, bool bOn);
        void SdouW(int nID, int nBit1, bool bOn);
        void SdouW(int nID, int nBit1, int nBit2);
        bool GdioW(int nID, int Bit);   
        //--------------------------------------------------
        bool GetGDIO_InputStatus(int nHCLID, int nBit);
        bool GetGDIO_OutputStatus(int nHCLID, int nBit);
        void SetGDIO_InputStatus(int nHCLID, int nBit, bool bOn);
        //--------------------------------------------------
        Dictionary<int, string> m_dicCancel { get; }
        Dictionary<int, string> m_dicController { get; }
        Dictionary<int, string> m_dicError { get; }

    }
}
