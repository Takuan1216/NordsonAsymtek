using System;
using RorzeUnit.Event;
using RorzeComm.Log;
using RorzeComm;
using RorzeComm.Threading;
using System.Collections.Concurrent;
using RorzeUnit.Class.Buffer.Event;
using RorzeUnit.Interface;
using System.Runtime.CompilerServices;
using System.Reflection;
using RorzeUnit.Class.Buffer.Enum;

namespace RorzeUnit.Class.Buffer
{
    public class SSBuffer : I_Buffer
    {
        #region =========================== private ============================================
        private SLogger _logger = SLogger.GetLogger("CommunicationLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[Buffer] : {1}  at line {2} ({3})", BodyNo, strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }
        private SWafer[] _waferList;

        private bool m_bRobotExtand = false;
        private string m_strSlotEnable;
        #endregion
        //==============================================================================
        #region =========================== public =============================================
        public bool Simulate { get; private set; }
        public int BodyNo { get; private set; }
        public bool Disable { get; private set; }
        public bool ProcessStart { get; private set; }
        public SWafer GetWafer(int nIndex)
        {
            if (nIndex > m_strSlotEnable.Length)
            {
                return null;
            }
            else if (m_strSlotEnable[nIndex] == '0')//disable
            {
                return null;
            }
            else
            {
                return _waferList[nIndex];
            }
        }
        public void SetWafer(int nIndex, SWafer wafer)
        {
            _waferList[nIndex] = wafer;
            OnAssignWaferData?.Invoke(this, new WaferDataEventArgs(wafer, nIndex + 1));
        }
        public int GetWaferInSlot(SWafer wafer)
        {
            int nSlot = -1;
            for (int i = 0; i < m_strSlotEnable.Length; i++)
            {
                if (m_strSlotEnable[i] == '0') continue;
                if (GetWafer(i) == null) continue;
                if (GetWafer(i).Owner == wafer.Owner && GetWafer(i).Slot == wafer.Slot)
                    nSlot = i + 1;
            }
            return nSlot;
        }

        public ConcurrentQueue<SWafer> queCommand { get; set; }
        public ConcurrentQueue<SWafer> quePreCommand { get; set; }
        public bool IsRobotExtend { get { return m_bRobotExtand; } }
        public bool SetRobotExtend { set { m_bRobotExtand = value; } }

        public int HardwareSlot { get; private set; }
        public bool IsWaferDetectOn(int nIndex)
        {
            if (nIndex > m_strSlotEnable.Length)
            {
                return false;
            }
            else if (m_strSlotEnable[nIndex] == '0')//disable
            {
                return false;
            }
            else
            {
                if (dlgSlotWaferExist[nIndex] != null)
                    return dlgSlotWaferExist[nIndex]();
                else return false;
            }
        }
        public int GetEmptySlot()
        {
            if (Disable) return -1;
            for (int i = 0; i < _waferList.Length; i++)
            {
                if (m_strSlotEnable[i] == '0') continue;
                if (IsWaferDetectOn(i) == false && _waferList[i] == null)//sensor與帳
                    return i + 1;
            }
            return 0;
        }
        public int GetWaferCount()
        {
            int n = 0;
            for (int i = 0; i < m_strSlotEnable.Length; i++)
            {
                if (m_strSlotEnable[i] == '0') continue;
                if (IsWaferDetectOn(i) == true || GetWafer(i) != null)//sensor與帳
                    n++;
            }
            return n;
        }
        public SWafer.enumWaferSize WaferType { get; private set; }
        #endregion
        //==============================================================================
        #region =========================== Event ==============================================
        public event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessEnd;
        public event EventHandler OnProcessAbort;
        public event AutoProcessingEventHandler DoAutoProcessing;
        #endregion
        #region =========================== Thread =============================================
        private SPollingThread _pollingAuto;
        #endregion     
        #region =========================== Delegate ===========================================
        public dlgb_v[] dlgSlotWaferExist { get; set; }
        public dlgb_v dlgAroundTrigger { get; set; }
        public dlgv_wafer AssignToRobotQueue { get; set; }//丟給robot作排程      
        #endregion
        //==============================================================================



