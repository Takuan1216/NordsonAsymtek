using Rorze.SecsDriver;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rorze.Secs
{
    class GWSecs : ISECS
    {

        public SECSConneetConfig GetSECSConfigData { get { return _SecsConfig; } }


        // private object 
        string SecsPath;
        SECSConneetConfig _SecsConfig;
        IntPtr Str;
        SLogger _logger;
        bool _SecsStart = false;
        IntPtr EventHandle; // GW Event catch
        int Status = 0;    // InternalConnecteStatus
        int PreStats = 0;  // PreInternalConnectStatus
        ushort deviceidTick = 0;
        ushort tkx = 0;
        SSDR_170.SDRMSG _Pmsg;
        ConnState state;
        ushort DEVICEID = 0;
        IntPtr bufferPtr;
        // Thread object
        SPollingThread _exeCheckConnectStatus; // Check InternalConnected
        SPollingThread _exeReciveSecsMsg;      // Recive MSG
        SPollingThread _pollingSend;           // send msg  by Queue
        SPollingThread _pollingRecv;           // Recive msg by Queue
        GWSECSMessage SECSMessage;
        Dictionary<ushort, GWSECSMessage> SECSMessageList;
        //SPollingThread _PollingSaveSecslog;    // Transfer log Thread 

        // Queue object
        private ConcurrentQueue<GWSECSMessageSend> _queSendBuffer;        //buffer of message to send host
        private ConcurrentQueue<SSDR_170.SDRMSG> _queRecvBuffer;        //buffer of message to receive and wait process

        private ConcurrentQueue<GWSECSMessage> _queRecvSecsMsg;
        public event SecsInternalArgs.SecsInternalHandle InternalStatusChange;
        public event SecsMessageObject1Args.SecsMessageHandle ReceiveSecsMessage;

        private bool m_bSECS_Enable;
        public GWSecs(bool secsEnable)
        {
            m_bSECS_Enable = secsEnable;
            if (m_bSECS_Enable) _logger = new SLogger("GW_SecsDriver");
            _Pmsg = new SSDR_170.SDRMSG();
            _Pmsg.buffer = Marshal.AllocHGlobal(700000);
            Str = Marshal.AllocHGlobal(70000);
            SecsPath = System.Environment.CurrentDirectory + @"\sdrconf.cfg";
            _queSendBuffer = new ConcurrentQueue<GWSECSMessageSend>();
            _queRecvBuffer = new ConcurrentQueue<SSDR_170.SDRMSG>();
            _queRecvSecsMsg = new ConcurrentQueue<GWSECSMessage>();
            SECSMessageList = new Dictionary<ushort, GWSECSMessage>();
            GetSecsData();
            // Taskkill("sdrl.exe");
            // Taskkill("sdr.exe");
            // SecsStop();
            bufferPtr = Marshal.AllocHGlobal(70000);
        }
        public ConnState GetSecsConnState()
        {
            return state;
        }




        private Dictionary<SecsFormateType, int> GWSecsFormateType = new Dictionary<SecsFormateType, int>()
        {
            {SecsFormateType.L,SSDR_170.S2_L},
            {SecsFormateType.A,SSDR_170.S2_LS2_STRING},
            {SecsFormateType.U1,SSDR_170.S2_U1},
            {SecsFormateType.U2,SSDR_170.S2_U2},
            {SecsFormateType.U4,SSDR_170.S2_U4},
            {SecsFormateType.B,SSDR_170.S2_B },
            {SecsFormateType.Bool,SSDR_170.S2_BOOLEAN},
            {SecsFormateType.F4,SSDR_170.S2_F4 },
            {SecsFormateType.F8,SSDR_170.S2_F8 },
            { SecsFormateType.I2,SSDR_170.S2_I2 }

        };
        /// <summary>
        /// Secs Message Action
        /// </summary>
        private int HostMsgItemInput(ref SSDR_170.SDRMSG Hostmsg, int type, ref int value)
        {

            if (type != Hostmsg.next)
                throw new StreamFuntionException(5, 5, string.Format("Formate illegal"));
            return SSDR_170.SdrItemInput(ref Hostmsg, type, ref value, 200);
        }
        private int HostMsgItemInput(ref SSDR_170.SDRMSG Hostmsg, int type, ref float  value)
        {

            if (type != Hostmsg.next)
                throw new StreamFuntionException(5, 5, string.Format("Formate illegal"));
            return SSDR_170.SdrItemInput(ref Hostmsg, type, ref value, 200);
        }
        private int HostMsgItemInput(ref SSDR_170.SDRMSG Hostmsg, int type, ref double value)
        {

            if (type != Hostmsg.next)
                throw new StreamFuntionException(5, 5, "Formate illegal");
            return SSDR_170.SdrItemInput(ref Hostmsg, type, ref value, 200);
        }
        private int HostMsgItemInput(ref SSDR_170.SDRMSG Hostmsg, int type, ref IntPtr value)
        {

            if (type != Hostmsg.next)
                throw new StreamFuntionException(5, 5, "Formate illegal");
            return SSDR_170.SdrItemInput(ref Hostmsg, type, value, 200);


        }

        public int DataItemIn(SecsFormateType formate, ref object msg, ref object data)
        {

            int MsgStatus = 0;
            SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)msg;
            switch (formate)
            {
                case SecsFormateType.L:
                    int nullvalue = 0;
                    MsgStatus = HostMsgItemInput(ref Hostmsg, GWSecsFormateType[formate], ref nullvalue);
                    break;
                case SecsFormateType.U1:
                case SecsFormateType.U2:
                case SecsFormateType.U4:
                case SecsFormateType.Bool: // >0 true ,<0 false 
                case SecsFormateType.B:
                case SecsFormateType.I2:
                    int value = 0;
                    MsgStatus = HostMsgItemInput(ref Hostmsg, GWSecsFormateType[formate], ref value);
                    data = (object)value;
                    break;
                case SecsFormateType.F4:
                    float  valuefloat = 0;
                    MsgStatus = HostMsgItemInput(ref Hostmsg, GWSecsFormateType[formate], ref valuefloat);
                    data = (object)valuefloat;
                    break;
                case SecsFormateType.F8:
                    double valuedouble = 0;
                    MsgStatus = HostMsgItemInput(ref Hostmsg, GWSecsFormateType[formate], ref valuedouble);
                    data = (object)valuedouble;
                    break;
                case SecsFormateType.A:
                    IntPtr TempPtr = Str;
                    MsgStatus = HostMsgItemInput(ref Hostmsg, GWSecsFormateType[formate], ref TempPtr);
                    data = Marshal.PtrToStringAnsi(TempPtr, MsgStatus);
                    break;
                

                   
                default:
                    throw new StreamFuntionException(5, 5, "Formate illegal");
            }
            msg = (object)Hostmsg;
            return MsgStatus;


        }
        public int DataIteminitin(ref object msg, SecsMessageObject1Args secsobject)
        {
            try
            {
                SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)msg;
                //  IntPtr MsgBuffer;
                long MsgStates = 0;
                //byte[] HostBuffer = new byte[20000];
                //char[] CMDTemp = new char[3000];

                //unsafe
                //{
                //    fixed (byte* p = HostBuffer)
                //    {
                //        MsgBuffer = new IntPtr();
                //        MsgBuffer = (IntPtr)p;
                //    }
                //    fixed (char* p = CMDTemp)
                //    {
                //        Str = new IntPtr();
                //        Str = (IntPtr)p;
                //    }
                //}
                //Hostmsg.buffer = MsgBuffer;
                //Hostmsg.length = 20000;
                MsgStates = SSDR_170.SdrMessageGet(secsobject._Tick, ref Hostmsg);
                MsgStates = SSDR_170.SdrItemInitI(ref Hostmsg);
                msg = (object)Hostmsg;

                return (int)MsgStates;
            }
            catch (Exception ex)
            {

                _logger.WriteLog(ex);
                return -1;
            }
        }




        public int DataIteminitoutRequest(QsStream Station, QsFunction Function, bool UseWbit, ref object msg)
        {

            try
            {

                //  IntPtr SBuffer;
                IntPtr SBuffer = Marshal.AllocHGlobal(70000);
                SSDR_170.SDRMSG Hostmsg = new SSDR_170.SDRMSG();
                //msg = new SSDR_170.SDRMSG();

                int StatusSend = 0;

                //byte[] textstr = new byte[20000];
                ////待修改

                //unsafe
                //{
                //    fixed (byte* p = textstr)
                //    {
                //        SBuffer = new IntPtr();
                //        SBuffer = (IntPtr)p;
                //    }
                //}
                Hostmsg.stream = (uint)Station;
                Hostmsg.function = (uint)Function;

                /*
                if (Hostmsg.stream == 6 && Hostmsg.function == 11)
                    Hostmsg.wbit = 0;
                else
                */
                Hostmsg.wbit = (UseWbit) ? (uint)1 : (uint)0;
                Hostmsg.buffer = SBuffer;
                Hostmsg.length = 70000;

                StatusSend = SSDR_170.SdrItemInitO(ref Hostmsg);
                msg = (object)Hostmsg;
                return StatusSend;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return -1;
            }
        }
        public int DataIteminitoutResponse(ref object msg, SecsMessageObject1Args secsobject)
        {
            try
            {
                SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)msg;
                IntPtr SBuffer = Marshal.AllocHGlobal(70000);
                // IntPtr bufferPtr;
                long MsgStates;
                //byte[] Bufferb = new byte[20000];
                ////int ack = 0;
                ////待修改
                //unsafe
                //{
                //    fixed (byte* p = Bufferb)
                //    {
                //        bufferPtr = new IntPtr();
                //        bufferPtr = (IntPtr)p;
                //    }
                //}
                Hostmsg.wbit = 0;

                Hostmsg.function++;
                Hostmsg.buffer = SBuffer;
                Hostmsg.length = 70000;
                byte[] nothing = new byte[0];
                MsgStates = SSDR_170.SdrItemInitO(ref Hostmsg);
                msg = (object)Hostmsg;
                return (int)MsgStates;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("<Secs>" + ex.Message);
                return -1;
            }
        }


        public int DataIteminitoutResponse(QsStream Station, QsFunction Function, ref object msg, SecsMessageObject1Args secsobject)  //220509
        {
            try
            {
                SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)msg;

                IntPtr SBuffer = Marshal.AllocHGlobal(70000);
                //IntPtr bufferPtr;
                long MsgStates;
                //byte[] Bufferb = new byte[20000];
                //int ack = 0;
                //待修改
                //unsafe
                //{
                //    fixed (byte* p = Bufferb)
                //    {
                //        bufferPtr = new IntPtr();
                //        bufferPtr = (IntPtr)p;
                //    }
                //}
                Hostmsg.wbit = 0;
                Hostmsg.stream = (uint)Station;
                Hostmsg.function = (uint)Function;
                Hostmsg.buffer = SBuffer;
                Hostmsg.length = 20000;
                byte[] nothing = new byte[0];
                MsgStates = SSDR_170.SdrItemInitO(ref Hostmsg);
                msg = (object)Hostmsg;
                return (int)MsgStates;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("<Secs>" + ex.Message);
                return -1;
            }
        }
        public int DataItemOut(SecsFormateType formate, ref object msg, object data, params int[] Listcount)
        {
            try
            {
                SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)msg;
                int MsgStatus = 0;
                switch (formate)
                {
                    case SecsFormateType.L:
                        MsgStatus = SSDR_170.SdrItemOutput(ref Hostmsg, GWSecsFormateType[formate], null, Listcount[0]);

                        break;
                    case SecsFormateType.U1:
                    case SecsFormateType.U2:
                    case SecsFormateType.U4:
                    case SecsFormateType.Bool: // >0 true ,<0 false 
                    case SecsFormateType.B:
                        int value = (int)data;
                        MsgStatus = SSDR_170.SdrItemOutput(ref Hostmsg, GWSecsFormateType[formate], ref value, 1);
                        break;

                    //  long valuelong = (long)data;
                    //   MsgStatus = SSDR_170.SdrItemOutput(ref Hostmsg, GWSecsFormateType[formate], ref valuelong, 1);
                    //  break;
                    case SecsFormateType.F4:
                    case SecsFormateType.F8:
                        double valuefloat = (double)data;
                        MsgStatus = SSDR_170.SdrItemOutput(ref Hostmsg, GWSecsFormateType[formate], ref valuefloat, 1);
                        data = (object)valuefloat;
                        break;
                    case SecsFormateType.A:
                        string tempstr = (string)data;
                        MsgStatus = SSDR_170.SdrItemOutput(ref Hostmsg, GWSecsFormateType[formate], tempstr, tempstr.Length);
                        break;
                    default:
                        throw new StreamFuntionException(5, 5, "Formate illegal");
                }
                msg = Hostmsg;
                return MsgStatus;

            }
            catch (StreamFuntionException ex) // Formate error
            {

                _logger.WriteLog("<Secs>" + ex.Message);
                return -1;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("<Secs>" + ex.Message);
                return -1;
            }
        }
        /*
          public void SendMessage(QsStream Station, QsFunction Function, object SecsMessage)
          {
              try
              {
                  SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)SecsMessage;
                  if ((int)Station == Hostmsg.stream && (int)Function == Hostmsg.function) // double check 
                      _queSendBuffer.Enqueue(Hostmsg);
              }
              catch (Exception ex)
              {
                  _logger.WriteLog("<Secs>" + ex.Message);
              }
          }
       */


        public void SendMessage(QsStream Station, QsFunction Function, object SecsMessage, SecsMessageObject1Args secsobject)
        {
            try
            {
                GWSECSMessageSend SendMsg;

                if (secsobject != null)
                    SendMsg = new GWSECSMessageSend((SSDR_170.SDRMSG)SecsMessage, secsobject._Tick, SecsMessage, Station, Function);
                else
                    SendMsg = new GWSECSMessageSend((SSDR_170.SDRMSG)SecsMessage, 0, SecsMessage, Station, Function);
                _queSendBuffer.Enqueue(SendMsg);


                //int SendStatus = 0;
                ////ushort DEVICEID = (ushort)_SecsConfig.DDEVICEID;
                //SSDR_170.SDRMSG _PrePmsg = (SSDR_170.SDRMSG)SecsMessage;
                //if (_PrePmsg.wbit == 1 ||
                //    (_PrePmsg.stream == 6 && _PrePmsg.function == 11) ||
                //    (_PrePmsg.stream == 5 && _PrePmsg.function == 1) ||
                //    (_PrePmsg.stream == 10 && _PrePmsg.function == 1)
                //     )
                //{
                //    SendStatus = SSDR_170.SdrRequest(DEVICEID, ref _PrePmsg, ref tkx);

                //    //   SSDR_170.SdrTicketPoll(ref tkx, ref _Pmsg);
                //    //   SSDR_170.SdrTicketDrop(tkx);
                //}
                //else if (_PrePmsg.wbit == 0)
                //{

                //    SendStatus = SSDR_170.SdrResponse(secsobject._Tick, ref _PrePmsg);
                //    // SSDR_170.SdrTicketDrop(secsobject._Tick);
                //}

            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Secs Data Get Set
        /// </summary>
        public bool GetInternalConnected()
        {
            bool Connected = (state == ConnState.SCS_SCONN) ? true : false;
            return Connected;
        }
        public bool GetSecsStarted()
        {
            return _SecsStart;
        }
        public string GetLocalIP()
        {
            return _SecsConfig.LocalIP;
        }
        public SECSMODE GetSecsMode()
        {
            return _SecsConfig.Mode;
        }
        public int GetLocalPort()
        {
            return _SecsConfig.LocalPort;
        }
        public int GetDeviceID()
        {
            return _SecsConfig.DDEVICEID;
        }
        public int GetT3TimeOut()
        {
            return _SecsConfig.T3;
        }
        public int GetT5TimeOut()
        {
            return _SecsConfig.T5;
        }
        public int GetT6TimeOut()
        {
            return _SecsConfig.T6;
        }
        public int GetT7TimeOut()
        {
            return _SecsConfig.T7;
        }
        public int GetT8TimeOut()
        {
            return _SecsConfig.T8;
        }

        public void SetDeviceID(int ID)
        {
            throw new NotImplementedException();
        }
        public void SetLocalIP(string IP)
        {
            _SecsConfig.LocalIP = IP;

        }
        public void SetLocalPort(int PortID)
        {
            _SecsConfig.LocalPort = PortID;

        }
        public void SetSECSMODE(SECSMODE mode)
        {
            throw new NotImplementedException();
        }
        public void SetT3TimeOut(int T3)
        {
            _SecsConfig.T3 = T3;
        }
        public void SetT5TimeOut(int T5)
        {
            _SecsConfig.T5 = T5;
        }
        public void SetT6TimeOut(int T6)
        {
            _SecsConfig.T6 = T6;
        }
        public void SetT7TimeOut(int T7)
        {
            _SecsConfig.T7 = T7;
        }
        public void SetT8TimeOut(int T8)
        {
            _SecsConfig.T8 = T8;
        }
        public void SaveConfig()
        {
            UpdateSecsData();
            GetSecsData();
        }
        void GetSecsData()
        {
            try
            {
                StreamReader _SecsIni;
                string[] Text;
                string[] stringSeparators = new string[] { "\r\n" };
                _SecsIni = new StreamReader(SecsPath);
                string str = _SecsIni.ReadToEnd();

                Text = str.Split(stringSeparators, StringSplitOptions.None);
                _SecsIni.Close();
                var Data = from strt in Text
                           where strt.Contains("PROTOCOL") || strt.Contains("PASSIVE ENTITY IPADDRESS") || strt.Contains("PASSIVE ENTITY TCPPORT")
                           || strt.Contains("T3") || strt.Contains("T5") || strt.Contains("T6") || strt.Contains("T7") || strt.Contains("T8")
                           select strt.Split()[strt.Split().Count() - 1];
                _SecsConfig.Mode = (Data.ToList()[0] == "HSMS94") ? SECSMODE.HSMS_MODE : SECSMODE.SECS_MODE;
                _SecsConfig.LocalIP = Data.ToList()[1];
                _SecsConfig.LocalPort = Convert.ToInt16(Data.ToList()[2]);
                _SecsConfig.T3 = Convert.ToInt16(Data.ToList()[3]);
                _SecsConfig.T5 = Convert.ToInt16(Data.ToList()[4]);
                _SecsConfig.T6 = Convert.ToInt16(Data.ToList()[5]);
                _SecsConfig.T7 = Convert.ToInt16(Data.ToList()[6]);
                _SecsConfig.T8 = Convert.ToInt16(Data.ToList()[7]);
                _SecsConfig.DDEVICEID = 0;


            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        void UpdateSecsData()
        {
            try
            {

                StreamReader _SecsIni;
                StreamWriter _Wirte;
                string[] stringSeparators = new string[] { "\r\n" };
                string[] Text;
                _SecsIni = new StreamReader(SecsPath);

                string str = _SecsIni.ReadToEnd();
                string UpdateData = "";
                Text = str.Split(stringSeparators, StringSplitOptions.None);
                _SecsIni.Close();
                int IPindex = Text.ToList().FindIndex(x => x.ToString().Contains("PASSIVE ENTITY IPADDRESS"));
                int Portindex = Text.ToList().FindIndex(x => x.ToString().Contains("PASSIVE ENTITY TCPPORT"));
                int T3index = Text.ToList().FindIndex(x => x.ToString().Contains("T3"));
                int T5index = Text.ToList().FindIndex(x => x.ToString().Contains("T5"));
                int T6index = Text.ToList().FindIndex(x => x.ToString().Contains("T6"));
                int T7index = Text.ToList().FindIndex(x => x.ToString().Contains("T7"));
                int T8index = Text.ToList().FindIndex(x => x.ToString().Contains("T8"));
                Text[IPindex] = string.Format("    PASSIVE ENTITY IPADDRESS {0}", _SecsConfig.LocalIP);
                Text[Portindex] = string.Format("    PASSIVE ENTITY TCPPORT {0}", _SecsConfig.LocalPort);
                Text[T3index] = string.Format("    T3 {0}", _SecsConfig.T3);
                Text[T5index] = string.Format("    T5 {0}", _SecsConfig.T5);
                Text[T6index] = string.Format("    T6 {0}", _SecsConfig.T6);
                Text[T7index] = string.Format("    T7 {0}", _SecsConfig.T7);
                Text[T8index] = string.Format("    T8 {0}", _SecsConfig.T8);
                for (int i = 0; i < Text.Count(); i++)
                {
                    UpdateData += Text[i] + '\r' + '\n';
                }
                _Wirte = new StreamWriter(SecsPath);
                _Wirte.Write(UpdateData);
                _Wirte.Close();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }



        /// <summary>
        /// Secs action
        /// </summary>
        public bool InitSecs()
        {
            try
            {
                int Status = 0;
                //ushort DEVICEID = (ushort)_SecsConfig.DDEVICEID;
                if ((Status = SSDR_170.SdrStart(4000, 60, null, null)) != 0)
                {
                    if (Status == -14) // SDR.exe還存在
                    {


                        Status = SSDR_170.SdrIdDisable(DEVICEID);
                        Status = SSDR_170.SdrPortDisable(DEVICEID);
                        // Status = SSDR_170.SdrPortSeparate(DEVICEID);
                        SSDR_170.SetEvent(EventHandle);
                        SSDR_170.SdrStop(null);
                        Thread.Sleep(2000);
                    }
                    else if (Status == -17) // SDRL.exe還存在 
                    {
                        CloseSecsLog();
                        Thread.Sleep(3000);
                    }
                    else
                        throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrStart\",Error code = " + Status);

                    if ((Status = SSDR_170.SdrStart(2500, 60, null, null)) != 0)
                    {
                        if (Status == -17) // SDRL.exe還存在 
                        {
                            CloseSecsLog();
                            Thread.Sleep(3000);
                        }
                        else
                            throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrStart\",Error code = " + Status);
                        if ((Status = SSDR_170.SdrStart(2500, 60, null, null)) != 0)
                            throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrStart\",Error code = " + Status);
                    }

                }
                if ((Status = SSDR_170.SdrConfigure("", "-i", SecsPath, 3, null)) != 0)
                    throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrConfigure\",Error code = " + Status);

                SDRAUTODROP();

                if ((Status = SSDR_170.SdrPortEnable(DEVICEID)) != 0)
                    throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrPortEnable\",Error code = " + Status);

                if ((Status = SSDR_170.SdrIdEnable(DEVICEID)) != 0)
                    throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrIdEnable\",Error code = " + Status);



                EventHandle = SSDR_170.CreateEvent(IntPtr.Zero, false, false, null);

                if ((Status = SSDR_170.SdrIdSemSet(DEVICEID, EventHandle)) != 0)
                    throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrIdSemSet\",Error code = " + Status);

                if ((Status = SSDR_170.SdrPortSemSet(DEVICEID, EventHandle)) != 0)
                    throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrIdSemSet\",Error code = " + Status);

                //if ((Status = SSDR_170.SdrAutoDropTimeSet(DEVICEID, 100)) != 0)
                //    throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrAutoDropTimeSet\",Error code = " + Status);

                // if ((Status = SSDR_170.SdrAutoDropTimeGet(DEVICEID, 50)) != 0)
                //     throw new Exception("Open Secs fail,Function:\"SSDR_170.SdrAutoDropTimeSet\",Error code = " + Status);

                _exeCheckConnectStatus = new SPollingThread(100);
                _exeCheckConnectStatus.DoPolling += _exeCheckConnectStatus_DoPolling;


                _exeReciveSecsMsg = new SPollingThread(50);
                _exeReciveSecsMsg.DoPolling += _exeReciveSecsMsg_DoPolling;

                _pollingRecv = new SPollingThread(10);
                _pollingRecv.DoPolling += _pollingRecv_DoPolling;

                _pollingSend = new SPollingThread(10);
                _pollingSend.DoPolling += _pollingSend_DoPolling;

                // waitFirstRec = true;

                // _PollingHotbeat.DoPolling += _PollingHotbeat_DoPolling;

                _exeCheckConnectStatus.Set();

                OpenSecsLog();
                _SecsStart = true;
                return true;
            }
            catch (Exception ex)
            {
                _SecsStart = false;
                _logger.WriteLog(ex);
                return false;
            }
        }
        public bool SecsReStart()
        {
            throw new NotImplementedException();
        }
        public bool SecsStop()
        {
            try
            {
                CloseSecsLog();
                //  Taskkill("sdrl.exe");
                // ushort DEVICEID = (ushort)_SecsConfig.DDEVICEID;
                if (_exeReciveSecsMsg != null)
                {
                    _exeReciveSecsMsg.Close();
                    _exeReciveSecsMsg.Dispose();
                }
                if (_pollingRecv != null)
                {
                    _pollingRecv.Close();
                    _pollingRecv.Dispose();
                }

                if (_pollingSend != null)
                {
                    _pollingSend.Close();
                    _pollingSend.Dispose();
                    Thread.Sleep(2000);
                }

                SSDR_170.SdrIdDisable(DEVICEID);
                SSDR_170.SdrPortDisable(DEVICEID);
                //  Status = SSDR_170.SdrPortSeparate(DEVICEID);


                // Taskkill("sdr.exe");

                if (_exeCheckConnectStatus != null)
                {
                    _exeCheckConnectStatus.Close();
                    _exeCheckConnectStatus.Dispose();
                }
                SSDR_170.SetEvent(EventHandle);
                SSDR_170.SdrStop(null);

                TransferSecsLog(true);
                _SecsStart = false;
                if (InternalStatusChange != null)
                    InternalStatusChange(new SecsInternalArgs(ConnState.SCS_ABDIS_SCS_INDIS));
                state = ConnState.SCS_ABDIS_SCS_INDIS;
                _logger.WriteLog("Secs Stop Success!!");
                Thread.Sleep(2000);
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("Secs Stop Fail!!");
                return false;
            }
        }

        private void Taskkill(string ProcessName)
        {
            try
            {
                using (Process P = new Process())
                {
                    P.StartInfo = new ProcessStartInfo()
                    {
                        FileName = "taskkill",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = "/F /IM \"" + ProcessName + "\""
                    };
                    P.Start();
                    P.WaitForExit(60000);
                }
            }
            catch
            {
                using (Process P = new Process())
                {
                    P.StartInfo = new ProcessStartInfo()
                    {
                        FileName = "tskill",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = "\"" + ProcessName + "\" /A /V"
                    };
                    P.Start();
                    P.WaitForExit(60000);
                }
            }
        }
        void checkHostConnStatus(int Status)
        {
            state = (ConnState)Status;
            PreStats = Status;
            try
            {
                switch (state)
                {
                    case ConnState.SCS_NCONN:
                        _logger.WriteLog("<Secs> never connected");

                        break;
                    case ConnState.SCS_SCONN:
                        _logger.WriteLog("<Secs> SECS-connected");
                        _exeReciveSecsMsg.Set();
                        _pollingRecv.Set();
                        _pollingSend.Set();
                        deviceidTick = 0;
                        break;
                    case ConnState.SCS_ABDIS:
                        _logger.WriteLog("<Secs> abrupt disconnect");

                        break;
                    case ConnState.SCS_INDIS:
                        _logger.WriteLog("<Secs> intentional disconnect");

                        break;
                    case ConnState.SCS_ABDIS_SCS_INDIS:
                        _logger.WriteLog("<Secs> disconnect");
                        _exeReciveSecsMsg.Reset();
                        _pollingRecv.Reset();
                        _pollingSend.Reset();
                        break;
                    default:
                        _logger.WriteLog("<Secs> NT error code.");
                        _exeReciveSecsMsg.Reset();
                        _pollingRecv.Reset();
                        _pollingSend.Reset();
                        break;
                }
                if (InternalStatusChange != null)
                    InternalStatusChange(new SecsInternalArgs(state));
            }
            catch (Exception ex)
            {
                _logger.WriteLog("<Secs>" + ex.Message);
            }

        }

        /// <summary>
        ///Secs log action
        /// </summary>
        int CloseSecsLog()
        {
            try
            {
                //  this.lognot("Stop Log !!");
                string CMD = string.Format("off stop");
                return SSDR_170.ShellExecute(null, "open", "c:\\sdr\\sdrlctl.exe", CMD, null, 0);

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return -1;
            }
        }
        int OpenSecsLog()
        {
            try
            {

                bool IsOverWrite = false;
                // int State = 0;
                string pathFile = System.Environment.CurrentDirectory + "\\GWASDRL.00";
                if (File.Exists(pathFile))
                {
                    if (File.GetLastWriteTime(pathFile).Day != DateTime.Now.Day)
                    {
                        TransferSecsLog(false);
                        File.Delete(pathFile);
                    }
                }
                string OverWrite = (IsOverWrite) ? "new" : "append";
                string CMD = string.Format("-on -f 15m -m 1m -{0}", OverWrite);

                //  State = SSDR_170.ShellExecute(null, "open", "c:\\sdr\\sdrl.exe", CMD, null, 1);

                return SSDR_170.ShellExecute(null, "open", "c:\\sdr\\sdrl.exe", CMD, null, 0);


            }
            catch (Exception ex)
            {
                _logger.WriteLog("<<Error>>Secs log Open file fail");
                return -1;
            }
        }

        void SDRAUTODROP()
        {
            SSDR_170.ShellExecute(null, "open", "c:\\sdr\\sdrdrop.exe", "0 3000", null, 0);
        }

        public void TransferSecsLog(bool IsClose)
        {
            try
            {
                //  StopLOG();
                string SoucepathFile = System.Environment.CurrentDirectory + "\\GWASDRL.00";
                if (File.Exists(SoucepathFile))
                {
                    string TargetPath = System.Environment.CurrentDirectory + "\\SecslogMsg\\";
                    string SDRLPCMD = System.Environment.CurrentDirectory + "\\sdrlp.exe ";
                    if (!Directory.Exists(TargetPath))
                        Directory.CreateDirectory(TargetPath);
                    string TargetPathFile = TargetPath + "Secslog-" + DateTime.Now.ToString("yyyMMdd");
                    File.Copy(SoucepathFile, TargetPathFile + ".bin", true);
                    string CMD = string.Format("/c sdrlp.exe {0} > {1}", TargetPathFile + ".bin", TargetPathFile + ".log");
                    SSDR_170.ShellExecute(null, "open", "cmd.exe", CMD, null, 0);
                    if (File.GetLastWriteTime(SoucepathFile).Day != DateTime.Now.Day && IsClose)
                    //IsOverWrite = true;
                    {
                        CloseSecsLog();
                        Thread.Sleep(3000);
                        File.Delete(SoucepathFile);
                        OpenSecsLog();
                    }
                }
                //  startLOG();

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        /// <summary>
        /// Thread Function
        /// </summary>
        private void _exeCheckConnectStatus_DoPolling()
        {
            int ConnectStatus = 0;

            ConnectStatus = SSDR_170.SdrConnectionStatusGet((ushort)_SecsConfig.DDEVICEID);

            if (ConnectStatus > 5 || ConnectStatus < 1)
            {
                _logger.WriteLog("<Secs> NT error code. Status = " + ConnectStatus.ToString());
                return;
            }

            if (ConnectStatus != PreStats)
                checkHostConnStatus(ConnectStatus);

        }
        private void _exeReciveSecsMsg_DoPolling()
        {
            try
            {

                //  SSDR_170.WaitForSingleObject(EventHandle, 900000);

                if ((Status = SSDR_170.SdrIdPoll(DEVICEID, ref deviceidTick, ref _Pmsg)) != 1)
                //if((Status = SSDR_170.SdrTicketPoll(ref deviceidTick, ref _Pmsg)) != 1)
                {
                    if (!_SecsStart)
                        SSDR_170.CloseHandle(EventHandle);

                    // SSDR_170.SdrTicketDrop(deviceidTick);
                    return;
                }

                // Stats need local and Remote ,

                //     if (SECSMessageList.ContainsKey(deviceidTick))
                //           SECSMessageList.Remove(deviceidTick);
                //     SECSMessageList.Add(deviceidTick, new GWSECSMessage(_Pmsg, deviceidTick));


                //   SECSMessage = new GWSECSMessage(_Pmsg, deviceidTick);
                // if(ReceiveSecsMessage != null)
                //     ReceiveSecsMessage(new SecsMessageObject1Args((QsStream)SECSMessageList[deviceidTick].msg.stream, (QsFunction)SECSMessageList[deviceidTick].msg.function, SECSMessageList[deviceidTick].msg, (int)SECSMessageList[deviceidTick].msg.wbit));

                //  if (_Pmsg.wbit == 0)
                //    {
                //       SSDR_170.SdrTicketDrop(deviceidTick);
                //        SECSMessageList.Remove(deviceidTick);
                //   }
                //  SSDR_170.SDRMSG _PrePmsg = _Pmsg;
                // Thread.Sleep(1000);
                //if ((Status = SSDR_170.SdrTicketPoll(deviceidTick, ref _Pmsg)) != 1)
                //{

                //}
                //if (_Pmsg.wbit == 0)
                //{


                //    SSDR_170.SdrTicketDrop(deviceidTick);
                //    return;
                // }
                // SSDR_170.SdrMessageGet(ref deviceidTick, ref _Pmsg);
                SECSMessage = new GWSECSMessage(_Pmsg, deviceidTick);
                if (SECSMessage.msg.wbit == 0)
                    SSDR_170.SdrTicketDrop(SECSMessage.Ticks);

                if (!(SECSMessage.msg.stream == 6 && (SECSMessage.msg.function == 2 || SECSMessage.msg.function == 12)))
                    _queRecvSecsMsg.Enqueue(SECSMessage);
                // if (ReceiveSecsMessage != null)
                //    ReceiveSecsMessage(new SecsMessageObject1Args((QsStream)_PrePmsg.stream, (QsFunction)_PrePmsg.function, _Pmsg, (int)_Pmsg.wbit, deviceidTick));
                // _queRecvBuffer.Enqueue(_PrePmsg);



                // ParseSecsMessage();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        private void _pollingRecv_DoPolling()
        {
            // SSDR_170.SDRMSG _PrePmsg;
            GWSECSMessage _PrePmsg;
            //  if (!_queRecvBuffer.TryDequeue(out _PrePmsg)) return;
            if (!_queRecvSecsMsg.TryDequeue(out _PrePmsg)) return;

            object SecsHead = null; // SDR not Head   //220509

            if (ReceiveSecsMessage != null)
                ReceiveSecsMessage(new SecsMessageObject1Args((QsStream)_PrePmsg.msg.stream, (QsFunction)_PrePmsg.msg.function, SecsHead, _PrePmsg.msg, (int)_PrePmsg.msg.wbit, _PrePmsg.Ticks));
            // if (ReceiveSecsMessage != null)
            //      ReceiveSecsMessage(new SecsMessageObject1Args((QsStream)_PrePmsg.stream, (QsFunction)_PrePmsg.function, _Pmsg, (int)_Pmsg.wbit));
        }
        private void _pollingSend_DoPolling()
        {
            try
            {
                GWSECSMessageSend _PrePmsg;
                long _StatusSend = 0;
                if (!_queSendBuffer.TryDequeue(out _PrePmsg)) return;

                if (_PrePmsg.msg.wbit == 1 ||
                    (_PrePmsg.msg.stream == 6 && _PrePmsg.msg.function == 11) ||
                    (_PrePmsg.msg.stream == 5 && _PrePmsg.msg.function == 1) ||
                    (_PrePmsg.msg.stream == 10 && _PrePmsg.msg.function == 1)
                     )
                {
                    _StatusSend = SSDR_170.SdrRequest((ushort)_SecsConfig.DDEVICEID, ref _PrePmsg.msg, ref tkx);
                }
                else if (_PrePmsg.msg.wbit == 0)
                    _StatusSend = SSDR_170.SdrResponse(_PrePmsg.Ticks, ref _PrePmsg.msg);

                Marshal.FreeHGlobal(_PrePmsg.msg.buffer);
            }
            catch (Exception)
            {

            }
        }

        public int DataIteminitoutResponse(ref object msg)
        {
            try
            {
                SSDR_170.SDRMSG Hostmsg = (SSDR_170.SDRMSG)msg;
                IntPtr bufferPtr;
                long MsgStates;
                byte[] Bufferb = new byte[20000];
                //int ack = 0;
                //待修改
                unsafe
                {
                    fixed (byte* p = Bufferb)
                    {
                        bufferPtr = new IntPtr();
                        bufferPtr = (IntPtr)p;
                    }
                }
                Hostmsg.wbit = 0;

                Hostmsg.function++;
                Hostmsg.buffer = bufferPtr;
                Hostmsg.length = 20000;
                byte[] nothing = new byte[0];
                MsgStates = SSDR_170.SdrItemInitO(ref Hostmsg);
                msg = (object)Hostmsg;
                return (int)MsgStates;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("<Secs>" + ex.Message);
                return -1;
            }
        }

        public bool InitSecsForEQP()
        {
            throw new NotImplementedException();
        }
    }
    public class GWSECSMessage
    {
        public SSDR_170.SDRMSG msg;
        public ushort Ticks;

        public GWSECSMessage(SSDR_170.SDRMSG SECSmsg, ushort Tick)
        {
            msg = SECSmsg;
            Ticks = Tick;
        }
    }
    public class GWSECSMessageSend
    {
        public SSDR_170.SDRMSG msg;
        public ushort Ticks;
        public object SecsMessage;
        public QsStream Station;
        public QsFunction Function;
        public GWSECSMessageSend(SSDR_170.SDRMSG SECSmsg, ushort Tick, object Messge, QsStream S, QsFunction F)
        {
            msg = SECSmsg;
            Ticks = Tick;
            SecsMessage = Messge;
            Station = S;
            Function = F;
        }
    }
}
