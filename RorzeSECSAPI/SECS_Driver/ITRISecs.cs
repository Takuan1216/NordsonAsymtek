using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QSACTIVEXLib;
using System.Collections.Concurrent;
using RorzeComm;

using Rorze.DB;
using RorzeComm.Threading;
using RorzeComm.Log;

namespace Rorze.Secs
{
    public class ITRISecs : ISECS
    {
        public event SecsInternalArgs.SecsInternalHandle InternalStatusChange;
        public event SecsMessageObject1Args.SecsMessageHandle ReceiveSecsMessage;
        private QSWrapperClass _qSecs;
        private ConcurrentQueue<SStreamFunction> _queSendBuffer;        //buffer of message to send host
        private ConcurrentQueue<SStreamFunction> _queRecvBuffer;        //buffer of message to receive and wait process
        private SPollingThread _pollingSend;                            //polling to send stream-function
        private SPollingThread _pollingRecv;                            //polling to receive stream-function
        private SLogger _logger ;
        SECSParameterConfig _SecsParameter;
        SECSConneetConfig _SecsConfig;
       // MainDB _DB;
        ConnState state;
        ConnState Prestate;
        bool _SecsStart;
        private object _objTemp;
        bool SECSStop = false;
        public ITRISecs(SECSConneetConfig Config, SECSParameterConfig Parameter)
        {
          
          
            _SecsConfig = Config;
            _SecsParameter = Parameter;

            _qSecs = new QSWrapperClass();
            _pollingRecv = new SPollingThread(10);
            _pollingRecv.DoPolling += _pollingRecv_DoPolling;
            _pollingSend = new SPollingThread(100);
            _pollingSend.DoPolling += _pollingSend_DoPolling;
            _queSendBuffer = new ConcurrentQueue<SStreamFunction>();
            _queRecvBuffer = new ConcurrentQueue<SStreamFunction>();

            _logger = SLogger.GetLogger("ITRI_SECSDriver1");
            _SecsStart = false;
            state = ConnState.SCS_NCONN;
            Prestate = ConnState.SCS_NCONN;
            _qSecs = new QSWrapperClass();
        }

