using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;

namespace Rorze.SocketObject
{
    public class STcpConnecter : IDisposable
    {
        private TcpListener _server;
        private TcpClient _client;
        private NetworkStream _stream;
        protected IPAddress _localaddr;
        protected int _nPort;

        protected SLogger _logger;

        //private Timer _tmrListener;
        private bool _bIsClient;
        private SPollingThread _pollingListen;

        public event MessageEventHandler OnReadData;

        public STcpConnecter(IPAddress localaddr, int port, bool bIsClient, string strLoggerName)
        {
            _logger = SLogger.GetLogger(strLoggerName);
            _localaddr = localaddr;
            _nPort = port;
            _bIsClient = bIsClient;
            if (_bIsClient)
            {
                _client = new TcpClient(localaddr.ToString(), port);
                //_tmrListener = new Timer(new TimerCallback(RunClientPolling));
            }
            else
            {
                _server = new TcpListener(localaddr, port);
                _server.Start();
                _pollingListen = new SPollingThread(0);
                //_pollingListen.DoPolling += new dlgv_v(_pollingListen_DoPolling);
                _pollingListen.DoPolling += new dlgv_v(RunServerPolling2);
                _pollingListen.Set();
                //_tmrListener = new Timer(new TimerCallback(RunServerPolling));
            }
            //_tmrListener.Change(100, 100);
        }

        void _pollingListen_DoPolling()
        {
            try
            {
                if (_server.Pending())
                    _client = _server.AcceptTcpClient();
                _pollingListen.Reset();
                Console.WriteLine("TCP connected............");
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        public bool Write(params byte[] bytes)
        {
            //是client端 or server端
            if (_bIsClient)
            {
                if (_client.Connected)
                {
                    _stream = _client.GetStream();
                }
                else return false;
            }
            else if (!_bIsClient)
            {
                //if (_server.Pending())
                {
                    if (_client == null) return false;
                    //_client = _server.AcceptTcpClient();
                    if (_client.Connected)
                        _stream = _client.GetStream();
                    else return false;
                    //_stream = _server.AcceptTcpClient().GetStream();
                }
                //else return false;
            }

            //資料流是否可以寫入
            if (_stream.CanWrite)
            {
                //_stream.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(RunAsyncWrite), null);
                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
            else return false;

            return true;
        }
        public bool Write(string strMessage)
        {
            return Write(Encoding.Default.GetBytes(strMessage));
        }
        public bool Write(string format, params object[] args)
        {
            return Write(string.Format(format, args));
        }

        private void RunAsyncWrite(IAsyncResult result)
        {
            _stream = _client.GetStream();
            if (_stream.CanWrite)
            {
                _stream.EndWrite(result);
                _stream.Flush();
            }
        }
        private void RunClientPolling(object state)
        {
            //if (!_client.Connected) _client.Connect(_localaddr.ToString(), _nPort);
            if (_client.Connected)
            {
                _stream = _client.GetStream(); //取得讀寫物件
                if (_stream.CanRead)
                {
                    byte[] abyteData = new byte[1024];
                    _stream.Read(abyteData, 0, abyteData.Length);
                    string str = Encoding.Default.GetString(abyteData);
                    str = str.Trim('\0');
                    if (str.Length > 0)
                    {
                        if (OnReadData != null) OnReadData(this, new MessageEventArgs(str));
                        //Console.WriteLine("{0} get data from server {1}", DateTime.Now, str); //just for trace.
                    }

                }
            }
        }
        private void RunServerPolling(object state)
        {
            try
            {
                //if (_server.Pending()) //有連線需求才暫停執行緒
                {
                    if (_server.Pending()) _client = _server.AcceptTcpClient(); //等待client端連線
                    if (_client == null) return;
                    _stream = _client.GetStream(); //取得讀寫物件
                    if (_stream.CanRead)
                    {
                        byte[] abyteData = new byte[1024];
                        _stream.Read(abyteData, 0, abyteData.Length);
                        string str = Encoding.Default.GetString(abyteData);
                        str = str.Trim('\0');
                        if (str.Length > 0)
                        {
                            if (OnReadData != null) OnReadData(this, new MessageEventArgs(str));
                            //Console.WriteLine("{0} get data from client {1}", DateTime.Now, str); //just for trace.
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        private void RunServerPolling2()
        {
            try
            {
                //if (_server.Pending()) //有連線需求才暫停執行緒
                {
                    if (_server.Pending()) _client = _server.AcceptTcpClient(); //等待client端連線
                    if (_client == null) return;
                    _stream = _client.GetStream(); //取得讀寫物件
                    if (_stream.CanRead)
                    {
                        byte[] abyteData = new byte[1024];
                        _stream.Read(abyteData, 0, abyteData.Length);
                        string str = Encoding.Default.GetString(abyteData);
                        str = str.Trim('\0');
                        if (str.Length > 0)
                        {
                            if (OnReadData != null) OnReadData(this, new MessageEventArgs(str));
                            //Console.WriteLine("{0} get data from client {1}", DateTime.Now, str); //just for trace.
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        public void Dispose()
        {
            //_tmrListener.Dispose();
        }
    }
}
