using System;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using RorzeComm.Log;
using RorzeUnit.Interface;

namespace RorzeUnit.Class.RFID
{
    public abstract class CIPC : I_RFID
    {
        protected SLogger _logger = SLogger.GetLogger("RFID");
        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        protected int m_nCom = 0;

        ~CIPC()
        {
            if (m_Comport != null && m_Comport.IsOpen)
                m_Comport.Close();
        }
        protected bool m_bSimulate;
        protected SerialPort m_Comport;
        protected bool m_bConnect;
        protected const int m_nDevice = 1;
        protected string m_strCommand;
        protected string m_strReply;

        protected void InitCommand()
        {
            m_strReply = "";
        }
        public bool IsConnect()
        {
            return m_bConnect;
        }
        public string GetCommand()
        {
            return m_strCommand;
        }


        public abstract bool CheckReader();
        public abstract string ReadMID();
        public abstract string GetReply();



        protected void Wait(int ms)
        {
            DateTime dtStart = DateTime.Now;

            while (true)
            {
                TimeSpan ts = DateTime.Now - dtStart;
                if (ts.TotalMilliseconds >= ms)
                {
                    return;
                }

                Thread.Sleep(1);

            }
        }


    }

    // Unision
    public sealed class RFID_Unison : CIPC
    {
        public RFID_Unison(int nCom, bool bSimulate = false)
        {
            m_bSimulate = bSimulate;

            string strCom = "COM" + nCom.ToString();
            if (!bSimulate)
            {
                try
                {
                    m_Comport = new SerialPort(strCom, 9600, Parity.Even, 8, StopBits.One);
                    m_Comport.Open();
                    m_bConnect = m_Comport.IsOpen;
                }
                catch
                {
                    m_bConnect = false;
                }
            }

            m_nCom = nCom;
        }


        public bool SendCommand(string cmd, string strOffset = null, string strLen = null, string strData = null)
        {
            bool bSucc = false;
            InitCommand();
            m_strCommand = "~" + m_nDevice.ToString("2D");
            m_strCommand += " " + cmd;

            if (strOffset != null)
                m_strCommand += " " + strOffset;

            if (strLen != null)
                m_strCommand += " " + strLen;

            if (strData != null)
                m_strCommand += " " + strData;

            m_strCommand += "*";

            try
            {
                m_Comport.Write(m_strCommand);

                m_strReply = m_Comport.ReadLine();
                if (m_strReply.Length > 0)
                {
                    if (m_strReply[0] == '~')
                    {
                        bSucc = true;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
            }

            if (m_bSimulate)
                bSucc = true;

            return bSucc;
        }

        public override string ReadMID()
        {
            string strMID = "ReadFail";
            bool bSucc = SendCommand("RMID");
            bool bOkHead = m_strReply.IndexOf("RMIDR NO") >= 0 ? true : false;
            bool bOkEnd = m_strReply[m_strReply.Length - 1] == '*';
            if (bSucc && bOkHead && bOkEnd)
            {
                int nOffset = 9;
                int nStart = m_strReply.IndexOf("RMIDR NO") + nOffset;
                strMID = m_strReply.Substring(nStart);

                strMID = strMID.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");//Ming
                //strFoupID.Trim();
            }

            return strMID;

        }

        public override bool CheckReader()
        {
            bool bSucc = SendCommand("RU");
            if (false == bSucc)
            {
                WriteLog(string.Format("[Com{0}] Unision RFID Send 'RU' to RFIDReader Failed", m_nCom));
                return false;
            }

            string strReply = m_strReply;
            bool bOkHead = strReply.IndexOf("RUR") >= 0 ? true : false;
            bool bOkEnd = strReply[strReply.Length - 1] == '*';
            if (false == bOkHead || false == bOkEnd)
            {
                WriteLog(string.Format("[Com{0}] Unision RFID Receive 'RUR' Reply From RFIDReader Failed.", m_nCom));
                return false;
            }
            WriteLog(string.Format("[Com{0}] Unision RFID Reader is connected!!", m_nCom));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strCommand));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strReply));

