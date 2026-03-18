using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Aligner.Event
{
    public delegate void AutoProcessingEventHandler(object sender);

    public class AlignerProtoclEventArgs : EventArgs
    {
        public StatFrame Frame { get; set; }
        public AlignerProtoclEventArgs(string frame)
        {
            Frame = new StatFrame(frame);
        }
    }
    public class StatFrame
    {
        private string _strFrame;   //aTRB1.STAT:00000/0000

        private char _charHeader;   //o,a,n,c,e
        private string _strID;      //TRB1.
        private string _strData;    //STAT:00000/0000

        private int _nBodyNo;       //1
        private string _strCommand; //STAT
        private string _strValue;   //00000/0000

        public StatFrame(string strFrame)
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

    public class AutoProcessingEventArgs : EventArgs
    {
        public I_Aligner Frame { get; set; }
        public AutoProcessingEventArgs(I_Aligner frame)
        {
            Frame = frame;
        }
    }
    public class SRA320GPIO
    {
        private string _strPi;
        private string _strPo;
        private int _nPi;
        private int _nPo;
        //DI
        private bool _iEmergencyStop_0;              //0
        private bool _iTemporarilyStop_1;            //1
        private bool _VacPressureOut_1;              //2
        private bool _VacPressureOut_2;              //2
        private bool _iExhaustFan_4;                 //4
        private bool _iWorkPositionSensor_5;         //5

        private bool _iLowerSpindlePresence_8;       //8
        private bool _iUpperSpindlePresence_9;       //9
        private bool _iLowerTemporarilyPresence_10;  //10
        private bool _iUpperTemporarilyPresence_11;  //11

        //DO
        private bool _oPreparationComplete;           //0
        private bool _oTemporarilyStop_1;             //1
        private bool _oSignificantError_2;            //2
        private bool _oLightError_3;                  //3

        private bool _oAlignerPossibleState_5;        //5
        private bool _oWorkDetection_6;               //6
        private bool _oAlignerComplete_7;             //7
        private bool _oChuckingWorkByTheSpindle_8;    //8
        private bool _oChuckingWorkByTheTemporarily_9;//9

        private bool _oSpindleChuckingOFF_16;         //16
        private bool _oSpindleChuckingON_17;          //17
        private bool _oTemporarilyValveOFF_18;        //18
        private bool _oTemporarilyValveON_19;         //19
        private bool _oUsingASecondForSmallWork_20;   //20

        public SRA320GPIO(string Pi, string Po)
        {
            _strPi = Pi;
            _strPo = Po;
            _nPi = Convert.ToInt32(_strPi, 16);
            _nPo = Convert.ToInt32(_strPo, 16);
            //DI
            _iEmergencyStop_0 = (_nPi & 1) != 0;                     //0
            _iTemporarilyStop_1 = (_nPi & 1 << 1) != 0;              //1
            _VacPressureOut_1 = (_nPi & 1 << 2) != 0;                //2
            _iExhaustFan_4 = (_nPi & 1 << 4) != 0;                   //4
            _iWorkPositionSensor_5 = (_nPi & 1 << 5) != 0;           //5
            _iLowerSpindlePresence_8 = (_nPi & 1 << 8) != 0;         //8 <-
            _iUpperSpindlePresence_9 = (_nPi & 1 << 9) != 0;         //9 <-
            _iLowerTemporarilyPresence_10 = (_nPi & 1 << 10) != 0;   //10
            _iUpperTemporarilyPresence_11 = (_nPi & 1 << 11) != 0;   //11

            //DO
            _oPreparationComplete = (_nPo & 1 << 0) != 0;            //0
            _oTemporarilyStop_1 = (_nPo & 1 << 1) != 0;              //1
            _oSignificantError_2 = (_nPo & 1 << 2) != 0;             //2
            _oLightError_3 = (_nPo & 1 << 3) != 0;                   //3
            _oAlignerPossibleState_5 = (_nPo & 1 << 5) != 0;         //5
            _oWorkDetection_6 = (_nPo & 1 << 6) != 0;                //6 <-
            _oAlignerComplete_7 = (_nPo & 1 << 7) != 0;              //7
            _oChuckingWorkByTheSpindle_8 = (_nPo & 1 << 8) != 0;     //8
            _oChuckingWorkByTheTemporarily_9 = (_nPo & 1 << 9) != 0; //9

            _oSpindleChuckingOFF_16 = (_nPo & 1 << 16) != 0;         //16
            _oSpindleChuckingON_17 = (_nPo & 1 << 17) != 0;          //17
            _oTemporarilyValveOFF_18 = (_nPo & 1 << 18) != 0;        //18
            _oTemporarilyValveON_19 = (_nPo & 1 << 19) != 0;         //19
            _oUsingASecondForSmallWork_20 = (_nPo & 1 << 20) != 0;   //20
        }
        public bool DI_EmergencyStop { get { return _iEmergencyStop_0; } }                                 //0
        public bool DI_TemporarilyStop { get { return _iTemporarilyStop_1; } }                             //1

        public bool DI_VacPressureOut_1 { get { return _VacPressureOut_1; } }                              //2

        public bool DI_VacPressureOut_2 { get { return _VacPressureOut_2; } }                              //2

        public bool DI_ExhaustFan { get { return _iExhaustFan_4; } }                                       //4
        public bool DI_WorkPositionSensor { get { return _iWorkPositionSensor_5; } }                       //5
        public bool DI_LowerSpindlePresence { get { return _iLowerSpindlePresence_8; } }                   //8
        public bool DI_UpperSpindlePresence { get { return _iUpperSpindlePresence_9; } }                   //9
        public bool DI_LowerTemporarilyPresence { get { return _iLowerTemporarilyPresence_10; } }          //10
        public bool DI_UpperTemporarilyPresence { get { return _iUpperTemporarilyPresence_11; } }          //11

        //DO
        public bool DO_PreparationComplete { get { return _oPreparationComplete; } }                        //0
        public bool DO_TemporarilyStop { get { return _oTemporarilyStop_1; } }                            //1
        public bool DO_SignificantError { get { return _oSignificantError_2; } }                          //2
        public bool DO_LightError { get { return _oLightError_3; } }                                      //3
        public bool DO_AlignerPossibleState { get { return _oAlignerPossibleState_5; } }                  //5
        public bool DO_WorkDetection { get { return _oWorkDetection_6; } }                                //6
        public bool DO_AlignerComplete { get { return _oAlignerComplete_7; } }                            //7
        public bool DO_ChuckingWorkByTheSpindle { get { return _oChuckingWorkByTheSpindle_8; } }          //8
        public bool DO_ChuckingWorkByTheTemporarily { get { return _oChuckingWorkByTheTemporarily_9; } }  //9
        public bool DO_SpindleChuckingOFF { get { return _oSpindleChuckingOFF_16; } }                    //16
        public bool DO_SpindleChuckingON { get { return _oSpindleChuckingON_17; } }                      //17
        public bool DO_TemporarilyValveOFF { get { return _oTemporarilyValveOFF_18; } }                  //18
        public bool DO_TemporarilyValveON { get { return _oTemporarilyValveON_19; } }                    //19
        public bool DO_UsingASecondForSmallWork { get { return _oUsingASecondForSmallWork_20; } }        //20
    }

    public delegate void SRA320IOChangelHandler(object sender, SRA32IOChengeEventArgs e);
    public class SRA32IOChengeEventArgs : EventArgs
    {
        public SRA320GPIO Frame { get; set; }
        public SRA32IOChengeEventArgs(SRA320GPIO frame)
        {
            Frame = frame;
        }
    }
}
