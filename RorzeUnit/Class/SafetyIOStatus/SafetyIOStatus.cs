using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.SafetyIOStatus
{
    public class SafetyIOStatus
    {
        private SLogger m_Logger;
        private sServer m_Sever;
        private Socket m_Socket;

        private SPollingThread m_PollingDequeueRecv;
        private ConcurrentQueue<string> m_QueRecvBuffer;//receive

        private string m_strUnitName;
        private string m_strIP;
        private bool m_bSimulate;
        private bool m_bDisable;
        public bool m_bConnect;
        public bool _Connected { get { return m_bConnect; } }
        public bool _Disable { get { return m_bDisable; } }
        public int _TotalModules { get { return m_dicGDIO_I.Keys.Count; } }

        public SafetyIOStatus(string strIP, bool bSimulate, bool bDisable, sServer sever)
        {
            m_strIP = strIP;

            m_bSimulate = bSimulate;
            m_bDisable = bDisable;
            m_strUnitName = "SafetyIO";
            m_Sever = sever;

            m_Sever.OnAssgnSocket += Sever_OnAssgnSocket;//收到連線成功

            m_QueRecvBuffer = new ConcurrentQueue<string>();

            m_PollingDequeueRecv = new SPollingThread(10);
            m_PollingDequeueRecv.DoPolling += PollingDequeueRecv;

            if (!_Disable && m_strIP != "127.0.0.1" && !m_bSimulate)
            {
                m_PollingDequeueRecv.Set();
            }
            if (_Disable == false)
            {
                m_Logger = SLogger.GetLogger("SafetyIOStatus");
            }
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[Safety] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_Logger.WriteLog(strMsg);
        }

        private void PollingDequeueRecv()
        {
            string strFrame;

            try
            {
                if (!m_QueRecvBuffer.TryDequeue(out strFrame)) return;

                //紀錄log
                WriteLog("Receive : " + strFrame + "\r\n");

                AnalysisIO(strFrame);
                /*switch (x)
                {
                    case 1:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_EMO1);
                        break;
                    case 2:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_EMO2);
                        break;
                    case 3:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_EMO3);
                        break;
                    case 4:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_EMO4);
                        break;
                    case 5:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_EMO5);
                        break;
                    case 6:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH1);
                        break;
                    case 7:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH2);
                        break;
                    case 8:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH3);
                        break;
                    case 9:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH4);
                        break;
                    case 10:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH5);
                        break;
                    case 11:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH6);
                        break;
                    case 12:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH7);
                        break;
                    case 13:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH8);
                        break;
                    case 14:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_DOOR_SWITCH9);
                        break;
                    case 15:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_AREA_SENSOR1);
                        break;
                    case 16:
                        _alarm.writeAlarm((int)SAlarm.enumAlarmCode.Safety_PLC_Alarm_AREA_SENSOR2);
                        break;
                    default:
                        //ErrStr1="Safety PLC Test->";
                        //ErLog->SetNewError(ErrLevel,ErrStr1.c_str(),"AREA_SENSOR2","R_PIO.cpp");
                        break;
                }*/

            }
            catch (Exception ex) { WriteLog("<<Exception>>" + ex); }
        }

        //  Server   
        private void Sever_OnAssgnSocket(object Sender, sServer.SocketEventArgs e)//Server收到Client連上線        
        {
            if (e._IP != m_strIP) return;

            WriteLog(string.Format("Socket accept client connected.{0}_[{1}]", m_strUnitName, e._Socket.RemoteEndPoint));

            if (m_Sever.AddClintList(m_strUnitName, e._Socket))
            {
                m_Socket = e._Socket;
                StartReceive();
            }
        }

        private void StartReceive()
        {
            Thread OnReceive = new Thread(Receive);
            OnReceive.IsBackground = true;
            OnReceive.Start();
        }

        private void Receive()
        {
            try
            {
                string strReceive;
                byte[] byteReceive = new byte[1024];
                while (m_Socket.Connected)
                {
                    SpinWait.SpinUntil(() => false, 1);
                    // 從已繫結的 Socket 接收資料。
                    Array.Clear(byteReceive, 0, byteReceive.Length);

                    int bytesReceived = m_Socket.Receive(byteReceive);//一個位元組包含8 個位元(bit)
                    byte[] result = new byte[bytesReceived];
                    Array.Copy(byteReceive, result, bytesReceived);

                    //  資料轉換成字串
                    strReceive = BitConverter.ToString(result);

                    m_QueRecvBuffer.Enqueue(strReceive);
                }

                if (!m_Socket.Connected)
                {
                    m_Sever.RemoveClint(m_strUnitName);  // 斷線後 Remove Socket item ...
                    //close progame
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("Exception : {0}", ex));
                if (!m_Socket.Connected)
                    m_Sever.RemoveClint(m_strUnitName);  // 斷線後 Remove Socket item ...
            }
        }

        // ==================================================================

        private Dictionary<int, string> m_dicGDIO_I = new Dictionary<int, string>();
        //private Dictionary<int, string> m_dicGDIO_O = new Dictionary<int, string>();
        private object m_lockIO = new object();

        public event NotifyGDIOEventHandler OnNotifyEvntGDIO;

        private void AnalysisIO(string strFrame)
        {
            try
            {
                lock (m_lockIO)
                {
                    string[] str = strFrame.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                    bool bChange = false;

                    for (int i = 0; i < str.Length; i++)
                    {
                        int nHCLID = i;
                        string strInput = str[i];
                        if (!m_dicGDIO_I.ContainsKey(nHCLID))
                        {
                            m_dicGDIO_I.Add(nHCLID, strInput);
                            bChange = true;
                        }
                        else
                        {
                            string strLastValue = m_dicGDIO_I[nHCLID];
                            m_dicGDIO_I[nHCLID] = strInput;
                            bChange = (strLastValue != strInput);
                        }

                        if (bChange)
                            SendEvent_OnNotifyEvntGDIO(this, new NotifyGDIOEventArgs(nHCLID, strInput, "0000"));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }

        }

        private void SendEvent_OnNotifyEvntGDIO(object sender, NotifyGDIOEventArgs e)
        {
            OnNotifyEvntGDIO?.Invoke(this, e);
        }

        private bool IsBitOn(string strValue, int nBit)
        {
            Int64 nValue = Convert.ToInt64(strValue, 16);
            Int64 nV = 0x01 << nBit;
            return ((nValue & nV) == nV);
        }

        public bool GetGDIO_InputStatus(int nHCLID, int nBit)
        {
            bool bOn = false;
            try
            {
                lock (m_lockIO)
                {
                    if (m_dicGDIO_I.ContainsKey(nHCLID) == false)
                        bOn = false;
                    else
                        bOn = IsBitOn(m_dicGDIO_I[nHCLID], nBit);
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);

            }
            return bOn;
        }



    }
}
