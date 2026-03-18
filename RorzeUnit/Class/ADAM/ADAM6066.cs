using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using System.Net.Sockets;
using Advantech.Adam;
using RorzeComm.Log;
using RorzeComm;
using RorzeApi;
using RorzeUnit.Class.RC500.RCEnum;
using System.Linq;
using RorzeComm.Threading;
using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Class.ADAM.Event;
using System.Windows.Controls;

namespace RorzeUnit.Class.ADAM
{
    public class ADAM6066
    {
        private AdamSocket m_Socket;
        private Adam6000Type m_Adam6000Type;
        private SLogger _logger;
        private SLogger m_errorlog = SLogger.GetLogger("Errorlog");          //  log
        private string m_strIP;
        private int m_iPort;
        private const int m_iDoTotal = 6;
        private const int m_iDiTotal = 6;

        // ===== Timer =====
        private System.Timers.Timer m_DioTimer;
        private readonly object m_DioLock = new object();

        private bool m_bIsSimulate;

        // ===== Thread =====
        private SPollingThread _exePolling;// Adam Recive

        // ===== Event =====
        public event IOAdam6066EventHandler OnNotifyAdamIO;
        void SendEvent_OnNotifyAdamIO(object sender, IOAdam6066DataEventArgs e)
        {
            OnNotifyAdamIO?.Invoke(this, e);
        }

        public bool IsConnect
        {
            get;
            private set;
        }

        public bool Disable
        {
            get;
            private set;
        }
        public int _BodyNo { get; private set; }

        public ADAM6066(int nBodyNo, string strIP, int iPort, bool bDisable, bool bSimulate)
        {
            _BodyNo = nBodyNo;
            m_strIP = strIP;
            m_iPort = iPort;
            Disable = bDisable;
            m_bIsSimulate = bSimulate;
            IsConnect = false;
            _logger = new SLogger($"ADAM{_BodyNo}");

            INIT();
            InputArrayInit(); //ADAM模組輸入是反的，所以預設要是true

            _exePolling = new SPollingThread(10);
            _exePolling.DoPolling += _exePolling_DoPolling;
            _exePolling.Set();
        }
        private bool[] bDoData = new bool[m_iDoTotal];
        private bool[] bDiData = new bool[m_iDiTotal];

        public bool[] bDo { get; private set; } = new bool[m_iDoTotal];
        public bool[] bDi { get; private set; } = new bool[m_iDiTotal];


        private bool[] bDo_compare = new bool[m_iDoTotal];
        private bool[] bDi_compare = new bool[m_iDiTotal];
        public void InputArrayInit()
        {
            for (int i = 0; i < bDiData.Length; i++)
            {
                bDiData[i] = true;
            }

            for (int i = 0; i < bDi.Length; i++)
            {
                bDi[i] = true;
            }

            for (int i = 0; i < bDi_compare.Length; i++)
            {
                bDi_compare[i] = false;
            }
        }

        private void _exePolling_DoPolling()
        {
            try
            {
                if (!IsConnect && !m_bIsSimulate)
                {
                    INIT();
                    return;
                }
                
                    RefreshDIO();

                bool bIsChange = false;
                for (int i = 0; i < m_iDoTotal; i++)
                {
                    if (bDo[i] != bDo_compare[i] || bDi[i] != bDi_compare[i])
                    {
                        bIsChange = true;
                        break;
                    }
                }

                if (bIsChange)
                {
                    SendEvent_OnNotifyAdamIO(this, new IOAdam6066DataEventArgs(bDo, getInputValue()));
                    bDo_compare = (bool[])bDo.Clone();
                    bDi_compare = (bool[])bDi.Clone();
                    _logger.WriteLog(
                        $"[ADAM{this._BodyNo}] <<AdamIOChanged>> " +
                        $"Di: {string.Join("", bDi_compare.Select(b => b ? "1" : "0"))} " +
                        $", Do: {string.Join("", bDo_compare.Select(b => b ? "1" : "0"))}"
                        );
                }

            }
            catch (SException ex)
            {
                _logger.WriteLog($"[ADAM{this._BodyNo}] <<Exception>> _exePolling_DoPolling: + {ex}");
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"[ADAM{this._BodyNo}] <<Exception>> _exePolling_DoPolling: + {ex}");
            }
        }

