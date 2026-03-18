using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

using RorzeComm;
using Rorze.SocketObject;
using RorzeComm.Threading;

namespace Rorze.SocketObject
{
  public class SClient : ISocket
    {
        Socket Client;
        SocketInfo ClientInfo;
      //  SPollingThread _exeCheckClinetConnect;
        public event SocketEventHandler OnAssgnSocket;
        byte[] CheckLiveByte = new byte[1];
        bool _Connected = false;
        SPollingThread _exeRetryConnect;
        public SClient()
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //_exeCheckClinetConnect = new SPollingThread(1000);
            //_exeCheckClinetConnect.DoPolling += _exeCheckClinetConnect_DoPolling;
      
            _exeRetryConnect = new SPollingThread(1000);
            _exeRetryConnect.DoPolling += _exeRetryConnect_DoPolling;
        }

        private void _exeRetryConnect_DoPolling()
        {
            bool connected = false;
            try
            {

                connected =  this.Open();
                if (connected)
                    _exeRetryConnect.Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        private void _exeCheckClinetConnect_DoPolling()
        {
            try
            {
                bool Close = false;
                if (Client.Connected
                      && Client.Poll(0, SelectMode.SelectRead))
                    Close = Client.Receive(CheckLiveByte, SocketFlags.Peek) == 0;
                else
                    return;
                if (Close)
                {
                   // _exeCheckClinetConnect.Reset();
                    _Connected = false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                _Connected = false;
            }
        }

        public bool Close()
        {
            try
            {
              //  _exeCheckClinetConnect.Reset();
                Client.Close();
                _Connected = false;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                return false;
            }
        }

        public bool Connected(string UnitName)
        {
            return Client.Connected;
        }

        public void InitConnect(List<SocketInfo> Info)
        {
            ClientInfo = Info[0];
        }

        public bool Open()
        {
            try
            {
                Client.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003));
                Client.Connect(new IPEndPoint(IPAddress.Parse(ClientInfo.SocketIP), ClientInfo.SocketPort));
                if (OnAssgnSocket != null)
                    OnAssgnSocket(this, new SocketEventArgs(ClientInfo.UnitName, Client, ClientInfo.SocketIP, ClientInfo.SocketPort, SocketTypes.Clinet));
                _Connected = true;
              //  _exeCheckClinetConnect.Set();
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine("{0} Exception caught.", ex);
                _exeRetryConnect.Set();
                return false;
            }
        }

        public bool Write(string Unit, string str)
        {
            str = (char)0x01 + str + "\r";
            //str = " " + str + "\r";
            string[] sRes = new string[] { };
            sRes = str.Split('\r');
            bool bData = false;
            for (int nDataCount = 0; nDataCount < sRes.Length - 1; nDataCount++)
            {
                bData = Write(Unit, Encoding.ASCII.GetBytes(sRes[nDataCount] + '\r'));
                if (!bData) break;
            }
            return bData;
        }
        public bool Write(string Unit, params byte[] bytes)
        {
            try
            {
                if (Client.Connected)
                {
                    Client.Send(bytes);
                }
                else
                {
                    //_logger.WriteLog("<<<Error>>> Client is disconnect. index = [{0}], data = [{1}].", nCnt, Encoding.ASCII.GetString(bytes));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                return false;
            }
            return true;
        }

        public void RemoveClint(string Unit)
        {
            this.Close();
            Client = null;
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _exeRetryConnect.Set();
        }
    }
}
