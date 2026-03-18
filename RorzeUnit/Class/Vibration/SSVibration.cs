using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Class.Vibration
{
    public class SSVibration
    {
        private SLogger m_logger = SLogger.GetLogger("Vibration_log");
        //private Dictionary<enumRobotCommand, SSignal> m_signalAck = new Dictionary<enumRobotCommand, SSignal>();
        //jan
        //private sRorzeSocket m_Socket;
        private SSVibrationSocket m_SendSocket, m_RecvSocket;
        //jan
        private SPollingThread m_PollingDequeue;
        private SSignal m_signalAck;
        private object m_lockSend;

        public int _BodyNo { get; }
        public bool _Disable { get; }
        public bool _Simulate { get; }
        public bool _Connected
        {
            get
            {
                //return m_Socket.isConnected(); 
                return (m_SendSocket.isConnected() && m_RecvSocket.isConnected());
            }
        }

        public SSVibration(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, sServer sever = null)
        {
            _BodyNo = nBodyNo;
            _Disable = bDisable;
            _Simulate = false;

            m_lockSend = new object();

            m_signalAck = new SSignal(true, System.Threading.EventResetMode.ManualReset);

            //for (int nCnt = 0; nCnt < (int)enumRobotCommand.Max; nCnt++)
            //    m_signalAck.Add((enumRobotCommand)nCnt, new SSignal(false, EventResetMode.ManualReset));

            //jan
            //m_Socket = new sRorzeSocket(IP, PortID, nBodyNo, "VMS", bSimulate, sever);
            //m_Socket.OnConnectChange += Socket_OnConnectChange;
            m_SendSocket = new SSVibrationSocket(IP, PortID, nBodyNo, "VMS", bSimulate, sever);
            m_RecvSocket = new SSVibrationSocket(IP, PortID + 1, nBodyNo, "VMS", bSimulate, sever);
            m_SendSocket.OnConnectChange += Socket_OnConnectChange;
            m_RecvSocket.OnConnectChange += Socket_OnConnectChange;
            //jan


            m_PollingDequeue = new SPollingThread(1);
            m_PollingDequeue.DoPolling += Polling_Dequeue;
            m_PollingDequeue.Set();
        }

        private void Socket_OnConnectChange(object sender, bool e)
        {
            //_Connected = e;
        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[VMS{0}] : {1}  at line {2} ({3})", _BodyNo, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }

        // Client去連接Server
        public void OpenConnect()
        {
            //jan
            //m_Socket.Open(); 
            m_SendSocket.Open();
            m_RecvSocket.Open();
            //jan
        }

        // 處理TCP收到
        private void Polling_Dequeue()
        {
            try
            {
                //string[] astrFrame;
                string[] astrSendFrame;
                string[] astrRecvFrame;
                //jan
                //if (false == m_Socket.QueRecvBuffer.TryDequeue(out astrFrame)) return;
                //if ((false == m_SendSocket.QueRecvBuffer.TryDequeue(out astrSendFrame)) || (false == m_RecvSocket.QueRecvBuffer.TryDequeue(out astrRecvFrame))) return;

                //string strFrame; 
                string strSendFrame;
                string strRecvFrame;
                if (m_SendSocket.QueRecvBuffer.TryDequeue(out astrSendFrame))
                {
                    for (int nCnt = 0; nCnt < astrSendFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                    {
                        if (astrSendFrame[nCnt].Length == 0) continue;

                        strSendFrame = astrSendFrame[nCnt];


                        /*enumRobotCommand cmd = enumRobotCommand.GetVersion;
                        bool bUnknownCmd = true;

                        foreach (string scmd in _dicCmdsTable.Values) //查字典
                        {
                            if (strFrame.Contains(string.Format("TRB{0}.{1}", this.BodyNo.ToString("X"), scmd)))
                            {
                                cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                                bUnknownCmd = false; //認識這個指令
                                break;
                            }
                        }

                        if (bUnknownCmd) //不認識的封包
                        {
                            WriteLog(string.Format("<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                            continue;
                        }*/

                        WriteLog("Received : " + strSendFrame);

                        /*
                        switch (strFrame[0]) //命令種類
                        {
                            case 'c': //cancel
                                OnCancelAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                                break;
                            case 'n': //nak
                                _signalAck[cmd].bAbnormalTerminal = true;
                                _signalAck[cmd].Set();
                                break;
                            case 'a': //ack
                                OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                                _signalAck[cmd].Set();
                                break;
                            case 'e':
                                OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                                break;
                            default:
                                break;
                        }*/

                        m_signalAck.Set();
                    }
                }

                if (m_RecvSocket.QueRecvBuffer.TryDequeue(out astrRecvFrame))
                {
                    for (int nCnt = 0; nCnt < astrRecvFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                    {
                        if (astrRecvFrame[nCnt].Length == 0) continue;

                        strRecvFrame = astrRecvFrame[nCnt];


                        /*enumRobotCommand cmd = enumRobotCommand.GetVersion;
                        bool bUnknownCmd = true;

                        foreach (string scmd in _dicCmdsTable.Values) //查字典
                        {
                            if (strFrame.Contains(string.Format("TRB{0}.{1}", this.BodyNo.ToString("X"), scmd)))
                            {
                                cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                                bUnknownCmd = false; //認識這個指令
                                break;
                            }
                        }

                        if (bUnknownCmd) //不認識的封包
                        {
                            WriteLog(string.Format("<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                            continue;
                        }*/

                        WriteLog("Received : " + strRecvFrame);

                        /*
                        switch (strFrame[0]) //命令種類
                        {
                            case 'c': //cancel
                                OnCancelAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                                break;
                            case 'n': //nak
                                _signalAck[cmd].bAbnormalTerminal = true;
                                _signalAck[cmd].Set();
                                break;
                            case 'a': //ack
                                OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                                _signalAck[cmd].Set();
                                break;
                            case 'e':
                                OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                                break;
                            default:
                                break;
                        }*/

                        m_signalAck.Set();
                    }
                }
                //jan



                /*for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                {
                    if (astrFrame[nCnt].Length == 0) continue;

                    strFrame = astrFrame[nCnt];


                    /*enumRobotCommand cmd = enumRobotCommand.GetVersion;
                    bool bUnknownCmd = true;

                    foreach (string scmd in _dicCmdsTable.Values) //查字典
                    {
                        if (strFrame.Contains(string.Format("TRB{0}.{1}", this.BodyNo.ToString("X"), scmd)))
                        {
                            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                            bUnknownCmd = false; //認識這個指令
                            break;
                        }
                    }

                    if (bUnknownCmd) //不認識的封包
                    {
                        WriteLog(string.Format("<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                        continue;
                    }

                    WriteLog("Received : " + strFrame);

                    
                    switch (strFrame[0]) //命令種類
                    {
                        case 'c': //cancel
                            OnCancelAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                            break;
                        case 'n': //nak
                            _signalAck[cmd].bAbnormalTerminal = true;
                            _signalAck[cmd].Set();
                            break;
                        case 'a': //ack
                            OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                            _signalAck[cmd].Set();
                            break;
                        case 'e':
                            OnAck(this, new RorzenumRobotProtoclEventArgs(strFrame));
                            break;
                        default:
                            break;
                    }

                    m_signalAck.Set();
                }*/


            }
            catch (SException ex)
            {
                WriteLog("<<SException>>" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
        }

        public void CommandW(int nTimeout, string strContent, bool bWaitAck, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            lock (m_lockSend)
            {
                if (_Connected == false)
                {
                    WriteLog("Socket not connected", meberName, lineNumber);
                    return;
                }
                else
                {
                    m_signalAck.Reset();
                    //jan
                    //m_Socket.SendContent(strContent);
                    m_SendSocket.SendContent(strContent);
                    //jan
                    if (bWaitAck)
                    {
                        if (false == m_signalAck.WaitOne(nTimeout))
                        {
                            //SendAlmMsg(enumCustomErr.AckTimeout);
                            //throw new SException((int)enumCustomErr.AckTimeout, string.Format("Send content and wait Ack was timeout. [{0}]", strContent));
                        }
                        if (m_signalAck.bAbnormalTerminal)
                        {
                            //SendAlmMsg(enumCustomErr.SendCommandFailure);
                            //throw new SException((int)enumCustomErr.SendCommandFailure, string.Format("Send content and wait Ack was failure. [{0}]", strContent));
                        }
                    }
                }

            }
        }


    }
}
