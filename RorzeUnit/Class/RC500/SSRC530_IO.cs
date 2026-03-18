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
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RorzeUnit.Class.RC500
{
    public class SSRC530_IO : SSRC550ParentsIO
    {
        //==============================================================================     
        public SSRC530_IO(string strIp, int nPort, int nBodyNo, bool bDisable, bool bSimulate, sServer Sever = null)
             : base(strIp, nPort, nBodyNo, bDisable, bSimulate, Sever)
        {

            m_dicGDIO_O[0] = "0000";
            m_dicGDIO_I[0] = "0000";
            if (bSimulate && nBodyNo == 1)
            {
                //m_dicGDIO_I[0] = "01EC";//RC530 DIO1 SystemIO maint mode                
                
                //m_dicGDIO_I[0] = "01ED";//0001 1110 1101 SystemIO run mode/ 2:負壓/3:正壓/5:正壓/6:靜電/8:光閘
            }
            else
            {
                m_dicGDIO_I[0] = "0000";
            }
        }
        protected override void OnAck(object sender, RorzeProtoclEventArgs e)
        {
            base.OnAck(sender, e);//呼叫父類別方法

            enumRC5X0Command_IO cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == e.Frame.Command).Key;

            switch (cmd)
            {
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

                for (int i = 0; i < 16; i++)
                {
                    SdouW(0, i, false);
                }

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
        public override void SdobW(int nID, int nBit, bool bOn)
        {
            base.SdobW(0, nBit, bOn);//呼叫父類別方法
        }
        public override void SdouW(int nID, int nBit, bool bOn)
        {
            base.SdobW(0, nBit, bOn);//呼叫父類別方法
        }
        public override bool GdioW(int nPort, int Bit)
        {
            _signalSubSequence.Reset();
            if (!Simulate)
            {
                _signalAck[enumRC5X0Command_IO.GDIO].Reset();
                m_Socket.SendCommand("GDIO");
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
            return GetGDIO_InputStatus(nPort, Bit);
        }

        //==============================================================================             
        #region =========================== CreateMessage ======================================   
        protected override void CreateMessage()
        {
            m_dicCancel[0x0005] = "0005Too many/too few parameters";
            m_dicCancel[0x0006] = "0006:Abnormal range of the parameter";
            m_dicCancel[0x0007] = "0007:Abnormal mode";
            m_dicCancel[0x0009] = "0009:System is preparing";
            m_dicCancel[0x000D] = "000D:Abnormal flash memory";
            m_dicCancel[0x000E] = "000E:Insufficient memory";
            m_dicCancel[0x000F] = "000F:Error-occurred state";

            m_dicController[0x00] = "[00:General] ";

            m_dicError[0x23] = "23:Failure reading of setting data.";
            //==============================================================================
            _dicCmdsTable = new Dictionary<enumRC5X0Command_IO, string>()
            {
                {enumRC5X0Command_IO.EVNT,"EVNT"},
                {enumRC5X0Command_IO.RSTA,"RSTA"},
                {enumRC5X0Command_IO.INIT,"INIT"},
                {enumRC5X0Command_IO.WTDT,"WTDT"},
                {enumRC5X0Command_IO.RTDT,"RTDT"},
                {enumRC5X0Command_IO.STAT,"STAT"},
                {enumRC5X0Command_IO.GDIO,"GDIO"},
                {enumRC5X0Command_IO.SDOU,"SDOU"},
                {enumRC5X0Command_IO.SDOB,"SDOB"},
                {enumRC5X0Command_IO.GVER,"GVER"},
                {enumRC5X0Command_IO.STIM,"STIM"},
                {enumRC5X0Command_IO.GTIM,"GTIM"},
                {enumRC5X0Command_IO.CNCT,"CNCT"},
            };
        }
        #endregion
        #region =========================== AnalysisGDIO =======================================
        //解析GDIO
        protected override void AnalysisGDIO(string strFrame)
        {
            try
            {
                if (strFrame.Contains('/') == false) { return; }
                string[] strArray = strFrame.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                //  InputOutput ex:00/00000000
                int nHCLID = int.Parse(strArray[0]);
                string temp = strArray[1];
                string strInput = temp.Substring(0, 4);
                string strOutput = temp.Substring(4, 4);
                bool[] bDI = new bool[16];
                bool[] bDO = new bool[16];

                lock (m_lockGDIO)
                {
                    if (nHCLID != 0) return;

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
            catch (Exception ex)
            {
                _logger.WriteLog("[DIO{0}] <<Exception>> AnalysisGDIO:" + ex, this.BodyNo);
            }

        }
        #endregion
    }
}
