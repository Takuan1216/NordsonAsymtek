using RorzeUnit.Net.Sockets;
using System;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Interface;

namespace RorzeUnit.Class.Aligner
{
    public class SSAlignerRA420 : SSAlignerParents
    {
        public SSAlignerRA420(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, bool bClampLiftPinUp, I_BarCode barcode, sServer Sever = null)
                : base(IP, PortID, nBodyNo, bDisable, bSimulate, bClampLiftPinUp, barcode, Sever)
        {
            WaferType =  SWafer.enumWaferSize.Inch12;
        }
        protected override void AnalysisGPOS(string strFrame)
        {
            try
            {
                base.AnalysisGPOS(strFrame);//呼叫父類別方法
                string[] str = strFrame.Split('/');
                // oALN1.GPOS:XAX1/YAX1/ZAX1/ROT1
                m_bZaxsInBottom = (int.Parse(str[2]) == 0 || int.Parse(str[2]) == 1);//0:at origin 1:at lower 5:upper                  

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
            m_Socket.SendCommand("HOME({0})", nP1);
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
                Clmp(1, bCheckVac);//very bottom
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
    }
}
