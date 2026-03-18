using RorzeComm;
using RorzeComm.Threading;
using RorzeUnit.Class.RC500.RCEnum;
using RorzeUnit.Class.RC500.Event;
using RorzeUnit.Event;
using RorzeComm.Log;
using RorzeUnit.Net.Sockets;

using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;

namespace RorzeUnit.Class.RC500
{
    public class SSRC550_IO : SSRC550ParentsIO
    {
        public override event EventHandler<int[]> OnOccurGPRS;//壓差計
        //==============================================================================
        #region private
        private int[] m_nFanGREV = new int[20];
        private int[] m_nSenGPRS = new int[11];

        private bool[] _gpioInputData = new bool[64];
        private bool[] _gpioOutputData = new bool[64];
        #endregion
        //==============================================================================
        #region public        
        public override int[] GetFanGrev { get { return m_nFanGREV; } }// only rc550
        public override int[] GetSenGprs { get { return m_nSenGPRS; } }// only rc550
        #endregion
        //==============================================================================
        public SSRC550_IO(string strIp, int nPort, int nBodyNo, bool bDisable, bool bSimulate, sServer Sever = null)
                : base(strIp, nPort, nBodyNo, bDisable, bSimulate, Sever)
        {
         
        }
        protected override void OnAck(object sender, RorzeProtoclEventArgs e)
        {
            base.OnAck(sender, e);//呼叫父類別方法

            enumRC5X0Command_IO cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
                case enumRC5X0Command_IO.GPIO:
                    AnalysisGPIO(e.Frame.Value);
                    break;
                case enumRC5X0Command_IO.GDIO:
                    AnalysisGDIO(e.Frame.Value);
                    break;
                case enumRC5X0Command_IO.GPRS:
                    AnalysisGPRS(e.Frame.Value);
                    break;
                case enumRC5X0Command_IO.GREV:
                    AnalysisGREV(e.Frame.Value);
                    break;
                default:
                    break;
            }
        }
        //==============================================================================
        protected override void ExeINIT()
        {
            try
            {

                _logger.WriteLog(string.Format("[DIO{0}] ExeINIT start", BodyNo));
                this.EvntW();
                ResetProcessCompleted();
                this.InitW();
                WaitProcessCompleted(3000);
                this.StimW();

                SendEvent_OnInitializationComplete(this, new EventArgs());
            }
            catch (SException ex)
            {
                _logger.WriteLog("[DIO{0}] <<SException>> ExeINIT:" + ex, this.BodyNo);
                SendEvent_OnInitializationFail(this, new EventArgs());
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> ExeINIT:" + ex, this.BodyNo);
                SendEvent_OnInitializationFail(this, new EventArgs());
            }
        }
        //==============================================================================
        public override void MoveW(int nValue)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.MOVE].Reset();
                m_Socket.SendCommand("MOVE(0," + nValue + ")");
                if (!_signalAck[enumRC5X0Command_IO.MOVE].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.MOVE]));
                }
                if (_signalAck[enumRC5X0Command_IO.MOVE].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.MOVE]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }
        public override void StopW(int nValue)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.STOP].Reset();
                m_Socket.SendCommand("STOP(" + nValue + ")");
                if (!_signalAck[enumRC5X0Command_IO.STOP].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.MOVE]));
                }
                if (_signalAck[enumRC5X0Command_IO.STOP].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.MOVE]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }


        public override void SdobW(int nID, int nBit, bool bOn)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.SDOB].Reset();
                m_Socket.SendCommand("SDOB(" + Convert.ToInt32(nID) + "," + Convert.ToInt32(nBit) + "," + Convert.ToInt32(bOn) + ")");
                if (!_signalAck[enumRC5X0Command_IO.SDOB].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOB]));
                }
                if (_signalAck[enumRC5X0Command_IO.SDOB].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOB]));
                }
            }
            else
            {
                SetGDIO_OutputStatus(nID, nBit, bOn);
            }
            _signalSubSequence.Set();
        }


        public override void SdouW(int nID, int nBit1, int nBit2)//DIO0.SDOU(400,0,0)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.SDOU].Reset();
                m_Socket.SendCommand("SDOU(" + Convert.ToInt32(nID) + "," + nBit1.ToString("X4") + "," + nBit2.ToString("X4") + ")");
                if (!_signalAck[enumRC5X0Command_IO.SDOU].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOU]));
                }
                if (_signalAck[enumRC5X0Command_IO.SDOU].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.SDOU]));
                }
            }
            else
            {
                if (m_dicGDIO_I.ContainsKey(nID) == false)
                {
                    m_dicGDIO_I.Add(nID, "0000");
                }
                if (m_dicGDIO_I.ContainsKey(nID + 1) == false)
                {
                    m_dicGDIO_I.Add(nID + 1, "0000");
                }
                if (m_dicGDIO_O.ContainsKey(nID) == false)
                {
                    m_dicGDIO_O.Add(nID, "0000");
                }
                m_dicGDIO_O[nID] = nBit1.ToString("X4");
                if (m_dicGDIO_O.ContainsKey(nID + 1) == false)
                {
                    m_dicGDIO_O.Add(nID + 1, "0000");
                }
                m_dicGDIO_O[nID + 1] = nBit2.ToString("X4");
                //  Notify Evnt
                SendEvent_OnNotifyEvntGDIO(this, new NotifyGDIOEventArgs(nID, m_dicGDIO_I[nID], m_dicGDIO_O[nID]));
                SendEvent_OnNotifyEvntGDIO(this, new NotifyGDIOEventArgs(nID + 1, m_dicGDIO_I[nID + 1], m_dicGDIO_O[nID + 1]));
            }
            _signalSubSequence.Set();
        }
        public override bool GdioW(int nID, int Bit)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.GDIO].Reset();
                m_Socket.SendCommand(string.Format("GDIO({0:D3},{1})", nID, 1));
                if (!_signalAck[enumRC5X0Command_IO.GDIO].WaitOne(m_nAckTimeout))
                {
                    SendAlmMsg(enumRC500Error.AckTimeout);
                    throw new SException((int)enumRC500Error.AckTimeout, string.Format("Send command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.GDIO]));
                }
                if (_signalAck[enumRC5X0Command_IO.GDIO].bAbnormalTerminal)
                {
                    SendAlmMsg(enumRC500Error.SendCommandFailure);
                    throw new SException((int)enumRC500Error.SendCommandFailure, string.Format("Send command and wait Ack was failure. [{0}]", _dicCmdsTable[enumRC5X0Command_IO.GDIO]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
            return GetGDIO_InputStatus(nID, Bit);
        }

        //解析GPIO  
        private void AnalysisGPIO(string strFrame)
        {
            lock (m_lockGPIO)
            {
                if (!strFrame.Contains('/'))
                {
                    _logger.WriteLog("<<<Error>>> the format of GPIO frame has error, [{0}]", strFrame);
                    return;
                }
                string Pi = strFrame.Split('/')[0];
                string Po = strFrame.Split('/')[1];
                if (Pi.Length != 16 || Po.Length != 16) return;

                string _strPiH = Pi.Substring(0, 8);
                string _strPiL = Pi.Substring(8, 8);
                string _strPoH = Po.Substring(0, 8);
                string _strPoL = Po.Substring(8, 8);

                int _nPiH = Convert.ToInt32(_strPiH, 16);
                int _nPiL = Convert.ToInt32(_strPiL, 16);
                int _nPoH = Convert.ToInt32(_strPoH, 16);
                int _nPoL = Convert.ToInt32(_strPoL, 16);

                for (int nCnt = 0; nCnt < 64; nCnt++)
                {
                    if (nCnt <= 31)
                    {
                        _gpioInputData[nCnt] = (_nPiL & 1 << nCnt) != 0;
                        _gpioOutputData[nCnt] = (_nPoL & 1 << nCnt) != 0;
                    }
                    else
                    {
                        _gpioInputData[nCnt] = (_nPiH & 1 << nCnt) != 0;
                        _gpioOutputData[nCnt] = (_nPoH & 1 << nCnt) != 0;
                    }
                }
            }
        }
        //解析GPRS pressure
        private void AnalysisGPRS(string strFrame)
        {
            if (!strFrame.Contains('|'))
            {
                _logger.WriteLog("<<<Error>>> the format of GDIO frame has error, [{0}]", strFrame);
                return;
            }
            //  aDIO0.GPRS:01|00000000/02|00000000
            string[] strArray = strFrame.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strArray.Length; i++)
            {
                string strId = strArray[i].Substring(0, strArray[i].IndexOf("|"));
                string strValue = strArray[i].Substring(strArray[i].IndexOf("|") + 1);
                m_nSenGPRS[int.Parse(strId) - 1] = int.Parse(strValue);
            }
            OnOccurGPRS?.Invoke(this,m_nSenGPRS);
        }
        //FFU speed
        private void AnalysisGREV(string strFrame)
        {
            if (!strFrame.Contains('|'))
            {
                _logger.WriteLog("<<<Error>>> the format of GDIO frame has error, [{0}]", strFrame);
                return;
            }
            //  aDIO0.GREV:01|00000000/02|00000000/03|00000000
            string[] strArray = strFrame.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strArray.Length; i++)
            {
                string strId = strArray[i].Substring(0, strArray[i].IndexOf("|"));
                string strValue = strArray[i].Substring(strArray[i].IndexOf("|") + 1);
                m_nFanGREV[int.Parse(strId) - 1] = int.Parse(strValue);
            }
        }
        //==============================================================================
        #region =========================== CreateMessage ======================================
        protected override void CreateMessage()
        {
            m_dicCancel[0x0002] = "0002:Designated motion target is not equipped";
            m_dicCancel[0x0003] = "0003:Too many/too few elements";
            m_dicCancel[0x0004] = "0004:Designated command is not supported";
            m_dicCancel[0x0005] = "0005:Too many/too few parameters";
            m_dicCancel[0x0006] = "0006:Abnormal parameter range";
            m_dicCancel[0x0008] = "0008:Abnormal data";
            m_dicCancel[0x0009] = "0009:System preparing";
            m_dicCancel[0x000D] = "000D:Abnormal flash memory";
            m_dicCancel[0x000E] = "000E:Initialization not completed";
            m_dicCancel[0x000F] = "000F:Error-occurred state";
            m_dicCancel[0x0017] = "0017:Command processing";
            m_dicCancel[0x0019] = "0019:Designated motion target is invalid";

            m_dicController[0x00] = "[00:General] ";
            m_dicController[0x10] = "[10:RC550 connection] ";
            m_dicController[0x11] = "[11:RC550 HCL0 ID1] ";
            m_dicController[0x12] = "[12:RC550 HCL0 ID2] ";
            m_dicController[0x13] = "[13:RC550 HCL0 ID3] ";
            m_dicController[0x14] = "[14:RC550 HCL1 ID0] ";
            m_dicController[0x15] = "[15:RC550 HCL1 ID1] ";
            m_dicController[0x16] = "[16:RC550 HCL1 ID2] ";
            m_dicController[0x17] = "[17:RC550 HCL1 ID3] ";
            m_dicController[0x18] = "[18:RC550 HCL2 ID0] ";
            m_dicController[0x19] = "[19:RC550 HCL2 ID1] ";
            m_dicController[0x1A] = "[1A:RC550 HCL2 ID2] ";
            for (int i = 0; i < 20; i++)
            {
                m_dicController[0x20 + i] = string.Format("[{0:X2}:FAN{1}] ", 0x20 + i, i + 1);
            }
            for (int i = 0; i < 0x05; i++)
            {
                m_dicController[0x50 + i] = string.Format("[{0:X2}:SB078 Port{1}] ", 0x50 + i, i + 1);
            }
            for (int i = 0; i < 0x18; i++)//0~23
            {
                // 11/4=2,11%4=3
                m_dicController[0x60 + i] = string.Format("[{0:X2}:SB078 Port1 HCL{1} ID{2}] ", 0x60 + i, i / 4, i % 4);
                m_dicController[0x80 + i] = string.Format("[{0:X2}:SB078 Port2 HCL{1} ID{2}] ", 0x80 + i, i / 4, i % 4);
                m_dicController[0xA0 + i] = string.Format("[{0:X2}:SB078 Port3 HCL{1} ID{2}] ", 0xA0 + i, i / 4, i % 4);
                m_dicController[0xC0 + i] = string.Format("[{0:X2}:SB078 Port4 HCL{1} ID{2}] ", 0xC0 + i, i / 4, i % 4);
                m_dicController[0xE0 + i] = string.Format("[{0:X2}:SB078 Port5 HCL{1} ID{2}] ", 0xE0 + i, i / 4, i % 4);
            }
            m_dicError[0x01] = "01:Processing timeout";
            m_dicError[0x02] = "02:Multi-drop communication abnormal";
            m_dicError[0x03] = "03:Emergency stop";
            m_dicError[0x04] = "04:Internal system error";
            m_dicError[0x05] = "05:Communication error";
            m_dicError[0x0D] = "0D:FPGA error";
            m_dicError[0x45] = "45:Setting data reading error";
            m_dicError[0x50] = "50:Function code error";
            m_dicError[0x51] = "51:Improper register No. error";
            m_dicError[0x52] = "52:Improper number error";
            m_dicError[0x53] = "53:Data setting error";
            m_dicError[0x54] = "54:Writing mode error";
            m_dicError[0x55] = "55:Writing error while main circuit voltage is lowered";
            //==============================================================================
            _dicCmdsTable = new Dictionary<enumRC5X0Command_IO, string>()
            {
                {enumRC5X0Command_IO.EVNT,"EVNT"},
                {enumRC5X0Command_IO.RSTA,"RSTA"},
                {enumRC5X0Command_IO.INIT,"INIT"},
                {enumRC5X0Command_IO.MOVE,"MOVE"},
                {enumRC5X0Command_IO.STOP,"STOP"},
                {enumRC5X0Command_IO.WTDT,"WTDT"},
                {enumRC5X0Command_IO.RTDT,"RTDT"},
                {enumRC5X0Command_IO.STAT,"STAT"},
                {enumRC5X0Command_IO.GPIO,"GPIO"},
                {enumRC5X0Command_IO.SPOT,"SPOT"},
                {enumRC5X0Command_IO.SPTM,"SPTM"},
                {enumRC5X0Command_IO.GVER,"GVER"},
                {enumRC5X0Command_IO.GLOG,"GLOG"},
                {enumRC5X0Command_IO.STIM,"STIM"},
                {enumRC5X0Command_IO.GTIM,"GTIM"},
                {enumRC5X0Command_IO.GREV,"GREV"},
                {enumRC5X0Command_IO.GPRS,"GPRS"},
                {enumRC5X0Command_IO.GDIO,"GDIO"},
                {enumRC5X0Command_IO.SDOU,"SDOU"},
                {enumRC5X0Command_IO.SDOB,"SDOB"},
                {enumRC5X0Command_IO.CNCT,"CNCT"},
            };
        }
        #endregion
        #region =========================== AnalysisGDIO =======================================
        protected override void AnalysisGDIO(string strFrame)
        {
            try
            {
                if (strFrame.Contains('/') == false) { return; }

                string[] strArray = strFrame.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 1; i < strArray.Length; i++)
                {
                    //  左Output右Input ex:400/00000000/00000000
                    int nHCLID = int.Parse(strArray[0]) + i - 1;
                    string temp = strArray[i];
                    string strInput = temp.Substring(4, 4);
                    string strOutput = temp.Substring(0, 4);
                    lock (m_lockGDIO)
                    {
                        if (!m_dicGDIO_I.ContainsKey(nHCLID))
                            m_dicGDIO_I.Add(nHCLID, strInput);
                        else
                            m_dicGDIO_I[nHCLID] = strInput;

                        if (!m_dicGDIO_O.ContainsKey(nHCLID))
                            m_dicGDIO_O.Add(nHCLID, strOutput);
                        else
                            m_dicGDIO_O[nHCLID] = strOutput;
                    }
                    //  Notify Evnt
                    SendEvent_OnNotifyEvntGDIO(this, new NotifyGDIOEventArgs(nHCLID, strInput, strOutput));
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> AnalysisGDIO:" + ex, this.BodyNo);
            }

        }
        #endregion
    }
}

