using Rorze.SocketObject;
using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using RorzeComm;
using RorzeComm.Threading;
using RorzeComm.Log;

namespace Rorze.SocketObject
{
    public class SocketControl: IDisposable
    {
        protected EventHandlerList listEventDelegates = new EventHandlerList();
        Dictionary<SocketTypes,Dictionary<string,ISocket>> _Socketlist;
        int SocketCount;
        Dictionary<string, SPollingThreadObj> _exeSocketRecive;
        Dictionary<string, SPollingThreadObj> _exeSocketReciveDeQueue;
        Dictionary<string, SPollingThreadObj> _exeSocketSendDeQueue;
        Dictionary<string, ConcurrentQueue<string[]>> _queRecvBufferList;
        Dictionary<string, ConcurrentQueue<SocketSendData>> _queSendBufferList;
        public Dictionary<string, SEventDictionary<SocketReplyArgs>> _EventMananger;
        SLogger _loger;
        bool IsSimulation;
        List<SocketInfo> _SocketInfo;
        static private object _objHolding = new object(); //鎖臨界區間
        byte[] CheckLiveByte = new byte[1];
        public SocketControl(string Logname,bool Simulation)
      {

            // _exeSocketRecive = new Dictionary<string, SPollingThread>();
            IsSimulation = Simulation;
            _exeSocketRecive = new Dictionary<string, SPollingThreadObj>();
            _queRecvBufferList = new Dictionary<string, ConcurrentQueue<string[]>>();
            _exeSocketReciveDeQueue = new Dictionary<string, SPollingThreadObj>();
            _exeSocketSendDeQueue = new Dictionary<string, SPollingThreadObj>();
            _queSendBufferList = new Dictionary<string, ConcurrentQueue<SocketSendData>>();
            _EventMananger = new Dictionary<string, SEventDictionary<SocketReplyArgs>>();
            _Socketlist = new Dictionary<SocketTypes, Dictionary<string, ISocket>>();

            _loger = new SLogger(Logname);


        }
        public void AssignUnitInfo(List<SocketInfo> Info)
      {
            _SocketInfo = Info;      
            for (int i = 0; i < Info.Count; i++)
            {
                switch (Info[i].type)
                {
                    case SocketTypes.Clinet:
                        List<SocketInfo> ClientInfo = new List<SocketObject.SocketInfo>();
                        if (!_Socketlist.ContainsKey(SocketTypes.Clinet))
                            _Socketlist.Add(SocketTypes.Clinet, new Dictionary<string, ISocket>());
                        ClientInfo.Add(Info[i]);
                        _Socketlist[SocketTypes.Clinet].Add(Info[i].UnitName, new SClient());
                        _Socketlist[SocketTypes.Clinet][Info[i].UnitName].InitConnect(ClientInfo);
                        _Socketlist[SocketTypes.Clinet][Info[i].UnitName].OnAssgnSocket += SocketControl_OnAssgnSocket;
                        break;
                    case SocketTypes.Server:
                        if(Info[i].UnitName== "RorzeSystem") // 不Check
                            continue;
                        if (_Socketlist.ContainsKey(SocketTypes.Server))
                            continue;

                        SocketInfo SocketServer;
                        var FindGem300 = from Sockets in Info
                                         where  Sockets.UnitName == "RorzeSystem"
                                         select Sockets;
                        if (FindGem300.Count() > 0)
                            SocketServer = FindGem300.ElementAtOrDefault(0);
                        else
                            SocketServer = new SocketInfo("RorzeSystem","127.0.0.1",5000, SocketTypes.Server,null);



                        SocketCount = 100;
                        _Socketlist.Add(SocketTypes.Server, new Dictionary<string, ISocket>());
                        _Socketlist[SocketTypes.Server].Add("RorzeSystem", new sServer(SocketServer, SocketCount,IsSimulation));

                        _Socketlist[SocketTypes.Server]["RorzeSystem"].InitConnect(Info);
                        _Socketlist[SocketTypes.Server]["RorzeSystem"].OnAssgnSocket += SocketControl_OnAssgnSocket;
                        break;
                }
            }
            List<string> infoName = new List<string>();
            foreach(SocketInfo socket in Info)
            {
                if (!_EventMananger.ContainsKey(socket.UnitName) && socket.UnitName != "Gem300")
                    _EventMananger.Add(socket.UnitName, new SEventDictionary<SocketReplyArgs>(socket.UnitName));
            }
      }
        private void SocketControl_OnAssgnSocket(object Sender, SocketEventArgs e)
        {

            if (_exeSocketRecive.ContainsKey(e.UnitName))
            {
                _exeSocketRecive.Remove(e.UnitName);
            }
            _exeSocketRecive.Add(e.UnitName, new SPollingThreadObj(50));
            _queRecvBufferList.Add(e.UnitName, new ConcurrentQueue<string[]>());
            _queSendBufferList.Add(e.UnitName, new ConcurrentQueue<SocketSendData>());
            _exeSocketReciveDeQueue.Add(e.UnitName, new SPollingThreadObj(50));
            _exeSocketSendDeQueue.Add(e.UnitName, new SPollingThreadObj(50));
            _exeSocketRecive[e.UnitName].DoPolling += SocketControl_DoPolling;
            _exeSocketReciveDeQueue[e.UnitName].DoPolling += SocketControl_Recivedequeue;
            _exeSocketSendDeQueue[e.UnitName].DoPolling += SocketControl_SendMsgdequeue;

            _exeSocketRecive[e.UnitName].Set(e);
            _exeSocketReciveDeQueue[e.UnitName].Set(e.UnitName);
            _exeSocketSendDeQueue[e.UnitName].Set(e.UnitName);

            // SendMessage(e.UnitName, "eCommState:1"); // 建立連線
            _EventMananger[e.UnitName].FistEvent(e.UnitName);
        }
        public void SendMessage(string Name,string Message)
        {
            lock (_objHolding)
            {
                var Findunit = from Sockets in _SocketInfo
                               where Sockets.UnitName == Name
                               select Sockets;
                SocketInfo SocketUnit = Findunit.ElementAtOrDefault(0);
                if (Findunit.Count() <= 0) return;

                _queSendBufferList[Name].Enqueue(new SocketSendData(SocketUnit, Message));
            }
        }
        private void SocketControl_SendMsgdequeue(object o)
        {
           
            try
            {
                string UnitName = (string)o;
                SocketSendData SendData;
                if (!_queSendBufferList[UnitName].TryDequeue(out SendData)) return;

                switch (SendData.UnitSocket.type)
                {
                    case SocketTypes.Clinet:
                        _Socketlist[SocketTypes.Clinet][UnitName].Write(UnitName, SendData.Message);
                        break;
                    case SocketTypes.Server:
                        _Socketlist[SocketTypes.Server]["RorzeSystem"].Write(SendData.UnitSocket.UnitName, SendData.Message);
                        _loger.WriteLog(string.Format("[Gem300->{0}] :{1}", SendData.UnitSocket.UnitName, SendData.Message));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
            
        }
        private void SocketControl_Recivedequeue(object o)
        {
            try
            {
                string UnitName = (string)o;
                string[] astrFrame;
                if (!_queRecvBufferList[UnitName].TryDequeue(out astrFrame)) return;

                _EventMananger[UnitName].ChangeEvent(UnitName, new SocketReplyArgs(astrFrame));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private void SocketControl_DoPolling(object o)
        {
            
            try
            {
               
                System.Net.Sockets.Socket _socket = (System.Net.Sockets.Socket)((SocketEventArgs)o)._Socket;
                string strReceive;
                bool closed = false;
                byte[] byteReceive = new byte[3000000];
                if(_socket.Connected && !closed)
                {
                    Array.Clear(byteReceive, 0, byteReceive.Length);
                    _socket.Receive(byteReceive);
                    strReceive = Encoding.Default.GetString(byteReceive);
                    
                    strReceive = strReceive.Trim('\0');
                    if (strReceive == "")
                    {
                        if (_socket.Connected
                         && _socket.Poll(0, SelectMode.SelectRead))
                            closed = _socket.Receive(CheckLiveByte, SocketFlags.Peek) == 0;
                        if (closed)
                            CloseConnect((SocketEventArgs)o);
                        return;
                    }
                    string[] astrFrame = strReceive.Split('\r');
                    _queRecvBufferList[((SocketEventArgs)o).UnitName].Enqueue(astrFrame);

                    foreach (string str in astrFrame)
                    {
                        if(str!= "")
                         _loger.WriteLog(string.Format("[{0}-> Gem300] :{1}", ((SocketEventArgs)o).UnitName, str));
                    }
                }
               
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                CloseConnect((SocketEventArgs)o);
            }
        }
        void CloseConnect(SocketEventArgs Unit)
        {
            switch (Unit.Unittype)
            {
                case SocketTypes.Clinet:
                    _Socketlist[SocketTypes.Clinet][Unit.UnitName].RemoveClint(Unit.UnitName);
                    break;
                case SocketTypes.Server:
                    _Socketlist[SocketTypes.Server]["RorzeSystem"].RemoveClint(Unit.UnitName);
                    break;
            }
            _EventMananger[Unit.UnitName].EndEvent(Unit.UnitName);
            _exeSocketRecive[Unit.UnitName].Close();
            _exeSocketRecive.Remove(Unit.UnitName);
            _queRecvBufferList.Remove(Unit.UnitName);
            _queSendBufferList.Remove(Unit.UnitName);
            _exeSocketReciveDeQueue[Unit.UnitName].Close();
            _exeSocketReciveDeQueue.Remove(Unit.UnitName);

            _exeSocketSendDeQueue[Unit.UnitName].Close();
            _exeSocketSendDeQueue.Remove(Unit.UnitName);


        }
        public bool Open()
        {
            try
            {
                bool SeverOpen = false;
                foreach (SocketInfo SocketItem in _SocketInfo)
                {
                    if (SocketItem.UnitName == "RorzeSystem") continue;
                    switch (SocketItem.type)
                    {
                        case SocketTypes.Clinet:
                           
                            _Socketlist[SocketTypes.Clinet][SocketItem.UnitName].Open();
                            break;
                        case SocketTypes.Server:
                            if (!SeverOpen)
                            {
                                SeverOpen = true;
                               _Socketlist[SocketTypes.Server]["RorzeSystem"].Open();
                            }
                            break;
                        default:
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                return false;
            }
        }

        public void Dispose()
        {
            foreach(SPollingThreadObj threads in _exeSocketRecive.Values)
                threads.Close();
            

            foreach(SPollingThreadObj threads in _exeSocketReciveDeQueue.Values)
                threads.Close();

            foreach(SPollingThreadObj threads in _exeSocketSendDeQueue.Values)
                threads.Close();

            if (_Socketlist.ContainsKey(SocketTypes.Server))
            {
                foreach (ISocket socket in _Socketlist[SocketTypes.Server].Values)
                {

                    socket.Close();
                    break;
                }
            }
            if (_Socketlist.ContainsKey(SocketTypes.Clinet))
            {
                foreach (ISocket socket in _Socketlist[SocketTypes.Clinet].Values)
                    socket.Close();
            }
        }
    }

    public delegate void SocketReplyHandler(object Sender, SocketReplyArgs e);
    public class SocketReplyArgs : EventArgs
    {
        public string [] SocketMessage;
        
        public SocketReplyArgs(string [] Message)
        {
            SocketMessage = Message;

        }
    }
    public class SocketSendData
    {
        public SocketInfo UnitSocket;
        public string Message;

        public SocketSendData(SocketInfo unit,string msg)
        {
            UnitSocket = unit;
            Message = msg;
        }
    }

}