        public SSBuffer(int nBodyNo, bool bDisable, bool bSimulate, string strSlotEnable, SWafer.enumWaferSize eWaferType,int nHardwareSlot)
        {
            Disable = bDisable;
            Simulate = bSimulate;
            BodyNo = nBodyNo;
            WaferType = eWaferType;
            m_strSlotEnable = strSlotEnable;

            int nSlotNum = m_strSlotEnable.Length;
            if (nSlotNum > 0)
            {
                _waferList = new SWafer[nSlotNum];
                dlgSlotWaferExist = new dlgb_v[nSlotNum];
            }
            HardwareSlot = nHardwareSlot;

            Cleanjobschedule();//20240704

            this._pollingAuto = new SPollingThread(1);
            this._pollingAuto.DoPolling += _pollingAuto_DoPolling;

        }
        ~SSBuffer()
        {
            this._pollingAuto.Close();
            this._pollingAuto.Dispose();
        }

        #region AutoProcess
        public void AutoProcessStart()
        {
            ProcessStart = true;
            this._pollingAuto.Set();
            OnProcessStart?.Invoke(this, new EventArgs());
        }
        public void AutoProcessEnd()
        {
            this._pollingAuto.Reset();
            ProcessStart = false;
            OnProcessEnd?.Invoke(this, new EventArgs());
        }
        private void _pollingAuto_DoPolling()
        {
            try
            {
                DoAutoProcessing?.Invoke(this);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
                _pollingAuto.Reset();
                OnProcessAbort?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
                _pollingAuto.Reset();
                OnProcessAbort?.Invoke(this, new EventArgs());
            }
        }
        public void AssignQueue(SWafer wafer)
        {
            quePreCommand.Enqueue(wafer);
        }
        #endregion

        public bool AroundTrigger()
        {
            if (Simulate) return false;
            if (dlgAroundTrigger != null && dlgAroundTrigger() == false) { return false; }
            return true;
        }

        public bool AllowedLoad(int nIndex)
        {
            //slot超過
            if (nIndex > m_strSlotEnable.Length) { return false; }
            //disable
            if (m_strSlotEnable[nIndex] == '0') { return false; }
            //無wafer
            if (IsWaferDetectOn(nIndex) == false) { return false; }
            //範圍觸發表示掉片
            if (AroundTrigger()) { return false; }
            return true;
        }
        public bool AllowedUnld(int nIndex)
        {
            //slot超過
            if (nIndex > m_strSlotEnable.Length) { return false; }
            //disable
            if (m_strSlotEnable[nIndex] == '0') { return false; }
            //有wafer
            if (IsWaferDetectOn(nIndex) == true) { return false; }
            //範圍觸發表示掉片
            if (AroundTrigger()) { return false; }
            return true;
        }
        /// <summary>
        /// 有片Recover判斷
        /// </summary>
        /// <returns></returns>
        public bool AnySlotHasWafer()
        {
            if (Disable) return false;
            bool bExist = false;
            for (int i = 0; i < m_strSlotEnable.Length; i++)
            {
                if (m_strSlotEnable[i] == '0') continue;        //disable
                if (GetWafer(i) != null || IsWaferDetectOn(i))
                {
                    bExist = true;
                    break;
                }
            }
            return bExist;
        }
        public void Cleanjobschedule()//20240704
        {
            queCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
            quePreCommand = new System.Collections.Concurrent.ConcurrentQueue<SWafer>();
        }

        public bool IsSlotDisable(int nIndex)
        {
            //slot超過
            if (nIndex > m_strSlotEnable.Length) { return true; }
            return m_strSlotEnable[nIndex] == '0';
        }

    }
}
