using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Rorze.SocketObject
{
      interface ISocket
    {
        event SocketEventHandler OnAssgnSocket;



        bool Connected(string UnitName);
        bool Write(string Unit, string str);
        void InitConnect(List<SocketInfo> Info);
        bool Open();
        bool Close();


        void RemoveClint(string Unit);
    }
      public enum SocketTypes {Clinet=0,Server=1 }
      public delegate void SocketEventHandler(object Sender, SocketEventArgs e);
      public class SocketEventArgs : EventArgs
    {
        public int _PortNo;
        public string _IP;
        public Socket _Socket;
        public string UnitName;
        public SocketTypes Unittype;
        public SocketEventArgs(string Name,Socket Sockets, string IP, int PortNo, SocketTypes type)
        {
            UnitName = Name;
            _Socket = Sockets;
            _IP = IP;
            _PortNo = PortNo;
            Unittype = type;

        }
    }
      public class SocketInfo
      {
        public string UnitName;
        public string SocketIP;
        public int SocketPort;
        public SocketTypes type;

        public EFEMConfig EFEM;

        public SocketInfo(string Name, string IP,int port, SocketTypes Stype, EFEMConfig EFEMsetting )
        {
            UnitName = Name;
            SocketIP = IP;
            SocketPort = port;
            type = Stype;
            EFEM = EFEMsetting;
        }
    }
    public class EFEMConfig
    {
        public  EquipmentType _Type;

        public int LoadPortCount;
        public int RobotArmCount;
        public int AlingerCount;
        public Dictionary<int, string> UnitPos;

        public int ChamberCount;
        public int ChamberSpace;
        public Dictionary<int, string> ChanberSpacenumUnitPos;
        public EFEMConfig()
        {

        }

    }

    public enum EquipmentType { EFEM = 0, EQ = 1, EFEMAndEQ = 2 }
}
