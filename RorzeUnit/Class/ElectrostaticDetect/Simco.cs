using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Net.Sockets;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using RorzeUnit.Interface;
using System.Drawing.Imaging;
using System.IO;

namespace RorzeUnit.Class.SIMCO
{
    public class Simco
    {

        private enum enumCommand : int
        {
            RequestRealTimeData = 0x38,
            ReadChannelRangandAlarmSetting = 0x62,
            SetChannelRangandAlarmSetting = 0x63,
        };

        private Dictionary<enumCommand, SSignal> m_dicSignalAck = new Dictionary<enumCommand, SSignal>();

        private SLogger m_Logger;
        private sClient m_Client;

        private SPollingThread m_PollingDequeueRecv;
        private SPollingThread m_PollingStatus;
        private ConcurrentQueue<string> m_QueRecvBuffer;

        private string m_strIP;
        private int m_nPort;
        private bool m_bSimulate;
        private bool m_bDisable;
        private bool m_bConnect;
        private object m_lockRecv = new object();
        private object m_lockSend = new object();

        private List<string> m_listBuffer = new List<string>();

        // Properties

        public bool IsConnect { get { return m_bConnect && m_Client.ConnectStat; } }
        public bool _Disable { get { return m_bDisable; } }

        public string _ProxV1 { get; private set; } = "0";
        public string _ProxV2 { get; private set; } = "0";
        public string _ProxV3 { get; private set; } = "0";

        public bool _ProxV1Alarm { get; private set; } = false;
        public bool _ProxV2Alarm { get; private set; } = false;
        public bool _ProxV3Alarm { get; private set; } = false;

        public string _CH1_Ranges { get; private set; } = "0";
        public string _CH2_Ranges { get; private set; } = "0";
        public string _CH3_Ranges { get; private set; } = "0";
        // Alarm values
        public string _CH1_Alarm { get; private set; } = "0";
        public string _CH2_Alarm { get; private set; } = "0";
        public string _CH3_Alarm { get; private set; } = "0";


