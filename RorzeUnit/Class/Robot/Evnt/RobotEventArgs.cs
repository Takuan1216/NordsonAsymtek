using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RorzeUnit.Class.Robot.Event
{
    public delegate void AutoProcessingEventHandler(object sender);

    public class RorzenumRobotProtoclEventArgs : EventArgs
    {
        public SRR717Frame Frame { get; set; }
        public RorzenumRobotProtoclEventArgs(string frame)
        {
            Frame = new SRR717Frame(frame);
        }
    }
    public delegate void RorzenumRobotProtoclHandler(object sender, RorzenumRobotProtoclEventArgs e);

    public class SRR717GPIO
    {
        private string _strPi;
        private string _strPo;

        private int _nPi;
        private int _nPo;
        //DI
        private bool _iBatteryError;        //5
        private bool _iUpperArmHasWafer;    //8
        private bool _iUpperArmStandbyPos;  //10
        private bool _iUpperArmLoadSensor;  //11
        private bool _iLowerArmHasWafer;    //18
        private bool _iLowerArmStandbyPos;  //20
        private bool _iLowerArmLoadSensor;  //21
        //DO
        private bool _oUpperArmSolenoidOn;  //8
        private bool _oUpperArmSolenoidOff; //9
        private bool _oLowerArmSolenoidOn;  //18
        private bool _oLowerArmSolenoidOff; //19

        public SRR717GPIO(string Pi, string Po)
        {
            _strPi = Pi;
            _strPo = Po;

            _nPi = Convert.ToInt32(_strPi, 16);
            _nPo = Convert.ToInt32(_strPo, 16);

            _iBatteryError = (_nPi & (1 << 5)) != 0;
            _iUpperArmHasWafer = (_nPi & (1 << 8)) != 0; //_strPi[8] == '1';
            _iUpperArmStandbyPos = (_nPi & (1 << 10)) != 0; //_strPi[10] == '1';
            _iUpperArmLoadSensor = (_nPi & (1 << 11)) != 0; //_strPi[11] == '1';
            _iLowerArmHasWafer = (_nPi & (1 << 18)) != 0;// _strPi[18] == '1';
            _iLowerArmStandbyPos = (_nPi & (1 << 20)) != 0; //_strPi[20] == '1';
            _iLowerArmLoadSensor = (_nPi & (1 << 21)) != 0; //_strPi[21] == '1';

            _oUpperArmSolenoidOn = (_nPo & (1 << 8)) != 0; //_strPo[8] == '1';
            _oUpperArmSolenoidOff = (_nPo & (1 << 9)) != 0; //_strPo[9] == '1';
            _oLowerArmSolenoidOn = (_nPo & (1 << 18)) != 0; //_strPo[18] == '1';
            _oLowerArmSolenoidOff = (_nPo & (1 << 19)) != 0; //_strPo[19] == '1';

            //_iBatteryError = _strPi[5] == '1';
            //_iUpperArmHasWafer = _strPi[8] == '1';
            //_iUpperArmStandbyPos = _strPi[10] == '1';
            //_iUpperArmLoadSensor = _strPi[11] == '1';
            //_iLowerArmHasWafer = _strPi[18] == '1';
            //_iLowerArmStandbyPos = _strPi[20] == '1';
            //_iLowerArmLoadSensor = _strPi[21] == '1';

            //_oUpperArmSolenoidOn = _strPo[8] == '1';
            //_oUpperArmSolenoidOff = _strPo[9] == '1';
            //_oLowerArmSolenoidOn = _strPo[18] == '1';
            //_oLowerArmSolenoidOff = _strPo[19] == '1';
        }

        public bool DI_BatteryError { get { return _iBatteryError; } }
        public bool DI_UpperArmHasWafer { get { return _iUpperArmHasWafer; } }
        public bool DI_UpperArmStandbyPos { get { return _iUpperArmStandbyPos; } }
        public bool DI_UpperArmLoadSensor { get { return _iUpperArmLoadSensor; } }
        public bool DI_LowerArmHasWafer { get { return _iLowerArmHasWafer; } }
        public bool DI_LowerArmStandbyPos { get { return _iLowerArmStandbyPos; } }
        public bool DI_LowerArmLoadSensor { get { return _iLowerArmLoadSensor; } }

        public bool DO_UpperArmSolenoidOn { get { return _oUpperArmSolenoidOn; } }
        public bool DO_UpperArmSolenoidOff { get { return _oUpperArmSolenoidOff; } }
        public bool DO_LowerArmSolenoidOn { get { return _oLowerArmSolenoidOn; } }
        public bool DO_LowerArmSolenoidOff { get { return _oLowerArmSolenoidOff; } }
    }
    public class SRR717Frame
    {
        private string _strFrame;   //aTRB1.STAT:00000/0000

        private char _charHeader;   //o,a,n,c,e
        private string _strID;      //TRB1.
        private string _strData;    //STAT:00000/0000

        private int _nBodyNo;       //1
        private string _strCommand; //STAT
        private string _strValue;   //00000/0000

        public SRR717Frame(string strFrame)
        {
            _strFrame = strFrame.Trim('\r', '\n');

            _charHeader = _strFrame[0];
            _strID = _strFrame.Substring(1, 5);
            _strData = _strFrame.Substring(6);

            _nBodyNo = _strID[3] >= 'A' ? _strID[3] - 'A' + 10 : _strID[3] - '0';
            _strCommand = _strData.Split(':')[0];
            _strValue = _strData.Contains(':') ? _strData.Split(':')[1] : "";
        }

        public char Header { get { return _charHeader; } }
        public string ID { get { return _strID; } }
        public string Data { get { return _strData; } }
        public int BodyNo { get { return _nBodyNo; } }
        public string Command { get { return _strCommand; } }
        public string Value { get { return _strValue; } }
    }

    public delegate void SRR717IOChangelHandler(object sender, SRR717IOChengeEventArgs e);
    public class SRR717IOChengeEventArgs : EventArgs
    {
        public SRR717GPIO Frame { get; set; }
        public SRR717IOChengeEventArgs(SRR717GPIO frame)
        {
            Frame = frame;
        }
    }

    public class SException : Exception
    {
        public int ErrorID { get; set; }
        public SException(int nErrorID, string strMsg)
            : base(strMsg)
        {
            ErrorID = nErrorID;
        }
    }

    public delegate void SendCommandHandler(string strCommand, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null);

    public delegate void RobotModeExchangeHandler(object sender, RobotModeExchangeEventArgs e);
    public class RobotModeExchangeEventArgs : EventArgs
    {
        public RobotModeExchangeEventArgs(enumRobotMode mode)
        {
            Mode = mode;
        }
        public enumRobotMode Mode { get; private set; }
    }

    public delegate void RobotSpeedExchangeHandler(object sender, RobotSpeedExchangeEventArgs e);
    public class RobotSpeedExchangeEventArgs : EventArgs
    {
        public RobotSpeedExchangeEventArgs(int nSpeed)
        {
            Speed = nSpeed;
        }
        public int Speed { get; private set; }
    }

    public delegate void RobotPositionChangedHandler(object sender, RobotPositionEventArgs e);
    public class RobotPositionEventArgs : EventArgs
    {
        public int Position { get; set; }
        public RobotPositionEventArgs(int pos)
        {
            Position = pos;
        }
    }

    public delegate void RobotOEMAlarmHandler(object sender, RobotOEMAlarmEventArgs e);
    public class RobotOEMAlarmEventArgs : EventArgs
    {
        public RobotOEMAlarmEventArgs(string strErrCode)
        {
            AlarmCode = strErrCode;
        }
        public string AlarmCode { get; set; }
    }
    //==============================================================================
    public class SRR757GPIO
    {
        private string _strPi;
        private string _strPo;
        private int _nPi;
        private int _nPo;
        //DI
        private bool _iEmergencyStop_0;       //0
        private bool _iTemporarilyStop_1;     //1
        private bool _iVacuumSource_2;        //2
        private bool _iAirSource_3;           //3
        private bool _iZBrake_4;              //4
        private bool _iExhaustFan_5;          //5
        private bool _iExhaustFanUpper_6;     //6
        private bool _iExhaustFanLower_7;     //7
        private bool _iUpper1Presence1_8;     //8
        private bool _iUpper1Presence2_9;     //9
        private bool _iUpper2Presence1_10;    //10
        private bool _iUpper2Presence2_11;    //11
        private bool _iUpper3Presence1_12;    //12
        private bool _iUpper3Presence2_13;    //13
        private bool _iUpper4Presence1_14;    //14
        private bool _iUpper4Presence2_15;    //15
        private bool _iUpper5Presence1_16;    //16
        private bool _iUpper5Presence2_17;    //17
        private bool _iLowerPresence1_18;     //18
        private bool _iLowerPresence2_19;     //19
        private bool _iSignalNotConnected_19; //20
        private bool _iEmergencyStop_21;      //21

        private bool _iSignalNotConnected_30; //30
        private bool _iSignalNotConnected_31; //31

        //DO
        private bool _oSignalNotConnected_0;   //0
        private bool _oSignalNotConnected_1;   //1
        private bool _oSignalNotConnected_2;   //2
        private bool _oSignalNotConnected_3;   //3
        private bool _oSignalNotConnected_4;   //4
        private bool _oSignalNotConnected_5;   //5
        private bool _oSignalNotConnected_6;   //6
        private bool _oSignalNotConnected_7;   //7
        private bool _oUpper1SolenoidOn_8;     //8  
        private bool _oUpper1SolenoidOff_9;    //9
        private bool _oLowerSolenoidOn_18;     //18
        private bool _oLowerSolenoidOff_19;    //19
        private bool _oSignalNotConnected_20;  //20
        private bool _oSignalNotConnected_21;  //21
        private bool _oSignalNotConnected_22;  //22
        private bool _oSignalNotConnected_23;  //23
        private bool _oXAxisExcitation_24;     //24
        private bool _oZAxisExcitation_25;     //25
        private bool _oRotAxisExcitation_26;   //26
        private bool _oUpperExcitation_27;     //27
        private bool _oLowerExcitation_28;     //28
        private bool _oSignalNotConnected_29;  //29
        private bool _oUpperArmOrigin_30;      //30
        private bool _oLowerArmOrigin_31;      //31

        public SRR757GPIO(string Pi, string Po)
        {
            _strPi = Pi;
            _strPo = Po;
            _nPi = Convert.ToInt32(_strPi, 16);
            _nPo = Convert.ToInt32(_strPo, 16);
            //DI
            _iEmergencyStop_0 = (_nPi & 1) != 0;
            _iTemporarilyStop_1 = (_nPi & 1 << 1) != 0;
            _iVacuumSource_2 = (_nPi & 1 << 2) != 0;
            _iAirSource_3 = (_nPi & 1 << 3) != 0;
            _iZBrake_4 = (_nPi & 1 << 4) != 0;
            _iExhaustFan_5 = (_nPi & 1 << 5) != 0;
            _iExhaustFanUpper_6 = (_nPi & 1 << 6) != 0;
            _iExhaustFanLower_7 = (_nPi & 1 << 7) != 0;
            _iUpper1Presence1_8 = (_nPi & 1 << 8) != 0;
            _iUpper1Presence2_9 = (_nPi & 1 << 9) != 0;
            _iUpper2Presence1_10 = (_nPi & 1 << 8) != 0;
            _iUpper2Presence2_11 = (_nPi & 1 << 9) != 0;


            _iLowerPresence1_18 = (_nPi & 1 << 18) != 0;
            _iLowerPresence2_19 = (_nPi & 1 << 19) != 0;
            _iEmergencyStop_21 = (_nPi & 1 << 21) != 0;
            //DO
            _oUpper1SolenoidOn_8 = (_nPo & 1 << 8) != 0;
            _oUpper1SolenoidOff_9 = (_nPo & 1 << 9) != 0;
            _oLowerSolenoidOn_18 = (_nPo & 1 << 18) != 0;
            _oLowerSolenoidOff_19 = (_nPo & 1 << 19) != 0;
            _oXAxisExcitation_24 = (_nPo & 1 << 24) != 0;
            _oZAxisExcitation_25 = (_nPo & 1 << 25) != 0;
            _oRotAxisExcitation_26 = (_nPo & 1 << 26) != 0;
            _oUpperExcitation_27 = (_nPo & 1 << 27) != 0;
            _oLowerExcitation_28 = (_nPo & 1 << 28) != 0;
            _oUpperArmOrigin_30 = (_nPo & 1 << 30) != 0;
            _oLowerArmOrigin_31 = (_nPo & 1 << 31) != 0;
        }
        public bool DI_ExhaustFan { get { return _iExhaustFan_5; } }
        public bool DI_ExhaustFanUpper { get { return _iExhaustFanUpper_6; } }   //6
        public bool DI_ExhaustFanLower { get { return _iExhaustFanLower_7; } }   //7
        public bool DI_UpperPresence1 { get { return _iUpper1Presence1_8; } }   //8
        public bool DI_UpperPresence2 { get { return _iUpper1Presence2_9; } }   //9

        public bool DI_LowerPresence1 { get { return _iLowerPresence1_18; } }   //18
        public bool DI_LowerPresence2 { get { return _iLowerPresence2_19; } }   //19
        public bool DI_EmergencyStop { get { return _iEmergencyStop_21; } }    //21
        //DO
        public bool DO_UpperSolenoidOn { get { return _oUpper1SolenoidOn_8; } }  //8
        public bool DO_UpperSolenoidOff { get { return _oUpper1SolenoidOff_9; } } //9
        public bool DO_LowerSolenoidOn { get { return _oLowerSolenoidOn_18; } } //18
        public bool DO_LowerSolenoidOff { get { return _oLowerSolenoidOff_19; } }  //19
        public bool DO_XAxisExcitation { get { return _oXAxisExcitation_24; } }   //24
        public bool DO_ZAxisExcitation { get { return _oZAxisExcitation_25; } }   //25
        public bool DO_RotAxisExcitation { get { return _oRotAxisExcitation_26; } } //26
        public bool DO_UpperExcitation { get { return _oUpperExcitation_27; } }  //27
        public bool DO_LowerExcitation { get { return _oLowerExcitation_28; } }   //28
        public bool DO_UpperArmOrigin { get { return _oUpperArmOrigin_30; } }   //30
        public bool DO_LowerArmOrigin { get { return _oLowerArmOrigin_31; } }   //31
    }

    public delegate void SRR757IOChangelHandler(object sender, SRR757IOChengeEventArgs e);
    public class SRR757IOChengeEventArgs : EventArgs
    {
        public SRR757GPIO Frame { get; set; }
        public SRR757IOChengeEventArgs(SRR757GPIO frame)
        {
            Frame = frame;
        }
    }


    //處理帳料
    public class LoadUnldEventArgs : EventArgs
    {
        public enumRobotArms Arm;
        public int StgeIndx;
        public int Slot;
        /// <summary>
        /// Robot load/unload function done
        /// </summary>
        /// <param name="arm"></param>
        /// <param name="stgeIndx">0~399</param>
        /// <param name="slot"></param>
        /// <remarks>stgeIndx:0~399</remarks>
        public LoadUnldEventArgs(enumRobotArms arm, int stgeIndx, int slot)
        {
            Arm = arm;
            StgeIndx = stgeIndx;
            Slot = slot;
        }
    }
}
