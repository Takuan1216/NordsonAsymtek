using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Rorze.SocketObject
{
    public class SSocketClient
    {
        private Socket _socketHost;
        private IPAddress _ipHost;
        private int _nPort;
        public SSocketClient(IPAddress hostAddr, int port)
        {
            _ipHost = hostAddr;
            _nPort = port;
            _socketHost = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void ConnectServer()
        {
            _socketHost.Connect(_ipHost, _nPort);
        }


    }
}