        ~ADAM6066()
        {
            if (m_bIsSimulate == false && m_Socket != null && IsConnect == true)
            {
                IsConnect = false;
                m_Socket.Disconnect();
            }
        }

        public void INIT()
        {
            if (m_bIsSimulate == false && Disable == false && IsConnect == false)
            {
                m_Socket = new AdamSocket(AdamType.Adam6000);
                m_Socket.SetTimeout(1000, 1000, 1000);
                m_Adam6000Type = Adam6000Type.Adam6066;

                //connect
                IsConnect = m_Socket.Connect(m_strIP, ProtocolType.Tcp, m_iPort);
                _logger.WriteLog(
                        $"[ADAM{this._BodyNo}] <<AdamIO INIT>> IsConnect: {IsConnect}" 
                        );
            }
            else
            {
                IsConnect = true;
                _logger.WriteLog(
                        $"[ADAM{this._BodyNo}] <<AdamIO INIT>> IsConnect: {IsConnect}"
                        );
            }
        }

        // ===== 核心：刷新 DI / DO =====
        private void RefreshDIO()
        {
            lock (m_DioLock)
            {
                try
                {
                    // 或丟自訂錯誤/重連
                    if (m_iDiTotal <= 0 || m_iDoTotal <= 0) return;

                    int iDiStart = 1;
                    int iDoStart = 17;
                    if (!m_bIsSimulate)
                    {
                        if (m_Socket == null) return;
                        m_Socket.Modbus().ReadInputStatus(iDiStart, m_iDiTotal, out bDiData);
                        m_Socket.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData);
                    }
                    bDo = bDoData;
                    bDi = getInputValue();
                }
                catch (Exception ex)
                {
                    m_errorlog.WriteLog("[ Adam ] <<Exception>> Adam refresh DIO:" + ex);
                }
            }
        }

        private bool GetDoData(int nIdx) { return bDoData[nIdx]; }
        private bool GetDiData(int nIdx) { return bDiData[nIdx]; }


        public bool getInputValue(int num)
        {
            bool fResult = GetDiData(num);
            return !fResult;
        }
        public bool[] getInputValue()
        {
            bool[] fResult = (bool[])bDiData.Clone();
            for (int i = 0; i < fResult.Length; i++)
            {
                fResult[i] = !fResult[i];
            }

            return fResult;
        }
        public bool getOutputValue(int num)
        {
            bool fResult = GetDoData(num);
            return fResult;
        }

        public void setOutputValue(int nbit, bool bOn)
        {
            int iStart = 17 + nbit; //- m_iDoTotal;
            int nOn = bOn == true ? 1 : 0;

            lock (m_DioLock)
            {
                if (!m_bIsSimulate)
                {
                    if (m_Socket.Modbus().ForceSingleCoil(iStart, nOn) == false)
                    {
                        m_errorlog.WriteLog("[ Adam ] <<Exception>> Adam Set Output Fail.");
                    }
                }
                else if (m_bIsSimulate)
                {
                    SetDoValue(nbit, nOn != 0);
                }
            }
        }

        public void setInputValue(int nbit, bool nOn) // 模擬用
        {
            int iStart = nbit; //- m_iDiTotal;

            lock (m_DioLock)
            {
                if (m_bIsSimulate)
                {
                    SetDiValue(iStart, nOn);
                }
                else
                {
                    return;
                }
            }
        }

        private void SetDoValue(int index, bool value) // 模擬用
        {
            if (index >= 0 && index < bDoData.Length)
            {
                bDoData[index] = value;
            }
        }
        private void SetDiValue(int index, bool value) // 模擬用
        {
            if (index >= 0 && index < bDiData.Length)
            {
                bDiData[index] = !value;
            }
        }

        public void setPutSignal(bool bOn)
        {
            setOutputValue(0, bOn);
        }

        public void setGetSignal(bool bOn)
        {
            setOutputValue(1, bOn);
        }

    }
}