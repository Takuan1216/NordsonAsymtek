using RorzeUnit.Net.Sockets;
using System;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Interface;

namespace RorzeUnit.Class.Aligner
{
    public class SSAlignerTAL303 : SSAlignerParents
    {
        public SSAlignerTAL303(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, bool bClampLiftPinUp, I_BarCode barcode, sServer Sever = null)
                : base(IP, PortID, nBodyNo, bDisable, bSimulate, bClampLiftPinUp, barcode, Sever)
        {
            WaferType = SWafer.enumWaferSize.Panel;
        }
        protected override void AnalysisGPOS(string strFrame)
        {
            try
            {
                base.AnalysisGPOS(strFrame);//呼叫父類別方法
                string[] str = strFrame.Split('/');
                // oALN1.GPOS:XAX1/YAX1/ZAX1/ROT1
                m_bZaxsInBottom = (int.Parse(str[2]) != 99);//0:at origin 1:at lower 5:upper                  

            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>> :" + ex);
            }
        }

        #region =========================== HOME =======================================
        protected override void Home(int nP1, int nP2)
        {
            _signalAck[enumAlignerCommand.Home].Reset();
            m_Socket.SendCommand("HOME", nP1);
        }
        #endregion =====================================================================

        #region =========================== YAX1.GPOS =======================================
        public override void GposY()
        {
            _signalAck[enumAlignerCommand.GetYAxisPos].Reset();
            m_Socket.SendCommand(string.Format("YAX1.GPOS"));
        }
        #endregion =====================================================================

