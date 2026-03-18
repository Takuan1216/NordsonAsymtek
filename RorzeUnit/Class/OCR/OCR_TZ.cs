using RorzeComm.Log;
using RorzeComm;
using RorzeComm.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RorzeUnit.Net.Sockets;
using RorzeUnit.Class.OCR.Enum;
using RorzeUnit.Class.OCR.Evnt;
using System.Runtime.CompilerServices;
using RorzeUnit.Event;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Markup;
using System.IO;
using System.Drawing;
using RorzeUnit.Interface;


namespace RorzeUnit.Class.OCR
{
    public class OCR_TZ : I_OCR
    {
        public event MessageEventHandler OnReadData;// TCP Receive
        public event OccurErrorEventHandler OnOccurStatErr;
        public event OccurErrorEventHandler OnOccurCancel;
        public event OccurErrorEventHandler OnOccurCustomErr;
        public event OccurErrorEventHandler OnOccurErrorRest;

        //==============================================================================

        public Dictionary<int, string> _DicCancel { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> _DicController { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> _DicError { get; } = new Dictionary<int, string>();

        public int _BodyNo { get; private set; }
        public bool Connected { get; private set; }
        public bool Disable { get; private set; }
        //public string _Version { get { return m_dicCmdsRecv[enumOcrCommand.GVER]; } }
        public string Name { get { return m_eName.ToString(); } }
        public bool IsFront { get { return (m_eName == enumName.A1 || m_eName == enumName.B1); } }
        public string SavePicturePath { get; private set; }

        //==============================================================================
        private SPollingThread m_exePolling;
        private sRorzeSocket m_Socket;
        private sRorzeSocket m_SocketForImg;

        private SSignal m_signalSubSequence;
        private Dictionary<enumOcrCommand, string> m_dicCmdsTable = new Dictionary<enumOcrCommand, string>();
        private Dictionary<enumOcrCommand, SSignal> m_signalAck = new Dictionary<enumOcrCommand, SSignal>();
        private Dictionary<enumOcrSignalTable, SSignal> m_signals = new Dictionary<enumOcrSignalTable, SSignal>();
        private Dictionary<enumOcrCommand, string> m_dicCmdsRecv = new Dictionary<enumOcrCommand, string>();

        private enumOcrMode m_eStatMode;    //記憶的STAT S1第1 bit
        private bool m_bStatOrgnComplete;   //記憶的STAT S1第2 bit
        private bool m_bStatProcessed;      //記憶的STAT S1第3 bit
        private enumMoveStatus m_eStatInPos;//記憶的STAT S1第4 bit
        private int m_nSpeed;               //記憶的STAT S1第5 bit
        private string m_strErrCode = "0000";//記憶的STAT S2

        private bool m_bSimulate;
        private enumName m_eName;
        private bool m_bMoving;//運動中flag由流程控制
        private int m_nAckTimeout = 3000;
        private int m_nMotionTimeout = 120000;
        private int m_nImageTimeout = 30000;//圖片接收等待timeout(ms)

        private string m_strWaferID = "";
        private string m_strImage;//SaveImage
        private byte[] m_byteImage;//SaveImage - raw byte data
        private List<byte> m_lstImageBuffer = new List<byte>();//累積圖片數據
        private bool m_bReceivingImage = false;//是否正在接收圖片
        private DateTime m_dtLastImageReceive;//最後收到圖片數據的時間
        private int m_nImageReceiveTimeout = 500;//圖片接收超時(ms)，超過此時間沒有新數據則認為接收完成
        private SSignal m_signalImageReceived;//圖片數據接收完成的signal（與12000 port的ACK分開）
        private string[] m_strRecipe = null;

        private SLogger m_logger = SLogger.GetLogger("CommunicationLog");

        private SInterruptOneThread m_threadInit;
        private SInterruptOneThread m_threadRsta;

        public OCR_TZ(enumName eName, int nBody, string strIP, bool bDisable, bool bSimulate, sServer sever = null)
        {
            m_eName = eName;
            _BodyNo = nBody;
            Disable = bDisable;
            m_bSimulate = bSimulate;
            //172.20.9.21
            //Port 說明
            //12000 供 sorter，外部觸發功能，開放指令如下，[CNCT] 、[INIT] 、
            //      [STAT] 、[RSTA] 、[EVNT] 、[GVER] 、[STIM] 、[GTIM] 、
            //      [LORE] 、[EXEC] (不帶參數)
            //12005 供 sorter，取得影像資料
            //12001 供 Teaching Tool，設定參數功能
            //12006 供 Teaching Tool，取得影像資料
            m_Socket = new sRorzeSocket(strIP, 12000, _BodyNo, "OCR", bSimulate, sever);

            m_SocketForImg = new sRorzeSocket(strIP, 12005, _BodyNo, "OCR", bSimulate, null, true);

            CreateMessage();

            foreach (enumOcrCommand item in System.Enum.GetValues(typeof(enumOcrCommand)))
                m_dicCmdsRecv[item] = "";

            m_signalSubSequence = new SSignal(false, EventResetMode.ManualReset);

            foreach (enumOcrCommand item in System.Enum.GetValues(typeof(enumOcrCommand)))
                m_signalAck.Add(item, new SSignal(false, EventResetMode.ManualReset));

            foreach (enumOcrSignalTable item in System.Enum.GetValues(typeof(enumOcrSignalTable)))
                m_signals.Add(item, new SSignal(false, EventResetMode.ManualReset));

            m_signalImageReceived = new SSignal(false, EventResetMode.ManualReset);

            m_threadInit = new SInterruptOneThread(ExeINIT);
            m_threadRsta = new SInterruptOneThread(ExeRSTA);


            m_exePolling = new SPollingThread(1);
            m_exePolling.DoPolling += _exePolling_DoPolling;
            if (false == Disable) { m_exePolling.Set(); }

            if (m_bSimulate == false && Disable == false)
            {
                SpinWait.SpinUntil(() => false, 100);
                Open();
            }

        }

        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[OCR{0}] : {1}  at line {2} ({3})", _BodyNo, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }

        private void CreateMessage()
        {

            _DicCancel[0x0001] = "0001:The instruction format does not meet the usage conditions";
            _DicCancel[0x0002] = "0002:Wrong range for parameter";
            _DicCancel[0x0003] = "0003:Recipe not found";
            _DicCancel[0x0004] = "0004:The execution instruction parameter content is incorrect";
            _DicCancel[0x0005] = "0005:Hardware error during EXEC execution";
            _DicCancel[0x0010] = "0010:The system is busy";
            _DicCancel[0x0020] = "0020:The system does not support the command given";
            _DicCancel[0x0021] = "0021:The system is in remote state";
            _DicCancel[0x0022] = "0022:The system is in Teaching Tool status";
            _DicCancel[0x0023] = "0023:LIMG No photo data when reading the map";
            _DicCancel[0x0024] = "0024:LIMG Failed to transfer image (Socket Error)";
            _DicCancel[0x0030] = "0030:Abnormal photo taking";

            _DicController[0x00] = "[00:OCR] ";
            _DicController[0x01] = "[01:LIG] ";
            _DicController[0x02] = "[02:CAM] ";
            _DicController[0x03] = "[03:ALG] ";
            _DicController[0x04] = "[04:TEC] ";

            _DicError[0x01] = "01:Module Socket Error";
            _DicError[0x02] = "02:Timeout Error";
            _DicError[0x03] = "03:Initialize Error";
            _DicError[0x04] = "04:Hardware Error";
            _DicError[0xFF] = "FF:Fetal Error";

            //==============================================================================
            m_dicCmdsTable = new Dictionary<enumOcrCommand, string>()
            {
                { enumOcrCommand.CNCT,"CNCT" },
                { enumOcrCommand.INIT,"INIT" },
                { enumOcrCommand.STAT,"STAT" },
                { enumOcrCommand.RSTA,"RSTA" },
                { enumOcrCommand.EVNT,"EVNT" },
                { enumOcrCommand.GVER,"GVER" },
                { enumOcrCommand.STIM,"STIM" },
                { enumOcrCommand.GTIM,"GTIM" },

                { enumOcrCommand.SARE,"SARE" },
                { enumOcrCommand.LORE,"LORE" },
                { enumOcrCommand.GRFN,"GRFN" },
                { enumOcrCommand.SEST,"SEST" },

                { enumOcrCommand.GIMG,"GIMG" },
                { enumOcrCommand.LIMG,"LIMG" },

                { enumOcrCommand.SPAR,"SPAR" },
                { enumOcrCommand.GPAR,"GPAR" },

                { enumOcrCommand.EXEC,"EXEC" },
                { enumOcrCommand.AUTN,"AUTN" },
                { enumOcrCommand.DATA,"DATA" },

                { enumOcrCommand.IPST,"IPST" },
            };
        }

        //==============================================================================

        public void Open()
        {
            m_Socket.Open();
            SpinWait.SpinUntil(() => false, 100);
            m_SocketForImg.Open();
        }//Client

        private void _exePolling_DoPolling()
        {
            try
            {

                int Emptycount = 0;
                string[] astrFrame;

                if (m_Socket.QueRecvBuffer.TryDequeue(out astrFrame))
                {
                    string strFrame;

                    if (OnReadData != null) OnReadData(this, new MessageEventArgs(astrFrame));

                    for (int nCnt = 0; nCnt < astrFrame.Count(); nCnt++) //只處理第一個封包 2014.11.24
                    {
                        if (astrFrame[nCnt].Length == 0)
                        {
                            Emptycount += 1;

                            continue;
                        }

                        strFrame = astrFrame[nCnt];

                        enumOcrCommand cmd = enumOcrCommand.GVER;
                        bool bUnknownCmd = true;

                        foreach (string scmd in m_dicCmdsTable.Values) //查字典
                        {
                            if (strFrame.Contains(string.Format("OCR{0}.{1}", this._BodyNo.ToString("X"), scmd)))
                            {
                                cmd = m_dicCmdsTable.FirstOrDefault(x => x.Value == scmd).Key;
                                bUnknownCmd = false; //認識這個指令
                                break;
                            }
                        }

                        if (bUnknownCmd) //不認識的封包
                        {
                            WriteLog(string.Format("<<<ByPassReceive>>> Got unknown frame and pass to process. [{0}]", strFrame));
                            continue;
                        }

                        if (strFrame.Contains("CNCT"))
                        {

                        }
                        WriteLog(string.Format("Receive : {0}", strFrame));

                        switch (strFrame[0]) //命令種類
                        {
                            case 'c': //cancel
                                OnCancelAck(this, new RorzeProtoclEventArgs(strFrame));
                                break;
                            case 'n': //nak
                                m_signalAck[cmd].bAbnormalTerminal = true;
                                m_signalAck[cmd].Set();
                                break;
                            case 'a': //ack
                                OnAck(this, new RorzeProtoclEventArgs(strFrame));
                                m_signalAck[cmd].Set();
                                break;
                            case 'e':
                                OnAck(this, new RorzeProtoclEventArgs(strFrame));
                                break;
                            default:

                                break;
                        }
                    }
                }
                // 處理圖片Socket的byte資料 - 累積多個封包
                byte[] byteData;
                while (m_SocketForImg.QueRecvByteBuffer.TryDequeue(out byteData))
                {
                    WriteLog(string.Format("Receive image byte data, length: {0}, total: {1}", byteData.Length, m_lstImageBuffer.Count + byteData.Length));
                    m_lstImageBuffer.AddRange(byteData);
                    m_bReceivingImage = true;
                    m_dtLastImageReceive = DateTime.Now;
                }

                //檢查是否接收完成（超過timeout沒有新數據）
                if (m_bReceivingImage && (DateTime.Now - m_dtLastImageReceive).TotalMilliseconds > m_nImageReceiveTimeout)
                {
                    m_byteImage = m_lstImageBuffer.ToArray();
                    WriteLog(string.Format("Image receive completed, total size: {0} bytes", m_byteImage.Length));
                    m_lstImageBuffer.Clear();
                    m_bReceivingImage = false;
                    m_signalImageReceived.Set();//使用專門的signal，與12000 port的ACK分開
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> _exePolling_DoPolling:" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> _exePolling_DoPolling:" + ex);
            }
        }

        private void OnAck(object sender, RorzeProtoclEventArgs e)
        {
            enumOcrCommand cmd = m_dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            m_dicCmdsRecv[cmd] = e.Frame.Value;

            switch (cmd)
            {
                case enumOcrCommand.CNCT:
                    m_signalAck[cmd].Set();

                    Connected = true;

                    INIT();

                    break;
                case enumOcrCommand.STAT:
                    AnalysisStatus(e.Frame.Value);
                    break;
                case enumOcrCommand.GVER:
                    break;
                case enumOcrCommand.GTIM:
                    break;
                case enumOcrCommand.GRFN://獲取系統內所有設定檔案的名稱
                    m_strRecipe = e.Frame.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    break;
                case enumOcrCommand.LIMG://取得最後一張 EXEC 辨識的影像(For Sorter)。
                    m_strImage = e.Frame.Value;
                    break;
                case enumOcrCommand.EXEC:

                    break;
                case enumOcrCommand.DATA:
                    if (e.Frame.Value != null && e.Frame.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length == 3)
                    {
                        m_strWaferID = e.Frame.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[2];
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnCancelAck(object sender, RorzeProtoclEventArgs e)
        {
            enumOcrCommand cmd = m_dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;
            AnalysisCancel(e.Frame.Value);
        }

        private void AnalysisCancel(string strFrame)
        {
            if (Convert.ToInt32(strFrame, 16) > 0)
            {
                m_signals[enumOcrSignalTable.MotionCompleted].bAbnormalTerminal = true;
                m_signals[enumOcrSignalTable.MotionCompleted].Set(); //有moving過才可以Set

                SendCancelMsg(strFrame);
            }
        }

        private void AnalysisStatus(string strFrame)
        {
            if (!strFrame.Contains('/'))
            {
                WriteLog(string.Format("the format of STAT frame has error, '/' not found! [{0}]", strFrame));
                return;
            }
            string[] str = strFrame.Split('/');
            string s1 = str[0];
            string s2 = str[1];

            //S1.bit#1 operation mode
            switch (s1[0])
            {
                case '0':
                    m_eStatMode = enumOcrMode.Initializing;
                    //_signals[enumLoadPortSignalTable.Remote].Reset();
                    break;
                case '1':
                    m_eStatMode = enumOcrMode.Remote;
                    m_signals[enumOcrSignalTable.Remote].Set();
                    break;
                case '2':
                    m_eStatMode = enumOcrMode.Maintenance;
                    m_signals[enumOcrSignalTable.Remote].Set();
                    break;
            }

            //S1.bit#2 origin return complete
            if (s1[1] == '0') m_signals[enumOcrSignalTable.OPRCompleted].Reset();
            else m_signals[enumOcrSignalTable.OPRCompleted].Set();
            m_bStatOrgnComplete = s1[1] == '1';

            //S1.bit#3 processing command
            if (s1[2] == '0') m_signals[enumOcrSignalTable.ProcessCompleted].Set();
            else m_signals[enumOcrSignalTable.ProcessCompleted].Reset();
            m_bStatProcessed = s1[2] == '1';

            //S1.bit#4 operation status
            switch (s1[3])
            {
                case '0': m_eStatInPos = enumMoveStatus.InPos; break;
                case '1': m_eStatInPos = enumMoveStatus.Moving; break;
                case '2': m_eStatInPos = enumMoveStatus.Pause; break;
            }

            //S1.bit#5 operation speed
            if (s1[4] >= '0' && s1[4] <= '9') m_nSpeed = s1[4] - '0';
            else if (s1[4] >= 'A' && s1[4] <= 'K') m_nSpeed = s1[4] - 'A' + 10;
            if (m_nSpeed == 0) m_nMotionTimeout = 60000;
            else m_nMotionTimeout = 60000 * 3;

            //S2
            if (Convert.ToInt32(s2, 16) > 0)
            {
                m_signals[enumOcrSignalTable.MotionCompleted].bAbnormalTerminal = true;
                m_signals[enumOcrSignalTable.MotionCompleted].Set();
                SendAlmMsg(s2);
                m_strErrCode = s2;
            }
            else
            {
                if (m_eStatInPos == enumMoveStatus.InPos)//運動到位               
                    m_signals[enumOcrSignalTable.MotionCompleted].Set();
                else
                    m_signals[enumOcrSignalTable.MotionCompleted].Reset();

                if (m_strErrCode != "0000")
                {
                    RestAlmMsg(m_strErrCode);
                    m_strErrCode = "0000";
                }
            }
        }

        //==============================================================================

        public void INIT() { (m_threadInit).Set(); }
        private void ExeINIT()
        {
            try
            {
                WriteLog("Start");

                this.StimW(m_nAckTimeout);

                this.EvntW(m_nAckTimeout);

                this.SparW(m_nAckTimeout, "IsOnline:1");

                ResetInPos();
                this.InitW(m_nAckTimeout);
                WaitInPos(m_nMotionTimeout);

                WriteLog("Complete");
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
        }

        public void RSTA() { (m_threadRsta).Set(); }
        private void ExeRSTA()
        {
            try
            {
                WriteLog("Start");
                this.RstaW(m_nAckTimeout);
                //SpinWait.SpinUntil(() => false, 3000);
                WriteLog("Complete");
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
        }



        #region Error Msg
        //  Cancel Code
        private void SendCancelMsg(string strCode)
        {
            WriteLog(string.Format("Occur cancel Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurCancel?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生STAT異常
        private void SendAlmMsg(string strCode)
        {
            WriteLog(string.Format("Occur stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurStatErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  解除STAT異常
        private void RestAlmMsg(string strCode)
        {
            WriteLog(string.Format("Reset stat Error : {0}", strCode));
            if (strCode.Length != 4) return;
            int nCode = Convert.ToInt32(strCode, 16);
            OnOccurErrorRest?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        //  發生自定義異常
        private void SendAlmMsg(enumCustomErr eAlarm)
        {
            WriteLog(string.Format("Occur eAlarm Error : {0}", eAlarm));
            int nCode = (int)eAlarm;
            OnOccurCustomErr?.Invoke(this, new OccurErrorEventArgs(nCode));
        }
        #endregion

        private void CommandW(sRorzeSocket socket, int nTimeout, enumOcrCommand eCommand, params object[] args)
        {
            #region Reset param
            switch (eCommand)
            {
                case enumOcrCommand.EXEC:
                    m_strWaferID = "";
                    break;
                case enumOcrCommand.LIMG:
                    // 清空圖片緩衝區，準備接收新圖片
                    m_lstImageBuffer.Clear();
                    //m_byteImage = null;
                    m_bReceivingImage = false;
                    break;
            }
            #endregion

            m_signalSubSequence.Reset();
            if (Connected)
            {
                m_signalAck[eCommand].Reset();

                string strCmd = m_dicCmdsTable[eCommand];
                if (args == null || args.Length == 0)
                {
                    strCmd += "()";
                }
                else
                {
                    string placeholders = string.Join(",", Enumerable.Range(0, args.Length).Select(i => "{" + i + "}"));
                    strCmd = string.Format(strCmd + "(" + placeholders + ")", args);//
                }

                socket.SendCommand(strCmd);

                if (!m_signalAck[eCommand].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumCustomErr.AckTimeout);
                    throw new SException((int)enumCustomErr.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", eCommand));
                }
                if (m_signalAck[eCommand].bAbnormalTerminal)
                {
                    SendAlmMsg(enumCustomErr.SendCommandFailure);
                    throw new SException((int)enumCustomErr.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", eCommand));
                }
            }
            else
            {
            }
            m_signalSubSequence.Set();
        }


        #region =========================== INIT =======================================
        public void InitW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.INIT);
        }
        #endregion

        #region =========================== STAT =======================================
        public void StatW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.STAT);
        }
        #endregion

        #region =========================== RSTA =======================================
        public void RstaW(int nTimeout, params object[] args)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.RSTA, args);
        }
        #endregion

        #region =========================== EVNT =======================================
        public void EvntW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.EVNT, 0, 1);
        }
        #endregion

        #region =========================== GVER =======================================
        public void GverW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.GVER);
        }
        #endregion

        #region =========================== STIM =======================================
        public void StimW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.STIM, DateTime.Now.ToString("yyyy, MM, dd, HH, mm, ss"));
        }
        #endregion

        #region =========================== GTIM =======================================
        public void GtimW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.GTIM);
        }
        #endregion

        #region =========================== SARE ======================================= //存儲目前所有參數至檔案。
        public void SareW(int nTimeout, string strFileName)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.SARE, strFileName);
        }
        #endregion

