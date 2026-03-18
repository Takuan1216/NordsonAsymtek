using Rorze.SocketObject;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using RorzeComm.Threading;

namespace Rorze.Equipments
{
    public abstract class SEFEM
    {
        // enum Staute
        public enum EFEMCommState
        { Disabled = 0, NotCommunicating = 1, Communicating = 2 }
        public enum EFEMControlState
        { Offline = 0, EqOffline = 1, AttemptOnline = 2, HostOffline = 3, OnlineLocal = 4, OnlineRemote = 5 }
        public enum EFEMTimeFormat
        { Format12 = 0, Format16 = 1 }
        public enum EFEMEvent
        {
            Unkown = -1,
            ConnectState,
            ControlState,

        }
        public enum EFEMACK
        {
            Unkown = -1,
            AttemptOnline,
            Hello
        }
        public enum EFEMCommand
        {
            Unkown = -1,
            Hello,

            GoOffline,
            GoOnline,
            GoLocal,
            GoRemote,
            Max,
        }
        public enum EFEMMsgType
        {
            Ack = 'a',
            Event = 'e',
            Order = 'o',
            Cancel = 'c',
            Nak = 'n',
        }

        // EFEM Status...
        EFEMCommState _commstate;
        EFEMControlState _controlstate;
      
        public bool ISFirstOnline = false;
        public event EFEMCommStateChangeHandler OnCommStateChange;
        public event EFEMCommStateChangeHandler OnControlStateChange;
        public event EventHandler OEFEMAttemptOnline;
        string SplitMessage = string.Empty;

        public EFEMCommState GetCommState { get { return _commstate; } }
        public EFEMCommState SetCommState
        {
            set
            {
                _commstate = value;
                if (OnCommStateChange != null)
                    OnCommStateChange(this, new EFEMCommStateChangeEventArgs(UnitName, _commstate, _controlstate));
            }

        }

        public EFEMControlState GetControlState { get { return _controlstate; } }
        public EFEMControlState SetControlState
        {
            set
            {
                _controlstate = value;
                if (OnControlStateChange != null)
                    OnControlStateChange(this, new EFEMCommStateChangeEventArgs(UnitName, _commstate, _controlstate));
            }
        }
        // EFEM object...
        string UnitName;
        public int CMDTimeOut = 60000; // 60 Sec...
        SocketControl _Control;
        Dictionary<string, SSignal> _signalAck = new Dictionary<string, SSignal>();
        // EFEM Common Message

        Dictionary<EFEMEvent, string> _dicEventTable = new Dictionary<EFEMEvent, string>()
        {
            {EFEMEvent.ConnectState, "CommState"},
            {EFEMEvent.ControlState, "ControlState"},

        };
        Dictionary<EFEMACK, string> _dicACKTable = new Dictionary<EFEMACK, string>()
        {
            { EFEMACK.AttemptOnline,"AttemptOnline"},
            { EFEMACK.Hello,"Hello"}
        };

        Dictionary<EFEMCommand, string> _dicCmdsTable = new Dictionary<EFEMCommand, string>()
        {
            {EFEMCommand.Hello,"Hello"},
            {EFEMCommand.GoOffline,"GoOffline"},
            {EFEMCommand.GoOnline,"GoOnline"},
            {EFEMCommand.GoLocal,"GoLocal"},
            {EFEMCommand.GoRemote,"GoRemote"},
        };


        // EFEM Event... 
        public SEFEM(string Name, SocketControl Control)
        {
            UnitName = Name;
            _Control = Control;
            if (_Control._EventMananger.ContainsKey(Name))
            {
                _Control._EventMananger[Name].EventFirst += SEFEM_EventConnect;
                _Control._EventMananger[Name].EventEnd += SEFEM_EventDisconnect;
                _Control._EventMananger[Name].EventAction += SEFEM_Receive; // Socket Receive
            }
            for (int nCnt = 0; nCnt < (int)EFEMCommand.Max; nCnt++)
                _signalAck.Add(_dicCmdsTable[(EFEMCommand)nCnt], new SSignal(false, EventResetMode.ManualReset));

        }

        private void SEFEM_EventDisconnect(object sender, EventArgs e)
        {
            //SendEventConnectStat(EFEMCommState.Disabled);
            SetCommState = EFEMCommState.Disabled;
        }

