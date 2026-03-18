using RorzeUnit.Class;
using RorzeUnit.Class.RC500;

using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RorzeApi;
using RorzeComm;
using RorzeUnit.Class.RC500.RCEnum;

namespace RorzeUnit.Interface
{
    public interface I_RC5X0_Motion
    {
        bool Simulate { get; }
        bool Connected { get; }
        int BodyNo { get; }
        bool Disable { get; }
        string VersionData { get; }
        enumRC5X0Status InPos { get; }
        bool IsError { get; }
        bool IsOrgnComplete { get; }
        bool IsProcessing { get; }
        bool IsMoving { get; }
        int GetSpeed { get; }
        string[] GetCurrentDMPR { get; }
        //--------------------------------------------------
        event EventHandler<bool> OnORGNComplete;
        event EventHandler<bool> OnMoveStepComplete;
        event EventHandler<string> OnSensorChange;

        event OccurErrorEventHandler OnOccurStatErr;
        event OccurErrorEventHandler OnOccurCancel;
        event OccurErrorEventHandler OnOccurCustomErr;
        event OccurErrorEventHandler OnOccurErrorRest;

        event MessageEventHandler OnReadData;
        event NotifyGPIOEventHandler OnIOChange;

        event EventHandler<bool> OnNotifyEvntCNCT;
        //--------------------------------------------------
        void Open();
        //--------------------------------------------------
        void INIT();
        void ORGN(int nAxis);
        void STEP(int nAxis, int nPluse);
        void MABS(int nAxis, int nPluse);
        void MREL(int nAxis, int nPluse);
        void RSTA(int nReset);
        void STOP();
        //--------------------------------------------------
        void OrgnW(int nTimeout, enumRC550Axis axis);
        void AxisMabsW(int nTimeout, enumRC550Axis axis, int pluse, int spd = 0);
        void AxisMrelW(int nTimeout, enumRC550Axis axis, int pluse, int spd = 0);
        void ResetW(int nTimeout, int nReset = 0);
        void StopW(int nTimeout);
        void PausW(int nTimeout);
        void SspdW(int nTimeout, int nSpeed);
        void ExctW(int nTimeout, int nVariable);
        void GpioW(int nTimeout = 1000);
        void EventW(int nTimeout);
        void InitW(int nTimeout);
        void StimW(int nTimeout);
        void GtdtW(int nTimeout, int nVariable);
        void WtdtW(int nTimeout);
        void AxisGposW(int nTimeout, enumRC550Axis axis);
        void GmapW(int nTimeout, int n = -1);
        void SmapW(int nTimeout, int n = -1);
        void SmapW_BypassCancel(int n = -1);
        void GetDmprW(int nTimeout, int n);
        void SetDmprW(int nTimeout, int n, string strDat);

        //--------------------------------------------------
        void ResetChangeModeCompleted();
        void WaitChangeModeCompleted(int nTimeout);

        void ResetProcessCompleted();
        void WaitProcessCompleted(int nTimeout);

        void ResetInPos();
        void WaitInPos(int nTimeout);
        void WaitInPos(int nTimeout, enumRC550Axis axis, int nPulse);

        void ResetOrgnSinal();
        void WaitOrgnCompleted(int TimeOut);
        //--------------------------------------------------
        int GetPulse(enumRC550Axis axis, bool oGPOS = false);
        bool GetOrgnSensor(enumRC550Axis axis);
        bool GetInput(int nBit);
        void SetInput(int nBit, bool bOn);
        bool GetOutput(int nBit);
        void SetOutput(int nBit, bool bOn);

        Dictionary<int, string> m_dicCancel { get; }
        Dictionary<int, string> m_dicController { get; }
        Dictionary<int, string> m_dicError { get; }
        string GetGmap(int n = -1);
    }
}