        private void _pollingSend_DoPolling()
        {
            try
            {
                SStreamFunction sfData;
                if (!_queSendBuffer.TryDequeue(out sfData)) return;

                //data transfer
                _qSecs.SendSECSIIMessage((int)sfData.S, (int)sfData.F, sfData.Wbit, ref sfData.SystemByte, sfData.Data[0]);
                //  Send(sfData.S, sfData.F, sfData.SystemByte, sfData.Wbit, sfData.Data);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        private Dictionary<SecsFormateType, SECSII_DATA_TYPE> ITRISecsFormateType = new Dictionary<SecsFormateType, SECSII_DATA_TYPE>()
        {
            {SecsFormateType.L,SECSII_DATA_TYPE.LIST_TYPE},
            {SecsFormateType.A,SECSII_DATA_TYPE.ASCII_TYPE},
            {SecsFormateType.U1,SECSII_DATA_TYPE.UINT_1_TYPE},
            {SecsFormateType.U2,SECSII_DATA_TYPE.UINT_2_TYPE},
            {SecsFormateType.U4,SECSII_DATA_TYPE.UINT_4_TYPE},
            {SecsFormateType.B,SECSII_DATA_TYPE.BINARY_TYPE },
            {SecsFormateType.Bool,SECSII_DATA_TYPE.BOOLEAN_TYPE},
            {SecsFormateType.F4,SECSII_DATA_TYPE.FT_4_TYPE },
            {SecsFormateType.F8,SECSII_DATA_TYPE.FT_8_TYPE },

        };
        private void _pollingRecv_DoPolling()
        {
            SStreamFunction sfData;
            if (!_queRecvBuffer.TryDequeue(out sfData)) return;

            int offset = 0;
            SecsMsgClass SecsMsg = new SecsMsgClass(sfData.Data[0], offset, sfData.SystemByte, sfData.Wbit);
            if (ReceiveSecsMessage != null)
                ReceiveSecsMessage(new SecsMessageObject1Args((QsStream)sfData.S, (QsFunction)sfData.F, SecsMsg, sfData.Wbit,1));
        }
        private void Send(QsStream S, QsFunction F,int WBit, int systemByte, params object[] objs)
        {
            int nWBit = 0;
            object objSend = null;
            /*
            switch (S)
            {
                case QsStream.S3:
                case QsStream.S16:
                case QsStream.S14:
                case QsStream.S1:
                case QsStream.S7:
                case QsStream.S2:
                case QsStream.S10:

                    nWBit = 1;
                    break;



               // case QsStream.S5: nWBit = _nWBitS5; break;
                    // case QsStream.S6: nWBit = _nWBitS6; break;
                    //   case QsStream.S10: nWBit = _nWBitS10; break;
            }
            //===== data format
            */
            TransferSECS_S(ref objSend, objs);
            _qSecs.SendSECSIIMessage((int)S, (int)F, nWBit, ref systemByte, objSend);
        }
        private int TransferSECS_S(ref object objData, object[] Data)
        {
            int nSystemByte = 0;
            try
            {
                foreach (object obj in Data)
                {
                    long[] lngData;
                    double[] dblData;
                    switch (obj.GetType().Name)
                    {
                        case "L":
                            _qSecs.DataItemOut(ref objData, ((L)obj).Root.Length, SECSII_DATA_TYPE.LIST_TYPE, null);
                            nSystemByte += TransferSECS_S(ref objData, ((L)obj).Root);
                            break;
                        case "ASCII_TYPE":
                            nSystemByte += _qSecs.DataItemOut(ref objData, ((ASCII_TYPE)obj).Data.Length, SECSII_DATA_TYPE.ASCII_TYPE, ((ASCII_TYPE)obj).Data);
                            break;
                        case "BOOLEAN_TYPE":
                            lngData = new long[1] { ((BOOLEAN_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.BOOLEAN_TYPE, lngData);
                            break;
                        case "BINARY_TYPE":
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.BINARY_TYPE, ((BINARY_TYPE)obj).Data);
                            break;
                        case "UINT_1_TYPE":
                            lngData = new long[1] { ((UINT_1_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.UINT_1_TYPE, lngData);
                            break;
                        case "UINT_2_TYPE":
                            lngData = new long[1] { ((UINT_2_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.UINT_2_TYPE, lngData);
                            break;
                        case "UINT_4_TYPE":
                            lngData = new long[1] { ((UINT_4_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.UINT_4_TYPE, lngData);
                            break;
                        case "INT_1_TYPE":
                            lngData = new long[1] { ((INT_1_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.INT_1_TYPE, lngData);
                            break;
                        case "INT_2_TYPE":
                            lngData = new long[1] { ((INT_2_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.INT_2_TYPE, lngData);
                            break;
                        case "INT_4_TYPE":
                            lngData = new long[1] { ((INT_4_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.INT_4_TYPE, lngData);
                            break;
                        case "FT_4_TYPE":
                            dblData = new double[1] { ((FT_4_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.FT_4_TYPE, dblData);
                            break;
                        case "FT_8_TYPE":
                            dblData = new double[1] { ((FT_8_TYPE)obj).Data };
                            nSystemByte += _qSecs.DataItemOut(ref objData, 1, SECSII_DATA_TYPE.FT_8_TYPE, dblData);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
            return nSystemByte;
        }
        private static int IntArrayCount(object obj) { return ((int[])obj).Count(); }
        private static int IntArrayZero(object obj) { return ((int[])obj)[0]; }
        private static double DoubleArrayZero(object obj) { return ((double[])obj)[0]; }
        private static double FloatArrayZero(object obj) { return ((float[])obj)[0]; }
        public int DataItemIn(SecsFormateType formate, ref object msg, ref object data)
        {
          
                SecsMsgClass SecsMsg = (SecsMsgClass)msg;
               int nLength = 0;
                switch (formate)
                {
                    case SecsFormateType.L:
                        // SecsMsg.Offset = _qSecs.DataItemIn(ref SecsMsg.Data, SecsMsg.Offset, SECSII_DATA_TYPE.LIST_TYPE, out nLength, null);
                        HostMsgItemInput(ref SecsMsg, SECSII_DATA_TYPE.LIST_TYPE, ref nLength, ref _objTemp);
                        break;
                    case SecsFormateType.U1:
                    case SecsFormateType.U2:
                    case SecsFormateType.U4:
                    case SecsFormateType.Bool:
                    case SecsFormateType.B:
                        // SecsMsg.Offset = _qSecs.DataItemIn(ref SecsMsg.Data, SecsMsg.Offset, ITRISecsFormateType[formate], out nLength, ref _objTemp);
                        HostMsgItemInput(ref SecsMsg, ITRISecsFormateType[formate], ref nLength, ref _objTemp);
                    if (_objTemp != null)
                    {

                        if(IntArrayCount(_objTemp) ==1)
                          data = IntArrayZero(_objTemp);
                        else
                           data = (int[])_objTemp;

                    }
                    else
                    {
                        data = 0;
                    }
                        break;
                    case SecsFormateType.F4:
                    HostMsgItemInput(ref SecsMsg, ITRISecsFormateType[formate], ref nLength, ref _objTemp);
                    if (_objTemp != null)
                        data = FloatArrayZero(_objTemp);
                    break;
                case SecsFormateType.F8:
                        // SecsMsg.Offset = _qSecs.DataItemIn(ref SecsMsg.Data, SecsMsg.Offset, ITRISecsFormateType[formate], out nLength, ref _objTemp);
                        HostMsgItemInput(ref SecsMsg, ITRISecsFormateType[formate], ref nLength, ref _objTemp);
                        if (_objTemp != null)
                         data = DoubleArrayZero(_objTemp);
                        break;
                    case SecsFormateType.A:
                        // SecsMsg.Offset = _qSecs.DataItemIn(ref SecsMsg.Data, SecsMsg.Offset, ITRISecsFormateType[formate], out nLength, ref _objTemp);
                        HostMsgItemInput(ref SecsMsg, ITRISecsFormateType[formate], ref nLength, ref _objTemp);
                    if (_objTemp != null)
                        data = _objTemp.ToString();
                    else
                        data = "";
                        break;
                    default:
                        throw new StreamFuntionException(5, 5, "Formate illegal");
                }

                msg = (object)SecsMsg;
                return nLength;
            
          
        }
        private void HostMsgItemInput(ref SecsMsgClass Hostmsg, SECSII_DATA_TYPE type,ref int Length, ref object value)
        {



            Hostmsg.Offset = _qSecs.DataItemIn(ref Hostmsg.Data, Hostmsg.Offset, type, out Length,ref value);
            if(Hostmsg.Offset<0)
              throw new StreamFuntionException(5, 5, string.Format("Formate illegal"));
        }
        public int DataItemIn(SecsFormateType formate, ref object msg, ref object data, params IntPtr[] Str)
        {
            throw new NotImplementedException();
        }

        public int DataIteminitin(ref object msg, SecsMessageObject1Args secsobject)
        {
            return 0;
        }

        public int DataIteminitin(ref object msg, ref IntPtr Str)
        {
            throw new NotImplementedException();
        }

        public int DataIteminitoutRequest(QsStream Station, QsFunction Function, bool UseWbit, ref object msg)
        {
            int Wbit = (UseWbit == true) ? 1 : 0;
            msg = new SecsMsgClass(null, 0, 0, Wbit);
            return 0;
        }

        public int DataIteminitoutResponse(ref object msg, SecsMessageObject1Args secsobject)
        {
            secsobject.WBit = 0;
            //  msg = new SecsMsgClass(null,0,0);
            SecsMsgClass SecsMsg = (SecsMsgClass)msg;
            SecsMsg.Data = null;
            
            SecsMsg.Offset = 0;
            SecsMsg.Wbit = 0;
            msg = SecsMsg;

            return 0;
        }

        public int DataItemOut(SecsFormateType formate, ref object msg, object data, params int[] Listcount)
        {
            try
            {

                int MsgStatus = 0;
                long[] value;
                object bvalue;
                double[] valuedouble;
                float[] valuefloat;
                SecsMsgClass SecsMsg =(SecsMsgClass) msg;
                switch (formate)
                {
                    case SecsFormateType.L:
                        _qSecs.DataItemOut(ref SecsMsg.Data, Listcount[0], SECSII_DATA_TYPE.LIST_TYPE, null);

                        break;
                    case SecsFormateType.U1:
                        //value = (long)data;
                        UINT_1_TYPE valueU1 = new UINT_1_TYPE((int)data);
                        value = new long[1] { ((UINT_1_TYPE)valueU1).Data };
                        _qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.UINT_1_TYPE, value);
                        break;
                    case SecsFormateType.U2:
                        UINT_2_TYPE valueU2 = new UINT_2_TYPE((int)data);
                        value = new long[1] { ((UINT_2_TYPE)valueU2).Data };
                        _qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.UINT_2_TYPE, value);
                        break;
                    case SecsFormateType.U4:
                        if (data != null)
                        {
                            UINT_4_TYPE valueU4 = new UINT_4_TYPE((int)data);
                            value = new long[1] { ((UINT_4_TYPE)valueU4).Data };
                            _qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.UINT_4_TYPE, value);
                        }
                        else
                        {
                            value = new long[0] ;
                            _qSecs.DataItemOut(ref SecsMsg.Data, 0, SECSII_DATA_TYPE.UINT_4_TYPE, value);
                        }
                        break;
                    case SecsFormateType.Bool: // >0 true ,<0 false
                        bool Temp = ((int)data > 0) ? true : false;
                        BOOLEAN_TYPE valueBool = new BOOLEAN_TYPE(Temp);
                        value = new long[1] { ((BOOLEAN_TYPE)valueBool).Data };
                        _qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.BOOLEAN_TYPE, value);
                        break;
                    case SecsFormateType.B:
                        bvalue = BitConverter.GetBytes((int)data);
                        _qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.BINARY_TYPE, (Byte[])bvalue);
                        break;
                      

                    //  long valuelong = (long)data;
                    //   MsgStatus = SSDR_170.SdrItemOutput(ref Hostmsg, GWSecsFormateType[formate], ref valuelong, 1);
                    //  break;
                    case SecsFormateType.F4:
                        //FT_4_TYPE valueF4 = new FT_4_TYPE((double)data);
                        //valuefloat = new double[1] { ((FT_4_TYPE)valueF4).Data }; 
                        //_qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.FT_4_TYPE, valuefloat);
                        //break;
                    case SecsFormateType.F8:
                        FT_8_TYPE valueF8 = new FT_8_TYPE((double)data);
                        valuedouble = new double[1] { ((FT_8_TYPE)valueF8).Data }; ;
                        _qSecs.DataItemOut(ref SecsMsg.Data, 1, SECSII_DATA_TYPE.FT_8_TYPE, valuedouble);
                        break;
                    case SecsFormateType.A:
                        string tempstr = (string)data;
                        _qSecs.DataItemOut(ref SecsMsg.Data, tempstr.Length, SECSII_DATA_TYPE.ASCII_TYPE, tempstr);
                        break;
                    default:
                        throw new StreamFuntionException(5, 5, "Formate illegal");
                }
               // msg = Hostmsg;
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

        public int GetDeviceID()
        {
            return _SecsConfig.DDEVICEID;
        }

        public bool GetInternalConnected()
        {
            bool Connected = (state == ConnState.SCS_SCONN) ? true : false;
            return Connected;
        }

        public string GetLocalIP()
        {
            return _SecsConfig.LocalIP;
        }

        public int GetLocalPort()
        {
            return _SecsConfig.LocalPort;
        }

        public ConnState GetSecsConnState()
        {
            return state;
        }

        public SECSMODE GetSecsMode()
        {
            return _SecsConfig.Mode;
        }

        public bool GetSecsStarted()
        {
            return _SecsStart;
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

        public bool InitSecs()
        {
            try
            {
              
                _qSecs.lCOMM_Mode = (COMMMODE)_SecsConfig.Mode;
                _qSecs.SECS_Connect_Mode = SECS_COMM_MODE.SECS_EQUIP_MODE;
                _qSecs.HSMS_Connect_Mode = HSMS_COMM_MODE.HSMS_PASSIVE_MODE;
                _qSecs.lDeviceID = _SecsConfig.DDEVICEID;
                _qSecs.lLogEnable = 1;
                _qSecs.szLocalIP = _SecsConfig.LocalIP;
                _qSecs.nLocalPort = _SecsConfig.LocalPort;
                if (SECSStop == false)
                {
                    _qSecs.Initialize();
                    _qSecs.QSEvent += _qSecs_QSEvent;
                }
                else
                {
                    _qSecs.Initialize();
                    _qSecs.QSEvent += _qSecs_QSEvent;
                    SECSStop = false;
                }
                _qSecs.Start();
                _SecsStart = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                _SecsStart = false ;
                return false;
            }
        }

        public bool InitSecsForEQP()  // For EQP
        {
            try
            {

                _qSecs.lCOMM_Mode = (COMMMODE)_SecsConfig.Mode;
                _qSecs.SECS_Connect_Mode = SECS_COMM_MODE.SECS_HOST_MODE;
                _qSecs.HSMS_Connect_Mode = HSMS_COMM_MODE.HSMS_ACTIVE_MODE;
                _qSecs.lDeviceID = _SecsConfig.DDEVICEID;
                _qSecs.lLogEnable = 1;
                _qSecs.szRemoteIP = _SecsConfig.LocalIP;
                _qSecs.nRemotePort = _SecsConfig.LocalPort;
                if (SECSStop == false)
                {
                    _qSecs.Initialize();
                    _qSecs.QSEvent += _qSecs_QSEventForEQP;
                }
                else
                {
                    _qSecs.Initialize();
                    _qSecs.QSEvent += _qSecs_QSEventForEQP;
                    SECSStop = false;
                }
                _qSecs.Start();
                _SecsStart = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                _SecsStart = false;
                return false;
            }
        }
        private void _qSecs_QSEvent(int lID, EVENT_ID lMsgID, int S, int F, int W_Bit, int ulSystemBytes, object RawData, object Head, string pEventText)
        {
            try
            {
                switch (lMsgID)
                {
                    case EVENT_ID.QS_EVENT_CONNECTED:
                        state = ConnState.SCS_SCONN;
                        _pollingRecv.Set();
                        _pollingSend.Set();
                        break;
                    case EVENT_ID.QS_EVENT_DISCONNECTED:
                        state = ConnState.SCS_ABDIS_SCS_INDIS;
                        _pollingRecv.Reset();
                        _pollingSend.Reset();
                        SecsReStart();
                        break;
                    case EVENT_ID.QS_EVENT_RECV_MSG:
                        _queRecvBuffer.Enqueue(new SStreamFunction((QsStream)S, (QsFunction)F, W_Bit, ulSystemBytes, RawData));
                        return;
                    case EVENT_ID.QS_EVENT_REPLY_TIMEOUT:
                        _logger.WriteLog(string.Format("S{0}F{1} Reply Time out...",S.ToString(),F.ToString()));
                        return;
                    case EVENT_ID.QS_EVENT_SEND_MSG:
                        return;
                    default:
                        break;
                }
                if (InternalStatusChange != null && Prestate != state)
                {
                    InternalStatusChange(new SecsInternalArgs(state));
                    Prestate = state;
                }
               
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }

        }
        private void _qSecs_QSEventForEQP(int lID, EVENT_ID lMsgID, int S, int F, int W_Bit, int ulSystemBytes, object RawData, object Head, string pEventText)
        {
            try
            {
                switch (lMsgID)
                {
                    case EVENT_ID.QS_EVENT_CONNECTED:
                        state = ConnState.SCS_SCONN;
                        _pollingRecv.Set();
                        _pollingSend.Set();
                        break;
                    case EVENT_ID.QS_EVENT_DISCONNECTED:
                        state = ConnState.SCS_ABDIS_SCS_INDIS;
                        _pollingRecv.Reset();
                        _pollingSend.Reset();
                        //  SecsReStart();
                        _qSecs.Stop();
                        _qSecs.Start();
                        break;
                    case EVENT_ID.QS_EVENT_RECV_MSG:
                        _queRecvBuffer.Enqueue(new SStreamFunction((QsStream)S, (QsFunction)F, W_Bit, ulSystemBytes, RawData));
                        return;
                    case EVENT_ID.QS_EVENT_REPLY_TIMEOUT:
                        _logger.WriteLog(string.Format("S{0}F{1} Reply Time out...", S.ToString(), F.ToString()));
                        return;
                    case EVENT_ID.QS_EVENT_SEND_MSG:
                        return;
                    default:
                        break;
                }
                if (InternalStatusChange != null && Prestate != state)
                {
                    InternalStatusChange(new SecsInternalArgs(state));
                    Prestate = state;
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }

        }
        public void SaveConfig()
        {
            //_DB.SetSECSConnectParameter(_SecsConfig);
        }

        public bool SecsReStart()
        {
            try
            {
                bool Status = false;
                _qSecs.Stop();
                _qSecs.QSEvent -= _qSecs_QSEvent;
                _qSecs.Destroy();

                Status = InitSecs();
                return Status;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                _SecsStart = false ;
                return false;
            }
        }

        public bool SecsStop()
        {
            try
            {
                bool Status = false;
                Status = (_qSecs.Stop() == 1) ? true:false;
               // _qSecs.Destroy();
                _SecsStart = false;
                SECSStop = true;
                _qSecs.QSEvent -= _qSecs_QSEvent;
                return Status;
            }
            catch(Exception ex)
            {
                _logger.WriteLog(ex);
                return false;
            }
        }

        public void SendMessage(QsStream Station, QsFunction Function, object SecsMessage)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(QsStream Station, QsFunction Function, object SecsMessage, SecsMessageObject1Args Secsobject = null)
        {
            SecsMsgClass msg =(SecsMsgClass) SecsMessage;
            
           _queSendBuffer.Enqueue(new SStreamFunction(Station, Function, msg.Wbit, msg.SystemBytes, msg.Data));
            
        }

        public void SendMessage(QsStream Station, QsFunction Function, object SecsMessage, ushort tick)
        {
            throw new NotImplementedException();
        }
        public int DataIteminitoutResponse(ref object msg)
        {
            throw new NotImplementedException();
        }
        public void SetDeviceID(int ID)
        {
            _SecsConfig.DDEVICEID = ID;
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
            _SecsConfig.Mode = mode;
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
    }
    //===== struct and sub-class
    public class SStreamFunction
    {
        public SStreamFunction(QsStream s, QsFunction f, int W_bit, int systemByte, params object[] data)
        {
            S = s;
            F = f;
            Wbit = W_bit;
            SystemByte = systemByte;
            Data = data;
        }
        public QsStream S { get; set; }
        public QsFunction F { get; set; }
        public int Wbit { get; set; }
        public int SystemByte;
        public object[] Data { get; set; }
    }
    public class L
    {
        public L(params object[] List)
        {
            Root = List;
        }
        public object[] Root { get; set; }
    }
    public class ASCII_TYPE
    {
        public ASCII_TYPE(object Message)
        {
            if (Message == null)
                Data = string.Empty;
            else
                Data = Message.ToString();
        }
        public string Data { get; set; }
    }
    public class BOOLEAN_TYPE
    {
        public BOOLEAN_TYPE(bool Flag)
        {
            Data = (Flag) ? 255 : 0;
        }

        public int Data { get; set; }
    }
    public class BINARY_TYPE
    {
        public BINARY_TYPE(Byte[] Value)
        {
            Data = Value;
        }
        public Byte[] Data { get; set; }
    }
    public class INT_1_TYPE
    {
        public INT_1_TYPE(int Value)
        {
            Data = Value;
        }
        public int Data { get; set; }
    }
    public class INT_2_TYPE
    {
        public INT_2_TYPE(int Value)
        {
            Data = Value;
        }
        public int Data { get; set; }
    }
    public class INT_4_TYPE
    {
        public INT_4_TYPE(int Value)
        {
            Data = Value;
        }
        public int Data { get; set; }
    }
    public class UINT_1_TYPE
    {
        public UINT_1_TYPE(int Value)
        {
            Data = Value;
        }
        public int Data { get; set; }
    }
    public class UINT_2_TYPE
    {
        public UINT_2_TYPE(int Value)
        {
            Data = Value;
        }
        public int Data { get; set; }
    }
    public class UINT_4_TYPE
    {
        public UINT_4_TYPE(Int64 Value)
        {
            Data = Value;
        }
        public Int64 Data { get; set; }
    }
    public class FT_4_TYPE
    {
        public FT_4_TYPE(float Value)
        {
            Data = Value;
        }
        public float Data { get; set; }
    }
    public class FT_8_TYPE
    {
        public FT_8_TYPE(Double Value)
        {
            Data = Value;
        }
        public Double Data { get; set; }
    }
    public class SecsMsgClass
    {
        public SecsMsgClass(object msg, int dataOffset,int Bytes,int bit)
        {
            Data = msg;
            Offset = dataOffset;
            SystemBytes = Bytes;
            Wbit = bit;
        }
        public object Data;
        public int Offset;
        public int SystemBytes;
        public int Wbit;
    }

}
