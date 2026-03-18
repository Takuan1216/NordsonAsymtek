using RorzeUnit.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Class.RC500
{
    public class SSRC560_Motion : SSRC5X0Parents_Motion
    {
        public SSRC560_Motion(string IP, int PortID, int nBodyNo, bool bDisable, bool bSimulate, sServer Sever = null)
               : base(IP, PortID, nBodyNo, bDisable, bSimulate, Sever)
        { }

        protected override void CreateMessage()
        {
            m_dicCancel[0x0001] = "0001:Command not designated";
            m_dicCancel[0x0002] = "0002:The designated target motion not equipped";
            m_dicCancel[0x0003] = "0003:Too many/too few elements";
            m_dicCancel[0x0004] = "0004:Command not equipped";
            m_dicCancel[0x0005] = "0005:Too many/too few parameter";
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
            m_dicCancel[0x0010] = "0010:Origin search has been completed but the motion cannot be started";
            m_dicCancel[0x0011] = "0011:The emergency stop signal is turned on";
            m_dicCancel[0x0012] = "0012:The temporarily stop signal is turned on";
            m_dicCancel[0x0013] = "0013:Abnormal interlock signal";
            m_dicCancel[0x0014] = "0014:Drive power is turned off";
            m_dicCancel[0x0015] = "0015:Without excited";
            m_dicCancel[0x0016] = "0016:Abnormal current position";
            m_dicCancel[0x0017] = "0017:Abnormal target position";
            m_dicCancel[0x0018] = "0018:Command processing";
            m_dicCancel[0x0019] = "0019:Servo amplifier is not in the ready condition";
            m_dicCancel[0x001A] = "001A:Magnetic pole detection not completed";
            m_dicCancel[0x001B] = "001B:Excitation is turned on";

            m_dicController[0x01] = "[01:Servo amplifier:The 1st Axis] ";
            m_dicController[0x02] = "[02:Servo amplifier:The 2nd Axis] ";
            m_dicController[0x03] = "[03:Servo amplifier:The 3rd Axis] ";
            m_dicController[0x04] = "[04:Servo amplifier:The 4th Axis] ";
            m_dicController[0x05] = "[05:Servo amplifier:The 5th Axis] ";
            m_dicController[0x06] = "[06:Servo amplifier:The 6th Axis] ";
            m_dicController[0x0F] = "[0F:Servo amplifier] ";
            m_dicController[0x11] = "[11:Motor controller:The 1st Axis] ";
            m_dicController[0x12] = "[12:Motor controller:The 2nd Axis] ";
            m_dicController[0x13] = "[13:Motor controller:The 3rd Axis] ";
            m_dicController[0x14] = "[14:Motor controller:The 4th Axis] ";
            m_dicController[0x15] = "[15:Motor controller:The 5th Axis] ";
            m_dicController[0x16] = "[16:Motor controller:The 6th Axis] ";
            m_dicController[0x17] = "[17:Motor controller:Interpolation] ";
            m_dicController[0x20] = "[20:Remote I/O HCL0 ID0] ";
            m_dicController[0x21] = "[21:Remote I/O HCL0 ID1] ";
            m_dicController[0x22] = "[22:Remote I/O HCL0 ID2] ";
            m_dicController[0x23] = "[23:Remote I/O HCL0 ID3] ";
            m_dicController[0x24] = "[24:Remote I/O HCL1 ID0] ";
            m_dicController[0x25] = "[25:Remote I/O HCL1 ID1] ";
            m_dicController[0x26] = "[26:Remote I/O HCL1 ID2] ";
            m_dicController[0x27] = "[27:Remote I/O HCL1 ID3] ";
            m_dicController[0x28] = "[28:Remote I/O HCL3 ID0] ";
            m_dicController[0x29] = "[29:Remote I/O HCL3 ID1] ";
            m_dicController[0x2A] = "[2A:Remote I/O HCL3 ID2] ";
            m_dicController[0x2B] = "[2B:Remote I/O HCL3 ID3] ";       
            m_dicController[0xF1] = "[F1:The 1st Axis] ";
            m_dicController[0xF2] = "[F2:The 2nd Axis] ";
            m_dicController[0xF3] = "[F3:The 3rd Axis] ";
            m_dicController[0xF4] = "[F4:The 4th Axis] ";
            m_dicController[0xF5] = "[F5:The 5th Axis] ";
            m_dicController[0xF6] = "[F6:The 6th Axis] ";
            m_dicController[0xFF] = "[FF:System] ";

            m_dicError[0x01] = "01:Operation timeout";
            m_dicError[0x02] = "02:Sensor abnormal";
            m_dicError[0x03] = "03:Emergency stop";
            m_dicError[0x04] = "04:Command error";
            m_dicError[0x05] = "05:Controller communication abnormal";
            m_dicError[0x06] = "06:Work retaining check sensor abnormal";
            m_dicError[0x08] = "08:Work fall";
            m_dicError[0x0C] = "0C:Origin reset not completed";
            m_dicError[0x0D] = "0D:Encoder abnormal";
            m_dicError[0x0E] = "0E:Motor abnormal";
            m_dicError[0x10] = "10:Control power voltage too low";
            m_dicError[0x11] = "11:Drive power voltage too low";
            m_dicError[0x12] = "12:Regeneration unit overheat";
            m_dicError[0x14] = "14:Exhaust fan abnormal";
            m_dicError[0x20] = "20:Servo amplifier alarm";
            m_dicError[0x21] = "21:Battary voltage too low";
            m_dicError[0x22] = "22:Origin return error";
            m_dicError[0x23] = "23:Magnetic pole detection error";
            m_dicError[0x24] = "24:Excitation turning on/off error";
            m_dicError[0x25] = "25:Magnetic pole detection timeout";
            m_dicError[0x43] = "43:Task start-up failure";
            m_dicError[0x46] = "46:Data reading failure";
            m_dicError[0x83] = "83:Origin search disabled";
            m_dicError[0x84] = "84:Work retaining error";
            m_dicError[0x85] = "85:Interlock signal abnormal";
            m_dicError[0x86] = "86:Target position abnormal";
            m_dicError[0x89] = "89:Exhaust fan abnormal";
            m_dicError[0x8A] = "8A:Battery voltage too low";
        }
        protected override void GTDT_OrgnSensorW()
        {
            GtdtW(m_nAckTimeout,11);           
        }
    }
}