        #region =========================== CLMP =======================================     
        protected override void ClmpAfterLiftPinDown(bool bCheckVac)
        {
            try
            {
                Clmp(0, bCheckVac);//very bottom
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        #endregion =====================================================================

        #region =========================== UCLM =======================================     
        protected override void UclmAfterLiftPinUp()
        {
            try
            {
                Uclm(4);//very top
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        protected override void UclmAfterLiftPinDown()
        {
            try
            {
                if (IsZaxsInBottom())
                {
                    WriteLog("UclmAfterLiftPinDown:It's already PIN DOWN.");
                    Uclm(0);
                }
                else
                    Uclm(1);//very bottom
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        public void Uclm()
        {
            try
            {
                _signalAck[enumAlignerCommand.UnClamp].Reset();
                m_Socket.SendCommand(string.Format("UCLM"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        public override void UclmW(int nTimeout)
        {
            _signalSubSequence.Reset();
            if (!m_bSimulate)
            {
                _signals[enumAlignerSignalTable.MotionCompleted].Reset();

                Uclm();

                if (!_signalAck[enumAlignerCommand.UnClamp].WaitOne(nTimeout))
                {
                    SendAlmMsg(enumAlignerError.AckTimeout);
                    throw new SException((int)enumAlignerError.AckTimeout, string.Format("Send UCLM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.UnClamp]));
                }
                if (_signalAck[enumAlignerCommand.UnClamp].bAbnormalTerminal)
                {
                    SendAlmMsg(enumAlignerError.SendCommandFailure);
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Send UCLM command and wait Ack was timeout. [{0}]", _dicCmdsTable[enumAlignerCommand.UnClamp]));
                }
            }
            else
            {
                //SpinWait.SpinUntil(() => false, 1000);//看LOG需一秒
                //SpinWait.SpinUntil(() => false, 500);
            }
            _signalSubSequence.Set();
        }
        #endregion =====================================================================    

        //#region =========================== GTID =======================================
        //protected override void Gtid()
        //{
        //    throw new NotImplementedException();
        //}
        //#endregion ===================================================================== 

        //#region =========================== GTMP =======================================
        //protected override void Gtmp()
        //{
        //    throw new NotImplementedException();
        //}
        //#endregion =====================================================================     

        //#region =========================== GPRS =======================================
        //protected override void Gprs()
        //{
        //    throw new NotImplementedException();
        //}
        //#endregion =====================================================================

        //#region =========================== ALGN =======================================
        protected override void Algn(int nMode, int nPos)
        {
            try
            {
                _signalAck[enumAlignerCommand.Alignment].Reset();
                m_Socket.SendCommand(string.Format("ALGN(" + nMode + "," + nPos + ",1)"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }

        protected override void AlgnD(string strPos)//0~360
        {
            try
            {
                float n = float.Parse(strPos);
                //0~360
                while (n > 360) { n -= 360; }

                _signalAck[enumAlignerCommand.Alignment].Reset();
                m_Socket.SendCommand(string.Format("ALGN(1,D" + n + ",1)"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        }
        //#endregion =====================================================================

        public override bool IsReadyToLoad() { return (Wafer == null && IsMoving == false && IsZaxsInBottom() && IsError == false); }

        protected override void CreateMessage()
        {
            m_dicCancel[0x0001] = "0001:Command not designated";
            m_dicCancel[0x0002] = "0002:The designated target motion not equipped";
            m_dicCancel[0x0003] = "0003:Too many/few parameters";
            m_dicCancel[0x0004] = "0004:Command not equipped";
            m_dicCancel[0x0005] = "0005:Too many/few parameters";
            m_dicCancel[0x0006] = "0006:Abnormal range of the parameter";
            m_dicCancel[0x0007] = "0007:Abnormal mode";
            m_dicCancel[0x0008] = "0008:Abnormal data";
            m_dicCancel[0x0009] = "0009:System in preparation";
            m_dicCancel[0x000A] = "000A:Origin search not completed";
            m_dicCancel[0x000B] = "000B:Moving/Processing";
            m_dicCancel[0x000C] = "000C:No motion";
            m_dicCancel[0x000D] = "000D:Abnormal flash memory";
            m_dicCancel[0x000E] = "000E:Insufficient memory";
            m_dicCancel[0x000F] = "000F:Error-occurred state";
            m_dicCancel[0x0010] = "0010:Origin search is completed but interlock on";
            m_dicCancel[0x0011] = "0011:The emergency stop signal is turned on";
            m_dicCancel[0x0012] = "0012:The temporarily stop signal is turned on";
            m_dicCancel[0x0013] = "0013:Abnormal interlock signal";
            m_dicCancel[0x0014] = "0014:Drive power is turned off";
            m_dicCancel[0x0015] = "0015:Not excited";
            m_dicCancel[0x0016] = "0016:Abnormal current position";
            m_dicCancel[0x0017] = "0017:Abnormal target position";
            m_dicCancel[0x0018] = "0018:Command processing";
            m_dicCancel[0x0019] = "0019:Invalid work state";

            m_dicController[0x00] = "[00:Whole of the Aligner] ";
            m_dicController[0x01] = "[01:X-axis] ";
            m_dicController[0x02] = "[02:Y-axis] ";
            m_dicController[0x03] = "[03:Unused ";
            m_dicController[0x04] = "[04:Spindle axis] ";

            m_dicError[0x01] = "01:Motor stall";
            m_dicError[0x02] = "02:Limit";
            m_dicError[0x03] = "03:Position error";
            m_dicError[0x04] = "04:Command error";
            m_dicError[0x05] = "05:Communication error";
            m_dicError[0x06] = "06:Abnormal chucking sensor";
            m_dicError[0x07] = "07:Driver EMS error";
            m_dicError[0x08] = "08:Work dropped error";
            m_dicError[0x0E] = "0E:Abnormal driver";
            m_dicError[0x0F] = "0F:Abnormal drive power";
            m_dicError[0x10] = "10:Abnormal control power";
            m_dicError[0x13] = "13:Abnormal temperature of driver";
            m_dicError[0x14] = "14:Driver FPGA error";
            m_dicError[0x15] = "15:Motor wire broken";
            m_dicError[0x16] = "16:Motor over load";
            m_dicError[0x17] = "17:Motor motion error";
            m_dicError[0x18] = "18:Abnormal Alignment sensor";
            m_dicError[0x19] = "19:Abnormal exhaust fan state";
            m_dicError[0x40] = "40:Internal error(abnormal device driver)";
            m_dicError[0x41] = "41:Internal error(abnormal driver control)";
            m_dicError[0x42] = "42:Internal error(task start failed)";
            m_dicError[0x45] = "45:Reading setting data failed";
            m_dicError[0x7F] = "7F:Intarnal memory error";
            m_dicError[0x83] = "83:Origin search failed";
            m_dicError[0x84] = "84:Chucking error";
            m_dicError[0x90] = "90:Notch detection error";
            m_dicError[0x91] = "91:Alignment sensor detects obstacle";
            m_dicError[0x92] = "92:Retry over(alignment failed)";
            m_dicError[0x93] = "93:ID reading failed";
        }
    }
}
