using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using RorzeUnit.Interface;
using RorzeUnit.Class.Aligner.Enum;
using System.Reflection;
using System.Windows.Media;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using RorzeApi;
using static System.Net.Mime.MediaTypeNames;
using RorzeComm.Log;
using System.Windows.Documents;
using System.Runtime.CompilerServices;
using RorzeUnit.Class.OCR.Enum;
using System.Security.Cryptography;
using Rorze.SocketObject;
using static System.Windows.Forms.AxHost;

namespace RorzeUnit.Class.OCR
{
    public class OCR_IS1740 : I_OCR
    {
        public class SocketState
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 36000;//256   26738
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }


        public bool Disable { get; private set; }
        public bool Connected { get { return m_bConnected; } }
        public string Name { get { return m_eName.ToString(); } }
        public string SavePicturePath { get; private set; }
        public bool IsFront { get { return (m_eName == enumName.A1 || m_eName == enumName.B1); } }
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[OCR]{0} : {1}  at line {2} ({3})", Name, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }


        private bool m_bSimulate = false;
        private int m_nControlPort = 23;
        private int m_nReadPort = 2000;

        private int m_nImageCount = 0;//SaveImage


        private string m_strIP = "127.0.0.1";

        private string m_strResp = "";

        private string m_strImage;//SaveImage

        private Socket m_scControl = null;
        private Socket m_scRead = null;
        private bool m_bConnected = false;
        private bool m_bGetFileList = false;
        private enumName m_eName;
        private SLogger m_logger;


        private string[] m_strRecipe = null;

        public Mutex m_mutSent = new Mutex();
        public ManualResetEvent m_mutWaitResponse = new ManualResetEvent(false);
        public ManualResetEvent m_mutImageReceiveDone = new ManualResetEvent(false);



        public OCR_IS1740(enumName eName, string strIP, bool bDisable, bool bSimulate)
        {
            m_eName = eName;
            m_strIP = strIP;

            Disable = bDisable || (strIP == "127.0.0.1");

            m_bSimulate = bSimulate;

            if (Disable == false) m_logger = SLogger.GetLogger("CommunicationLog");

            if (!Disable && strIP != "127.0.0.1" && !m_bSimulate)
            {
                MakeConnect(ref m_scControl, m_strIP, m_nControlPort);
            }

        }
        ~OCR_IS1740()
        {
            if (m_scControl != null)
            {
                if (m_scControl.Connected)
                {
                    m_scControl.Close();
                }
                m_scControl = null;
            }

            if (m_scRead != null)
            {
                if (m_scRead.Connected)
                {
                    m_scRead.Close();
                }
                m_scRead = null;
            }
        }

