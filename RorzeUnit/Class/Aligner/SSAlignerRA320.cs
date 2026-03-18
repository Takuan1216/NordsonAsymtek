using RorzeUnit.Net.Sockets;
using System;
using RorzeUnit.Class.Aligner.Enum;
using System.Windows;
using RorzeUnit.Interface;

namespace RorzeUnit.Class.Aligner
{
    public class SSAlignerRA320 : SSAlignerParents
    {
        //public readonly string[] m_strAxis = { "XAX1.", "ZAX1.", "ROT1." };
        public SSAlignerRA320(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, bool bClampLiftPinUp, I_BarCode barcode, sServer Sever = null)
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
                // oALN1.GPOS:XAX1/ZAX1/ROT1         
                m_bZaxsInBottom = (int.Parse(str[1]) == 0 || int.Parse(str[1]) == 1);//0:at origin 1:at lower 4:at top     
            }
            catch (Exception ex)
            {
                _logger.WriteLog("<<Exception>> :" + ex);
            }
        }

        #region =========================== HOME =======================================
        protected override void Home(int nP1, int nP2)
        {
            _signalAck[enumAlignerCommand.Home].Reset();
            //m_Socket.SendCommand("HOME({0},{1})", nP1, nP2);
            m_Socket.SendCommand(string.Format("HOME({0},{1})", nP1, nP2));
        }
        #endregion =====================================================================      

        //#region =========================== YAX1.GPOS =======================================
        //public override void GposY()
        //{
        //    throw new NotImplementedException();
        //}
        //#endregion =====================================================================

        #region =========================== CLMP =======================================     
        protected override void ClmpAfterLiftPinDown(bool bCheckVac)
        {
            try
            {
                Clmp(4, bCheckVac);//very bottom
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
                Uclm(1);//very top
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception Caught");
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
                    Uclm(4);//very bottom
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception Caught");
            }
        }
        #endregion =====================================================================    

        #region =========================== GTID =======================================
        protected override void Gtid()
        {
            _signalAck[enumAlignerCommand.GetID].Reset();
            m_Socket.SendCommand(string.Format("GTID"));
        }
        #endregion =====================================================================     

        #region =========================== GTMP =======================================
        protected override void Gtmp()
        {
            _signalAck[enumAlignerCommand.GetMP].Reset();
            m_Socket.SendCommand(string.Format("GTMP"));
        }
        #endregion =====================================================================    

        #region =========================== GPRS =======================================
        protected override void Gprs()
        {
            _signalAck[enumAlignerCommand.GetVacuumValue].Reset();
            m_Socket.SendCommand(string.Format("GPRS"));
        }
        #endregion =====================================================================
    }
}