        #region =========================== SARE ======================================= //讀取檔案並載入所有參數。
        public void LoreW(int nTimeout, string strFileName)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.LORE, strFileName);
        }
        #endregion

        #region =========================== GRFN ======================================= //獲取系統內所有設定檔案的名稱。
        public void GrfnW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.GRFN);
        }
        #endregion

        #region =========================== SEST ======================================= //設定系統開始時自動開啟的 recipe 名稱。
        public void SestW(int nTimeout, string strFileName)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.SEST, strFileName);
        }
        #endregion

        #region =========================== GIMG ======================================= //獲取相機拍攝的影像(For Teaching Tool)。
        public void GimgW(int nTimeout, int nConfigIndex)
        {
            throw new NotImplementedException();
            CommandW(m_Socket, nTimeout, enumOcrCommand.GIMG, nConfigIndex);
        }
        #endregion

        #region =========================== LIMG ======================================= //取得最後一張 EXEC 辨識的影像(For Sorter)。
        public void LimgW(int nTimeout)
        {
            // Reset 圖片接收完成的 signal
            m_signalImageReceived.Reset();

            // 發送 LIMG 命令並等待 12000 port 的 ACK（使用較短的 timeout）
            CommandW(m_Socket, m_nAckTimeout, enumOcrCommand.LIMG);

            // 等待 12005 port 的圖片數據接收完成
            if (!m_signalImageReceived.WaitOne(nTimeout))
            {
                SendAlmMsg(enumCustomErr.AckTimeout);
                throw new SException((int)enumCustomErr.AckTimeout, "Wait image data receive was timeout.");
            }
        }
        #endregion

        #region =========================== SPAR ======================================= //設定多個參數的數值或內容。
        public void SparW(int nTimeout)
        {
            throw new NotImplementedException();
        }
        public void SparW(int nTimeout, string strValue)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.SPAR, strValue);
        }
        #endregion

        #region =========================== GPAR ======================================= //獲取多個參數的數值或內容。
        public void GperW(int nTimeout)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region =========================== EXEC =======================================
        public void ExecW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.EXEC);
        }
        public void ExecW(int nTimeout, int nConfigIndex)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.EXEC, nConfigIndex);
        }
        #endregion

        #region =========================== AUTN =======================================
        public void AutnW(int nTimeout)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region =========================== DATA =======================================
        public void DataW(int nTimeout)
        {
            CommandW(m_Socket, nTimeout, enumOcrCommand.DATA);
        }
        #endregion

        #region =========================== IPST =======================================
        public void IpstW(int nTimeout)
        {
            throw new NotImplementedException();
        }
        #endregion

        public void ResetInPos()
        {
            m_signals[enumOcrSignalTable.MotionCompleted].Reset();
            m_bMoving = true;
        }
        public void WaitInPos(int nTimeout)
        {
            if (Connected)
            {
                //motion complete
                if (!m_signals[enumOcrSignalTable.MotionCompleted].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumCustomErr.MotionTimeout);
                    throw new SException((int)enumCustomErr.MotionTimeout, string.Format("Wait motion complete was timeout. [Timeout = {0} ms]", nTimeout));
                }
                if (m_signals[enumOcrSignalTable.MotionCompleted].bAbnormalTerminal)
                {
                    SendAlmMsg(enumCustomErr.MotionAbnormal);
                    throw new SException((int)enumCustomErr.MotionAbnormal, string.Format("Wait process flag complete was failure. [Timeout = {0} ms]", nTimeout));
                }
                m_bMoving = false;
            }
            else if (m_bSimulate)
            {
                SpinWait.SpinUntil(() => false, 200);
                m_bMoving = false;
            }

        }

        //==============================================================================
        public bool Initial(string strRecipe)
        {
            bool bSucc = false;
            try
            {
                WriteLog("Initial");

                StimW(m_nAckTimeout);

                EvntW(m_nAckTimeout);

                SpinWait.SpinUntil(() => false, 100);

                ResetInPos();
                this.InitW(m_nAckTimeout);
                WaitInPos(m_nMotionTimeout);
                this.SestW(m_nAckTimeout, strRecipe);

                bSucc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            return bSucc;
        }

        public bool OnLine()
        {
            bool bSucc = false;
            try
            {
                WriteLog("OnLine");
                this.SparW(m_nAckTimeout, "IsOnline:1");
                bSucc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            return bSucc;
        }

        public bool OffLine()
        {
            bool bSucc = false;
            try
            {
                WriteLog("OffLine");
                this.SparW(m_nAckTimeout, "IsOnline:1"); // NO NEED
                bSucc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            return bSucc;
        }

        public bool SetRecipe(string strName)
        {
            bool bSucc = false;
            try
            {
                if (strName.IndexOf(".job") < 0)
                    strName += ".job";
                WriteLog("SetRecipe");
                //this.SestW(m_nAckTimeout, strName);

                this.LoreW(m_nAckTimeout, strName);

                bSucc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            return bSucc;
        }

        public bool Read(ref string strResult, bool bOKSaveImage, string strCarrierID = "", string strLotID = "")
        {
            bool bSucc = false;
            string strWaferID = "";
            try
            {
                OnLine();//測試看要不要切ONLINE?

                ResetInPos();
                ExecW(m_nAckTimeout);
                WaitInPos(m_nMotionTimeout);

                DataW(m_nAckTimeout);

                strResult = m_strWaferID;

                bSucc = true;
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            strWaferID = strResult;
            return bSucc;
        }

        public bool SaveImage(string strFaileName)
        {
            bool bSucc = false;
            try
            {
                string strFolder = Path.Combine(Directory.GetCurrentDirectory(), "OCR_Image");
                strFolder = Path.Combine(strFolder, DateTime.Now.ToString("yyyMMdd"));

                string strPath = Path.Combine(strFolder, strFaileName + ".jpg");

                if (Directory.Exists(strFolder) == false)
                {
                    Directory.CreateDirectory(strFolder);
                }

                LimgW(m_nImageTimeout);

                if (m_byteImage != null && m_byteImage.Length > 0)
                {
                    // Use raw byte data directly
                    using (MemoryStream memStream = new MemoryStream(m_byteImage))
                    {
                        // Save the memorystream to file
                        Bitmap.FromStream(memStream).Save(strPath);
                    }
                    bSucc = true;
                    m_byteImage = null;
                }
                else
                {
                    WriteLog("<<Warning>> SaveImage: m_byteImage is null or empty");
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("Exception>> :" + ex);
            }
            return bSucc;
        }

        public bool SaveImage(string strFolder, string strFaileName = "test.jpg")
        {
            bool bSucc = false;
            try
            {
                string strPath = Path.Combine(strFolder, strFaileName);

                if (Directory.Exists(strFolder) == false)
                {
                    Directory.CreateDirectory(strFolder);
                }

                LimgW(m_nImageTimeout);

                if (m_byteImage != null && m_byteImage.Length > 0)
                {
                    // Use raw byte data directly
                    using (MemoryStream memStream = new MemoryStream(m_byteImage))
                    {
                        // Save the memorystream to file
                        Bitmap.FromStream(memStream).Save(strPath);
                    }
                    bSucc = true;
                }
                else
                {
                    WriteLog("<<Warning>> SaveImage: m_byteImage is null or empty");
                }
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("Exception>> :" + ex);
            }
            return bSucc;
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

        public string[] getRecipt()
        {
            try
            {
                GrfnW(m_nAckTimeout);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            catch (Exception ex)
            {
                WriteLog("<<SException>> :" + ex);
            }
            return m_strRecipe;
        }
    }
}