        public Simco(string strIP, int nPort, bool bSimulate, bool bDisable)
        {
            m_strIP = strIP;
            m_nPort = nPort;
            m_bSimulate = bSimulate;
            m_bDisable = bDisable;
            m_Client = new sClient();

            foreach (enumCommand item in System.Enum.GetValues(typeof(enumCommand)))
            {
                m_dicSignalAck.Add(item, new SSignal(false, EventResetMode.ManualReset));
            }

            m_QueRecvBuffer = new ConcurrentQueue<string>();
            m_PollingDequeueRecv = new SPollingThread(10);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;
            m_PollingStatus = new SPollingThread(5000);
            m_PollingStatus.DoPolling += PollingStatus;

            m_Client.onDataReciveByByte += _Client_onDataRecive;
            m_Client.OnConnectChange += _Client_OnConnectChange;

            if (m_strIP != "" && m_strIP != "127.0.0.1" && !m_bSimulate && !m_bDisable)
            {
                m_PollingDequeueRecv.Set();
                m_PollingStatus.Set();
                System.Threading.Tasks.Task.Run(() => { StartCommunication(); });
            }
        }


        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[SIMCO] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }
        private void _Client_OnConnectChange(object sender, bool bConnect)
        {
            m_bConnect = bConnect;
            if (bConnect)
            {
                m_Logger = SLogger.GetLogger("SIMCO");
                //OnCommunicationOpen?.Invoke(this, new EventArgs());
            }
            else
            {
                //OnCommunicationClose?.Invoke(this, new EventArgs());
            }
        }
        // Step1 : 收到事件
        private void _Client_onDataRecive(sClient.ReciveByByte args)
        {
            byte[] byteMsg = args.sReciveData;
            string strMsg = BitConverter.ToString(byteMsg);
            m_QueRecvBuffer.Enqueue(strMsg);
        }
        // Step2 : 解Queue
        private void PollingDequeueRecv()
        {
            try
            {
                SpinWait.SpinUntil(() => false, 1);
                if (IsConnect == false) return;
                string strContent;
                if (m_QueRecvBuffer.TryDequeue(out strContent) == false) return;
                ProcessingRecive(strContent);
            }
            catch (Exception ex)
            {
                WriteLog("Exception:" + ex);
            }
        }
        // Step3 : 解析
        private bool ProcessingRecive(string strContent)
        {
            bool bSuc = false;
            try
            {
                lock (m_lockRecv)
                {
                    WriteLog("Recv : " + strContent);

                    string[] str = strContent.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                    m_listBuffer.AddRange(str);

                    string[] mergedArray = m_listBuffer.ToArray();

                    if (mergedArray.Length > 2)
                    {
                        string hexString = mergedArray[1];
                        int nValue = Convert.ToInt32(hexString, 16);
                        if (Enum.IsDefined(typeof(enumCommand), nValue))
                        {
                            switch ((enumCommand)nValue)
                            {
                                case enumCommand.RequestRealTimeData:
                                    {
                                        //Send: 00 - 38 - 00 - 62  at line 273(SockectSend)
                                        //Recv: 00 - 38 - 00 - 00 - 00 - 00 - 00  at line 149(ProcessingRecive)
                                        //Recv: 00 - 00 - 00 - 00 - 87 - F1 - 00  at line 149(ProcessingRecive)
                                        if (mergedArray.Length >= 14)
                                        {
                                            string combinedHex = string.Concat(mergedArray[2], mergedArray[3]);// 组合所有字符串                                                                                                         
                                            short decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _ProxV1 = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[4], mergedArray[5]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _ProxV2 = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[6], mergedArray[7]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _ProxV3 = (((float)decimalValue) / 10).ToString();

                                            decimalValue = Convert.ToInt16(mergedArray[10], 16);// 转换为10进制整数

                                            // Alarm status
                                            _ProxV1Alarm = (decimalValue & 2) != 0;
                                            _ProxV2Alarm = (decimalValue & 4) != 0;
                                            _ProxV3Alarm = (decimalValue & 8) != 0;

                                            WriteLog(string.Format("\t\t Prox#0:{0} Prox#1:{1} Prox#2:{2} {3}{4}{5}", _ProxV1, _ProxV2, _ProxV3, _ProxV1Alarm, _ProxV2Alarm, _ProxV3Alarm));

                                            m_listBuffer.RemoveRange(0, 14);
                                            m_dicSignalAck[(enumCommand)nValue].Set();
                                        }
                                    }
                                    break;
                                case enumCommand.ReadChannelRangandAlarmSetting:
                                case enumCommand.SetChannelRangandAlarmSetting:
                                    {
                                        //Send: 00 - 62 - 80 - 59  at line 252(SockectSend)
                                        //Recv: 00 - 62 - 00 - 7D - 00 - 7D - 00 - 7D - 00 - 32  at line 144(ProcessingRecive)
                                        //Recv: 00 - 32 - 00 - 32 - 35 - 05 - 13  at line 144(ProcessingRecive)
                                        if (mergedArray.Length >= 17)
                                        {
                                            string combinedHex = string.Concat(mergedArray[2], mergedArray[3]);// 组合所有字符串                                                                                                         
                                            short decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _CH1_Ranges = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[4], mergedArray[5]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _CH2_Ranges = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[6], mergedArray[7]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _CH3_Ranges = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[8], mergedArray[9]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _CH1_Alarm = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[10], mergedArray[11]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _CH2_Alarm = (((float)decimalValue) / 10).ToString();

                                            combinedHex = string.Concat(mergedArray[12], mergedArray[13]);// 组合所有字符串                                                                                                         
                                            decimalValue = Convert.ToInt16(combinedHex, 16);// 转换为10进制整数
                                            _CH3_Alarm = (((float)decimalValue) / 10).ToString();

                                            WriteLog(string.Format("\t\t Ranges#:{0}, {1}, {2} Alarm#:{3}, {4}, {5}", _CH1_Ranges, _CH2_Ranges, _CH3_Ranges, _CH1_Alarm, _CH2_Alarm, _CH3_Alarm));


                                            m_listBuffer.RemoveRange(0, 17);
                                            m_dicSignalAck[(enumCommand)nValue].Set();
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { WriteLog("Exception:" + ex); }
            return bSuc;
        }

        private void PollingStatus()
        {
            try
            {
                if (IsConnect == false) return;

                SendCmd(enumCommand.RequestRealTimeData);
                SpinWait.SpinUntil(() => false, 100);
                SendCmd(enumCommand.ReadChannelRangandAlarmSetting);

            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
        }

        // ===================================================================

        public bool StartCommunication()
        {

            if (m_bSimulate == false)
                m_Client.connect(m_strIP, m_nPort);
            return true;
        }

        private bool SockectSend(enumCommand eCmd, bool bPassLog = false)
        {
            bool bSucc = false;
            try
            {
                lock (m_lockSend)
                {
                    /*if (bPassLog == false) WriteLog("Send:" + strCmd);                 
                    byte[] byteArry = Encoding.ASCII.GetBytes(strCmd);
                    m_Comport.Write(byteArry, 0, byteArry.Length);*/

                    byte[] q = new byte[4];
                    q[0] = 0x00;
                    q[1] = (byte)eCmd;

                    byte[] crcResult = GetCRC16(q, q.Length - 2);
                    q[2] = crcResult[0];
                    q[3] = crcResult[1];

                    if (bPassLog == false) WriteLog("Send : " + BitConverter.ToString(q));

                    m_Client.Write(q);

                    bSucc = true;
                }
            }
            catch (Exception ex) { WriteLog("<<SException>>" + ex); }
            return bSucc;
        }

        private bool SendCmd(enumCommand eCmd, bool bPassLog = false)
        {
            bool bSuc = false;
            while (true)
            {
                if (SockectSend(eCmd, bPassLog) == false)
                    break;
                //if (m_dicSignalAck[eCmd].WaitOne(3000) == false)
                //    break;
                //if (m_dicSignalAck[eCmd].bAbnormalTerminal)
                //    break;
                bSuc = true;
                break;
            }
            return bSuc;
        }


        //預設125,125,125,50,50,50 要除以10 => 12.5, 12.5, 12.5, 5, 5, 5
        private bool SendSetCHAlarmRang(int rCh1, int rCh2, int rCh3, int alarm1, int alarm2, int alarm3)
        {
            bool bSucc = false;
            try
            {
                lock (m_lockSend)
                {
                    int[] values = { rCh1, rCh2, rCh3, alarm1, alarm2, alarm3 };
                    byte[] tempData = new byte[16];

                    tempData[0] = 0x00;
                    tempData[1] = (byte)enumCommand.SetChannelRangandAlarmSetting;

                    for (int i = 2; i < 14; i += 2)
                    {
                        tempData[i] = (byte)(values[(i - 2) / 2] >> 8);
                        tempData[i + 1] = (byte)(values[(i - 2) / 2] & 0xFF);
                    }

                    byte[] crcResult = GetCRC16(tempData, tempData.Length - 2);
                    tempData[14] = crcResult[0];
                    tempData[15] = crcResult[1];

                    m_Client.Write(tempData);

                    bSucc = true;
                }
            }
            catch (Exception ex) { WriteLog("<<SException>>" + ex); }
            return bSucc;
        }



        /*
        public void SendCMD_SimCo(string addr, eCMDTYPE cmdSimCo)
        {
            try
            {
                lock (lockObject)
                {
                    if (m_bSimulate == false && IsConnected())
                    {
                        // Convert enum to string then parse as hex
                        string tempCmdByte = ((int)0x14).ToString();

                        // Convert hex string to int
                        int tempAddByte1 = Convert.ToInt32(addr, 16);
                        byte temp1 = (byte)tempAddByte1;

                        // Convert command string as hex to int
                        //int tempCmdByte1 = Convert.ToInt32(tempCmdByte, 16);
                        int tempCmdByte1 = 0x38;
                        byte temp2 = (byte)tempCmdByte1;

                        byte[] q = new byte[4];
                        q[0] = temp1;
                        q[1] = temp2;

                        byte[] crcResult = GetCRC16(q, q.Length - 2);
                        q[2] = crcResult[0];
                        q[3] = crcResult[1];

                        // Send data
                        stream.Write(q, 0, q.Length);

                        Thread.Sleep(300);

                        // Receive response
                        string strReply = ReceiveText();
                        ParseMessage(strReply);

                        _logger.WriteLog("RECV:" + strReply);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"SendCMD_SimCo error: {ex.Message}");
            }
        }

        public bool SendSetCHAlarmRang(string addr, int rCh1, int rCh2, int rCh3, int alarm1, int alarm2, int alarm3)
        {
            try
            {
                if (m_bSimulate == false && IsConnected())
                {
                    int[] values = { rCh1, rCh2, rCh3, alarm1, alarm2, alarm3 };
                    byte[] tempData = new byte[16];

                    int tempAddByte1 = Convert.ToInt32(addr, 16);
                    byte temp1 = (byte)tempAddByte1;

                    cmdSimCo = eCMDTYPE.SetChannelRangandAlarmSetting;

                    string tempCmdByte = ((int)cmdSimCo).ToString();
                    int tempCmdByte1 = Convert.ToInt32(tempCmdByte, 16);
                    byte temp2 = (byte)tempCmdByte1;

                    tempData[0] = temp1;
                    tempData[1] = temp2;

                    for (int i = 2; i < 14; i += 2)
                    {
                        tempData[i] = (byte)(values[(i - 2) / 2] >> 8);
                        tempData[i + 1] = (byte)(values[(i - 2) / 2] & 0xFF);
                    }

                    byte[] crcResult = GetCRC16(tempData, tempData.Length - 2);
                    tempData[14] = crcResult[0];
                    tempData[15] = crcResult[1];

                    stream.Write(tempData, 0, tempData.Length);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ParseMessage(string reply)
        {
            try
            {
                if (string.IsNullOrEmpty(reply) || reply.Length < 3)
                    return false;

                byte[] replyBytes = Encoding.ASCII.GetBytes(reply);

                switch ((char)replyBytes[1])
                {
                    case '8': // Read time Data
                        if (replyBytes.Length >= 11)
                        {
                            string[] proxV = new string[3];
                            ushort temp1;

                            // Parse proxV1, proxV2, proxV3
                            for (int i = 2; i < 8; i += 2)
                            {
                                temp1 = (ushort)((replyBytes[i] << 8) + replyBytes[i + 1]);
                                if (temp1 > 32768)
                                {
                                    temp1 = (ushort)(65536 - temp1);
                                    proxV[(i - 2) / 2] = $"{temp1 * -0.1:000.0} V";
                                }
                                else
                                {
                                    proxV[(i - 2) / 2] = $"{temp1 * 0.1:000.0} V";
                                }
                            }

                            ProxV1 = proxV[0];
                            ProxV2 = proxV[1];
                            ProxV3 = proxV[2];

                            // Alarm status
                            if (replyBytes.Length > 10)
                            {
                                ProxV1Alarm = (replyBytes[10] & 2) != 0;
                                ProxV2Alarm = (replyBytes[10] & 4) != 0;
                                ProxV3Alarm = (replyBytes[10] & 8) != 0;
                            }
                        }
                        break;

                    case 'b': // read alarm Setting & CH Range
                    case 'c': // reply alarm and CHRange Message
                        if (replyBytes.Length >= 14)
                        {
                            // Channel ranges
                            R_CH1 = ((replyBytes[2] << 8) + replyBytes[3]).ToString();
                            R_CH2 = ((replyBytes[4] << 8) + replyBytes[5]).ToString();
                            R_CH3 = ((replyBytes[6] << 8) + replyBytes[7]).ToString();

                            // Alarm values
                            ushort alTemp1 = (ushort)((replyBytes[8] << 8) + replyBytes[9]);
                            R_Alarm1 = $"{alTemp1 * 0.1:0000.000} V";

                            ushort alTemp2 = (ushort)((replyBytes[10] << 8) + replyBytes[11]);
                            R_Alarm2 = $"{alTemp2 * 0.1:0000.000} V";

                            ushort alTemp3 = (ushort)((replyBytes[12] << 8) + replyBytes[13]);
                            R_Alarm3 = $"{alTemp3 * 0.1:0000.000} V";
                        }
                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        */

        private byte[] GetCRC16(byte[] msg, int len)
        {
            try
            {
                ushort crcFull = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crcFull = (ushort)(crcFull ^ msg[i]);
                    for (int j = 0; j < 8; j++)
                    {
                        if ((crcFull & 1) != 0)
                            crcFull = (ushort)((crcFull >> 1) ^ 0xA001);
                        else
                            crcFull = (ushort)(crcFull >> 1);
                    }
                }

                byte crcHi = (byte)((crcFull & 0xFF00) >> 8);
                byte crcLo = (byte)(crcFull & 0x00FF);

                //crc[0] = crcLo;
                //crc[1] = crcHi;
                return new byte[] { crcLo, crcHi };
            }
            catch
            {
                return new byte[2];
            }
        }













    }
}