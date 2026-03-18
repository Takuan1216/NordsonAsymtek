using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;

using RorzeUnit.Class.Keyence_MP.Enum;
using RorzeUnit.Event;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Net.Sockets;

namespace RorzeUnit.Class.Keyence_MP
{
    public class Keyence_MP
    {

        public event EventHandler OnCommunicationOpen;
        public event EventHandler OnCommunicationClose;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public bool _Connected { get { return m_bConnect; } }
        public bool _Disable { get { return m_bDisable; } }

        private Dictionary<enumKeyence_MPCommand, SSignal> m_dicSignalAck = new Dictionary<enumKeyence_MPCommand, SSignal>();

        private SLogger m_Logger;
        private sClient m_Client;

        private SPollingThread m_PollingDequeueRecv;
        private SPollingThread m_PollingStatus;
        private ConcurrentQueue<byte[]> m_QueRecvBuffer;

        private string m_strIP;
        private int m_nPort;
        private bool m_bSimulate;
        private bool m_bDisable;
        private bool m_bConnect;
        private string m_strReply;




        //  識別ID    通訊協定ID  資料長度    模組ID    功能代碼    資料部
        //  2位元組    2位元組    2位元組    1位元組    1位元組    4至251位元組

        // 2020 07E4 壓力_當前值INT 2byte R-9999 至 9999
        private byte[] m_GetAirPressure = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x07, 0xE4, 0x00, 0x01 };
        // 2000 07D0 42001瞬時流量 UINT 2byte R 0 至 65535
        private byte[] m_GetAirFlow = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x07, 0xD0, 0x00, 0x01 };
        // 2030 07EE 42031溫度_當前值INT 2byte R-999 至 999
        private byte[] m_GetAirTemperature = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x07, 0xEE, 0x00, 0x01 };
        // 2106 083A 42107 A_線電壓_3 UINT 2byte R 0 至 8281
        private byte[] m_GetInputVoltage = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x08, 0x3A, 0x00, 0x01 };
        // 2108 083C 42109 A_電流_1 UDINT 4byte R 0 至 3600001
        private byte[] m_GetInputCurrent = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x08, 0x3C, 0x00, 0x01 };
        // 2004 07D4 42005累積流量 UDINT 4byte R 0 至 99999999
        private byte[] m_GetCumulativeAirConsumption = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x07, 0xD4, 0x00, 0x01 };
        // 2140 085C 42141 A_累積有效電量UDINT 4byte R 0 至 999999999
        private byte[] m_GetCumulativePowerConsumption = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x08, 0x5C, 0x00, 0x01 };

        enumKeyence_MPCommand m_eLastCommand;

        Dictionary<enumKeyence_MPCommand, string> m_dicRecvData = new Dictionary<enumKeyence_MPCommand, string>();


        public Keyence_MP(string strIP, int nPort, bool bSimulate, bool bDisable)
        {
            m_strIP = strIP;
            m_nPort = nPort;
            m_bSimulate = bSimulate;
            m_bDisable = bDisable;
            m_Client = new sClient();

            foreach (enumKeyence_MPCommand item in System.Enum.GetValues(typeof(enumKeyence_MPCommand)))
            {
                m_dicSignalAck.Add(item, new SSignal(false, EventResetMode.ManualReset));
            }

            m_QueRecvBuffer = new ConcurrentQueue<byte[]>();
            m_PollingDequeueRecv = new SPollingThread(10);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;
            m_PollingStatus = new SPollingThread(3000);
            m_PollingStatus.DoPolling += PollingStatus;

            m_Client.onDataReciveByByte += _Client_onDataRecive;
            m_Client.OnConnectChange += _Client_OnConnectChange;

            if (m_strIP != "" && m_strIP != "127.0.0.1" && !m_bSimulate && !m_bDisable)
            {
                m_PollingDequeueRecv.Set();
                m_PollingStatus.Set();
                System.Threading.Tasks.Task.Run(() => { Open(); });
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[Keyence_MP] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        private void _Client_onDataRecive(sClient.ReciveByByte args)
        {
            byte[] strMsg = args.sReciveData;
            m_QueRecvBuffer.Enqueue(strMsg);
        }

        private void _Client_OnConnectChange(object sender, bool bConnect)
        {
            m_bConnect = bConnect;
            if (bConnect)
            {
                m_Logger = SLogger.GetLogger("Keyence_MP");
                OnCommunicationOpen?.Invoke(this, new EventArgs());
            }
            else
            {
                OnCommunicationClose?.Invoke(this, new EventArgs());
            }
        }

        private void Sendcommand(byte[] data)
        {
            try
            {
                WriteLog("Send : " + BitConverter.ToString(data));
                m_Client.Write(data);
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>" + ex);
            }
        }

        private void PollingDequeueRecv()
        {
            byte[] tempData;

            try
            {
                if (!m_QueRecvBuffer.TryDequeue(out tempData)) return;

                //紀錄log
                WriteLog("Recv : " + BitConverter.ToString(tempData));

                string str = "", Ans2 = "";
                float Ans1;
                int receivedBytes = tempData.Length;
                if (receivedBytes > 0)
                {
                    if (receivedBytes < 12)   // 取回傳值得9~11碼,因為這些參數是回傳2 BYTE的資料
                    {
                        for (int i = receivedBytes - 2; i < receivedBytes; i++)
                        {
                            str += (0xFF & tempData[i]).ToString("X");
                        }
                    }
                    else      // 取回傳值得9~13碼,因為這些參數是回傳4 BYTE的資料
                    {
                        str += (0xFF & tempData[11]).ToString("X");
                        str += (0xFF & tempData[12]).ToString("X");
                        str += (0xFF & tempData[9]).ToString("X");
                        str += (0xFF & tempData[10]).ToString("X");
                    }
                }

                Ans1 = uint.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier);

                switch (m_eLastCommand)
                {
                    case enumKeyence_MPCommand.AirPressure:
                        {
                            Ans2 = (Ans1 / 1000.0).ToString("0.000");
                            m_strReply = Ans2;
                            WriteLog("Get Air Pressure Data : " + m_strReply + " MPa");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;

                    case enumKeyence_MPCommand.AirFlow:
                        {
                            Ans2 = (Ans1 / 100.0).ToString("0.000");
                            m_strReply = Ans2;
                            WriteLog("Get Air Flow Data : " + m_strReply + " m^3/h");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;

                    case enumKeyence_MPCommand.AirTemperature:
                        {
                            Ans2 = (Ans1 / 10.0).ToString("00.00");
                            m_strReply = Ans2;
                            WriteLog("Get Air Temperature Data : " + m_strReply + " ℃");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;

                    case enumKeyence_MPCommand.InputVoltage:
                        {
                            Ans2 = (Ans1 / 10.0).ToString("000.0");
                            m_strReply = Ans2;
                            WriteLog("Get Input Voltage Data : " + m_strReply + " V");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;

                    case enumKeyence_MPCommand.InputCurrent:
                        {
                            Ans2 = (Ans1 / 1000.0).ToString("0.000");
                            m_strReply = Ans2;
                            WriteLog("Get Input Current Data : " + m_strReply + " A");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;

                    case enumKeyence_MPCommand.CumulativeAirConsumption:
                        {
                            Ans2 = (Ans1 / 1000.0).ToString("000.000");
                            m_strReply = Ans2;
                            WriteLog("Get Cumulative Air Consumption Data : " + m_strReply + " m^3");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;

                    case enumKeyence_MPCommand.CumulativePowerConsumption:
                        {
                            Ans2 = (Ans1 / 1000.0).ToString("000.000");
                            m_strReply = Ans2;
                            WriteLog("Get Cumulative Power Consumption Data : " + m_strReply + " kW/h");
                            m_dicSignalAck[m_eLastCommand].Set();
                        }
                        break;
                }

                m_dicRecvData[m_eLastCommand] = Ans2;
            }
            catch (Exception ex)
            {
                WriteLog("<Exception>" + ex);
            }
        }

        #region =========================== Air Pressure ===================================
        private void GetAirPressure()
        {
            m_eLastCommand = enumKeyence_MPCommand.AirPressure;
            m_dicSignalAck[enumKeyence_MPCommand.AirPressure].Reset();
            Sendcommand(m_GetAirPressure);
        }
        private string GetAirPressureW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetAirPressure();
                if (!m_dicSignalAck[enumKeyence_MPCommand.AirPressure].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.AirPressure).ToString()));
                }
            }
            else
            {
                m_strReply = "0.111";
            }
            return m_strReply;
        }
        #endregion 

        #region =========================== Air Flow =======================================
        private void GetAirFlow()
        {
            m_eLastCommand = enumKeyence_MPCommand.AirFlow;
            m_dicSignalAck[enumKeyence_MPCommand.AirFlow].Reset();
            Sendcommand(m_GetAirFlow);
        }
        private string GetAirFlowW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetAirFlow();
                if (!m_dicSignalAck[enumKeyence_MPCommand.AirFlow].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.AirFlow).ToString()));
                }
            }
            else
            {
                m_strReply = "0.222";
            }
            return m_strReply;
        }
        #endregion 

        #region =========================== Air Temperature ================================
        private void GetAirTemperature()
        {
            m_eLastCommand = enumKeyence_MPCommand.AirTemperature;
            m_dicSignalAck[enumKeyence_MPCommand.AirTemperature].Reset();
            Sendcommand(m_GetAirTemperature);
        }
        private string GetAirTemperatureW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetAirTemperature();
                if (!m_dicSignalAck[enumKeyence_MPCommand.AirTemperature].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.AirTemperature).ToString()));
                }
            }
            else
            {
                m_strReply = "0.333";
            }
            return m_strReply;
        }
        #endregion 

        #region =========================== Input Voltage ==================================
        private void GetInputVoltage()
        {
            m_eLastCommand = enumKeyence_MPCommand.InputVoltage;
            m_dicSignalAck[enumKeyence_MPCommand.InputVoltage].Reset();
            Sendcommand(m_GetInputVoltage);
        }
        private string GetInputVoltageW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetInputVoltage();
                if (!m_dicSignalAck[enumKeyence_MPCommand.InputVoltage].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.InputVoltage).ToString()));
                }
            }
            else
            {
                m_strReply = "0.444";
            }
            return m_strReply;
        }
        #endregion 

        #region =========================== Input Current ==================================
        private void GetInputCurrent()
        {
            m_eLastCommand = enumKeyence_MPCommand.InputCurrent;
            m_dicSignalAck[enumKeyence_MPCommand.InputCurrent].Reset();
            Sendcommand(m_GetInputCurrent);
        }
        private string GetInputCurrentW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetInputCurrent();
                if (!m_dicSignalAck[enumKeyence_MPCommand.InputCurrent].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.InputCurrent).ToString()));
                }
            }
            else
            {
                m_strReply = "0.555";
            }
            return m_strReply;
        }
        #endregion 

        #region =========================== Cumulative Air Consumption =====================
        private void GetCumulativeAirConsumption()
        {
            m_eLastCommand = enumKeyence_MPCommand.CumulativeAirConsumption;
            m_dicSignalAck[enumKeyence_MPCommand.CumulativeAirConsumption].Reset();
            Sendcommand(m_GetCumulativeAirConsumption);
        }
        private string GetCumulativeAirConsumptionW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetCumulativeAirConsumption();
                if (!m_dicSignalAck[enumKeyence_MPCommand.CumulativeAirConsumption].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.CumulativeAirConsumption).ToString()));
                }
            }
            else
            {
                m_strReply = "0.666";
            }
            return m_strReply;
        }
        #endregion 

        #region =========================== Cumulative Power Consumption ===================
        private void GetCumulativePowerConsumption()
        {
            m_eLastCommand = enumKeyence_MPCommand.CumulativePowerConsumption;
            m_dicSignalAck[enumKeyence_MPCommand.CumulativePowerConsumption].Reset();
            Sendcommand(m_GetCumulativePowerConsumption);
        }
        private string GetCumulativePowerConsumptionW(int nTimeout)
        {
            m_strReply = "";
            if (_Connected)
            {
                GetCumulativePowerConsumption();
                if (!m_dicSignalAck[enumKeyence_MPCommand.CumulativePowerConsumption].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumKeyence_MPError.AckTimeout);
                    throw new SException((int)enumKeyence_MPError.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", (enumKeyence_MPCommand.CumulativePowerConsumption).ToString()));
                }
            }
            else
            {
                m_strReply = "0.777";
            }
            return m_strReply;
        }
        #endregion

        //  發生自定義異常
        private void SendAlmMsg(enumKeyence_MPError eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            int nCode = (int)eAlarm;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }

        private void PollingStatus()
        {
            try
            {
                if (_Connected == false) return;

                GetAirPressureW(3000);
                GetAirFlowW(3000);
                GetAirTemperatureW(3000);
                GetInputVoltageW(3000);
                GetInputCurrentW(3000);
                GetCumulativeAirConsumptionW(3000);
                GetCumulativePowerConsumptionW(3000);

            }
            catch (SException ex) { WriteLog("<SException>:" + ex); }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
        }

        // ===================================================================

        public void Open()
        {
            if (m_bSimulate == false)
                m_Client.connect(m_strIP, m_nPort);
        }



    }
}