            return true;
        }

        public override string GetReply()
        {
            if (m_bSimulate)
            {
                if (m_strCommand.IndexOf("RMID") >= 0)
                    m_strReply = "~03 RMIDR NO 000AN5024hhhghyt*";
                else if (m_strCommand.IndexOf("RUR") >= 0)
                    m_strReply = "~03 RUR Unison RF Ver:1.36*";
            }
            return m_strReply;
        }


    }
    // Heart
    public sealed class RFID_Heart : CIPC
    {
        public RFID_Heart(int nCom, bool bSimulate = false)
        {
            m_bSimulate = bSimulate;
            string strCom = "COM" + nCom.ToString();
            if (!bSimulate)
            {
                try
                {
                    m_Comport = new SerialPort(strCom, 9600, Parity.Even, 8, StopBits.One);
                    m_Comport.Open();
                    m_bConnect = m_Comport.IsOpen;
                }
                catch
                {
                    m_bConnect = false;
                }
                m_strCommand = '\x01'.ToString();
            }
            m_nCom = nCom;
        }

        public bool SendCommand(string strCmd, string strParam = null)
        {
            bool bSucc = false;
            InitCommand();
            string strAddr = m_nDevice.ToString("D2");
            string strLen = (strAddr.Length + strCmd.Length + strParam.Length + 2).ToString("D3");
            m_strCommand = '\x01' + strLen + strAddr + strCmd + strParam;

            char chCheck = m_strCommand[1];
            for (int i = 2; i < m_strCommand.Length; i++)
            {
                chCheck ^= m_strCommand[i];
            }
            m_strCommand += (((int)chCheck).ToString("X2")).ToString() + '\x0d';

            try
            {
                if (false == m_Comport.IsOpen)
                    m_Comport.Open();

                WriteLog(string.Format("[Com{0}] Send->{1}", m_nCom, m_strCommand));

                m_Comport.Write(m_strCommand);
                Wait(1200);

                m_strReply = m_Comport.ReadExisting();
                if (m_strReply.Length > 0)
                {
                    WriteLog(string.Format("[Com{0}] Recv<-{1}", m_nCom, m_strReply));
                    bSucc = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
            }

            if (false == bSucc)
            {
                WriteLog(string.Format("[Com{0}] ReplyCommandD[0]!= SOH", m_nCom));
            }

            return bSucc;
        }

        public override string ReadMID()
        {
            string strMID = "ReadFail";

            bool bSucc = false;
            bool bOkResp = false;
            int nRetry = 0;
            int nLimit = 3;

            while (nRetry < nLimit)
            {
                bSucc = SendCommand("RD", "MPT0102");
                bOkResp = m_strReply.IndexOf("OK") >= 0 ? true : false;

                if (bSucc && bOkResp)
                    break;
                nRetry++;
            }

            if (bSucc && bOkResp)
            {
                int nStart = m_strReply.IndexOf("OK") + 2;
                string temp = m_strReply;

                temp = temp.Substring(nStart);
                string str = "";

                for (int i = 0; i < temp.Length / 2; i++)
                {
                    string s = temp.Substring(2 * i, 2);
                    if (s == "  ") break;

                    string sssss = temp.Substring(2 * i, 2);

                    int dd2d = Convert.ToInt32("A1", 16);

                    int ddd = Convert.ToInt32(temp.Substring(2 * i, 2), 16);

                    string st = char.ConvertFromUtf32(Convert.ToInt32(temp.Substring(2 * i, 2), 16));
                    if (st == "\r" || st == "\0" || st == "?")
                        break;
                    str = str + st;
                }
                WriteLog(string.Format("[Com{0}] Decode ID : {1}", m_nCom, str));
                strMID = str;

                strMID = strMID.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");//Ming
                //strFoupID.Trim();
            }

            return strMID;//讀失敗回復空的
        }

        public override bool CheckReader()
        {
            bool bSucc = SendCommand("SN");
            if (false == bSucc)
            {
                WriteLog(string.Format("[Com{0}] HEART RFID Send 'SN' to RFIDReader Failed", m_nCom));
                return false;
            }

            string strReply = m_strReply;
            bool bOkAck = strReply.IndexOf("OK") >= 0 ? true : false;
            if (false == bOkAck)
            {
                WriteLog(string.Format("[Com{0}] HEART RFID Receive 'OK' Reply From RFIDReader Failed.", m_nCom));
                return false;
            }

            WriteLog(string.Format("[Com{0}] HEART RFID Reader is connected!!", m_nCom));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strCommand));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strReply));

            return true;
        }

        public override string GetReply()
        {
            if (m_bSimulate)
            {
                if (m_strCommand.IndexOf("RD") >= 0)
                    m_strReply = "03800OK4E46463630303034350000000000000049";
                else if (m_strCommand.IndexOf("SN") >= 0)
                    m_strReply = "00900OKHeartVer1.54321";
            }
            return m_strReply;
        }


    }

    // OMROM
    public sealed class RFID_Omron : CIPC
    {
        private readonly int m_cnPage = 1;
        public RFID_Omron(int nCom, bool bSimulate = false)
        {
            m_bSimulate = bSimulate;
            string strCom = "COM" + nCom.ToString();
            if (!bSimulate)
                try
                {
                    m_Comport = new SerialPort(strCom, 9600, Parity.None, 8, StopBits.One);

                    if (false == m_Comport.IsOpen)
                    {
                        m_Comport.Open();
                    }

                    m_bConnect = m_Comport.IsOpen;
                }
                catch
                {
                    m_bConnect = false;
                }
            m_nCom = nCom;
        }

        //[Com6] send : 01010000000FFC73 at line 398 (SendCommand)
        //[Com6] recv : 01004C593636363838383900000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000007E at line 413 (SendCommand)

        public bool SendCommand(string strCmd, int nPage)
        {
            bool bSucc = false;
            InitCommand();

            m_strCommand = '\x01' + "01010000000FFC73";//1405   0C73

            m_strCommand += '\x0d';
            string temp = "";
            try
            {
                WriteLog(string.Format("[Com{0}] send : {1}", m_nCom, m_strCommand));

                m_Comport.Write(m_strCommand);

                SpinWait.SpinUntil(() => false, 1500);
                //收到的內容取出放入到容器temp
                while (m_Comport.BytesToRead > 0)
                {
                    int c = m_Comport.ReadChar();
                    if (c >= 0x30)
                    {
                        temp += ((char)c);
                    }
                }
                WriteLog(string.Format("[Com{0}] recv : {1}", m_nCom, temp));
                m_strReply = "";
                if (temp.Length > 0)
                {
                    //將temp取出兩個一組解出ASCII
                    for (int i = 4; i < temp.Length; i += 2)
                    {
                        int n = Convert.ToInt32(temp.Substring(i, 2), 16);
                        char myc = Convert.ToChar(n);
                        if (myc >= 0x30)
                            m_strReply += myc;
                        else if (myc < 0x30)
                            break;
                    }
                    bSucc = m_strReply != "";
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
            }

            if (false == bSucc)
            {
                WriteLog(string.Format("[Com{0}] ReplyCommandD[0]!= SOH", m_nCom));
            }

            return bSucc;
        }

        public override string ReadMID()
        {
            string strMID = "ReadFail";
            bool bSucc = SendCommand("0101", m_cnPage);

            if (bSucc)
            {
                strMID = m_strReply;

                strMID = strMID.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");//Ming
                //strFoupID.Trim();
            }
            return strMID;
        }

        private string IDParse(string strReply, int nPage)
        {
            string strMID = "";
            string strMark = strReply.Substring(3, 2);


            if (strMark == "00")
            {
                int nCount = 0;
                int nDecodeValue = 0;

                if (nPage == 1)
                {
                    for (int i = 0; i < 8; i++) // two characters to one actual character.
                    {
                        nCount = 0;
                        nDecodeValue = 0;
                        while (nCount <= 1)
                        {
                            if (strReply[5 + (i * 2) + nCount] >= '0' && strReply[5 + (i * 2) + nCount] <= '9')
                            {
                                if (nCount == 0) nDecodeValue += (16 * (strReply[5 + (i * 2) + nCount] - '0'));
                                else nDecodeValue += strReply[5 + (i * 2) + nCount] - '0';
                            }
                            else if (strReply[5 + (i * 2) + nCount] >= 'A' && strReply[5 + (i * 2) + nCount] <= 'F')
                            {
                                if (nCount == 0) nDecodeValue += (16 * ((strReply[5 + (i * 2) + nCount] - 'A') * 10));
                                else nDecodeValue += (strReply[5 + (i * 2) + nCount] - 'A') + 10;
                            }
                            nCount++;
                        }
                        strMID = nDecodeValue.ToString();
                    }
                }
                else if (nPage == 2)
                {
                    for (int i = 0; i < 16; i++) // two characters to one actual character.
                    {
                        nCount = 0;
                        nDecodeValue = 0;
                        while (nCount <= 1)
                        {

                            if (strReply[5 + (i * 2) + nCount] >= '0' && strReply[5 + (i * 2) + nCount] <= '9')
                            {
                                if (nCount == 0) nDecodeValue += (16 * (strReply[5 + (i * 2) + nCount] - '0'));
                                else nDecodeValue += strReply[5 + (i * 2) + nCount] - '0';
                            }
                            else if (strReply[5 + (i * 2) + nCount] >= 'A' && strReply[5 + (i * 2) + nCount] <= 'F')
                            {
                                if (nCount == 0) nDecodeValue += (16 * ((strReply[5 + (i * 2) + nCount] - 'A') * 10));
                                else nDecodeValue += (strReply[5 + (i * 2) + nCount] - 'A') + 10;
                            }
                            nCount++;
                        }
                        strMID = nDecodeValue.ToString();
                    }
                }

                WriteLog(string.Format("[Com{0}] RFID_Omron Read Successfully ID = {1}", m_nCom, strMID));

                if (strMID.Length < 8)
                {
                    WriteLog(string.Format("[Com{0}] RFID_Omron Read Successfully But The RFID Is Not Correct = {1}", m_nCom, strMID));
                }
            }
            else if (strMark == "14")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = Command Format Error !", m_nCom));
            }
            else if (strMark == "70")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = Communication Error Between Reader and Tag!", m_nCom));
            }
            else if (strMark == "71")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = Verification Error. Correct Data Cann't Be Writed Into ID Tag.!", m_nCom));
            }
            else if (strMark == "72")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = No Tag Error!!"));
            }
            else if (strMark == "7B")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = Outside Write Area Error. The Position For Tag Reading Ok,but Not For Writing.!", m_nCom));
            }
            else if (strMark == "7E")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = The ID Tag In The Staus That Can't Execute The Command.!", m_nCom));
            }
            else if (strMark == "7F")
            {
                WriteLog(string.Format("[Com{0}] RFID_Omron Readed Result = An Inapplicable ID Tag Has Been Used!", m_nCom));
            }

            return strMID;
        }

        public override bool CheckReader()
        {
            //MessageBox.Show("OMROM has no check function!");
            return true;
        }

        public override string GetReply()
        {
            if (m_bSimulate)
            {
                m_strReply = "01004E4646363030303471";
            }
            return m_strReply;
        }
    }
    // Brillian
    public sealed class RFID_Brillian : CIPC
    {
        public RFID_Brillian(int nCom, bool bSimulate = false)
        {
            m_bSimulate = bSimulate;
            string strCom = "COM" + nCom.ToString();
            if (!bSimulate)
                try
                {
                    m_Comport = new SerialPort(strCom, 9600, Parity.Even, 8, StopBits.One);
                    if (false == m_Comport.IsOpen)
                        m_Comport.Open();
                    m_bConnect = true;
                }
                catch (Exception ex)
                {
                    m_bConnect = false;
                    WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
                }
            m_nCom = nCom;
        }

        public bool SendCommand(string strCmd, string strParam = null)
        {
            bool bSucc = false;

            InitCommand();
            string strAddr = m_nDevice.ToString("00");
            int nLen = strAddr.Length + strCmd.Length + 2;
            if (strParam != null)
                nLen += strParam.Length;
            string strLen = nLen.ToString("000");//(strAddr.Length + strCmd.Length + strParam.Length + 2).ToString("3D");
            m_strCommand = '\x01' + strLen + strAddr + strCmd + strParam;

            char chCheck = m_strCommand[1];
            for (int i = 2; i < m_strCommand.Length; i++)
            {
                chCheck ^= m_strCommand[i];
            }

            m_strCommand += (((int)chCheck).ToString("X2")).ToString() + '\x0d' + '\0';

            try
            {
                m_Comport.Write(m_strCommand);
                Thread.Sleep(1000);//MING 210804
                //m_strReply = m_Comport.ReadLine();
                m_strReply = m_Comport.ReadExisting();
                if (m_strReply.Length > 0)
                {
                    bSucc = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog(string.Format("[Com{0}] Exception : {1}", m_nCom, ex));
            }

            if (m_bSimulate)
                bSucc = true;
            return bSucc;
        }

        public override string ReadMID()
        {
            string strMID = "ReadFail";
            bool bSucc = SendCommand("RD", "MPT0102");
            bool bOkResp = m_strReply.IndexOf("OK") >= 0 ? true : false;
            if (bSucc && bOkResp)
            {
                int nStart = m_strReply.IndexOf("OK") + 1;
                strMID = m_strReply.Substring(nStart);

                strMID = strMID.Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");//Ming
                //strFoupID.Trim();
            }
            return strMID;
        }

        public override bool CheckReader()
        {
            bool bSucc = SendCommand("SN");
            if (false == bSucc)
            {
                WriteLog(string.Format("[Com{0}] Brillian RFID Send 'SN' to RFIDReader Failed.", m_nCom));
                return false;
            }

            string strReply = m_strReply;
            bool bOkAck = strReply.IndexOf("OK") >= 0 ? true : false;
            if (false == bOkAck)
            {
                WriteLog(string.Format("[Com{0}] Brillian RFID Receive 'OK' Reply From RFIDReader Failed.", m_nCom));
                return false;
            }

            WriteLog(string.Format("[Com{0}] Brillian RFID Reader is connected!!", m_nCom));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strCommand));
            WriteLog(string.Format("[Com{0}] {1}", m_nCom, m_strReply));

            return true;
        }

        public override string GetReply()
        {
            if (m_bSimulate)
            {
                if (m_strCommand.IndexOf("RD") >= 0)
                    m_strReply = "03800OK4E46463630303034350000000000000049";
                else if (m_strCommand.IndexOf("SN") >= 0)
                    m_strReply = "00900OKHeartVer1.54321";
            }
            return m_strReply;
        }
    }
}
