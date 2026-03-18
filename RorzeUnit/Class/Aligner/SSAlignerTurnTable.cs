using RorzeApi;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Class.Aligner.Event;
using RorzeUnit.Interface;
using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RorzeUnit.Class.Aligner
{
    public class SSAlignerTurnTable : SSAlignerParents
    {
        private enum enumClmpStat { Unknow, Clmp, Uclm }

        public override event EventHandler<bool> OnORGNComplete;

        //private I_RC5X0_IO m_rc550;


        /*private int m_nDOClmpBit = 10;
        private int m_nDOUclmBit = 11;

        private int m_nDICWaferBit = 10;
        private int m_nDICDABit = 11;
        private int m_nDIClmpBit = 12;
        private int m_nDIUclmBit = 13;*/

        private int m_nDOClmpBit = 2;
        private int m_nDOUclmBit = 3;

        private int m_nDICDABit = 0;
        private int m_nDIClmpBit = 2;
        private int m_nDIUclmBit = 3;
        private int m_nDICWaferBit = 4;
        private int m_nDIFanBit = 5;

        private enumClmpStat m_eClmpStat;

        private bool m_bWaferEmpty;//故意判斷有片

        private bool m_bIsAirOK;
        private bool m_bIsClamp;
        private bool m_bIsUnClamp;
        private bool m_bIsFanOK;

        //public override bool Connected { get { return m_rc550.Connected; } }


        //public SSAlignerTurnTable(I_RC5X0_IO rc530, int nBodyNo, bool bDisable, bool bSimulate, I_BarCode barcode)
        //    : base("", 0, nBodyNo, bDisable, bSimulate, false, barcode, null)
        public SSAlignerTurnTable(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, I_BarCode barcode)
            : base(IP, PortID, nBodyNo, bDisable, bSimulate, false, barcode, null)
        {
            //m_rc550 = rc530;
            WaferType = SWafer.enumWaferSize.Frame;

            //m_rc550.OnNotifyEvntGDIO += _rc530_1_OnOccurInIOChange;

            if (m_bSimulate)
            {
                m_bWaferEmpty = true;//模擬空的
                m_eClmpStat = enumClmpStat.Uclm;
            }
        }

        //public override void Open() { }//甚麼都不用做

        protected override void ExeORGN()
        {
            try
            {
                WriteLog("ExeORGN:Start");

                EventW(3000);

                Exct();

                //ResetInPos();
                ClmpW(3000);
                //WaitInPos(10000);

                SpinWait.SpinUntil(() => false, 500);

                SspdW(3000, 7);//原點時降速

                ResetInPos();
                Rot1EXTD(GParam.theInst.GetTurnTable_angle_0(1));
                WaitInPos(100000);

                SspdW(3000, 0);//轉完切回全速

                SpinWait.SpinUntil(() => false, 500);

                //ResetInPos();
                UclmW(3000);
                //WaitInPos(10000);

                m_bStatOrgnComplete = true;

                OnORGNComplete?.Invoke(this, true);
            }
            catch (SException ex)
            {
                WriteLog("<<SException>> :" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
                OnORGNComplete?.Invoke(this, false);
            }
        }

        private void Exct()
        {
            _signalAck[enumAlignerCommand.Exct].Reset();
            m_Socket.SendCommand("EXCT(1)");
        }

        protected override void ClmpAfterLiftPinDown(bool bCheckVac)
        {
            throw new NotImplementedException();
        }

        protected override void Home(int nP1, int nP2)
        {
            throw new NotImplementedException();
        }

        protected override void UclmAfterLiftPinDown()
        {
            throw new NotImplementedException();
        }

        protected override void UclmAfterLiftPinUp()
        {
            throw new NotImplementedException();
        }

        protected override void RotationExtd(int nPos)
        {
            while (nPos >= 360000)
                nPos -= 360000;

            _signalAck[enumAlignerCommand.RAxisAbsolute].Reset();
            m_Socket.SendCommand("ROT1.MABS(" + nPos + ")");
        }
        public override void RotationExtdW(int nTimeout, int nPos)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                RotationExtd(nPos);
                if (!_signalAck[enumAlignerCommand.RAxisAbsolute].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Rotation MABS command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationExtd]));
                }
                if (_signalAck[enumAlignerCommand.RAxisAbsolute].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Rotation MABS command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationExtd]));
                }
            }
            else
            {

            }
            _signalSubSequence.Set();
        }

        protected override void RotStep(int nPos)
        {
            while (nPos >= 360000)
                nPos -= 360000;

            _signalAck[enumAlignerCommand.RAxisRelative].Reset();
            m_Socket.SendCommand("ROT1.MREL(" + nPos + ")");
        }
        protected override void RotStepW(int nTimeout, int nPos)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                RotStep(nPos);
                if (!_signalAck[enumAlignerCommand.RAxisRelative].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send Rotation Step command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationStep]));
                }
                if (_signalAck[enumAlignerCommand.RAxisRelative].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send Rotation Step command is Canecl. [{0}]", _dicCmdsTable[enumAlignerCommand.RotationStep]));
                }
            }
            else
            {
                while (nPos >= 360000)
                    nPos -= 360000;
                Raxispos += nPos;
            }
            _signalSubSequence.Set();
        }
        //----------------------------------------------------------------


        public override void ClmpW(int nTimeout, bool bCheckVac = true)
        {
            //m_rc550.SdouW(0, m_nDOClmpBit, true);
            //m_rc550.SdouW(0, m_nDOUclmBit, false);

            m_Socket.SendCommand("SPOT(" + m_nDOUclmBit + ",0)");
            m_Socket.SendCommand("SPOT(" + m_nDOClmpBit + ",1)");


            if (m_bSimulate == false && SpinWait.SpinUntil(() => m_eClmpStat == enumClmpStat.Clmp, 3000) == false)
            {
                SendAlmMsg(enumAlignerError.ClampTimeout);
                throw new SException((int)enumAlignerError.ClampTimeout, string.Format("Unclamp was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.Clamp]));
            }


            if (m_eClmpStat == enumClmpStat.Clmp)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Set();
                m_eStatInPos = enumAlignerStatus.InPos;
            }

            if (m_bSimulate)
            {
                /*Task.Run(() =>
                {
                    SpinWait.SpinUntil(() => false, 100);
                    m_rc550.SetGDIO_InputStatus(0, m_nDIClmpBit, false);//注意False是到位
                    m_rc550.SetGDIO_InputStatus(0, m_nDIUclmBit, true);//注意False是到位
                    SpinWait.SpinUntil(() => false, 500);
                    m_rc550.SetGDIO_InputStatus(0, m_nDIClmpBit, false);//注意False是到位
                    m_rc550.SetGDIO_InputStatus(0, m_nDIUclmBit, true);//注意False是到位
                });*/
            }
        }
        public override void UclmW(int nTimeout)
        {
            //m_rc550.SdouW(0, m_nDOClmpBit, false);
            //m_rc550.SdouW(0, m_nDOUclmBit, true);

            m_Socket.SendCommand("SPOT(" + m_nDOClmpBit + ",0)");
            m_Socket.SendCommand("SPOT(" + m_nDOUclmBit + ",1)");

            if (m_bSimulate == false && SpinWait.SpinUntil(() => m_eClmpStat == enumClmpStat.Uclm, 3000) == false)
            {
                SendAlmMsg(enumAlignerError.UnClampTimeout);
                throw new SException((int)enumAlignerError.UnClampTimeout, string.Format("Unclamp was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.UnClamp]));
            }

            if (m_eClmpStat == enumClmpStat.Uclm)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Set();
                m_eStatInPos = enumAlignerStatus.InPos;
            }

            if (m_bSimulate)
            {
                /*Task.Run(() =>
                {
                    SpinWait.SpinUntil(() => false, 100);
                    m_rc550.SetGDIO_InputStatus(0, m_nDIClmpBit, true);//注意False是到位
                    m_rc550.SetGDIO_InputStatus(0, m_nDIUclmBit, false);//注意False是到位
                    SpinWait.SpinUntil(() => false, 500);
                    m_rc550.SetGDIO_InputStatus(0, m_nDIClmpBit, true);//注意False是到位
                    m_rc550.SetGDIO_InputStatus(0, m_nDIUclmBit, false);//注意False是到位
                });*/
            }
        }

        /*void _rc530_1_OnOccurInIOChange(object sender, RorzeUnit.Class.RC500.Event.NotifyGDIOEventArgs e)
        {
            try
            {
                if (m_bSimulate) return;
                if (e.HCLID != 0 || e.Input == null || e.Input.Length != 16) return;

                if (e.Input[m_nDICDABit])
                {
                    m_bIsAirOK = true;
                }
                else
                {
                    m_bIsAirOK = false;
                }

                if (e.Input[m_nDICWaferBit])
                { 
                    m_bWaferEmpty = true; 
                }//true是空
                else
                { 
                    m_bWaferEmpty = false; 
                }//false是有片


                enumClmpStat eClmpStat;

                if (e.Input[m_nDIClmpBit] == false)//注意False是到位
                {
                    m_bIsClamp = true;
                }
                else
                {
                    m_bIsClamp = false;
                }

                if (e.Input[m_nDIUclmBit] == false)//注意False是到位
                {
                    m_bIsUnClamp = true;
                }
                else
                {
                    m_bIsUnClamp = false;
                }

                if (e.Input[m_nDIFanBit])
                {
                    m_bIsFanOK = true;
                }
                else
                {
                    m_bIsFanOK = false;
                }

                if (e.Input[m_nDIClmpBit] == false && e.Input[m_nDIUclmBit] == true)//注意False是到位
                {
                    eClmpStat = enumClmpStat.Clmp;
                }
                else if (e.Input[m_nDIClmpBit] == true && e.Input[m_nDIUclmBit] == false)//注意False是到位
                {
                    eClmpStat = enumClmpStat.Uclm;
                }
                else
                {
                    eClmpStat = enumClmpStat.Unknow;
                }

                if (eClmpStat != m_eClmpStat)//狀態改變
                {
                    if (e.Output[m_nDOClmpBit] == true && e.Output[m_nDOUclmBit] == false && eClmpStat == enumClmpStat.Clmp)
                    {
                        m_eClmpStat = eClmpStat;
                        _signals[enumAlignerSignalTable.MotionCompleted].Set();
                    }
                    else if (e.Output[m_nDOClmpBit] == false && e.Output[m_nDOUclmBit] == true && eClmpStat == enumClmpStat.Uclm)
                    {
                        m_eClmpStat = eClmpStat;
                        _signals[enumAlignerSignalTable.MotionCompleted].Set();
                    }
                    m_eStatInPos = enumAlignerStatus.InPos;
                }

            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }*/

        protected override void AnalysisGPIO(string strFrame)
        {
            if (m_bSimulate) return;

            try
            {
                if (!strFrame.Contains('/')) { return; }

                //GPIO = new SRA320GPIO(strFrame.Split('/')[0], strFrame.Split('/')[1]);

                //if (strFrame == null || strFrame.Length != 16) return;

                string strInput = strFrame.Substring(0, 8);
                Int64 nValue = Convert.ToInt64(strInput, 16);

                if (isBitOn(ref nValue, m_nDICDABit))
                {
                    m_bIsAirOK = true;
                }
                else
                {
                    m_bIsAirOK = false;
                }

                if (isBitOn(ref nValue, m_nDICWaferBit))
                {
                    m_bWaferEmpty = true;
                }//true是空
                else
                {
                    m_bWaferEmpty = false;
                }//false是有片


                enumClmpStat eClmpStat;

                if (isBitOn(ref nValue, m_nDIClmpBit) == false)//注意False是到位
                {
                    m_bIsClamp = true;
                }
                else
                {
                    m_bIsClamp = false;
                }

                if (isBitOn(ref nValue, m_nDIUclmBit) == false)//注意False是到位
                {
                    m_bIsUnClamp = true;
                }
                else
                {
                    m_bIsUnClamp = false;
                }

                if (isBitOn(ref nValue, m_nDIFanBit))
                {
                    m_bIsFanOK = true;
                }
                else
                {
                    m_bIsFanOK = false;
                }

                if (isBitOn(ref nValue, m_nDIClmpBit) == false && isBitOn(ref nValue, m_nDIUclmBit) == true)//注意False是到位
                {
                    eClmpStat = enumClmpStat.Clmp;
                }
                else if (isBitOn(ref nValue, m_nDIClmpBit) == true && isBitOn(ref nValue, m_nDIUclmBit) == false)//注意False是到位
                {
                    eClmpStat = enumClmpStat.Uclm;
                }
                else
                {
                    eClmpStat = enumClmpStat.Unknow;
                }

                if (eClmpStat != m_eClmpStat)//狀態改變
                {
                    if (isBitOn(ref nValue, m_nDOClmpBit) == true && isBitOn(ref nValue, m_nDOUclmBit) == false && (m_eClmpStat == enumClmpStat.Clmp || m_eClmpStat == enumClmpStat.Unknow))
                    {
                        m_eClmpStat = eClmpStat;
                        _signals[enumAlignerSignalTable.MotionCompleted].Set();
                    }
                    else if (isBitOn(ref nValue, m_nDOClmpBit) == false && isBitOn(ref nValue, m_nDOUclmBit) == true && (m_eClmpStat == enumClmpStat.Uclm || m_eClmpStat == enumClmpStat.Unknow))
                    {
                        m_eClmpStat = eClmpStat;
                        _signals[enumAlignerSignalTable.MotionCompleted].Set();
                    }
                    m_eStatInPos = enumAlignerStatus.InPos;
                }

            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>:" + ex);
            }
        }

        private bool isBitOn(ref Int64 nValue, int nBit)
        {
            Int64 n = nValue >> nBit;
            return ((n & 0x01) == 0x01);
        }

        public override bool IsAirOK()
        {
            return m_bSimulate ? true : m_bIsAirOK;
        }

        public override bool WaferExists()
        {
            bool b = (m_bWaferEmpty == false);
            return b;
        }

        public override bool IsClamp()
        {
            return m_bSimulate ? true : m_bIsClamp;
        }

        public override bool IsUnClamp()
        {
            return m_bSimulate ? true : m_bIsUnClamp;
        }

        public override bool IsFanOK()
        {
            return m_bSimulate ? true : m_bIsFanOK;
        }
    }
}