        private void SEFEM_EventConnect(object sender, EventArgs e)
        {
        //    SendEventConnectStat(EFEMCommState.NotCommunicating);
        //    SetCommState = EFEMCommState.NotCommunicating;

            SendEventConnectStat(EFEMCommState.Communicating);
            SetCommState = EFEMCommState.Communicating;
        }

        // EFEM Action...
        public virtual void SEFEM_Receive(object sender, SocketReplyArgs e)
        {
          //  bool haveBreakLine = false;
            string[] astrFrame = e.SocketMessage;
            string UnitMessage = string.Empty;

            for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++)
            {

                try
                {
                    if (astrFrame[nCnt].Length == 0)
                        continue;

                    if(astrFrame.Count()<2&& astrFrame[nCnt].Length==200000) // 截斷
                    {
                        SplitMessage += astrFrame[nCnt];
                        continue;
                    }
                    /*
                      if (astrFrame[nCnt][0] != 0x01)
                      {
                          haveBreakLine = true;
                          UnitMessage += astrFrame[nCnt];
                          continue;
                      }
                      if(haveBreakLine)
                      {
                          nCnt = nCnt - 1;

                      }
                      else
                      {
                          UnitMessage = astrFrame[nCnt];
                      }*/
                    if(SplitMessage != string.Empty)
                    {
                        SplitMessage += astrFrame[nCnt];
                        UnitMessage = SplitMessage;
                        SplitMessage = string.Empty;
                    }
                    else
                     UnitMessage = astrFrame[nCnt];
                    switch ((EFEMMsgType)UnitMessage[1])
                    {

                        case EFEMMsgType.Cancel:

                            break;
                        case EFEMMsgType.Event:
                            if (!CheckEventexist(UnitMessage))
                                continue; // Unkwon event
                            ParseEventMsg(new EFEMFrame(UnitMessage));
                            break;
                        case EFEMMsgType.Nak:

                            break;
                        case EFEMMsgType.Order:
                            if (!CheckACKexist(UnitMessage))
                                continue;// Unkwon ACK
                            ParseACKMsg(new EFEMFrame(UnitMessage));
                            break;
                        case EFEMMsgType.Ack:
                            if (!CheckCmdexist(UnitMessage))
                                continue; // Unkwon CMD
                            ParseCmdMsg(new EFEMFrame(UnitMessage));
                            break;
                        default:

                            break;

                    }

                    UnitMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    UnitMessage = string.Empty;
                    Console.WriteLine("{0} Exception caught.", ex);
                }
            }

        }
        string CombinationStr(string[] data)
        {
            string Parm = "";
            if (data != null)
            {
                for (int i = 0; i < data.Count(); i++)
                {
                    if (data[i] == "") continue;
                    if (i + 2 > data.Count())
                        Parm = Parm + data[i];
                    else
                        Parm = Parm + data[i] + ",";

                }
            }
            return Parm;
        }
        public void SendEvent(string Eventdata, params string[] data)
        {
            string Parm = "";
            Parm = CombinationStr(data);
            string CmsData = string.Format("e{0}:{1}", Eventdata, Parm);
            SendCommand(CmsData);
        }
        public void SendOrder(string Orderdata, params string[] data)
        {
            string Parm = "";
            Parm = CombinationStr(data);
            string CmsData = string.Format("o{0}:{1}", Orderdata, Parm);
            SendCommand(CmsData);
        }
        public void SendAck(string ACKdata, params string[] data)
        {
            string Parm = "";
            Parm = CombinationStr(data);
            string CmsData = string.Format("a{0}:{1}", ACKdata, Parm);
            SendCommand(CmsData);
        }
        void SendCommand(string data)
        {
            _Control.SendMessage(UnitName, data);
        }

        // Check Unit message Formate , can override this Function...  
        public virtual bool CheckEventexist(string Msg)
        {
            bool FindEvent = false;
            foreach (string scmd in _dicEventTable.Values) //查字典
            {
                if (Msg.Contains(string.Format("{0}", scmd)))
                {
                    FindEvent = true; //認識這個指令
                    break;
                }
            }
            return FindEvent;
        }
        public virtual bool CheckACKexist(string Msg)
        {
            bool FindEvent = false;
            foreach (string scmd in _dicACKTable.Values) //查字典
            {
                if (Msg.Contains(string.Format("{0}", scmd)))
                {
                    FindEvent = true; //認識這個指令
                    break;
                }
            }
            return FindEvent;
        }
        public virtual bool CheckCmdexist(string Msg)
        {
            bool FindCMD = false;
            foreach (string scmd in _dicCmdsTable.Values) //查字典
            {
                if (Msg.Contains(string.Format("{0}", scmd)))
                {
                    FindCMD = true; //認識這個指令
                    break;
                }
            }
            return FindCMD;
        }

