using RorzeComm;
using RorzeComm.Threading;
using RorzeUnit.Class.E84.Enum;
using RorzeUnit.Class.E84.Event;
using RorzeUnit.Event;
using RorzeUnit.Interface;
using System;

namespace RorzeUnit.Class.E84
{
    /// <summary>
    /// Null Object for LP with built-in E84.
    /// Handles only AutoMode toggle; all hardware signal properties return false.
    /// </summary>
    public class LPBuiltInE84 : I_E84
    {
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }

        private bool m_bAutoMode = false;
        public bool GetAutoMode { get { return m_bAutoMode; } }

        private SPollingThread _pollingAuto;

        public LPBuiltInE84(int bodyNo, bool disable)
        {
            BodyNo = bodyNo;
            Disable = disable;

            _pollingAuto = new SPollingThread(100);
            _pollingAuto.DoPolling += threadPolling_DoPolling;
            _pollingAuto.Set();
        }

        private void threadPolling_DoPolling()
        {
            try
            {
                DoAutoProcessing?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                _pollingAuto.Reset();
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        public void Close()
        {
            _pollingAuto.Close();
        }

        public bool SetAutoMode(bool bOn)
        {
            m_bAutoMode = bOn;
            OnAceessModeChange?.Invoke(this, new E84ModeChangeEventArgs(bOn));
            return true;
        }

        // ¢w¢w Interface members ¢w¢w all no-op or false ¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w

        public enumE84Step E84Step { get; set; }
        public enumE84Proc E84_Proc { get; set; }
        public bool ResetFlag { get; set; }
        public DateTime[] TmrTP { get; set; } = new DateTime[5];
        public DateTime TmrTD { get; set; }
        public int[] SetTpTime { set { } }
        public int HCLID { get { return -1; } }

        public bool isValidOn  { get { return false; } }
        public bool isCs0On    { get { return false; } }
        public bool isCs1On    { get { return false; } }
        public bool isAvblOn   { get { return false; } }
        public bool isTrReqOn  { get { return false; } }
        public bool isBusyOn   { get { return false; } }
        public bool isComptOn  { get { return false; } }
        public bool isContOn   { get { return false; } }

        public bool isSetLReq  { get { return false; } }
        public bool isSetUReq  { get { return false; } }
        public bool isSetVa    { get { return false; } }
        public bool isSetReady { get { return false; } }
        public bool isSetVs0   { get { return false; } }
        public bool isSetVs1   { get { return false; } }
        public bool isSetAvbl  { get { return false; } }
        public bool isSetEs    { get { return false; } }

        public void SetLReq(bool bOn)  { }
        public void SetUReq(bool bOn)  { }
        public void SetVa(bool bOn)    { }
        public void SetReady(bool bOn) { }
        public void SetVs0(bool bOn)   { }
        public void SetVs1(bool bOn)   { }
        public void SetAvbl(bool bOn)  { }
        public void SetEs(bool bOn)    { }

        public bool ResetError() { return true; }
        public void ClearSignal() { }

        public bool isTimeoutTD()  { return false; }
        public bool isTimeoutTP1() { return false; }
        public bool isTimeoutTP2() { return false; }
        public bool isTimeoutTP3() { return false; }
        public bool isTimeoutTP4() { return false; }
        public bool isTimeoutTP5() { return false; }

        public dlgb_v dlgAreaTrigger { get; set; }
        public bool AreaTrigger { get { return false; } }

        public event E84ModeChangeEventHandler OnAceessModeChange;
        public event OccurErrorEventHandler OnOccurError;
        public event OccurErrorEventHandler OnOccurErrorRest;
        public event EventHandler OnOccurE84InIOChange;
        public event AutoProcessingEventHandler DoAutoProcessing;
    }
}