        #region Socket
        private bool MakeConnect(ref Socket sc, string strIP, int nPort)
        {
            bool bSucc = false;
            try
            {
                sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(strIP), nPort);
                sc.BeginConnect(remoteEP, new AsyncCallback(OnConnect), sc);
                //if (m_mutConnect.WaitOne(20000))
                {
                    bSucc = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bSucc;
        }
        private void OnConnect(IAsyncResult ar)
        {
            Socket mySocket = (Socket)ar.AsyncState;
            try
            {
                if (mySocket.Connected)
                {
                    SocketState state = new SocketState();
                    state.workSocket = mySocket;
                    WriteLog(string.Format("Connect : {0}", mySocket.RemoteEndPoint));


                    mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);
                    mySocket.EndConnect(ar);

                    m_bConnected = true;
                }
                else
                {
                    //MessageBox.Show("Unable to connect to In-Sight 1740 OCR , Connect Failed!");
                    SpinWait.SpinUntil(() => false, 1000);
                    WriteLog(string.Format("ReConnect : {0}:{1}", m_strIP, m_nControlPort));
                    MakeConnect(ref m_scControl, m_strIP, m_nControlPort);
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }

        }
        private void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            SocketState state = (SocketState)ar.AsyncState;
            Socket mySocket = state.workSocket;
            try
            {


                // Read data from the client socket. 
                int bytesRead = mySocket.EndReceive(ar);
                SpinWait.SpinUntil(() => false, 1);
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    //Console.WriteLine(state.sb.ToString());
                    string str = state.sb.ToString();
                    state.sb.Clear();

                    WriteLog(string.Format("Recv : {0}", str));

                    char[] sp = new char[] { '\r', '\n' };
                    string[] strs = str.Split(sp, StringSplitOptions.RemoveEmptyEntries);

                    #region ReceiveReadImage
                    //1,IMAGE.JPG,26738
                    if (str.Contains("1\r\nIMAGE.JPG"))
                    {
                        m_strImage = "";
                        m_nImageCount = int.Parse(strs[2]);//34160      
                    }
                    else if (m_nImageCount != 0)
                    {
                        foreach (string item in strs)
                        {
                            m_strImage += item;//字串疊加
                        }
                        if (m_nImageCount <= m_strImage.Length)//判斷字串接收完成
                        {
                            m_nImageCount = 0;
                            m_mutImageReceiveDone.Set();
                        }
                    }
                    #endregion

                    #region ReceiveRecipeList
                    if (m_bGetFileList)//GetFileList
                    {
                        string ss = "";
                        foreach (string fn in strs)
                        {
                            if (fn.IndexOf(".job") > 0)
                                ss += fn + ",";
                        }
                        m_strRecipe = ss.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        m_bGetFileList = false;
                    }
                    #endregion

                    m_strResp = strs[strs.Length - 1];
                    m_mutWaitResponse.Set();
                    mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);

                    for (int i = 0; i < strs.Length; i++)
                    {
                        m_strResp = strs[i];
                        if (strs[i].IndexOf("User: ") >= 0 && mySocket == m_scControl)
                        {
                            SendData(ref mySocket, "admin");
                        }
                        else if (strs[i].IndexOf("Password: ") >= 0)
                        {
                            SendData(ref mySocket, "");
                        }
                    }
                }
                else
                {
                    // Do something else
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
                //MessageBox.Show(ex.ToString());

                m_mutWaitResponse.Set();
                mySocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ReadCallback), state);
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        private string SendData(ref Socket sc, string strMsg)
        {
            int nLen = strMsg.Length;
            if (nLen < 2 || strMsg.Substring(nLen - 2) != "\r\n")
            {
                strMsg += "\r\n";
            }

            m_mutSent.WaitOne();
            m_mutWaitResponse.Reset();

            m_strResp = "";

            if (sc != null && sc.Connected)
            {
                WriteLog(string.Format("Send : {0}", strMsg));
                byte[] myByte = Encoding.ASCII.GetBytes(strMsg);
                sc.BeginSend(myByte, 0, myByte.Length, 0, new AsyncCallback(SendCallback), sc);
                bool bSucc = m_mutWaitResponse.WaitOne(3000);
                if (false == bSucc)
                {
                    WriteLog(string.Format("Send failure IS-1740 send command failure!: {0}", strMsg));
           
                }
            }

            m_mutSent.ReleaseMutex();
            return m_strResp;
        }
        #endregion

        private bool LogIn()
        {
            bool bSucc = false;
            while (true)
            {
                if (SendData(ref m_scControl, "admin").IndexOf("Password:") < 0)
                    break;
                if (SendData(ref m_scControl, "").Length == 0)
                    break;
                bSucc = true;
                break;
            }
            return bSucc;
        }
        public bool Initial(string strRecipe)
        {
            bool bSucc = false;
            while (true)
            {
                if (false == OffLine())
                    break;
                if (false == SetRecipe(strRecipe))
                    break;
                if (false == OnLine())
                    break;
                bSucc = true;
                break;
            }
            return bSucc;
        }
        public bool OnLine()
        {
            SendData(ref m_scControl, "SO1");
            return (m_strResp == "1");
        }
        public bool OffLine()
        {
            SendData(ref m_scControl, "SO0");
            return (m_strResp == "1");
        }
        public bool SetRecipe(string strName)
        {
            if (strName.IndexOf(".job") < 0)
            {
                strName += ".job";
            }
            SendData(ref m_scControl, "LF" + strName);
            return (m_strResp.IndexOf("1") >= 0);
        }
        public bool Read(ref string strResult, bool bOKSaveImage, string strCarrierID = "", string strLotID = "")
        {
            //if (m_bSimulate)
            //{
            //    Random crandom = new Random();
            //    OcrID = crandom.Next(1, 10) > 5 ? "noread" : "ABCDEFG-" + waferData.Slot;
            //}


            string strOcr = "";
            Socket myReader = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            myReader.Connect(new IPEndPoint(IPAddress.Parse(m_strIP), m_nReadPort));
            string strCmd = "READ\r\n";
            WriteLog(string.Format("Send : {0}", strCmd));

            SpinWait.SpinUntil(() => false, 1);
            myReader.Send(Encoding.ASCII.GetBytes(strCmd));
            SpinWait.SpinUntil(() => false, 1000);

            byte[] buffer = new byte[64];
            int n = myReader.Receive(buffer);

            string s = Encoding.Default.GetString(buffer);

            string[] strssssss = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            strOcr = strssssss[0].Split(',')[0];
            strResult = strOcr;

            myReader.Close();
            WriteLog(string.Format("Recv : {0}", strOcr));

            //if (strResult == null || strResult == string.Empty || bOKSaveImage || strResult == "*")
            //{
            //    SaveImage(m_eName + "_" + strLotID + "_" + strCarrierID, true);
            //}

            return (strResult != null || strResult != string.Empty);
        }
        public bool SaveImage(string strFaileName)
        {
            if (strFaileName.Contains(".jpg") == false) strFaileName = strFaileName + ".jpg";

            string strFolder = Path.Combine(Directory.GetCurrentDirectory(), "OCR_Image");
            strFolder = Path.Combine(strFolder, DateTime.Now.ToString("yyyMMdd"));  //日期是資料夾

            if (Directory.Exists(strFolder) == false)
            {
                Directory.CreateDirectory(strFolder);
            }

          
          

            bool bSucc = SaveImage(strFolder, strFaileName);

            return bSucc;
        }
        public bool SaveImage(string strFolder, string strFaileName = "test.jpg")
        {
            string strPath = Path.Combine(strFolder, strFaileName);

            if (Directory.Exists(strFolder) == false)
            {
                Directory.CreateDirectory(strFolder);
            }

            m_mutImageReceiveDone.Reset();
            SendData(ref m_scControl, "RFIMAGE.JPG");
            bool bSucc = m_mutImageReceiveDone.WaitOne(5000);
            if (bSucc)
            {
                // Some junk hex image data
                string hexImgData = m_strImage;
                // Call function to Convert the hex data to byte array
                byte[] newByte = ToByteArray(hexImgData);
                MemoryStream memStream = new MemoryStream(newByte);
                // Save the memorystream to file
                Bitmap.FromStream(memStream).Save(strPath);
            }
            return bSucc;
        }





        public string[] getRecipt()
        {
            //Get FileList
            //1
            //7
            //Index.html
            //MA12.job
            //WafID450.job
            //hosts.net
            //image.bmp
            //image.jpg
            //proc.set

            m_bGetFileList = true;
            SendData(ref m_scControl, "Get FileList");

            if (m_bSimulate)
            {
                string str1 = "1  7  Index.html  T7.job  WafID450.job  hosts.net  image.bmp  image.jpg  proc.set";
                char[] sp = new char[] { '\r', '\n', ' ' };
                string[] strs = str1.Split(sp, StringSplitOptions.RemoveEmptyEntries);

                #region ReceiveRecipeList
                if (m_bGetFileList)//GetFileList
                {
                    string ss = "";
                    foreach (string fn in strs)
                    {
                        if (fn.IndexOf(".job") > 0)
                            ss += fn + ",";
                    }
                    m_strRecipe = ss.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    m_bGetFileList = false;
                }
                #endregion
            }

            return m_strRecipe;
        }








        // Function converts hex data into byte array
        private static byte[] ToByteArray(String HexString)
        {
            int NumberChars = HexString.Length;

            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }





    }
}