        // Parse unit message , can override this Function...  
        public virtual bool ParseEventMsg(EFEMFrame frame)
        {
            if (!_dicEventTable.ContainsValue(frame.Command))
                return false;
            EFEMEvent Event = EFEMEvent.Unkown;
            Event = _dicEventTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (Event)
            {
                case EFEMEvent.ConnectState:
                    _commstate = (EFEMCommState)Convert.ToInt16(frame.Parameter);
                    break;
                case EFEMEvent.ControlState:
                    SetControlState = (EFEMControlState)Convert.ToInt16(frame.Parameter);
                    break;
            }
            return true;
        }
        public virtual bool ParseACKMsg(EFEMFrame frame)
        {
            if (!_dicACKTable.ContainsValue(frame.Command))
                return false;
            EFEMACK ACK = EFEMACK.Unkown;
            ACK = _dicACKTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (ACK)
            {
                case EFEMACK.AttemptOnline:
                    // connect EQ
                    int ack = 0;
                    if (UnitName != "Screen")
                        SendACKAttemptOnline(ack);
                    //  SendEventConnectStat(EFEMCommState.Communicating);
                    //   SetCommState = EFEMCommState.Communicating;
                    else
                    {
                        ISFirstOnline = true;
                        if (OEFEMAttemptOnline != null)
                            OEFEMAttemptOnline(this, new EventArgs());
                    }
                    break;
                case EFEMACK.Hello:
                   SendAck(_dicACKTable[EFEMACK.Hello], new string[] { "0" });
                    break;
            }

            return true;
        }
        public virtual bool ParseCmdMsg(EFEMFrame frame)
        {
            if (!_dicCmdsTable.ContainsValue(frame.Command))
                return false;
            EFEMCommand cmd = EFEMCommand.Unkown;
            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == frame.Command).Key;

            switch (cmd)
            {
                case EFEMCommand.GoOffline:
                case EFEMCommand.GoOnline:
                case EFEMCommand.GoLocal:
                case EFEMCommand.GoRemote:
                    if (frame.Parameter != "0")
                    {
                        _signalAck[_dicCmdsTable[cmd]].bAbnormalTerminal = true;
                    }
                    break;
            }
            _signalAck[_dicCmdsTable[cmd]].Set();
            return true;
        }

        // Common CMD
        public  void SendACKAttemptOnline(int Ack,params string[] data)
        {
            string strStatus = "0";
            if (Ack == 0)
                strStatus = "1";
            if(Ack == 0)
             SendAck(_dicACKTable[EFEMACK.AttemptOnline], new string[] { Ack.ToString(), strStatus });
            else
              SendAck(_dicACKTable[EFEMACK.AttemptOnline], new string[] { Ack.ToString(), strStatus , data[0]});
        }
        public void SendCMDGoOffline()
        {
            _signalAck[_dicCmdsTable[EFEMCommand.GoOffline]].Reset();
            SendOrder(_dicCmdsTable[EFEMCommand.GoOffline]);
            if (!_signalAck[_dicCmdsTable[EFEMCommand.GoOffline]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMCommand.GoOffline]));
            if (_signalAck[_dicCmdsTable[EFEMCommand.GoOffline]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMCommand.GoOffline]));

        }
        public void SendCMDGoOnline()
        {
            _signalAck[_dicCmdsTable[EFEMCommand.GoOnline]].Reset();
            SendOrder(_dicCmdsTable[EFEMCommand.GoOnline]);
            if (!_signalAck[_dicCmdsTable[EFEMCommand.GoOnline]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMCommand.GoOnline]));
            if (_signalAck[_dicCmdsTable[EFEMCommand.GoOnline]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMCommand.GoOnline]));
        }
        public void SendCMDGoLocal()
        {
            _signalAck[_dicCmdsTable[EFEMCommand.GoLocal]].Reset();
            SendOrder(_dicCmdsTable[EFEMCommand.GoLocal]);
            if (!_signalAck[_dicCmdsTable[EFEMCommand.GoLocal]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMCommand.GoLocal]));
            if (_signalAck[_dicCmdsTable[EFEMCommand.GoLocal]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMCommand.GoLocal]));
        }
        public void SendCMDGoRemote()
        {
            _signalAck[_dicCmdsTable[EFEMCommand.GoRemote]].Reset();
            SendOrder(_dicCmdsTable[EFEMCommand.GoRemote]);
            if (!_signalAck[_dicCmdsTable[EFEMCommand.GoRemote]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[EFEMCommand.GoRemote]));
            if (_signalAck[_dicCmdsTable[EFEMCommand.GoRemote]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[EFEMCommand.GoRemote]));
        }
        public void SendEventConnectStat(EFEMCommState Stats)
        {
            SendEvent(_dicEventTable[EFEMEvent.ConnectState], ((int)Stats).ToString());
        }
    }
    public class EFEMFrame
    {
        private string _strFrame;

        private char _charHeader;
       // private string _strID;
        private string _strData;


        private string _strCommand;
       // private string _strStates;
        private string _strParameter;
        public EFEMFrame(string frame)
        {
            _strFrame = frame.Trim('\r', '\n'); ;
            _charHeader = _strFrame[1];
            _strData = _strFrame.Substring(2, _strFrame.Length - 2);
            if (_strData.Split(':').Count() == 2)
            {
                _strCommand = _strData.Split(':')[0];
                _strParameter = _strData.Split(':')[1];
            }
        }
        public char Header { get { return _charHeader; } }
     //   public string ID { get { return _strID; } }
        public string Data { get { return _strData; } }
        //  public int BodyNo { get { return _nBodyNo; } }
        public string Parameter { get { return _strParameter; } }
        public string Command { get { return _strCommand; } }
       // public string States { get { return _strStates; } }
    }
    public class EFEMException : Exception
    {

        public EFEMException(string ErrorStr)
            : base(ErrorStr)
        {

        }
    }
    public class EFEMAlarmhappenEventArgs : EventArgs
    {
        public int AlarmID;
        public bool AlarmSet;
        public EFEMAlarmhappenEventArgs(int ID, bool Set)
        {
            AlarmID = ID;
            AlarmSet = Set;
        }
    }
    public delegate void EFEMAlarmhappenMsgHandler(object sender, EFEMAlarmhappenEventArgs e);

    public class EFEMWaferTranferEventArgs : EventArgs
    {
        public bool IsRobotTake;
        public int Position;
        public string MaterialID;
        public int SlotID;
        public int ArmNo;
        public EFEMWaferTranferEventArgs(bool Take, int Pos, string ID, int Slot,int Arm)
        {
            IsRobotTake = Take;
            Position = Pos;
            MaterialID = ID;
            SlotID = Slot;
            ArmNo = Arm;
        }
    }
    public delegate void EFEMWaferTranferEventHandler(object sender, EFEMWaferTranferEventArgs e);

    public class EFEMRecipeChangeEventArgs : EventArgs
    {
        public enum RecipeStats
        {
            Deleted = 0,
            Created = 1,
            Modified = 2
        }
        public Dictionary<string, RecipeStats> ReccipeStatusList;
        public EFEMRecipeChangeEventArgs(string RecipeData)
        {
            ReccipeStatusList = new Dictionary<string, RecipeStats>();
            int RecipeCount = RecipeData.Split(',').Count() / 2;
            for (int i = 0; i < RecipeCount; i++)
            {
                if (!ReccipeStatusList.ContainsKey(RecipeData.Split(',')[i]))
                    ReccipeStatusList.Add(RecipeData.Split(',')[i*2], (RecipeStats)Convert.ToInt32(RecipeData.Split(',')[i*2 + 1]));
            }
        }
    }
    public delegate void EFEMRecipeChangeHandler(object sender, EFEMRecipeChangeEventArgs e);


    public class EFEMCommStateChangeEventArgs : EventArgs
    {
        public string EQName;
        public SEFEM.EFEMCommState EQCommState;
        public SEFEM.EFEMControlState EQControlState;
        public EFEMCommStateChangeEventArgs(string Name, SEFEM.EFEMCommState commstate, SEFEM.EFEMControlState controlstate)
        {
            EQName = Name;
            EQCommState = commstate;
            EQControlState = controlstate;
        }
    }
    public delegate void EFEMCommStateChangeHandler(object sender, EFEMCommStateChangeEventArgs e);

}
