using RorzeComm.Threading;
using RorzeUnit.Class;
using RorzeUnit.Class.Robot.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;
using static RorzeUnit.Class.SWafer;

namespace RorzeApi
{
    // connect 介面依單元為主 卡片、讀碼機

    public struct IO_Signal_Information
    {
        public int _DioBodyNo { get; set; }
        public int _Bit { get; set; }
        public bool _NormalOff { get; set; }
    }


    #region ======================= Enum =======================
    public enum enumSystemLanguage { Default, zn_TW, en_US, zh_CN };
    public enum enumSystemType : int { None, ActiveEFEM = 1, PassiveEFEM = 2, Sorter = 3 };
    public enum enumTCPType { None = 0, Client = 1, Server = 2 }
    public enum enumRobotType : int { None = -1, RR75X = 0, RR73X, Other };
    public enum enumAlignerType : int { None = -1, RA320 = 0, RA420, TurnTable, PanelXYR, TAL303 };
    public enum enumLoadportType : int { None = -1, RV201, RB201, Other };
    public enum enumRFID { None = -1, UNISON = 0, HEART, OMRON, BRILLIAN };
    public enum enumE84Type : int { SB058 = 0, FITC, LPBuiltInE84 };
    public enum enumCameraType : int { None = 0, NPD }
    public enum enumBarcodeType : int { None = 0, KeyenceSR2000 = 1, CognexDM370 = 2, KeyenceSR710 = 3 }
    public enum enumOcrType : int { IS1740 = 0, WID120 = 1, TZ0031 };
    public enum enumIOModuleType : int { RC530 = 0, RC550 = 1 }
    public enum enumTblType : int { None = -1, RC560 = 0, RC550 = 1 }
    public enum enumFfuType : int { None = 0, TOPWELL = 1, AirTech = 2, NicotraGebhardt = 3 }//奇立、富泰、CIANYI
    public enum enumTransfeStatus : int { Idle = 0, Transfe, Abort, Stop, Pause };
    public enum enumTpTime { TP1, TP2, TP3, TP4, TP5 };
    public enum enumOCRReadFailProcess : int { Continue = 0, Abort, BackFoup, UserKeyIn }
    public enum enumXYZMode : int { Auto = 0, Manual = 1 }
    public enum enumTransferMode : int
    {
        [Description("Display")]
        Display = 0,
        [Description("Notch")]
        Notch = 1,
        [Description("Random")]
        Random = 2,
        [Description("All")]
        All = 3,
        [Description("Pack")]
        Pack = 4,
    }//Order Notch
    public enum enumTransferModeType
    {
        [Description("FromTop")]
        FromTop,
        [Description("FromBottom")]
        FromBottom,
        [Description("SameSlot")]
        SameSlot,
        [Description("FromTop_S")]
        FromTop_S,
        [Description("FromBottom_S")]
        FromBottom_S,
        [Description("Match")]
        Match,
    }
    public enum enumUIPickWaferStat { None = 0, NoWafer, HasWafer, PutWafer, ExeHasWafer, PutWaferAndGet, ExeHasWaferAndPut };
    //  Robot的地址，先放這裡後續看要不要移動，(0~399 Robot底層會加1)
    public enum enumRbtAddress : int
    {  
        EQM1 = 1, //001   
        EQM2 = 2, //002   
        EQM3 = 3, //003
        EQM4 = 4, //004
        AOI = 5,  //005
        STG1_12 = 10,   //010
        STG1_08 = 26,
        STG2_12 = 30,   //030
        STG2_08 = 46,
        STG3_12 = 50,   //050
        STG3_08 = 66,
        STG4_12 = 70,   //070
        STG4_08 = 86,
        STG5_12 = 90,   //090
        STG5_08 = 106,
        STG6_12 = 110,  //110
        STG6_08 = 126,
        STG7_12 = 130,  //130
        STG7_08 = 146,
        STG8_12 = 150,  //150
        STG8_08 = 166,
        ALEX = 170, //170
        STG1_Flip = 171,
        STG2_Flip = 172,
        STG3_Flip = 173,
        STG4_Flip = 174,
        STG5_Flip = 175,
        STG6_Flip = 176,
        STG7_Flip = 177,
        STG8_Flip = 178,
        BUF1 = 179,
        BUF2 = 180,
        BarCode = 181,
        ALN1 = 182,
        ALN2 = 183,
        BUF1_Flip = 184,
        BUF2_Flip = 185,
        BarCode_Flip = 186,
        ALN1_Flip = 187,
        ALN2_Flip = 188,
        EQM1_Flip = 189,
        EQM2_Flip = 190,
        EQM3_Flip = 191,
        EQM4_Flip = 192,
        STG1_12Back = 210,  //210
        STG1_08Back = 226,
        STG2_12Back = 230,  //230
        STG2_08Back = 246,
        STG3_12Back = 250,  //250
        STG3_08Back = 266,
        STG4_12Back = 270,  //270
        STG4_08Back = 286,
        STG5_12Back = 290,  //290
        STG5_08Back = 306,
        STG6_12Back = 310,  //310
        STG6_08Back = 326,
        STG7_12Back = 330,  //330
        STG7_08Back = 346,
        STG8_12Back = 350,  //350
        STG8_08Back = 366,
    };

    //  目前用在連線與原點視窗
    public enum enumUnit
    {
        TRB1, TRB2,
        TBL1, TBL2, TBL3, TBL4, TBL5, TBL6,
        ALN1, ALN2,
        STG1, STG2, STG3, STG4, STG5, STG6, STG7, STG8,
        DIO0, DIO1, DIO2, DIO3, DIO4, DIO5,
        BUF1, BUF2,
        OCRA1, OCRA2, OCRB1, OCRB2,
        BCR1, BCR2, BCR3, BCR4, BCR5, BCR6, BCR7, BCR8,
        EQM1, EQM2, EQM3, EQM4,
        AOI,
    };
    public enum enumRobot { TRB1 = 0, TRB2 };
    public enum enumAligner { ALN1 = 0, ALN2 };
    public enum enumLoadport { STG1 = 0, STG2, STG3, STG4, STG5, STG6, STG7, STG8 };
    public enum enumIOModule { DIO0, DIO1, DIO2, DIO3, DIO4, DIO5 };
    public enum enumTBLModule { TBL1, TBL2, TBL3, TBL4, TBL5, TBL6 };
    public enum enumBuffer { BUF1, BUF2 };
    public enum enumOCR { OCRA1 = 0, OCRA2, OCRB1, OCRB2 };
    public enum enumFFU { FFU1 = 0, FFU2 = 1 };
    public enum enumCamera { CMP1, CMP2 }
    public enum enumBarcode { BCR1, BCR2, BCR3, BCR4, BCR5, BCR6, BCR7, BCR8 }
    public enum enumEQM { EQM1, EQM2, EQM3, EQM4 }
    public enum enumAdam { Adam1, Adam2 }
    public enum enumNotchAngle
    {
        [Description("AlignerA(RobotA to LoadPortA)")]
        ALN1_RB1_STG1 = 0,
        [Description("AlignerA(RobotA to LoadPortB)")]
        ALN1_RB1_STG2,
        [Description("AlignerA(RobotA to LoadPortC)")]
        ALN1_RB1_STG3,
        [Description("AlignerA(RobotA to LoadPortD)")]
        ALN1_RB1_STG4,
        [Description("AlignerA(RobotA to LoadPortE)")]
        ALN1_RB1_STG5,
        [Description("AlignerA(RobotA to LoadPortF)")]
        ALN1_RB1_STG6,
        [Description("AlignerA(RobotA to LoadPortG)")]
        ALN1_RB1_STG7,
        [Description("AlignerA(RobotA to LoadPortH)")]
        ALN1_RB1_STG8,
        [Description("AlignerA(RobotB to LoadPortA)")]
        ALN1_RB2_STG1,
        [Description("AlignerA(RobotB to LoadPortB)")]
        ALN1_RB2_STG2,
        [Description("AlignerA(RobotB to LoadPortC)")]
        ALN1_RB2_STG3,
        [Description("AlignerA(RobotB to LoadPortD)")]
        ALN1_RB2_STG4,
        [Description("AlignerA(RobotB to LoadPortE)")]
        ALN1_RB2_STG5,
        [Description("AlignerA(RobotB to LoadPortF)")]
        ALN1_RB2_STG6,
        [Description("AlignerA(RobotB to LoadPortG)")]
        ALN1_RB2_STG7,
        [Description("AlignerA(RobotB to LoadPortH)")]
        ALN1_RB2_STG8,

        [Description("RobotA to RobotB")]
        RB1_RB2,

        [Description("AlignerB(RobotA to LoadPortA)")]
        ALN2_RB1_STG1,
        [Description("AlignerB(RobotA to LoadPortB)")]
        ALN2_RB1_STG2,
        [Description("AlignerB(RobotA to LoadPortC)")]
        ALN2_RB1_STG3,
        [Description("AlignerB(RobotA to LoadPortD)")]
        ALN2_RB1_STG4,
        [Description("AlignerB(RobotA to LoadPortE)")]
        ALN2_RB1_STG5,
        [Description("AlignerB(RobotA to LoadPortF)")]
        ALN2_RB1_STG6,
        [Description("AlignerB(RobotA to LoadPortG)")]
        ALN2_RB1_STG7,
        [Description("AlignerB(RobotA to LoadPortH)")]
        ALN2_RB1_STG8,
        [Description("AlignerB(RobotB to LoadPortA)")]
        ALN2_RB2_STG1,
        [Description("AlignerB(RobotB to LoadPortB)")]
        ALN2_RB2_STG2,
        [Description("AlignerB(RobotB to LoadPortC)")]
        ALN2_RB2_STG3,
        [Description("AlignerB(RobotB to LoadPortD)")]
        ALN2_RB2_STG4,
        [Description("AlignerB(RobotB to LoadPortE)")]
        ALN2_RB2_STG5,
        [Description("AlignerB(RobotB to LoadPortF)")]
        ALN2_RB2_STG6,
        [Description("AlignerB(RobotB to LoadPortG)")]
        ALN2_RB2_STG7,
        [Description("AlignerB(RobotB to LoadPortH)")]
        ALN2_RB2_STG8,

        [Description("RobotB to RobotA")]
        RB2_RB1,

        Total
    };
    public enum enumSignalTowerColorSetting
    {
        [Description("Error Occurring")]
        AtErrorOccurring,
        [Description("Maintenance")]
        AtMaintenance,
        [Description("Load UnLoad Request")]
        AtLoadUnLoadRequest,
        [Description("Operator")]
        AtOperator,
        [Description("Idle")]
        AtIdle,
        [Description("Processing")]
        AtProcessing,
        [Description("Online Local")]
        AtOnlineLocal,
        [Description("Online Remote")]
        AtOnlineRemote,
        [Description("At Offline")]
        AtOffline,
        Total
    }
    public enum enumSignalTowerColor { None, Red, RedBlinking, Yellow, YellowBlinking, Green, GreenBlinking, Blue, BlueBlinking }


    public enum enumIO_Signal { IsMaint, LightCurtain1 }

    #endregion

    #region ======================= Clas =======================
    public class RobPos
    {
        private string m_strName;       //對應到enumPosition字串
        private string m_strSECSName;   //SECS使用名稱

        private int m_narm1;
        private int m_narm2;
        private int m_nAOIMovingDist;
        public RobPos(string name, string secsname, int Arm1, int Arm2, int AOIMovingDist)
        {
            m_strName = name;
            m_strSECSName = secsname;

            m_narm1 = Arm1;
            m_narm2 = Arm2;
            m_nAOIMovingDist = AOIMovingDist;
        }
        public string Name { set { m_strName = value; } get { return m_strName; } }
        public string SECSName { set { m_strSECSName = value; } get { return m_strSECSName; } }

        public int Pos_ARM1 { set { m_narm1 = value; } get { return m_narm1; } }
        public int Pos_ARM2 { set { m_narm2 = value; } get { return m_narm2; } }
        public int Pos_AOIMovingDist { set { m_nAOIMovingDist = value; } get { return m_nAOIMovingDist; } }
    }

    public class RorzePosition
    {
        public enumRbtAddress strDefineName;
        public string strDisplayName = "";
        public int Stge0to399 = 0;
    }
    public class OCRecipeData
    {
        public int Number = 0;
        public int Stored = 0;
        public string Name = "M12";
        public double Angle_A = 0;
        public double Angle_B = 0;
        public int WaferIDLength = 12;
        public int LotIDFirstPosition = 1;
        public int LotIDLength = 10;
        public int WaferNoFirstPosition = 11;
        public int WaferSize = 1;
        public int Hyphen = 0;
        public int MaskLength = 0;
    }
    public class clsSelectWaferInfo
    {
        private int m_nSourceLpBodyNo = -1;//Loadport 1,2,3,4
        private int m_nTargetLpBodyNo = -1;//Loadport 1,2,3,4
        private int m_nSourceSlotIdx = -1;
        private int m_nTargetSlotIdx = -1;
        private double m_dNotchAngle = -1;

        public clsSelectWaferInfo(int nStg1to08, int sourceSlotIndx, int targetSlotIndx = -1)
        {
            m_nSourceLpBodyNo = nStg1to08;
            m_nSourceSlotIdx = sourceSlotIndx;
            m_nTargetSlotIdx = targetSlotIndx;
        }
        public void SetTargetSlotIdx(int nIndex) { m_nTargetSlotIdx = nIndex; }
        public void SetTargetLpBodyNo(int nBodyNo)//Loadport 1,2,3,4
        {
            m_nTargetLpBodyNo = nBodyNo;
        }
        public void SetNotchAngle(double dNotchAngle) { m_dNotchAngle = dNotchAngle; }

        public int SourceLpBodyNo { get { return m_nSourceLpBodyNo; } }//Loadport
        public int TargetLpBodyNo { get { return m_nTargetLpBodyNo; } }//Loadport
        public int SourceSlotIdx { get { return m_nSourceSlotIdx; } }
        public int TargetSlotIdx { get { return m_nTargetSlotIdx; } }
        public double NotchAngle { get { return m_dNotchAngle; } }
    }
    #endregion

    public class GParam
    {
        #region     ======================= Singleton =======================
        private static readonly GParam _instancce = new GParam();
        public static GParam theInst { get { return _instancce; } }
        #endregion  =========================================================

        private string m_strPath;
        private string m_strFileIni = "";
        private object m_lockINI = new object();
        private object m_lockAdamINI = new object();

        List<OCRecipeData> m_OCRecipeData_Front = new List<OCRecipeData>();    //  儲存OCR Recipe容器
        List<OCRecipeData> m_OCRecipeData_Back = new List<OCRecipeData>();     //  儲存OCR Recipe容器    

        Dictionary<int, string[]> m_dicPLC0_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicPLC0_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicDIO0_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicDIO0_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicDIO1_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicDIO1_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicDIO2_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicDIO2_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicDIO3_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicDIO3_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicDIO4_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicDIO4_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicDIO5_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicDIO5_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicAdamDIO0_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicAdamDIO0_DOName = new Dictionary<int, string[]>();

        Dictionary<int, string[]> m_dicAdamDIO1_DIName = new Dictionary<int, string[]>();
        Dictionary<int, string[]> m_dicAdamDIO1_DOName = new Dictionary<int, string[]>();

        public int[] GetDIO_HCL(int nBody)
        {
            switch (nBody)
            {
                case 0: { return m_dicDIO0_DIName.Keys.ToArray(); }
                case 1: { return m_dicDIO1_DIName.Keys.ToArray(); }
                case 2: { return m_dicDIO2_DIName.Keys.ToArray(); }
                case 3: { return m_dicDIO3_DIName.Keys.ToArray(); }
                case 4: { return m_dicDIO4_DIName.Keys.ToArray(); }
                case 5: { return m_dicDIO5_DIName.Keys.ToArray(); }
            }
            return null;
        }

        public int[] GetAdamDIO_HCL(int nBody)
        {
            switch (nBody)
            {
                case 0: { return m_dicAdamDIO0_DIName.Keys.ToArray(); }
                case 1: { return m_dicAdamDIO1_DIName.Keys.ToArray(); }
            }
            return null;
        }

        public string[] GetDIO_DIName(int nBody, int nNo)
        {
            switch (nBody)
            {
                case 0:
                    if (m_dicDIO0_DIName.ContainsKey(nNo)) { return m_dicDIO0_DIName[nNo]; }
                    break;
                case 1:
                    if (m_dicDIO1_DIName.ContainsKey(nNo)) { return m_dicDIO1_DIName[nNo]; }
                    break;
                case 2:
                    if (m_dicDIO2_DIName.ContainsKey(nNo)) { return m_dicDIO2_DIName[nNo]; }
                    break;
                case 3:
                    if (m_dicDIO3_DIName.ContainsKey(nNo)) { return m_dicDIO3_DIName[nNo]; }
                    break;
                case 4:
                    if (m_dicDIO4_DIName.ContainsKey(nNo)) { return m_dicDIO4_DIName[nNo]; }
                    break;
                case 5:
                    if (m_dicDIO5_DIName.ContainsKey(nNo)) { return m_dicDIO5_DIName[nNo]; }
                    break;
            }
            return null;
        }
        public string[] GetDIO_DOName(int nBody, int nNo)
        {
            switch (nBody)
            {
                case 0:
                    if (m_dicDIO0_DOName.ContainsKey(nNo)) { return m_dicDIO0_DOName[nNo]; }
                    break;
                case 1:
                    if (m_dicDIO1_DOName.ContainsKey(nNo)) { return m_dicDIO1_DOName[nNo]; }
                    break;
                case 2:
                    if (m_dicDIO2_DOName.ContainsKey(nNo)) { return m_dicDIO2_DOName[nNo]; }
                    break;
                case 3:
                    if (m_dicDIO3_DOName.ContainsKey(nNo)) { return m_dicDIO3_DOName[nNo]; }
                    break;
                case 4:
                    if (m_dicDIO4_DOName.ContainsKey(nNo)) { return m_dicDIO4_DOName[nNo]; }
                    break;
                case 5:
                    if (m_dicDIO5_DOName.ContainsKey(nNo)) { return m_dicDIO5_DOName[nNo]; }
                    break;
            }
            return null;
        }
        public string[] GetPLC_DIName(int nHCL) { return m_dicPLC0_DIName[nHCL]; }

        public string[] GetAdamIO_DIName(int nBody, int nNo)
        {
            switch (nBody)
            {
                case 0:
                    if (m_dicAdamDIO0_DIName.ContainsKey(nNo)) { return m_dicAdamDIO0_DIName[nNo]; }
                    break;
                case 1:
                    if (m_dicAdamDIO1_DIName.ContainsKey(nNo)) { return m_dicAdamDIO1_DIName[nNo]; }
                    break;
            }
            return null;
        }
        public string[] GetAdamIO_DOName(int nBody, int nNo)
        {
            switch (nBody)
            {
                case 0:
                    if (m_dicAdamDIO0_DOName.ContainsKey(nNo)) { return m_dicAdamDIO0_DOName[nNo]; }
                    break;
                case 1:
                    if (m_dicAdamDIO1_DOName.ContainsKey(nNo)) { return m_dicAdamDIO1_DOName[nNo]; }
                    break;
            }
            return null;
        }

        #region ======================= INI File =======================
        public enumSystemLanguage SystemLanguage { get; private set; }
        Dictionary<enumSignalTowerColorSetting, enumSignalTowerColor> m_dicSignalTowerColor = new Dictionary<enumSignalTowerColorSetting, enumSignalTowerColor>();

        private enumLoadportType[] m_eLoadportMode = new enumLoadportType[Enum.GetNames(typeof(enumLoadport)).Count()];//STG
        private enumTCPType[] m_eSTG_TCPType = new enumTCPType[Enum.GetNames(typeof(enumLoadport)).Count()];           //STG
        private string[] m_strloadportWaferType = new string[Enum.GetNames(typeof(enumLoadport)).Count()];             //STG Wafer Type
        private List<bool>[] m_LPInfoPadEnableList = new List<bool>[Enum.GetNames(typeof(enumLoadport)).Count()];      //STG Info-Pad啟用
        private List<string>[] m_LPInfoPadName = new List<string>[Enum.GetNames(typeof(enumLoadport)).Count()];        //STG Info-Pad名稱
        private string[] m_strSimulateGmap = new string[Enum.GetNames(typeof(enumLoadport)).Count()];                  //STG
        private List<bool>[] m_LPForTrbMapInfoEnableList = new List<bool>[Enum.GetNames(typeof(enumLoadport)).Count()];//STG
        private int[] m_strStgBarcodeIndex = new int[Enum.GetNames(typeof(enumLoadport)).Count()];                     //STG

        private enumAlignerType[] m_eAlignerMode = new enumAlignerType[Enum.GetNames(typeof(enumAligner)).Count()];    //ALN
        private bool[] m_bUnClampLiftPinUp = new bool[Enum.GetNames(typeof(enumAligner)).Count()];                     //ALN
        private int[] m_nAngleBetweenNotchAndRbAFinger = new int[Enum.GetNames(typeof(enumAligner)).Count()];          //ALN
        private int[] m_nAngleBetweenNotchAndRbBFinger = new int[Enum.GetNames(typeof(enumAligner)).Count()];          //ALN
        private int[] m_strAlnBarcodeIndex = new int[Enum.GetNames(typeof(enumAligner)).Count()];                      //ALN

        private enumTCPType[] m_eTRB_TCPType = new enumTCPType[Enum.GetNames(typeof(enumRobot)).Count()];              //TRB
        private enumArmFunction[] m_eUpperArmWaferType = new enumArmFunction[Enum.GetNames(typeof(enumRobot)).Count()];//TRB 
        private enumArmFunction[] m_eLowerArmWaferType = new enumArmFunction[Enum.GetNames(typeof(enumRobot)).Count()];//TRB 
        private int[] m_nFrameTwoStepLoadArmBackPulse = new int[Enum.GetNames(typeof(enumRobot)).Count()];             //TRB 
        private bool[] m_bXaxisDisable = new bool[Enum.GetNames(typeof(enumRobot)).Count()];                           //TRB
        private bool[] m_bExtXaxisDisable = new bool[Enum.GetNames(typeof(enumRobot)).Count()];                        //TRB
        private bool[] m_bExtXaxisSimulate = new bool[Enum.GetNames(typeof(enumRobot)).Count()];                       //TRB
        private string[] m_strRobot_AllowPort = new string[Enum.GetNames(typeof(enumRobot)).Count()];                  //TRB 
        private string[] m_strRobot_AllowAligner = new string[Enum.GetNames(typeof(enumRobot)).Count()];               //TRB
        private string[] m_strRobot_AllowEquipment = new string[Enum.GetNames(typeof(enumRobot)).Count()];             //TRB

        private bool[] m_bUseArmSameMovement = new bool[Enum.GetNames(typeof(enumRobot)).Count()];                     //TRB 
        private bool[] m_bAlignerExchange = new bool[Enum.GetNames(typeof(enumRobot)).Count()];                        //TRB 
        private int[] m_nAngleBetweenOrgnAndXaxis = new int[Enum.GetNames(typeof(enumRobot)).Count()];                 //TRB 
        private int[] m_bUnldUseClmpCheckWaferTime = new int[Enum.GetNames(typeof(enumRobot)).Count()];                //TRB 
        private bool[] m_bCheckRobotAir = new bool[Enum.GetNames(typeof(enumRobot)).Count()];                          //TRB 
        private int[] m_nMaintSpeed = new int[Enum.GetNames(typeof(enumRobot)).Count()];                               //TRB 
        private int[] m_nRunSpeed = new int[Enum.GetNames(typeof(enumRobot)).Count()];                                 //TRB 

        Dictionary<enumPosition, RobPos> dicRobPos = new Dictionary<enumPosition, RobPos>();

        private int[] m_nBufferWaferType = new int[Enum.GetNames(typeof(enumBuffer)).Count()];                         //BUF
        private bool[] m_nBufferPosDetect = new bool[Enum.GetNames(typeof(enumBuffer)).Count()];                       //BUF
        private List<int>[] m_nBufferSlotRc530Bit = new List<int>[Enum.GetNames(typeof(enumBuffer)).Count()];          //BUF
        private List<int>[] m_nBufferAroundRc530Bit = new List<int>[Enum.GetNames(typeof(enumBuffer)).Count()];        //BUF
        private enumOcrType[] m_eOcrType = new enumOcrType[Enum.GetNames(typeof(enumOCR)).Count()];                    //OCR
        private enumIOModuleType[] m_eDioType = new enumIOModuleType[Enum.GetNames(typeof(enumIOModule)).Count()];     //DIO 
        private enumTblType[] m_eTblType = new enumTblType[Enum.GetNames(typeof(enumTBLModule)).Count()];

        private enumFfuType[] m_eFfuType = new enumFfuType[Enum.GetNames(typeof(enumFFU)).Count()];                //FFU
        private int[] m_nFfuFanCount = new int[Enum.GetNames(typeof(enumFFU)).Count()];                            //FFU
        private int[] m_nFfuComort = new int[Enum.GetNames(typeof(enumFFU)).Count()];                              //FFU
        private string[] m_strFfuIp = new string[Enum.GetNames(typeof(enumFFU)).Count()];                          //FFU

        private enumBarcodeType[] m_eBarcodeType = new enumBarcodeType[Enum.GetNames(typeof(enumBarcode)).Count()];//BCR
        private int[] m_nBarcodeComport = new int[Enum.GetNames(typeof(enumBarcode)).Count()];                     //BCR
        private string[] m_strBarcodeIP = new string[Enum.GetNames(typeof(enumBarcode)).Count()];                  //BCR

        private enumCameraType[] m_eCameraType = new enumCameraType[Enum.GetNames(typeof(enumCamera)).Count()];  //CMP     
        private string[] m_strCameraIP = new string[Enum.GetNames(typeof(enumCamera)).Count()];                  //CMP


        private bool[] m_bTrbDisable = new bool[Enum.GetNames(typeof(enumRobot)).Count()];       //  Disable    
        private bool[] m_bTblDisable = new bool[Enum.GetNames(typeof(enumTBLModule)).Count()];   //  Disable  
        private bool[] m_bStgDisable = new bool[Enum.GetNames(typeof(enumLoadport)).Count()];    //  Disable    
        private bool[] m_bAlnDisable = new bool[Enum.GetNames(typeof(enumAligner)).Count()];     //  Disable          
        private bool[] m_bOCRDisable = new bool[Enum.GetNames(typeof(enumOCR)).Count()];         //  Disable    
        private bool[] m_bDioDisable = new bool[Enum.GetNames(typeof(enumIOModule)).Count()];    //  Disable    
        private string[] m_strBufEnableSlotNum = new string[Enum.GetNames(typeof(enumBuffer)).Count()];  //  Disable

        private bool[] m_bEqmDisable = new bool[Enum.GetNames(typeof(enumEQM)).Count()];                //equipment
        private bool[] m_bEqmSimulate = new bool[Enum.GetNames(typeof(enumEQM)).Count()];               //equipment
        private string[] m_bEqmName = new string[Enum.GetNames(typeof(enumEQM)).Count()];               //equipment
        private enumTCPType[] m_eEqmTCPType = new enumTCPType[Enum.GetNames(typeof(enumEQM)).Count()];  //equipment
        private string[] m_strEqmIP = new string[Enum.GetNames(typeof(enumEQM)).Count()];               //equipment
        private int[] m_nEqmPort = new int[Enum.GetNames(typeof(enumEQM)).Count()];                     //equipment
        private string[] m_strEqmDefaultRecipe = new string[Enum.GetNames(typeof(enumEQM)).Count()];    //equipment
        private string[] m_bEqmGetRecipeListEnable = new string[Enum.GetNames(typeof(enumEQM)).Count()];//equipment
        private int[] m_nEqmProcessTimeout = new int[Enum.GetNames(typeof(enumEQM)).Count()];           //equipment

        private bool[] m_bAdamDisable = new bool[Enum.GetNames(typeof(enumAdam)).Count()];                //Adam
        private string[] m_strAdamIP = new string[Enum.GetNames(typeof(enumAdam)).Count()];               //Adam
        private int[] m_nAdamPort = new int[Enum.GetNames(typeof(enumAdam)).Count()];                     //Adam

        private int[] m_nComRfid = new int[Enum.GetNames(typeof(enumLoadport)).Count()];            //  RFID Comport

        private int[] m_nComFITC = new int[Enum.GetNames(typeof(enumLoadport)).Count()];            //  琺藍希絲 Comport
        private bool[] m_bE84Disable = new bool[Enum.GetNames(typeof(enumLoadport)).Count()];       //  E84 Disable
        private int[] m_nE84Tp = new int[Enum.GetNames(typeof(enumTpTime)).Count()];                //  E84 TP timeout
        private string[] m_strIPTrb = new string[Enum.GetNames(typeof(enumRobot)).Count()];         //  IP
        private string[] m_strIPTbl = new string[Enum.GetNames(typeof(enumTBLModule)).Count()];     //  IP
        private string[] m_strIPStg = new string[Enum.GetNames(typeof(enumLoadport)).Count()];      //  IP
        private string[] m_strIPAln = new string[Enum.GetNames(typeof(enumAligner)).Count()];       //  IP     
        private string[] m_strIPDio = new string[Enum.GetNames(typeof(enumIOModule)).Count()];      //  IP
        private string[] m_strIPOcr = new string[Enum.GetNames(typeof(enumOCR)).Count()];           //  IP

        private int[] m_nNotchAngle = new int[Enum.GetNames(typeof(enumNotchAngle)).Count()];

        private int m_nComEFEM_FFU = 0;

        private int m_nComKeyence_DL_RS1A = 0;

        // -------------------- Gem300
        private string m_strIP1Gem = "127.0.0.1";
        private string m_strIP2Gem = "127.0.0.1";
        private int m_nGemPort = 6000;
        private int m_nClientPort = 5005;

        // -------------------- MotionEventManager
        private string m_strMotionEventManagerUrl = "http://localhost:61723";
        private bool m_bGRPC_Disable = false;
        private bool m_bRobotAlignment_Enable = false;

        private int[] m_nTurnTable_angle_0 = new int[] { 0, 0 };
        private int[] m_nTurnTable_angle_180 = new int[] { 0, 0 };

        // -------------------- Smart RFID
        private bool m_bSmartRfid_Disable;
        private string m_strSmartRfid_IP;// Server IP
        private int m_nSmartRfid_Port;   // Server Port
        private string m_strnSmartRfid_RfidIP;

        // -------------------- Safety IO Status
        private bool m_SafetyIOStatus_Disable;
        private string m_strSafetyIOStatus_IP;// Server IP
        private int m_nSafetyIOStatus_Port;   // Server Port
        private string m_strSafetyIOStatus_PlcIP;

        // -------------------- Keyence MP
        private bool m_bKeyenceMP_Disable;
        private string m_strKeyenceMP_IP;//  IP
        private int m_nKeyenceMP_Port;   //  Port

        // -------------------- SIMCO
        private bool m_bSimco_Disable;
        private string m_strSimco_IP;
        private int m_nSimco_Port;

        public enumXYZMode XYZMode { get; private set; } // 初始 Manual
        public void setXYZMode( enumXYZMode mode )
        {
            XYZMode = mode;
        }



        Dictionary<enumIO_Signal, IO_Signal_Information> m_dicIO_Signal = new Dictionary<enumIO_Signal, IO_Signal_Information>();






        public enumE84Type E84Type { get; private set; }



        #region public function [System]
        public bool IsSimulate
        { get; private set; }
        public bool IsAutoRemote { get; private set; }
        public bool IsAutoDock { get; private set; }
		public bool E84LightCurtainCheck { get; private set; }
        public bool IsSecsEnable { get; private set; }
        public string GetServerIP { get; private set; }
        public int GetServerPort { get; private set; }
        public string EquipmentShowName { get; set; }
        public void SetEquipmentShowName(string strName)
        {
            EquipmentShowName = strName;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("System", "Equipment Name", EquipmentShowName);
            }
        }


        public enumSystemType GetSystemType { get; private set; }

        public bool GetDBAlarmlistUpdate { get; private set; }//DB Alarm List 更新判斷
        public bool SetDBAlarmlistUpdate//DB Alarm List 更新判斷
        {
            set
            {
                GetDBAlarmlistUpdate = value;
                lock (m_lockINI)
                {
                    CINIFile myIni = new CINIFile(m_strFileIni);
                    myIni.WriteIni("System", "DBAlarmlistUpdate", value);
                }
            }
        }

        public enumSystemLanguage SetSystemLanguage//語言
        {
            set
            {
                SystemLanguage = value;
                lock (m_lockINI)
                {
                    CINIFile myIni = new CINIFile(m_strFileIni);
                    myIni.WriteIni("System", "Language(0:Default, 1:zn_TW, 2:en_US, 3:zh_CN)", (int)value);
                }
            }
        }
        public int GetRFID_Bit { get; private set; }
        public void SetRFID_Bit(int nBit)
        {
            GetRFID_Bit = nBit;

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("System", "RFID_Bit", nBit);
            }
        }

        public string GetBufEnableSlot(int nIdx) { return m_strBufEnableSlotNum[nIdx]; }

        public bool FreeStyle { get; private set; }

        public int GetTurnTable_angle_0(int nAligner)
        {
            return m_nTurnTable_angle_0[nAligner];
        }
        public void SetTurnTable_angle_0(int nAligner, int nPos)
        {
            m_nTurnTable_angle_0[nAligner] = nPos;

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("TurnTable String", nAligner == 0 ? "TurnTableA_angle_0" : "TurnTableB_angle_0", nPos);
            }
        }

        public int GetTurnTable_angle_180(int nAligner)
        {
            return m_nTurnTable_angle_180[nAligner];
        }
        public void SetTurnTable_angle_180(int nAligner, int nPos)
        {
            m_nTurnTable_angle_180[nAligner] = nPos;

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("TurnTable String", nAligner == 0 ? "TurnTableA_angle_180" : "TurnTableB_angle_180", nPos);
            }
        }

        #endregion
        #region public function [Process]
        public int EQIOSwitchToExtend { get; private set; }

        #endregion
        #region public function [TRB]
        public enumTCPType GetTRB_TCPType(int nIdx) { return m_eTRB_TCPType[nIdx]; }
        public enumArmFunction GetRobot_UpperArmWaferType(int nIdx) { return m_eUpperArmWaferType[nIdx]; }//硬體安裝
        public enumArmFunction GetRobot_LowerArmWaferType(int nIdx) { return m_eLowerArmWaferType[nIdx]; }//硬體安裝
        public int GetRobot_FrameTwoStepLoadArmBackPulse(int nIdx) { return m_nFrameTwoStepLoadArmBackPulse[nIdx]; }
        public bool GetRobot_XaxsDisable(int nIdx) { return m_bXaxisDisable[nIdx]; }
        public bool GetRobot_ExtXaxsDisable(int nIdx) { return m_bExtXaxisDisable[nIdx]; }
        public bool GetRobot_ExtXaxsSimulate(int nIdx) { return m_bExtXaxisSimulate[nIdx]; }
        public string GetRobot_AllowPort(int nIdx) { return m_strRobot_AllowPort[nIdx]; }//硬體位置是否能夠抵達
        public string GetRobot_AllowAligner(int nIdx) { return m_strRobot_AllowAligner[nIdx]; }//硬體位置是否能夠抵達
        public string GetRobot_AllowEquipment(int nIdx) { return m_strRobot_AllowEquipment[nIdx]; }//硬體位置是否能夠抵達

        public bool GetRobot_UseArmSameMovement(int nIdx) { return m_bUseArmSameMovement[nIdx]; }//雙取雙放啟用
        public bool GetRobot_AlignerExchange(int nIdx) { return m_bAlignerExchange[nIdx]; }//Exchange
        public int GetRobot_AngleBetweenOrgnAndXaxis(int nIdx) { return m_nAngleBetweenOrgnAndXaxis[nIdx]; }//Notch計算使用
        public int GetRobot_UnldUseClmpCheckWaferTime(int nIdx) { return m_bUnldUseClmpCheckWaferTime[nIdx]; }//unload動作完手臂伸出
        public bool GetRobot_CheckAir(int nIdx) { return m_bCheckRobotAir[nIdx]; }//edge clamp
        public int GetRobot_MaintSpeed(int nIdx) { return m_nMaintSpeed[nIdx]; }
        public void SetRobot_MaintSpeed(int nIdx, int nSpeed)
        {
            m_nMaintSpeed[nIdx] = nSpeed;
            lock (m_lockINI)
            {
                string strSection = "TRB" + (nIdx + 1);
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni(strSection, "MaintSpeed", nSpeed);
            }
        }
        public int GetRobot_RunSpeed(int nIdx) { return m_nRunSpeed[nIdx]; }
        public void SetRobot_RunSpeed(int nIdx, int nSpeed)
        {
            m_nRunSpeed[nIdx] = nSpeed;
            lock (m_lockINI)
            {
                string strSection = "TRB" + (nIdx + 1);
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni(strSection, "RunSpeed", nSpeed);
            }
        }

        public Dictionary<enumPosition, RobPos> DicRobPos { get { return dicRobPos; } }

        public void SetDicRobPos(enumPosition eType, RobPos newData)
        {
            if (dicRobPos.ContainsKey(eType))
            {
                dicRobPos[eType] = newData;
            }
            else
            {
                dicRobPos.Add(eType, newData);
            }
            WriteRobotPos();
        }



        public bool GetRobotAlignment_Enable()
        {
            return m_bRobotAlignment_Enable;
        }

        public void SetRobotAlignment_Enable(bool enable)
        {
            m_bRobotAlignment_Enable = enable;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("Robot", "Alignment_Enable", m_bRobotAlignment_Enable ? "1" : "0");
            }
        }


        #endregion
        #region  public function [STG]
        public enumTCPType GetSTG_TCPType(int nIdx) { return m_eSTG_TCPType[nIdx]; }
        public enumLoadportType GetLoadportMode(int nIdx) { return m_eLoadportMode[nIdx]; }
        public string GetLoadportWaferType(int nIdx) { return m_strloadportWaferType[nIdx]; }
        public bool GetFoupTypeEnableList(int nLpIdx, int nTypeIdx) { return m_LPInfoPadEnableList[nLpIdx][nTypeIdx]; }
        public List<bool> GetFoupTypeEnableList(int nLpIdx) { return m_LPInfoPadEnableList[nLpIdx]; }
        public void SetFoupTypeEnableList(int nLpIdx, int nTypeIdx, bool bEnable)
        {
            m_LPInfoPadEnableList[nLpIdx][nTypeIdx] = bEnable;
            string str = string.Empty;
            foreach (bool b in m_LPInfoPadEnableList[nLpIdx].ToArray())
            { str += b ? '1' : '0'; }
            lock (m_lockINI)
            {
                string strSection = "STG" + (nLpIdx + 1);
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni(strSection, "Info-Pad Enable(0:disable,1:enable)", str);
            }
        }
        public bool GetTrbMapInfoEnableList(int nLpIdx, int nTypeIdx) { return m_LPForTrbMapInfoEnableList[nLpIdx][nTypeIdx]; }
        public List<bool> GetTrbMapInfoEnableList(int nLpIdx) { return m_LPForTrbMapInfoEnableList[nLpIdx]; }
        public void SetTrbMapInfoEnableList(int nLpIdx, int nTypeIdx, bool bEnable)
        {
            m_LPForTrbMapInfoEnableList[nLpIdx][nTypeIdx] = bEnable;
            string str1 = string.Empty;
            string str2 = string.Empty;

            for (int i = 0; i < m_LPForTrbMapInfoEnableList[nLpIdx].Count; i++)
            {
                bool b = m_LPForTrbMapInfoEnableList[nLpIdx][i];
                if (i < 16)
                {
                    str1 += b ? '1' : '0';
                }
                else
                {
                    str2 += b ? '1' : '0';
                }
            }

            lock (m_lockINI)
            {
                string strSection = "STG" + (nLpIdx + 1);
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni(strSection, "RobotMapInfoEnable(0:disable,1:enable)", str1);
                myIni.WriteIni(strSection, "Adapter-RobotMapInfoEnable(0:disable,1:enable)", str2);
            }
        }
        public string GetSimulateGmap(int nIdx) { return m_strSimulateGmap[nIdx]; }
        public void SetSimulateGmap(int nIdx, string strMap)
        {
            if (strMap == string.Empty) return;
            m_strSimulateGmap[nIdx] = strMap;
            string strSection = "STG" + (nIdx + 1);
            CINIFile myIni = new CINIFile(m_strFileIni);
            myIni.WriteIni(strSection, "SimulateGmap", strMap);

        }
        public int GetStgBarcodeIndex(int nIdx) { return m_strStgBarcodeIndex[nIdx]; }
        #endregion
        #region  public function [ALN] 
        public enumAlignerType GetAlignerMode(int nIdx) { return m_eAlignerMode[nIdx]; }
        public bool GetAlignerUnClampLiftPinUp(int nIdx) { return m_bUnClampLiftPinUp[nIdx]; }
        public int GetAngleBetweenNotchAndRbAFinger(int nIdx) { return m_nAngleBetweenNotchAndRbAFinger[nIdx]; }
        public int GetAngleBetweenNotchAndRbBFinger(int nIdx) { return m_nAngleBetweenNotchAndRbBFinger[nIdx]; }
        public int GetAlnBarcodeIndex(int nIdx) { return m_strAlnBarcodeIndex[nIdx]; }
        #endregion        
        #region public function [BUF]   
        public int GetBufferWaferType(int nIdx) { return m_nBufferWaferType[nIdx]; }
        public bool GetBufferPosDetect(int nIdx) { return m_nBufferPosDetect[nIdx]; }//一代機有偵測Buffer有沒有被維修使用翻轉沒到定位
        public List<int> GetBufferSlotRc530Bit(int nIdx) { return m_nBufferSlotRc530Bit[nIdx]; }//v1.006
        public List<int> GetBufferAroundRc530Bit(int nIdx) { return m_nBufferAroundRc530Bit[nIdx]; }//v1.006
        #endregion

        #region  public function [BCR]
        public enumBarcodeType GetBarcodeType(int nIdx) { return m_eBarcodeType[nIdx]; }
        public string GetBarcodeIP(int nIdx) { return m_strBarcodeIP[nIdx]; }
        public int GetBarCodeComport(int nIdx) { return m_nBarcodeComport[nIdx]; }
        #endregion
        #region  public function [CMP]
        public enumCameraType GetCameraType(int nIdx) { return m_eCameraType[nIdx]; }
        public string GetCameraIP(int nIdx) { return m_strCameraIP[nIdx]; }
        #endregion

        #region  public function [RorzeUnit Disable]
        public bool IsUnitDisable(enumUnit eUnit)
        {
            bool bDisable = false;
            try
            {
                switch (eUnit)
                {
                    case enumUnit.TRB1: bDisable = m_bTrbDisable[0]; break;
                    case enumUnit.TRB2: bDisable = m_bTrbDisable[1]; break;
                    case enumUnit.TBL1: bDisable = m_bTblDisable[0]; break;
                    case enumUnit.TBL2: bDisable = m_bTblDisable[1]; break;
                    case enumUnit.TBL3: bDisable = m_bTblDisable[2]; break;
                    case enumUnit.TBL4: bDisable = m_bTblDisable[3]; break;
                    case enumUnit.TBL5: bDisable = m_bTblDisable[4]; break;
                    case enumUnit.TBL6: bDisable = m_bTblDisable[5]; break;
                    case enumUnit.ALN1: bDisable = m_bAlnDisable[0]; break;
                    case enumUnit.ALN2: bDisable = m_bAlnDisable[1]; break;
                    case enumUnit.STG1: bDisable = m_bStgDisable[0]; break;
                    case enumUnit.STG2: bDisable = m_bStgDisable[1]; break;
                    case enumUnit.STG3: bDisable = m_bStgDisable[2]; break;
                    case enumUnit.STG4: bDisable = m_bStgDisable[3]; break;
                    case enumUnit.STG5: bDisable = m_bStgDisable[4]; break;
                    case enumUnit.STG6: bDisable = m_bStgDisable[5]; break;
                    case enumUnit.STG7: bDisable = m_bStgDisable[6]; break;
                    case enumUnit.STG8: bDisable = m_bStgDisable[7]; break;
                    case enumUnit.DIO0: bDisable = m_bDioDisable[0]; break;
                    case enumUnit.DIO1: bDisable = m_bDioDisable[1]; break;
                    case enumUnit.DIO2: bDisable = m_bDioDisable[2]; break;
                    case enumUnit.DIO3: bDisable = m_bDioDisable[3]; break;
                    case enumUnit.DIO4: bDisable = m_bDioDisable[4]; break;
                    case enumUnit.DIO5: bDisable = m_bDioDisable[5]; break;
                    case enumUnit.BUF1: bDisable = m_strBufEnableSlotNum[0].Contains("1") == false; break;
                    case enumUnit.BUF2: bDisable = m_strBufEnableSlotNum[1].Contains("1") == false; break;
                    case enumUnit.OCRA1: bDisable = m_bOCRDisable[0]; break;
                    case enumUnit.OCRA2: bDisable = m_bOCRDisable[1]; break;
                    case enumUnit.OCRB1: bDisable = m_bOCRDisable[2]; break;
                    case enumUnit.OCRB2: bDisable = m_bOCRDisable[3]; break;
                    case enumUnit.BCR1: bDisable = m_eBarcodeType[0] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR2: bDisable = m_eBarcodeType[1] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR3: bDisable = m_eBarcodeType[2] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR4: bDisable = m_eBarcodeType[3] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR5: bDisable = m_eBarcodeType[4] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR6: bDisable = m_eBarcodeType[5] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR7: bDisable = m_eBarcodeType[6] == enumBarcodeType.None; ; break;
                    case enumUnit.BCR8: bDisable = m_eBarcodeType[7] == enumBarcodeType.None; ; break;
                    case enumUnit.EQM1: bDisable = m_bEqmDisable[0]; break;
                    case enumUnit.EQM2: bDisable = m_bEqmDisable[1]; break;
                    case enumUnit.EQM3: bDisable = m_bEqmDisable[2]; break;
                    case enumUnit.EQM4: bDisable = m_bEqmDisable[3]; break;
                }
            }
            catch { }
            return bDisable;
        }
        public bool IsAllOcrDisable()
        {
            bool bDisable = true;
            foreach (bool item in m_bOCRDisable) { bDisable &= item; }
            return bDisable;
        }
        public bool IsAllAlnDisable()
        {
            foreach (bool item in m_bAlnDisable)
            {
                if (item == false) return false;
            }
            return true;
        }

        #endregion

        #region public function [OCR Setting]
        public int GetOCRRecipeMax { get; private set; }
        public int GetOCR_Front_RecipeLast { get; private set; }
        //public void SetOCR_Front_RecipeLast(int nValue)
        //{
        //    GetOCR_Front_RecipeLast = nValue;

        //    lock (m_lockINI)
        //    {
        //        CINIFile myIni = new CINIFile(m_strFileIni);
        //        myIni.WriteIni("OCR Setting", "LastFrontRecipe", nValue);
        //    }
        //}     

        public int GetOCR_Back_RecipeLast { get; private set; }
        //public void SetOCR_Back_RecipeLast(int nValue)
        //{
        //    GetOCR_Back_RecipeLast = nValue;

        //    lock (m_lockINI)
        //    {
        //        CINIFile myIni = new CINIFile(m_strFileIni);
        //        myIni.WriteIni("OCR Setting", "LastBackRecipe", nValue);
        //    }
        //}
        public enumOcrType GetOcrType(int nIndex)
        {
            return m_eOcrType[nIndex];
        }
        public bool GetOCR_ReadSucGetImage { get; private set; }
        public void SetOCR_ReadSucGetImage(bool b)
        {
            GetOCR_ReadSucGetImage = b;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("OCR Setting", "ReadOKGetImage", b);
            }

        }
        #endregion
        #region public function [Comport]

        public enumRFID GetRFIDType { get; private set; }
        public int GetRfidComport(int nIdx) { return m_nComRfid[nIdx]; }

        public int GetFITCComport(int nIdx) { return m_nComFITC[nIdx]; }

        public int GetEFEM_FFUComport() { return m_nComEFEM_FFU; }
        public int GetKeyence_DL_RS1A_Comport() { return m_nComKeyence_DL_RS1A; }

        #endregion
        #region public function [E84 Disable]
        public bool IsE84Disable(int nIdx)
        {
            return m_bE84Disable[nIdx];
        }
        #endregion
        #region public function [E84 TP TimeOut]
        public int[] GetTpTime()
        {
            return m_nE84Tp;
        }
        public int GetTpTime(enumTpTime eTp)
        {
            int nSec = 2;
            switch (eTp)
            {
                case enumTpTime.TP1: nSec = m_nE84Tp[0]; break;
                case enumTpTime.TP2: nSec = m_nE84Tp[1]; break;
                case enumTpTime.TP3: nSec = m_nE84Tp[2]; break;
                case enumTpTime.TP4: nSec = m_nE84Tp[3]; break;
                case enumTpTime.TP5: nSec = m_nE84Tp[4]; break;
            }
            return nSec;
        }
        public void SetTpTime(enumTpTime eTp, int nTime)
        {
            switch (eTp)
            {
                case enumTpTime.TP1:
                    m_nE84Tp[0] = nTime;
                    break;
                case enumTpTime.TP2:
                    m_nE84Tp[1] = nTime;
                    break;
                case enumTpTime.TP3:
                    m_nE84Tp[2] = nTime;
                    break;
                case enumTpTime.TP4:
                    m_nE84Tp[3] = nTime;
                    break;
                case enumTpTime.TP5:
                    m_nE84Tp[4] = nTime;
                    break;
            }
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                string strItem = "TP" + ((int)eTp + 1).ToString() + " TimeOut";
                myIni.WriteIni("E84 TP TimeOut", strItem, nTime);
            }
        }
        #endregion
        #region public function [RorzeUnit IP Address]   
        public string GetTblIP(int nIdx) { return m_strIPTbl[nIdx]; }
        #endregion
        #region public function [RorzeUnit IP Address]
        public string GetTrbIP(int nIdx) { return m_strIPTrb[nIdx]; }
        public string GetAlnIP(int nIdx) { return m_strIPAln[nIdx]; }
        public string GetStgIP(int nIdx) { return m_strIPStg[nIdx]; }
        public string GetDioIP(int nIdx) { return m_strIPDio[nIdx]; }
        public string GetOcrIP(int nIdx) { return m_strIPOcr[nIdx]; }
        public string Getm_blIP(int nIdx) { return m_strIPTbl[nIdx]; }
        #endregion
        #region public function [Gem IP Address]
        public string GetGem1IP()
        {
            return m_strIP1Gem;
        }
        public int GetGemPort()
        {
            return m_nGemPort;
        }
        public string GetGem2IP()
        {
            return m_strIP2Gem;
        }
        public int GetGemClientPort()
        {
            return m_nClientPort;
        }
        #endregion

        #region public function [MotionEventManager]
        public string GetMotionEventManagerUrl()
        {
            return m_strMotionEventManagerUrl;
        }
        public void SetMotionEventManagerUrl(string url)
        {
            m_strMotionEventManagerUrl = url;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("GRPC", "BaseUrl", m_strMotionEventManagerUrl);
            }
        }

        public bool GetGRPC_Disable()
        {
            return m_bGRPC_Disable;
        }

        public void SetGRPC_Disable(bool disable)
        {
            m_bGRPC_Disable = disable;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("GRPC", "GRPC_Disable", m_bGRPC_Disable ? "1" : "0");
            }
        }
        #endregion

        #region public function [RC550_0]
        public bool RC550ctrlFFU { get; private set; }
        public bool RC550Pressure_Enable { get; private set; }
        public void SetPressure_Enable(bool b)
        {
            RC550Pressure_Enable = b;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("RC550_0", "HCL0_SB068A_Pressure_Enable", b);
            }
        }
        public int RC550Pressure_Threshold { get; private set; }
        public void SetPressure_Threshold(int n)
        {
            RC550Pressure_Threshold = n;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("RC550_0", "HCL0_SB068A_Pressure_Threshold(1pa~3pa)", n);
            }
        }
        public int GetFanDefaultSpeed { get; private set; }
        public void SetFanDefaultSpeed(int nSpeed)
        {
            GetFanDefaultSpeed = nSpeed;

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("RC550_0", "FanSpeed", nSpeed);
            }
        }
        public bool DVRAlarmn_Disable { get; private set; }
        public void SetDVRAlarmn_Disable(bool b)
        {
            DVRAlarmn_Disable = b;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("RC550_0", "DVRAlarmn_Disable", b);
            }
        }
        public bool PowerFan1Alarmn_Disable { get; private set; }
        public bool PowerFan2Alarmn_Disable { get; private set; }
        #endregion
        #region public function [RC530]       
        public string DIO1_CheckIO_Enable { get; private set; }
        public string DIO1_CheckIO_AorB { get; private set; }

        public string DIO2_CheckIO_Enable { get; private set; }
        public string DIO2_CheckIO_AorB { get; private set; }

        public string DIO3_CheckIO_Enable { get; private set; }
        public string DIO3_CheckIO_AorB { get; private set; }

        public string DIO4_CheckIO_Enable { get; private set; }
        public string DIO4_CheckIO_AorB { get; private set; }

        public bool Check_DIO_IO_Abnormal(int nBodyNo, int nBit, bool bValue)
        {
            switch (nBodyNo)
            {
                case 1:
                    if (DIO1_CheckIO_Enable[nBit] == '0') return false;
                    if (DIO1_CheckIO_AorB[nBit] == 'A') return bValue;
                    if (DIO1_CheckIO_AorB[nBit] == 'B') return !bValue;
                    break;
                case 2:
                    if (DIO2_CheckIO_Enable[nBit] == '0') return false;
                    if (DIO2_CheckIO_AorB[nBit] == 'A') return bValue;
                    if (DIO2_CheckIO_AorB[nBit] == 'B') return !bValue;
                    break;
                case 3:
                    if (DIO3_CheckIO_Enable[nBit] == '0') return false;
                    if (DIO3_CheckIO_AorB[nBit] == 'A') return bValue;
                    if (DIO3_CheckIO_AorB[nBit] == 'B') return !bValue;
                    break;
                case 4:
                    if (DIO4_CheckIO_Enable[nBit] == '0') return false;
                    if (DIO4_CheckIO_AorB[nBit] == 'A') return bValue;
                    if (DIO4_CheckIO_AorB[nBit] == 'B') return !bValue;
                    break;
            }
            return false;
        }



        #endregion

        #region public function [CustomFunction]
        public bool EnableRandomSelectWafer { get; private set; }
        public bool EnableAutoZoom { get; private set; }
        public int IdleLogOutTime { get; private set; }
        public void SetIdleLogOutTime(int nTime)
        {
            IdleLogOutTime = nTime;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("CustomFunction", "IdleLogOutTime(ms)", nTime);
            }
        }
        public int FoupArrivalIdleTimeout { get; private set; }
        public void SetFoupArrivalIdleTimeout(int n)
        {
            FoupArrivalIdleTimeout = n;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("CustomFunction", "FoupArrivalIdleTimeout(s)", n);
            }
        }
        public int FoupWaitTransferTimeout { get; private set; }
        public void SetFoupWaitTransferTimeout(int n)
        {
            FoupWaitTransferTimeout = n;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("CustomFunction", "FoupWaitTransferTimeout(s)", n);
            }
        }
        public enumOCRReadFailProcess GetOCRReadFailProcess { get; private set; }
        public void SetOCRReadFailProcess(enumOCRReadFailProcess eOCRReadFailProcess)
        {
            GetOCRReadFailProcess = eOCRReadFailProcess;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("CustomFunction", "OCRReadFailProcess(0:Continue,1:Abort,2:BackFoup,3:UserKeyIn)", (int)eOCRReadFailProcess);
            }
        }
        public int WaferIDFilterBit { get; private set; }
        public void SetWaferIDFilterBit(int n)
        {
            WaferIDFilterBit = n;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("CustomFunction", "WaferIDFilterBit", n);
            }
        }
        public int OCRWarningsAutoRestTime { get; private set; }
        public void SetOCRWarningsAutoRestTime(int n)
        {
            OCRWarningsAutoRestTime = n;
            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                myIni.WriteIni("CustomFunction", "OCRWarningsAutoRestTime(s)", n);
            }
        }

        #endregion

        #region public function [Notch Angle]
        public int GetNotchData(enumNotchAngle eNotchAngle)
        {
            return m_nNotchAngle[(int)eNotchAngle];
        }
        public int GetNotchData(int nTrbBody, int nAlnBody, int nStgBody)
        {
            int nAngle = 0;
            if (nTrbBody == 1 && nAlnBody == 1)
                switch (nStgBody)
                {
                    case 1: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG1); break;
                    case 2: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG2); break;
                    case 3: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG3); break;
                    case 4: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG4); break;
                    case 5: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG5); break;
                    case 6: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG6); break;
                    case 7: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB1_STG7); break;
                }
            else if (nTrbBody == 1 && nAlnBody == 2)
                switch (nStgBody)
                {
                    case 1: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG1); break;
                    case 2: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG2); break;
                    case 3: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG3); break;
                    case 4: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG4); break;
                    case 5: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG5); break;
                    case 6: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG6); break;
                    case 7: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB1_STG7); break;
                }
            else if (nTrbBody == 2 && nAlnBody == 1)
                switch (nStgBody)
                {
                    case 1: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG1); break;
                    case 2: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG2); break;
                    case 3: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG3); break;
                    case 4: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG4); break;
                    case 5: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG5); break;
                    case 6: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG6); break;
                    case 7: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN1_RB2_STG7); break;
                }
            else if (nTrbBody == 2 && nAlnBody == 2)
                switch (nStgBody)
                {
                    case 1: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG1); break;
                    case 2: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG2); break;
                    case 3: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG3); break;
                    case 4: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG4); break;
                    case 5: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG5); break;
                    case 6: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG6); break;
                    case 7: nAngle += GParam.theInst.GetNotchData(enumNotchAngle.ALN2_RB2_STG7); break;
                }
            return nAngle;
        }
        public void SetNotchData(enumNotchAngle eNotchAngle, int nValue)
        {
            m_nNotchAngle[(int)eNotchAngle] = nValue;

            CINIFile myIni = new CINIFile(m_strFileIni);
            myIni.WriteIni("Notch Angle Adjustment Data", GetEnumDescription(eNotchAngle), nValue);
        }
        #endregion

        #region public function [Signal Tower Color Setting]
        public enumSignalTowerColor GetSignalTowerColor(enumSignalTowerColorSetting eSignalTowerColorSetting)
        {
            return m_dicSignalTowerColor[eSignalTowerColorSetting];
        }
        public void SetSignalTowerColor(enumSignalTowerColorSetting eSignalTowerColorSetting, enumSignalTowerColor eSignalTowerColor)
        {
            m_dicSignalTowerColor[eSignalTowerColorSetting] = eSignalTowerColor;

            CINIFile myIni = new CINIFile(m_strFileIni);
            myIni.WriteIni("Signal Tower Color Setting(None, Red, RedBlinking, Yellow, YellowBlinking, Green, GreenBlinking, Blue, BlueBlinking)",
                GetEnumDescription(eSignalTowerColorSetting), (int)eSignalTowerColor);

        }
        #endregion

        #region public function [DataBase]
        public string DBSever { get; private set; }
        public string DBUser { get; private set; }
        public string DBPassWord { get; private set; }
        public string DBName { get; private set; }
        #endregion

        #region public function [DIO]
        public enumIOModuleType GetDioType(int nIndex) { return m_eDioType[nIndex]; }
        #endregion

        #region public function [Equipment]

        public bool EqmDisable(int nIdx) { return m_bEqmDisable[nIdx]; }
        public bool[] EqmDisableArray { get { return m_bEqmDisable; } }
        public bool EqmSimulate(int nIdx) { return m_bEqmSimulate[nIdx]; }

        public string EqmName(int nIdx) { return m_bEqmName[nIdx]; }

        public enumTCPType EqmTCPType(int nIdx) { return m_eEqmTCPType[nIdx]; }

        public string EqmIP(int nIdx) { return m_strEqmIP[nIdx]; }

        public int EqmPort(int nIdx) { return m_nEqmPort[nIdx]; }

        public int GetEQAckTimeout { get; private set; } = 3000000;
        #endregion
        #region public function [Adam]

        public bool AdamDisable(int nIdx) { return m_bAdamDisable[nIdx]; }
        public bool[] AdamDisableArray { get { return m_bAdamDisable; } }
        public string AdamIP(int nIdx) { return m_strAdamIP[nIdx]; }
        public int AdamPort(int nIdx) { return m_nAdamPort[nIdx]; }
        #endregion

        #region public function [FFU]
        public enumFfuType GetFfuType(int nIndex) { return m_eFfuType[nIndex]; }
        public int GetFfuFanCount(int nIndex) { return m_nFfuFanCount[nIndex]; }
        public int GetFfuComport(int nIndex) { return m_nFfuComort[nIndex]; }
        public string GetFfuIP(int nIndex) { return m_strFfuIp[nIndex]; }
        #endregion

        #region public function [Smart RFID]
        public bool GetSmartRFID_Disable() { return m_bSmartRfid_Disable; }
        public string GetSmartRFID_IP() { return m_strSmartRfid_IP; }
        public int GetSmartRFID_Port() { return m_nSmartRfid_Port; }
        public string GetSmartRFID_RfidIP() { return m_strnSmartRfid_RfidIP; }
        #endregion

        #region public function [Safety IOStatus]

        public bool GetSafetyIOStatus_Disable() { return m_SafetyIOStatus_Disable; }
        public string GetSafetyIOStatus_IP() { return m_strSafetyIOStatus_IP; }
        public int GetSafetyIOStatus_Port() { return m_nSafetyIOStatus_Port; }
        public string GetSafetyIOStatus_PlcIP() { return m_strSafetyIOStatus_PlcIP; }
        #endregion

        #region public function [Keyence MP]
        public bool GetKeyenceMP_Disable() { return m_bKeyenceMP_Disable; }
        public string GetKeyenceMP_IP() { return m_strKeyenceMP_IP; }
        public int GetKeyenceMP_Port() { return m_nKeyenceMP_Port; }
        #endregion

        #region public function [SIMCO]
        public bool GetSimco_Disable() { return m_bSimco_Disable; }
        public string GetSimco_IP() { return m_strSimco_IP; }
        public int GetSimco_Port() { return m_nSimco_Port; }
        #endregion


        public enumTblType GetTblType(int nIndex) { return m_eTblType[nIndex]; }

        public IO_Signal_Information GetIO_Information(enumIO_Signal eIO_Signal)
        {
            if (m_dicIO_Signal.ContainsKey(eIO_Signal) == false) return new IO_Signal_Information();

            return m_dicIO_Signal[eIO_Signal];
        }



        #endregion

        //===========================================================================
        private GParam()
        {
            m_strPath = Directory.GetCurrentDirectory() + "\\SettingFile";
            m_strFileIni = m_strPath + "\\Setting.ini";

            #region 英文轉簡中
            m_DicAllLanguageTranfer.Clear();
            m_DicAllLanguageTranfer.Add("Info", "信息");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Information", "信息");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Warning", "警告");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Error", "错误");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Confirm", "确认");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Abort", "中止");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Retry", "重试");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Ignore", "忽略");//frmMessageBox
            m_DicAllLanguageTranfer.Add("OK", "确认");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Cancel", "取消");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Yes", "是");//frmMessageBox
            m_DicAllLanguageTranfer.Add("No", "否");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Please login first.", "请先登录");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Please reset eror. ", "请重置错误。");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Now control status is Online Remote. ", "现在控制状态为 在线远程。");//frmMessageBox
            m_DicAllLanguageTranfer.Add("_loader StatusMachine is PS_Process. ", "机器状态为传输过程。");//frmMessageBox
            m_DicAllLanguageTranfer.Add("The system will shut down.", "系统将关闭。");//frmMessageBox
            m_DicAllLanguageTranfer.Add("EMO is turned on and the system will shut down!", "EMO 已开启，系统将关闭！");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Is T-key ON!", "储存柜机器手臂是操作示教器!");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Oxygen concentration can't be unlocked.", "氧气浓度无法解锁");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Please run the initialization first.", "请先运行设备初始化");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Upper Buffer Is Not Safety,Place Check Position", "上晶片缓冲区不安全，放置检查翻转位置");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Lower Buffer Is Not Safety,Place Check Position", "下晶片缓冲区不安全，放置检查翻转位置");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Please select Grade.", "请选择等级");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Robot arm isn't back", "机械臂没有回来");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Cannot delete wafer data, Please run the mapping again", "无法直接删除晶圆数据,请再次运行扫描");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Do not perform a Stocker mapping to use the database records.", "不要执行储存柜扫描，使用数据库记录");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Please check pin safety.", "请检查插销的安全性。");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Insufficient authority.", "权限不足");//frmMessageBox
            m_DicAllLanguageTranfer.Add("OCR reading failure whether to terminate the process or input manually..", "OCR 读取失败，是否终止流程或手动输入。");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Stocker has wafer!", "储存柜有晶片！");//frmMessageBox
            m_DicAllLanguageTranfer.Add("Are you sure you want to run the initialization?", "您确定要运行初始化？");
            m_DicAllLanguageTranfer.Add("This User can't Delete", "该默认用户不能删除");

            m_DicAllLanguageTranfer.Add("wafer", "晶圆");

            m_DicAllLanguageTranfer.Add("Main", "主要");//frmMDI
            m_DicAllLanguageTranfer.Add("Teaching", "教导");//frmMDI
            m_DicAllLanguageTranfer.Add("Robot", "机器手臂");//frmMDI
            m_DicAllLanguageTranfer.Add("Robot Copy Data", "机器人复制数据");//frmMDI
            m_DicAllLanguageTranfer.Add("Robot Mapping", "机器手扫描");//frmMDI
            m_DicAllLanguageTranfer.Add("Loadport", "晶圆装载口");//frmMDI
            m_DicAllLanguageTranfer.Add("ALN OCR", "校准器 OCR");//frmMDI
            m_DicAllLanguageTranfer.Add("Notch", "晶圆缺口");//frmMDI
            m_DicAllLanguageTranfer.Add("Stocker", "储存柜");//frmMDI
            m_DicAllLanguageTranfer.Add("Origin", "原点复归");//frmMDI
            m_DicAllLanguageTranfer.Add("IO", "数字信号");//frmMDI
            m_DicAllLanguageTranfer.Add("Maintain", "手动");//frmMDI
            m_DicAllLanguageTranfer.Add("Parameter", "机台");//frmMDI
            m_DicAllLanguageTranfer.Add("SECSSetting", "自动化");//frmMDI
            m_DicAllLanguageTranfer.Add("Permission", "权限许可");//frmMDI
            m_DicAllLanguageTranfer.Add("GroupRecipe", "群组配方");//frmMDI
            m_DicAllLanguageTranfer.Add("Signal Tower", "信号塔");//frmMDI
            m_DicAllLanguageTranfer.Add("DataBase", "数据库");//frmMDI
            m_DicAllLanguageTranfer.Add("Alarm", "报警");//frmMDI
            m_DicAllLanguageTranfer.Add("Event", "事件");//frmMDI
            m_DicAllLanguageTranfer.Add("Process", "过程");//frmMDI
            m_DicAllLanguageTranfer.Add("SECSControl", "控制");//frmMDI
            m_DicAllLanguageTranfer.Add("Unit connection failed", "元件连线失败");//frmMDI
            m_DicAllLanguageTranfer.Add("Unit Home Return failed", "设备初始化失败");//frmMDI
            m_DicAllLanguageTranfer.Add("Compare Stock MapData failed", "资料库帐料不匹配");//frmMDI
            m_DicAllLanguageTranfer.Add("Wafer recover failed", "回收晶圆失败");//frmMDI
            m_DicAllLanguageTranfer.Add("Undo failed", "晶圆撤回失败");//frmMDI

            m_DicAllLanguageTranfer.Add("Undo failed Clear wafer transfer record?", "撤消失败 清除晶圆传输记录？");//frmMDI
            m_DicAllLanguageTranfer.Add("Loader StatusMachine is PS_Process.Do you want to Exit?", "传送中你想退出吗？");//frmMDI

            m_DicAllLanguageTranfer.Add("Initializing...", "初始化...");//frmOrgn
            m_DicAllLanguageTranfer.Add("Done.", "完成.");//frmOrgn
            m_DicAllLanguageTranfer.Add("Fail.", "失败.");//frmOrgn
            m_DicAllLanguageTranfer.Add("Waitting...", "等待中...");//frmOrgn
            m_DicAllLanguageTranfer.Add("Ready to initial.", "准备初始.");//frmOrgn
            m_DicAllLanguageTranfer.Add("Wait Robot.", "等待机器手.");//frmOrg
            m_DicAllLanguageTranfer.Add("Check Connection!!", "检查连接!!");
            m_DicAllLanguageTranfer.Add("Connected!!", "连接!!");
            m_DicAllLanguageTranfer.Add("Disable!!", "禁用!!");
            m_DicAllLanguageTranfer.Add("RobotA", "机器手A");
            m_DicAllLanguageTranfer.Add("RobotB", "机器手B");
            m_DicAllLanguageTranfer.Add("LoadportA", "晶圆装载口A");
            m_DicAllLanguageTranfer.Add("LoadportB", "晶圆装载口B");
            m_DicAllLanguageTranfer.Add("LoadportC", "晶圆装载口C");
            m_DicAllLanguageTranfer.Add("LoadportD", "晶圆装载口D");
            m_DicAllLanguageTranfer.Add("LoadportE", "晶圆装载口E");
            m_DicAllLanguageTranfer.Add("LoadportF", "晶圆装载口F");
            m_DicAllLanguageTranfer.Add("LoadportG", "晶圆装载口G");
            m_DicAllLanguageTranfer.Add("LoadportH", "晶圆装载口H");
            m_DicAllLanguageTranfer.Add("AlignerA", "晶圆校准机A");
            m_DicAllLanguageTranfer.Add("AlignerB", "晶圆校准机B");
            m_DicAllLanguageTranfer.Add("StockerA", "储存柜A");
            m_DicAllLanguageTranfer.Add("StockerB", "储存柜B");
            m_DicAllLanguageTranfer.Add("StockerC", "储存柜C");
            m_DicAllLanguageTranfer.Add("StockerD", "储存柜D");
            m_DicAllLanguageTranfer.Add("IO card0", "数字信号卡0");
            m_DicAllLanguageTranfer.Add("IO card1", "数字信号卡1");
            m_DicAllLanguageTranfer.Add("IO card2", "数字信号卡2");
            m_DicAllLanguageTranfer.Add("IO card3", "数字信号卡3");
            m_DicAllLanguageTranfer.Add("IO card4", "数字信号卡4");
            m_DicAllLanguageTranfer.Add("IO card5", "数字信号卡5");
            m_DicAllLanguageTranfer.Add("waiting AlignerA", "等待 晶圆校准机A");

            m_DicAllLanguageTranfer.Add("Stop", "停止");//frmMain
            m_DicAllLanguageTranfer.Add("Differential pressure(Pa):", "压差计(帕):");//frmMain
            m_DicAllLanguageTranfer.Add("IDLE", "准备");//frmMain
            m_DicAllLanguageTranfer.Add("CYCLE", "循环");//frmMain
            m_DicAllLanguageTranfer.Add("TRANSFER", "传送");//frmMain
            m_DicAllLanguageTranfer.Add("ABORT", "中止");//frmMain
            m_DicAllLanguageTranfer.Add("STOP", "停止");//frmMain
            m_DicAllLanguageTranfer.Add("PAUSE", "暂停");//frmMain
            m_DicAllLanguageTranfer.Add("Display", "显示刻号");//frmMain  
            m_DicAllLanguageTranfer.Add("Random", "任意传送");//frmMain
            m_DicAllLanguageTranfer.Add("All", "全部传送");//frmMain
            m_DicAllLanguageTranfer.Add("Pack", "整批堆放");//frmMain
            m_DicAllLanguageTranfer.Add("Stock", "储存柜");//frmMain
            m_DicAllLanguageTranfer.Add("FunctionType", "类型");//frmMain
            m_DicAllLanguageTranfer.Add("FromTop", "顶部堆放");//frmMain
            m_DicAllLanguageTranfer.Add("FromBottom", "底部堆放");//frmMain
            m_DicAllLanguageTranfer.Add("SameSlot", "相同槽位");//frmMain
            m_DicAllLanguageTranfer.Add("FromTop_S", "顶部堆放同槽位");//frmMain
            m_DicAllLanguageTranfer.Add("FromBottom_S", "底部堆放同槽位");//frmMain
            m_DicAllLanguageTranfer.Add("Match", "Match");//frmMain
            m_DicAllLanguageTranfer.Add("WaferInOut", "进出晶圆");//frmMain
            m_DicAllLanguageTranfer.Add("ReadID", "读取条码");//frmMain
            m_DicAllLanguageTranfer.Add("No Aligner", "不校准");//frmMain
            m_DicAllLanguageTranfer.Add("Alignment", "校准晶圆");//frmMain
            m_DicAllLanguageTranfer.Add("Please select wafer.", "请选择晶圆");//frmMain
            m_DicAllLanguageTranfer.Add("WaferIn need alignment.", "请选择校准");//frmMain
            m_DicAllLanguageTranfer.Add("Display need alignment.", "请选择校准");//frmMain
            m_DicAllLanguageTranfer.Add("Are you want to Process start?", "您是否希望流程开始?");//frmMain
            m_DicAllLanguageTranfer.Add("Loadport satus {0} can't dock", "晶圆装载口{0}状态不许执行开");//frmMain
            m_DicAllLanguageTranfer.Add("Loadport satus {0} can't undock", "晶圆装载口{0}状态不许执行");//frmMain
            m_DicAllLanguageTranfer.Add("↓Tool", "↓工具");//frmMain
            m_DicAllLanguageTranfer.Add("↑Tool", "↑工具");//frmMain

            m_DicAllLanguageTranfer.Add("Search Stock Wafers...", "搜寻晶圆");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("Wafer Search Completed.", "搜寻完成");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("Wafer Search Error.", "搜尋失敗");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("Tower", "晶圆储存塔");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("DataBase Data", "数据资料");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("Mapping Data", "扫描资料");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("Slot", "槽位");//frmCompareStockMapData
            m_DicAllLanguageTranfer.Add("Comparison", "比对结果");//frmCompareStockMapData

            m_DicAllLanguageTranfer.Add("Please place FOUP and Docking first.", "请放置晶圆盒并执行开口.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Manually select delivery.", "手动选择恢复位置");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Wafer recovery start, please wait...", "恢复流程启动请稍等...");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("wafer target:", "晶圆目标:");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Robot moving to standby potion.", "机器人移动至待命位置.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Robot upper arm place wafer.", "机械上臂放置晶圆.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Robot lower arm place wafer.", "机械下臂放置晶圆.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Aligner unclamp!", "校准元件解除真空!");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Robot upper arm take wafer from aligner.", "机器上手臂取走校准器晶圆.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Robot upper arm take wafer from buffer.", "机器上手臂取走缓冲区晶圆");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("The robot complete and loadport undocking.", "机器人完成卸载.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("RobotB recover complete and watting for Robot_A", "机器人B 恢复完毕，等待机器人手臂 A");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("RobotA recover complete and watting for Robot_B", "机器人A 恢复完毕，等待机器人手臂 B");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Recover fail and click [Exit] the system will shut down!", "恢复失败点击 [取消]，系统将关闭！");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("RobotA recover fail and click [Exit] the system will shut down!", "机器人A 恢复失败点击 [取消]，系统将关闭！");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("RobotB recover fail and click [Exit] the system will shut down!", "机器人B 恢复失败点击 [取消]，系统将关闭！");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Recover completes the end of process!", "恢复完成进程结束！");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Please check loadport status.", "请检查晶圆装载口状态.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("The loaport is in moving.", "晶圆装载口在移动.");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("Robot Arm is not in a safe position and the system will shut down?", "机械臂不在安全位置，系统将关闭");//frmWaferRecover
            m_DicAllLanguageTranfer.Add("This location is not supported.", "不支持此位置.");//frmWaferRecover

            m_DicAllLanguageTranfer.Add("Open fail, Please start manually", "打开失败，请手动启动");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Pls select wafer", "请选择晶圆");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Loadport disable", "晶圆装载口禁用");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Loadport isn't docked", "晶圆盒未开门");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Loadport is frame type", "铁框无法执行");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Please select recipe number", "请选择配方");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("The Aligner isn't find.", "请选择校准元件");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("The Aligner is disable.", "校准元件禁用");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("The OCR isn't find.", "请选择光学字元辨识器");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("The OCR is disable", "光学字元辨识器禁用");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("No Robot available.", "没有可用的机器人。");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Aligner cannot be used on robots.", "机器人不可使用对准器。");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Button [Next] to start the process.\r", "按钮[下一步]开始该过程。\r");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Name not found in OCR", "OCR 中未找到名称");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("OCR Read failure!!", "读取失败!!");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("WaferID : ", "晶圆ID : ");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Executing Robot move wafer to Alinger,please Wait.\r", "执行机器人正在将晶圆移至校准机，请稍候。\r");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Teaching OCR is over, please click [Next]\r", "OCR教学结束，请点击[下一步]\r");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Abort teaching ? Recipe:", "中止教导？名称:");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Are you sure complete teaching ?", "你确定教导完成吗？");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Start Teaching OCR, click[Next] when finished\r", "开始OCR教学，完成后点击[下一步]\r");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Executing Robot move wafer to Alinger fail,click[Next] when retry\r", "机器人执行移动晶圆到校准机失败，重试时点击[下一步]\r");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Please wait for wafer recover\r", "晶圆回收请等待\r");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Parameter will not be recorded. Are you sure you want to give up the teaching?", "参数更改不会被记录，您确定要放弃教学动作吗？");//frmTeachOCR
            m_DicAllLanguageTranfer.Add("Please select Aligner.", "请选择校准元件.");//frmTeachOCR

            m_DicAllLanguageTranfer.Add("Please select robot!", "请选择机器人!");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Robot isn't find!", "找不到机器人!");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Is RunMode!!!Can't teaching.", "是运行模式!!!无法教学。");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Is RunMode!!!Can't save data.", "是运行模式!!!无法保存数据。");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Please select stage", "请选择位置");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Please select Foup type or Jig type", "请选择类型");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Mode speed is too large,sure to continue?", "速度太大，确定继续吗？");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Excuting Origin,Please wait.\r", "正在执行原点，请稍候.\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Step value is too large,sure to continue?", "距离太大，确定继续吗？");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Excuting Stage Clamp,Please Wait.\r", "正在执行位置到位，请稍候.\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Abort teaching ? pos:", "中止教导 ? 位置:");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Are you sure moving robot to standby position of ", "您确定将机器人移至位置 ");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Are you sure moving robot to standby top position of ", "您确定将机器人移至上次高位置 ");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Are you sure moving robot to standby bottom position of ", "您确定将机器人移至上次低位置 ");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("name:", "名称:");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("stage:", "编号:");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Move to previous teaching position.Please Wait.\r", "移动到之前的教学位置。 请稍等。\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Top Teach\r", "高位置教学\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Bottom Teach\r", "低位置教学\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Use below button to excuting teaching\r", "使用下面的按钮来执行教学\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("If complete teaching , press 'Next' button\r", "如果完成教学，请按[下一步]按钮\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("If cancel teaching , press 'Cancel' button\r", "如果取消示教，请按[取消]按钮\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Please wait a moment with arm extended.\r", "手臂伸出请稍等\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Are you sure robot to stage ", "你确定机器手臂移动至 ");//frmTeachRobot
            m_DicAllLanguageTranfer.Add(" and Extd ", " 並且伸出 ");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("That ends the teaching process.\r", "至此教学过程结束.\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("The teaching process to begin, please place the jig\r", "即將進行教導流程，請放置置具\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Please put jig complete,press [Next] Button\r", "请放置夹具，按[下一步]按钮.\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("If you need cancel teaching mode,press [Cancel] Button.\r", "如果需要取消示教模式，请按[取消]按钮\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Move to previous teaching position？\r", "移动到以前的教学位置？\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("If need move,press [Next] Button\r", "如果需要移动，请按[下一步]按钮\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("If need skip this step,press [Cancel] Button\r", "如果需要跳过此步骤，请按[取消]按钮\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Does the arm extend directly?\r", "手臂直接伸出吗？\r");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Clamp fail!", "夹钳失败！");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Unclamp fail!", "松开失败！");//frmTeachRobot
            m_DicAllLanguageTranfer.Add("Robot isn't find.", "机器人没找到！");//frmTeachRobot

            m_DicAllLanguageTranfer.Add("Foup Type is abnormal.", "晶圆盒类型异常。");//frmTeachLoadport
            m_DicAllLanguageTranfer.Add("Saving data completed.", "保存数据完成。");//frmTeachLoadport
            m_DicAllLanguageTranfer.Add("Please choose type!", "请选择类型!");//frmTeachLoadport
            m_DicAllLanguageTranfer.Add("Loadport has no foup.", "没有晶圆盒。");//frmTeachLoadport

            m_DicAllLanguageTranfer.Add("Button [Next] to start the process\r", "按钮[下一步]开始程序\r");//frmTeachAngle
            m_DicAllLanguageTranfer.Add("Start Teaching Notch Angle, click[Next] when finished\r", "开始缺口教学，完成后点击[下一步]\r");//frmTeachAngle
            m_DicAllLanguageTranfer.Add("Teaching is over, please click Next\r", "教学结束，请点击[下一步]\r");//frmTeachAngle

            m_DicAllLanguageTranfer.Add("Run Stocker to open the door.\r", "运行晶圆塔开门.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Please turn on the T-key.\r", "请打开 T 钥匙.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Please turn on the T-key.", "请打开 T 钥匙.");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Press [Next] to continue.\r", "按[下一步]继续.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Failed to open the door.\r", "开门失败.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Press [Next] to retry.\r", "按 [下一步] 重试.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Abort teaching ?", "中止教学？");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Perform robot mode switching.\r", "执行机器人模式切换.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Please select robot.", "请选择机器人.");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Please select Area!", "请选择区域!");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Please select Tower!", "请选择塔!");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Area", "区域");//frmTeachStock
            m_DicAllLanguageTranfer.Add("No.", "号");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Execute the teaching process.\r", "执行教学过程.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Switching failed.\r", "切换失败.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Start operating Teaching Pendant.\r", "开始操作示教器.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("When the teaching is complete close the T-key and press the [Next] button to continue.\r", "教学完成后，关闭 T 键并按下 [Next] 按钮继续.\r");
            m_DicAllLanguageTranfer.Add("Turn T-key off.\r", "关闭 T 键.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Please turn off the T-key.", "请关闭 T 键.");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Completion of the origin.\r", "完成原点.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Press the [Next] or [Cancel] button.\r", "按[下一步]或[取消]按钮.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Failed at the origin.\r", "原点失败.\r");//frmTeachStock
            m_DicAllLanguageTranfer.Add("Press [Next] or [Cancel] to retry.\r", "按 [下一步] 或 [取消] 重试.\r");//frmTeachStock

            m_DicAllLanguageTranfer.Add("Running in cycles.", "循环运行。");//frmManual
            m_DicAllLanguageTranfer.Add("Robot stage search fail!!!", "机器人位置搜索失败!!!");//frmManual
            m_DicAllLanguageTranfer.Add("Loadport is disable!!!", "端口已禁用!!!");//frmManual
            m_DicAllLanguageTranfer.Add("Please confirm the loadport dock.", "请确认装载口没有开门.");//frmManual
            m_DicAllLanguageTranfer.Add("Please confirm the loadport do not open door!!", "请确认装载口没有开门!");//frmManual
            m_DicAllLanguageTranfer.Add("Aligner is disable!!!", "对准器已禁用!!!");//frmManual
            m_DicAllLanguageTranfer.Add("Buffer is disable!!!", "缓冲区已禁用!!!");//frmManual
            m_DicAllLanguageTranfer.Add("The target has no wafer.", "目标没有晶圆。");//frmManual
            m_DicAllLanguageTranfer.Add("The target has wafer.", "目标有晶圆。");//frmManual
            m_DicAllLanguageTranfer.Add("Please confirm the wafer on the robot", "请确认机器人手上的晶圆");//frmManual
            m_DicAllLanguageTranfer.Add("Please check if the robot arm is extended?", "请检查机器人手臂是否伸出?");//frmManual

            m_DicAllLanguageTranfer.Add("The maximum level setting is 1000 strokes, add failed!", "最大设置为 1000，添加失败！");//frmDataBase
            m_DicAllLanguageTranfer.Add("Grade Name must not be blank!", "等级名称不得为空！");//frmDataBase
            m_DicAllLanguageTranfer.Add("The upper and lower limits cannot be empty.", "上限和下限不能为空。");//frmDataBase
            m_DicAllLanguageTranfer.Add("Single-Tower setup", "单柱设定");//frmDataBase
            m_DicAllLanguageTranfer.Add("Multi-Tower setting", "多柱设定");//frmDataBase
            m_DicAllLanguageTranfer.Add("Single-Area setup", "单区域设定");//frmDataBase
            m_DicAllLanguageTranfer.Add("Multi-Area setting", "多区域设定");//frmDataBase
            m_DicAllLanguageTranfer.Add("GradeName Repeat", "等级名称 重复");//frmDataBase
            m_DicAllLanguageTranfer.Add("High level cannot be set to 0.", "高水位不能为0");//frmDataBase
            m_DicAllLanguageTranfer.Add("The L-Limit must not be less than 0.", "下限值不得小于 0");//frmDataBase
            m_DicAllLanguageTranfer.Add("Make sure to delete the selected 『", "确保删除选定的第『");//frmDataBase
            m_DicAllLanguageTranfer.Add("Pls select GradeName!", "请选择等级名称");//frmDataBase

            m_DicAllLanguageTranfer.Add("Read fail Execution UNDO?!", "读取失败持行撤回?");//frmDataBase

            m_DicAllLanguageTranfer.Add("Data has change  do you want to save data?", "数据已更改 您想保存数据吗？");//frmParameter
            #endregion
        }

        public void DoInit()
        {
            try
            {
                LoadIni();

                for (int i = 0; i < Enum.GetNames(typeof(enumIOModule)).Count(); i++)
                {
                    LoadDIOName_ini(i);
                }
                for (int i = 0; i < Enum.GetNames(typeof(enumAdam)).Count(); i++)
                {
                    LoadAdamDIOName_ini(i);
                }
                LoadPlcIOName_ini();

                LoadOCRRecipeIniFile();

                LoadPositionData();
                LoadRobotToPosition();
            }
            catch
            {

            }
        }
        //  Parameter
        private void LoadIni()
        {
            CINIFile myIni = new CINIFile(m_strFileIni);

            lock (m_lockINI)
            {
                int nValue; //   暫存用

                //---------------------------------------------------------------------------
                IsSimulate = myIni.GetIni("System", "Simulate", false);
                XYZMode = (enumXYZMode)myIni.GetIni("System", "XYZ Mode( Auto:0, Manual:1)", 0);
                IsAutoRemote = myIni.GetIni("System", "AutoRemote", false);
                IsAutoDock = myIni.GetIni("System", "AutoDock", false);
				E84LightCurtainCheck = myIni.GetIni("System", "E84LightCurtainCheck", true);
                IsSecsEnable = myIni.GetIni("System", "SECS Enable", false);
                GetServerIP = myIni.GetIni("System", "Server IP", "172.20.9.200");
                GetServerPort = myIni.GetIni("System", "Server Port", 12000);
                GetSystemType = (enumSystemType)myIni.GetIni("System", "SystemType(1:ActiveEFEM,2:PassiveEFEM,3:Sorter)", 0);
                GetDBAlarmlistUpdate = myIni.GetIni("System", "DBAlarmlistUpdate", false);
                GetRFID_Bit = myIni.GetIni("System", "RFID_Bit", -1);
                FreeStyle = myIni.GetIni("System", "FreeStyle", false);
                SystemLanguage = (enumSystemLanguage)myIni.GetIni("System", "Language(0:Default, 1:zn_TW, 2:en_US, 3:zh_CN)", 0);//上Arm偵測下垂

                EquipmentShowName = myIni.GetIni("System", "Equipment Name", "");
                m_bRobotAlignment_Enable = myIni.GetIni("System", "Robot_Alignment_Enable", false);
                //---------------------------------------------------------------------------
                EQIOSwitchToExtend = myIni.GetIni("Process", "EQIOSwitchToExtend", 0);
                //---------------------------------------------------------------------------  
                for (int i = 0; i < Enum.GetNames(typeof(enumRobot)).Count(); i++)
                {
                    string strSection = "TRB" + (i + 1);
                    m_eTRB_TCPType[i] = (enumTCPType)myIni.GetIni(strSection, "TCP_Type(0:None,1:Client,2:Server)", 1);
                    m_eUpperArmWaferType[i] = (enumArmFunction)myIni.GetIni(strSection, "UpperArmFunction(0:NONE,1:NORMAL,2:I,3:FRAME)", 0);
                    m_eLowerArmWaferType[i] = (enumArmFunction)myIni.GetIni(strSection, "LowerArmFunction(0:NONE,1:NORMAL,2:I,3:FRAME)", 0);
                    m_nFrameTwoStepLoadArmBackPulse[i] = myIni.GetIni(strSection, "FrameTwoStepLoadArmBackPulse", 10000);
                    m_bXaxisDisable[i] = myIni.GetIni(strSection, "Xaxis_Disable", true);
                    m_bExtXaxisDisable[i] = myIni.GetIni(strSection, "External_Xaxis_Disable", true);
                    m_bExtXaxisSimulate[i] = myIni.GetIni(strSection, "External_Xaxis_Simulate", true);
                    m_strRobot_AllowPort[i] = myIni.GetIni(strSection, "HardwareAllow Loadport(0:Disable,1:Enable)", "00000000");
                    m_strRobot_AllowAligner[i] = myIni.GetIni(strSection, "HardwareAllow Aligner(0:Disable,1:Enable)", "00");
                    m_strRobot_AllowEquipment[i] = myIni.GetIni(strSection, "HardwareAllow Equipment(0:Disable,1:Enable)", "0000");

                    if (m_eUpperArmWaferType[i] != m_eLowerArmWaferType[i])
                    {
                        m_bUseArmSameMovement[i] = m_bAlignerExchange[i] = false;
                    }
                    else
                    {
                        m_bUseArmSameMovement[i] = myIni.GetIni(strSection, "UseArmSameMovement", false);//雙取雙放
                        m_bAlignerExchange[i] = myIni.GetIni(strSection, "AlignerExchange", false);//雙取雙放
                    }
                    m_nAngleBetweenOrgnAndXaxis[i] = myIni.GetIni(strSection, "AngleBetweenOrgnAndXaxis", 20000);//手臂原點與X軸夾角                  
                    m_bUnldUseClmpCheckWaferTime[i] = myIni.GetIni(strSection, "UnldUseClmpCheckWaferTime(Enable > 0)", 0);
                    m_bCheckRobotAir[i] = myIni.GetIni(strSection, "CheckRobotAir", false);
                    m_nMaintSpeed[i] = myIni.GetIni(strSection, "MaintSpeed", 6);
                    if (m_nMaintSpeed[i] > 6 || m_nMaintSpeed[i] == 0)
                    {
                        SetRobot_MaintSpeed(i, 6);
                    }

                    m_nRunSpeed[i] = myIni.GetIni(strSection, "RunSpeed", 0);
                }
                //---------------------------------------------------------------------------    
                for (int i = 0; i < Enum.GetNames(typeof(enumLoadport)).Count(); i++)
                {
                    string strSection = "STG" + (i + 1);
                    m_eSTG_TCPType[i] = (enumTCPType)myIni.GetIni(strSection, "TCP_Type(0:None,1:Client,2:Server)", 1);
                    nValue = myIni.GetIni(strSection, "Mode(RV201,RB201,Other)", 0);
                    m_eLoadportMode[i] = (Enum.IsDefined(typeof(enumLoadportType), nValue)) ? (enumLoadportType)nValue : enumLoadportType.None;
                    string str1 = myIni.GetIni(strSection, "Info-Pad(Unknow:0,Inch12:1,Inch08:2,Inch06:3,Frame:4,Panel:5)", "1111111111111111");
                    string str2 = myIni.GetIni(strSection, "Adapter-Info-Pad(Unknow:0,Inch12:1,Inch08:2,Inch06:3,Frame:4,Panel:5)", "0000000000000000");
                    if ((str1 + str2).Length < 32)
                        m_strloadportWaferType[i] = "11111111111111110000000000000000";
                    else
                        m_strloadportWaferType[i] = str1 + str2;
                    //Info-Pad Enable
                    string str3 = myIni.GetIni(strSection, "Info-Pad Enable(0:disable,1:enable)", "1111111111111111");
                    if (str3.Length < 16) { str3 = "1111111111111111"; }
                    m_LPInfoPadEnableList[i] = new List<bool>();
                    for (int j = 0; j < 16; j++) { m_LPInfoPadEnableList[i].Add(str3[j] == '1'); }

                    //Info-Pad Name
                    string[] strArry = myIni.GetIni(strSection, "Info-Pad Name", "FUP1,FUP2,FUP3,FUP4,FUP5,FUP6,FUP7,FSB1,FSB2,FSB3,FSB4,FSB5,OCP1,OCP2,OCP3,FPO1").Split(',');
                    if (strArry.Length < 16) { strArry = "FUP1,FUP2,FUP3,FUP4,FUP5,FUP6,FUP7,FSB1,FSB2,FSB3,FSB4,FSB5,OCP1,OCP2,OCP3,FPO1".Split(','); }
                    m_LPInfoPadName[i] = new List<string>(strArry);

                    //Robot Mapp Enable
                    string str4 = myIni.GetIni(strSection, "RobotMapInfoEnable(0:disable,1:enable)", "0000000000000000");
                    string str5 = myIni.GetIni(strSection, "Adapter-RobotMapInfoEnable(0:disable,1:enable)", "0000000000000000");
                    string str6 = str4 + str5;
                    if (str6.Length < 32) { str6 = "00000000000000000000000000000000"; }
                    m_LPForTrbMapInfoEnableList[i] = new List<bool>();
                    for (int j = 0; j < 32; j++)
                    {
                        m_LPForTrbMapInfoEnableList[i].Add(str6[j] == '1');
                    }
                    m_strSimulateGmap[i] = myIni.GetIni(strSection, "SimulateGmap", "0000000000000");
                    m_strStgBarcodeIndex[i] = myIni.GetIni(strSection, "BarcodeIndex", -1);
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumAligner)).Count(); i++)
                {
                    string strSection = "ALN" + (i + 1);
                    nValue = myIni.GetIni(strSection, "Mode(0:RA320,1:RA420,2:TurnTable,3:PanelXYR,4:TAL303)", 0);
                    m_eAlignerMode[i] = Enum.IsDefined(typeof(enumAlignerType), nValue) ? (enumAlignerType)nValue : enumAlignerType.None;
                    m_bUnClampLiftPinUp[i] = myIni.GetIni(strSection, "UnClampLiftPinUp(0:false/1:true)", false);
                    m_nAngleBetweenNotchAndRbAFinger[i] = myIni.GetIni(strSection, "AngleBetweenNotchAndRbAFinger", 0);
                    m_nAngleBetweenNotchAndRbBFinger[i] = myIni.GetIni(strSection, "AngleBetweenNotchAndRbBFinger", 0);
                    m_strAlnBarcodeIndex[i] = myIni.GetIni(strSection, "BarcodeIndex", -1);
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumBuffer)).Count(); i++)
                {
                    string strSection = "BUF" + (i + 1);
                    nValue = myIni.GetIni(strSection, "Type(Unknow:0,Inch12:1,Inch08:2,Inch06:3,Frame:4,Panel:5)", 0);
                    m_nBufferWaferType[i] = nValue;
                    m_nBufferPosDetect[i] = myIni.GetIni(strSection, "PositionDetection", true);

                    int nSlot1Bit = myIni.GetIni(strSection, "Slot1ExistBit", i == 0 ? 9 : 11);//一代機的點位
                    int nSlot2Bit = myIni.GetIni(strSection, "Slot2ExistBit", i == 0 ? 8 : 10);//一代機的點位
                    int nSlot3Bit = myIni.GetIni(strSection, "Slot3ExistBit", -1);
                    int nSlot4Bit = myIni.GetIni(strSection, "Slot4ExistBit", -1);
                    m_nBufferSlotRc530Bit[i] = new List<int> { nSlot1Bit, nSlot2Bit, nSlot3Bit, nSlot4Bit };

                    int nAround1Bit = myIni.GetIni(strSection, "Around1Bit", i == 0 ? 0 : 4);//一代機的點位
                    int nAround2Bit = myIni.GetIni(strSection, "Around2Bit", i == 0 ? 1 : 5);//一代機的點位
                    int nAround3Bit = myIni.GetIni(strSection, "Around3Bit", i == 0 ? 2 : 6);//一代機的點位
                    int nAround4Bit = myIni.GetIni(strSection, "Around4Bit", i == 0 ? 3 : 7);//一代機的點位
                    m_nBufferAroundRc530Bit[i] = new List<int> { nAround1Bit, nAround2Bit, nAround3Bit, nAround4Bit };
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumIOModule)).Count(); i++)
                {
                    string strSection = "DIO";
                    nValue = myIni.GetIni(strSection, string.Format("DIO{0} Type(RC530:0,RC550:1)", i), 0);
                    m_eDioType[i] = (-1 < nValue && nValue < 2) ? (enumIOModuleType)nValue : enumIOModuleType.RC530;
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumTBLModule)).Count(); i++)
                {
                    string strSection = string.Format("TBL{0}", i + 1);
                    nValue = myIni.GetIni(strSection, string.Format("TBL{0} Type(RC560:0,RC550:1)", i + 1), 1);
                    m_eTblType[i] = Enum.IsDefined(typeof(enumTblType), nValue) ? (enumTblType)nValue : enumTblType.None;
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumFFU)).Count(); i++)
                {
                    string strSection = "FFU" + (i + 1);
                    nValue = myIni.GetIni(strSection, string.Format("Type(0:None,1:TOPWELL,2:AirTech,3:NicotraGebhardt)"), 0);
                    m_eFfuType[i] = Enum.IsDefined(typeof(enumFfuType), nValue) ? (enumFfuType)nValue : enumFfuType.None;
                    m_nFfuFanCount[i] = myIni.GetIni(strSection, string.Format("FanCount"), 1);
                    m_nFfuComort[i] = myIni.GetIni(strSection, string.Format("Comport"), 0);
                    m_strFfuIp[i] = myIni.GetIni(strSection, string.Format("IP"), "");
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumCamera)).Count(); i++)
                {
                    string strSection = "CMP" + (i + 1);
                    nValue = myIni.GetIni(strSection, string.Format("Camera(0:None,1:NPD)"), 0);
                    m_eCameraType[i] = Enum.IsDefined(typeof(enumCameraType), nValue) ? (enumCameraType)nValue : enumCameraType.None;
                    m_strCameraIP[i] = myIni.GetIni(strSection, string.Format("IP"), "");
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumBarcode)).Count(); i++)
                {
                    string strSection = "BCR" + (i + 1);
                    nValue = myIni.GetIni(strSection, string.Format("Type(0:None,1:KeyenceSR2000,2:CognexDM370,3:KeyenceSR710)"), 0);
                    m_eBarcodeType[i] = Enum.IsDefined(typeof(enumBarcodeType), nValue) ? (enumBarcodeType)nValue : enumBarcodeType.None;
                    m_nBarcodeComport[i] = myIni.GetIni(strSection, "Comport", 0);
                    m_strBarcodeIP[i] = myIni.GetIni(strSection, "IP", "");
                }
                //---------------------------------------------------------------------------
                #region Disable Flag
                for (int i = 0; i < Enum.GetNames(typeof(enumRobot)).Count(); i++)
                {
                    string strKey = string.Format("TRB{0} Disable", i + 1);
                    m_bTrbDisable[i] = myIni.GetIni("RorzeUnit Disable", strKey, true);
                }
                for (int i = 0; i < Enum.GetNames(typeof(enumTBLModule)).Count(); i++)
                {
                    string strKey = string.Format("TBL{0} Disable", i + 1);
                    m_bTblDisable[i] = myIni.GetIni("RorzeUnit Disable", strKey, true);
                }                
                for (int i = 0; i < Enum.GetNames(typeof(enumLoadport)).Count(); i++)
                {
                    string strKey = string.Format("STG{0} Disable", i + 1);
                    m_bStgDisable[i] = myIni.GetIni("RorzeUnit Disable", strKey, true);
                }
                for (int i = 0; i < Enum.GetNames(typeof(enumAligner)).Count(); i++)
                {
                    string strKey = string.Format("ALN{0} Disable", i + 1);
                    m_bAlnDisable[i] = myIni.GetIni("RorzeUnit Disable", strKey, true);
                }
                for (int i = 0; i < Enum.GetNames(typeof(enumOCR)).Count(); i++)
                {
                    string strKey = string.Format("OCR{0} Disable", i + 1);
                    m_bOCRDisable[i] = myIni.GetIni("RorzeUnit Disable", strKey, true);
                }
                for (int i = 0; i < Enum.GetNames(typeof(enumIOModule)).Count(); i++)
                {
                    string strKey = string.Format("DIO{0} Disable", i);
                    m_bDioDisable[i] = myIni.GetIni("RorzeUnit Disable", strKey, true);
                }
                for (int i = 0; i < Enum.GetNames(typeof(enumBuffer)).Count(); i++)
                {
                    string strKey = string.Format("BUF{0} Enable Slot(0:disable,1)", i + 1);
                    m_strBufEnableSlotNum[i] = myIni.GetIni("RorzeUnit Disable", strKey, "1100");
                }
                #endregion
                //---------------------------------------------------------------------------
                GetOCRRecipeMax = myIni.GetIni("OCR Setting", "Recipe Number", 30);
                GetOCR_Front_RecipeLast = myIni.GetIni("OCR Setting", "LastFrontRecipe", 1);
                GetOCR_Back_RecipeLast = myIni.GetIni("OCR Setting", "LastBackRecipe", 1);
                m_eOcrType[0] = (enumOcrType)myIni.GetIni("OCR Setting", "A1 Type(0:IS1740,1:WID120,2:TZ0031)", 0);
                m_eOcrType[1] = (enumOcrType)myIni.GetIni("OCR Setting", "A2 Type(0:IS1740,1:WID120,2:TZ0031)", 0);
                m_eOcrType[2] = (enumOcrType)myIni.GetIni("OCR Setting", "B1 Type(0:IS1740,1:WID120,2:TZ0031)", 0);
                m_eOcrType[3] = (enumOcrType)myIni.GetIni("OCR Setting", "B2 Type(0:IS1740,1:WID120,2:TZ0031)", 0);
                GetOCR_ReadSucGetImage = myIni.GetIni("OCR Setting", "ReadOKGetImage", false);
                //---------------------------------------------------------------------------
                nValue = myIni.GetIni("Comport", "RFID Reader Maker 0:Unison/1:Heart/2:Omron/3:Brillian", 0);
                GetRFIDType = (-1 < nValue && nValue < 4) ? (enumRFID)nValue : enumRFID.None;
                m_nComRfid[0] = myIni.GetIni("Comport", "RFID_1", 0);
                m_nComRfid[1] = myIni.GetIni("Comport", "RFID_2", 0);
                m_nComRfid[2] = myIni.GetIni("Comport", "RFID_3", 0);
                m_nComRfid[3] = myIni.GetIni("Comport", "RFID_4", 0);
                m_nComRfid[4] = myIni.GetIni("Comport", "RFID_5", 0);
                m_nComRfid[5] = myIni.GetIni("Comport", "RFID_6", 0);
                m_nComRfid[6] = myIni.GetIni("Comport", "RFID_7", 0);
                m_nComRfid[7] = myIni.GetIni("Comport", "RFID_8", 0);

                m_nComFITC[0] = myIni.GetIni("Comport", "FITC_1", 0);
                m_nComFITC[1] = myIni.GetIni("Comport", "FITC_2", 0);
                m_nComFITC[2] = myIni.GetIni("Comport", "FITC_3", 0);
                m_nComFITC[3] = myIni.GetIni("Comport", "FITC_4", 0);
                m_nComFITC[4] = myIni.GetIni("Comport", "FITC_5", 0);
                m_nComFITC[5] = myIni.GetIni("Comport", "FITC_6", 0);
                m_nComFITC[6] = myIni.GetIni("Comport", "FITC_7", 0);
                m_nComFITC[7] = myIni.GetIni("Comport", "FITC_8", 0);
                m_nComEFEM_FFU = myIni.GetIni("Comport", "EFEM FFU", 0);
                m_nComKeyence_DL_RS1A = myIni.GetIni("Comport", "Keyence_DL_RS1A", 0);
                //---------------------------------------------------------------------------
                m_bE84Disable[0] = myIni.GetIni("E84 Disable", "E84_1_Disable", true);
                m_bE84Disable[1] = myIni.GetIni("E84 Disable", "E84_2_Disable", true);
                m_bE84Disable[2] = myIni.GetIni("E84 Disable", "E84_3_Disable", true);
                m_bE84Disable[3] = myIni.GetIni("E84 Disable", "E84_4_Disable", true);
                m_bE84Disable[4] = myIni.GetIni("E84 Disable", "E84_5_Disable", true);
                m_bE84Disable[5] = myIni.GetIni("E84 Disable", "E84_6_Disable", true);
                m_bE84Disable[6] = myIni.GetIni("E84 Disable", "E84_7_Disable", true);
                m_bE84Disable[7] = myIni.GetIni("E84 Disable", "E84_8_Disable", true);
                //---------------------------------------------------------------------------
                E84Type = (enumE84Type)myIni.GetIni("E84 TP TimeOut", " E84 Type(0:Remote IO,1:FITC,2:LPBuiltInE84)", 0);
                m_nE84Tp[0] = myIni.GetIni("E84 TP TimeOut", "TP1 TimeOut", 2);
                m_nE84Tp[1] = myIni.GetIni("E84 TP TimeOut", "TP2 TimeOut", 2);
                m_nE84Tp[2] = myIni.GetIni("E84 TP TimeOut", "TP3 TimeOut", 60);
                m_nE84Tp[3] = myIni.GetIni("E84 TP TimeOut", "TP4 TimeOut", 60);
                m_nE84Tp[4] = myIni.GetIni("E84 TP TimeOut", "TP5 TimeOut", 2);
                //---------------------------------------------------------------------------               
                m_strIPTrb[0] = myIni.GetIni("RorzeUnit IP Address", "Rorze Robot1", "172.20.9.151");
                m_strIPTrb[1] = myIni.GetIni("RorzeUnit IP Address", "Rorze Robot2", "172.20.9.152");
                m_strIPTbl[0] = myIni.GetIni("RorzeUnit IP Address", "Rorze TBL1", "172.20.9.171");
                m_strIPTbl[1] = myIni.GetIni("RorzeUnit IP Address", "Rorze TBL2", "172.20.9.172");
                m_strIPTbl[2] = myIni.GetIni("RorzeUnit IP Address", "Rorze TBL3", "172.20.9.173");
                m_strIPTbl[3] = myIni.GetIni("RorzeUnit IP Address", "Rorze TBL4", "172.20.9.174");
                m_strIPTbl[4] = myIni.GetIni("RorzeUnit IP Address", "Rorze TBL5", "172.20.9.175");
                m_strIPTbl[5] = myIni.GetIni("RorzeUnit IP Address", "Rorze TBL6", "172.20.9.176");
                m_strIPAln[0] = myIni.GetIni("RorzeUnit IP Address", "Rorze Aligner_1", "172.20.9.161");
                m_strIPAln[1] = myIni.GetIni("RorzeUnit IP Address", "Rorze Aligner_2", "172.20.9.162");
                m_strIPStg[0] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_1", "172.20.9.101");
                m_strIPStg[1] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_2", "172.20.9.102");
                m_strIPStg[2] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_3", "172.20.9.103");
                m_strIPStg[3] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_4", "172.20.9.104");
                m_strIPStg[4] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_5", "172.20.9.105");
                m_strIPStg[5] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_6", "172.20.9.106");
                m_strIPStg[6] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_7", "172.20.9.107");
                m_strIPStg[7] = myIni.GetIni("RorzeUnit IP Address", "Rorze Loadport_8", "172.20.9.108");
                m_strIPOcr[0] = myIni.GetIni("RorzeUnit IP Address", "OCR_A1", "172.20.9.1");
                m_strIPOcr[1] = myIni.GetIni("RorzeUnit IP Address", "OCR_A2", "172.20.9.2");
                m_strIPOcr[2] = myIni.GetIni("RorzeUnit IP Address", "OCR_B1", "172.20.9.3");
                m_strIPOcr[3] = myIni.GetIni("RorzeUnit IP Address", "OCR_B2", "172.20.9.4");
                m_strIPDio[0] = myIni.GetIni("RorzeUnit IP Address", "Rorze DIO0", "172.20.9.180");
                m_strIPDio[1] = myIni.GetIni("RorzeUnit IP Address", "Rorze DIO1", "172.20.9.181");
                m_strIPDio[2] = myIni.GetIni("RorzeUnit IP Address", "Rorze DIO2", "172.20.9.182");
                m_strIPDio[3] = myIni.GetIni("RorzeUnit IP Address", "Rorze DIO3", "172.20.9.183");
                m_strIPDio[4] = myIni.GetIni("RorzeUnit IP Address", "Rorze DIO4", "172.20.9.184");
                m_strIPDio[5] = myIni.GetIni("RorzeUnit IP Address", "Rorze DIO5", "172.20.9.185");
                //---------------------------------------------------------------------------
                m_strIP1Gem = myIni.GetIni("Gem IP Address", "GemIP1", "127.0.0.1");
                m_nGemPort = myIni.GetIni("Gem IP Address", "Port", 6000);
                m_strIP2Gem = myIni.GetIni("Gem IP Address", "GemIP2", "127.0.0.1");
                m_nClientPort = myIni.GetIni("Gem IP Address", "ClientPort", 5005);
                //---------------------------------------------------------------------------
                m_strMotionEventManagerUrl = myIni.GetIni("GRPC", "BaseUrl", "http://localhost:61723");
                m_bGRPC_Disable = myIni.GetIni("GRPC", "GRPC_Disable", false);
                //---------------------------------------------------------------------------
                RC550ctrlFFU = myIni.GetIni("RC550_0", "Driver4_FFU_Enable", false);
                RC550Pressure_Enable = myIni.GetIni("RC550_0", "HCL0_SB068A_Pressure_Enable", false);
                RC550Pressure_Threshold = myIni.GetIni("RC550_0", "HCL0_SB068A_Pressure_Threshold(1pa~3pa)", 1);
                GetFanDefaultSpeed = myIni.GetIni("RC550_0", "FanSpeed", 1000);
                DVRAlarmn_Disable = myIni.GetIni("RC550_0", "DVRAlarmn_Disable", false);
                PowerFan1Alarmn_Disable = myIni.GetIni("RC550_0", "PowerFan1Alarmn_Disable", false);
                PowerFan2Alarmn_Disable = myIni.GetIni("RC550_0", "PowerFan2Alarmn_Disable", false);
                //---------------------------------------------------------------------------     
                DIO1_CheckIO_Enable = myIni.GetIni("RC530_1", "CheckIO_Enable ", "0000000000000000");
                DIO1_CheckIO_AorB = myIni.GetIni("RC530_1", "CheckIO_AorB ", "AAAAAAAAAAAAAAAA");
                DIO2_CheckIO_Enable = myIni.GetIni("RC530_2", "CheckIO_Enable ", "0000000000000000");
                DIO2_CheckIO_AorB = myIni.GetIni("RC530_2", "CheckIO_AorB ", "AAAAAAAAAAAAAAAA");
                DIO3_CheckIO_Enable = myIni.GetIni("RC530_3", "CheckIO_Enable ", "0000000000000000");
                DIO3_CheckIO_AorB = myIni.GetIni("RC530_3", "CheckIO_AorB ", "AAAAAAAAAAAAAAAA");
                DIO4_CheckIO_Enable = myIni.GetIni("RC530_4", "CheckIO_Enable ", "0000000000000000");
                DIO4_CheckIO_AorB = myIni.GetIni("RC530_4", "CheckIO_AorB ", "AAAAAAAAAAAAAAAA");
                //---------------------------------------------------------------------------
                EnableRandomSelectWafer = myIni.GetIni("CustomFunction", "RandomSelectWafer", false);
                EnableAutoZoom = myIni.GetIni("CustomFunction", "AutoZoom", false);
                IdleLogOutTime = myIni.GetIni("CustomFunction", "IdleLogOutTime(ms)", 60000);
                GetOCRReadFailProcess = (enumOCRReadFailProcess)myIni.GetIni("CustomFunction", "OCRReadFailProcess(0:Continue,1:Abort,2:BackFoup,3:UserKeyIn)", 0);
                FoupArrivalIdleTimeout = myIni.GetIni("CustomFunction", "FoupArrivalIdleTimeout(s)", -1);
                FoupWaitTransferTimeout = myIni.GetIni("CustomFunction", "FoupWaitTransferTimeout(s)", -1);
                WaferIDFilterBit = myIni.GetIni("CustomFunction", "WaferIDFilterBit", 10);
                OCRWarningsAutoRestTime = myIni.GetIni("CustomFunction", "OCRWarningsAutoRestTime(s)", 10);
                //---------------------------------------------------------------------------
                foreach (enumNotchAngle eType in Enum.GetValues(typeof(enumNotchAngle)))
                {
                    if (eType == enumNotchAngle.Total) continue;
                    m_nNotchAngle[(int)eType] = myIni.GetIni("Notch Angle Adjustment Data", GetEnumDescription(eType), 0);
                }
                //---------------------------------------------------------------------------
                foreach (enumSignalTowerColorSetting eType in Enum.GetValues(typeof(enumSignalTowerColorSetting)))
                {
                    if (eType == enumSignalTowerColorSetting.Total) continue;
                    m_dicSignalTowerColor[eType] = (enumSignalTowerColor)myIni.GetIni("Signal Tower Color Setting(None, Red, RedBlinking, Yellow, YellowBlinking, Green, GreenBlinking, Blue, BlueBlinking)", GetEnumDescription(eType), 0);
                }
                //---------------------------------------------------------------------------
                DBSever = myIni.GetIni("DataBase", "Sever", "127.0.0.1");
                DBUser = myIni.GetIni("DataBase", "User", "root");
                DBPassWord = myIni.GetIni("DataBase", "PassWord", "RORZE");
                DBName = myIni.GetIni("DataBase", "DBName", "bwsdb-3200");
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumEQM)).Count(); i++)
                {
                    string strKey = string.Format("Equipment{0}", i + 1);
                    m_bEqmDisable[i] = myIni.GetIni(strKey, "Disable", true);
                    m_bEqmSimulate[i] = myIni.GetIni(strKey, "Simulate", true);
                    m_bEqmName[i] = myIni.GetIni(strKey, "Name", strKey);
                    nValue = myIni.GetIni(strKey, "TCP_Type(0:None,1:Client,2:Server)", 0);
                    m_eEqmTCPType[i] = Enum.IsDefined(typeof(enumTCPType), nValue) ? (enumTCPType)nValue : enumTCPType.None;
                    m_strEqmIP[i] = myIni.GetIni(strKey, "IP", "127.0.0." + (i + 1));
                    m_nEqmPort[i] = myIni.GetIni(strKey, "Port", 5000);
                    m_strEqmDefaultRecipe[i] = myIni.GetIni(strKey, "DefaultRecipe", "");
                    m_bEqmGetRecipeListEnable[i] = myIni.GetIni(strKey, "GetRecipeListEnable", "");
                    m_nEqmProcessTimeout[i] = myIni.GetIni(strKey, "ProcessTimeout", 60000);
                }
                //---------------------------------------------------------------------------
                for (int i = 0; i < Enum.GetNames(typeof(enumAdam)).Count(); i++)
                {
                    string strKey = string.Format("Adam{0}", i + 1);
                    m_bAdamDisable[i] = myIni.GetIni(strKey, "Disable", true);
                    m_strAdamIP[i] = myIni.GetIni(strKey, "IP", "127.0.0." + (i + 1));
                    m_nAdamPort[i] = myIni.GetIni(strKey, "Port", 502);                    
                }
                //---------------------------------------------------------------------------
                m_nTurnTable_angle_0[0] = myIni.GetIni("TurnTable String", "TurnTableA_angle_0", 0);
                m_nTurnTable_angle_180[0] = myIni.GetIni("TurnTable String", "TurnTableA_angle_180", 180000);
                m_nTurnTable_angle_0[1] = myIni.GetIni("TurnTable String", "TurnTableB_angle_0", 0);
                m_nTurnTable_angle_180[1] = myIni.GetIni("TurnTable String", "TurnTableB_angle_180", 180000);
                //---------------------------------------------------------------------------
                m_bSmartRfid_Disable = myIni.GetIni("Smart RFID", "Disable", false);
                m_strSmartRfid_IP = myIni.GetIni("Smart RFID", "ServerIP", "172.20.9.200");
                m_nSmartRfid_Port = myIni.GetIni("Smart RFID", "Port", 6100);
                m_strnSmartRfid_RfidIP = myIni.GetIni("Smart RFID", "RFID", "172.20.9.210");
                //---------------------------------------------------------------------------
                m_SafetyIOStatus_Disable = myIni.GetIni("Safety IOStatus", "Disable", false);
                m_strSafetyIOStatus_IP = myIni.GetIni("Safety IOStatus", "ServerIP", "172.20.9.200");
                m_nSafetyIOStatus_Port = myIni.GetIni("Safety IOStatus", "Port", 12001);
                m_strSafetyIOStatus_PlcIP = myIni.GetIni("Safety IOStatus", "PLC", "172.20.9.241");
                //---------------------------------------------------------------------------
                m_bKeyenceMP_Disable = myIni.GetIni("Keyence MP", "Disable", false);
                m_strKeyenceMP_IP = myIni.GetIni("Keyence MP", "IP", "172.20.9.240");
                m_nKeyenceMP_Port = myIni.GetIni("Keyence MP", "Port", 502);
                //---------------------------------------------------------------------------
                m_bSimco_Disable = myIni.GetIni("SIMCO", "Disable", false);
                m_strSimco_IP = myIni.GetIni("SIMCO", "IP", "192.168.10.50");
                m_nSimco_Port = myIni.GetIni("SIMCO", "Port", 10001);
                //---------------------------------------------------------------------------
                foreach (enumIO_Signal item in Enum.GetValues(typeof(enumIO_Signal)))
                {
                    string str = myIni.GetIni("IO_Signal_Information", item.ToString(), "_DioBodyNo:0,_Bit:0,_NormalOff:0");

                    // 解析 _DioBodyNo:2,_Bit:1,_NormalOff:1
                    var ioInfo = new IO_Signal_Information();
                    var props = str.Split(',');
                    foreach (var prop in props)
                    {
                        var propParts = prop.Trim().Split(':');
                        string propName = propParts[0].Trim();
                        string propValue = propParts[1].Trim();

                        switch (propName)
                        {
                            case "_DioBodyNo":
                                ioInfo._DioBodyNo = int.Parse(propValue);
                                break;
                            case "_Bit":
                                ioInfo._Bit = int.Parse(propValue);
                                break;
                            case "_NormalOff":
                                ioInfo._NormalOff = propValue == "1";
                                break;
                        }
                    }

                    m_dicIO_Signal[item] = ioInfo;
                }




            }

        }

        private void LoadRobotToPosition()
        {
            string strFile = m_strPath + "\\ExtXPos.ini";
            //  Robot
            if (false == File.Exists(strFile))
            {
                System.Windows.Forms.MessageBox.Show("External X Position file does not exist!!", "Error");
                //return;
            }

            CINIFile myIni = new CINIFile(strFile);

            foreach (enumPosition eType in Enum.GetValues(typeof(enumPosition)))
            {
                string strName = eType.ToString();
                string strSecsName = myIni.GetIni(strName, "SecsName", strName);
                int nArm1 = myIni.GetIni(strName, "ARM1", 0);
                int nArm2 = myIni.GetIni(strName, "ARM2", 0);
                int nAOIMovingDist = myIni.GetIni(strName, "Moving_Distance", 0);            
                RobPos pos = new RobPos(strName, strSecsName, nArm1, nArm2, nAOIMovingDist);

                if (dicRobPos.ContainsKey(eType))
                {
                    dicRobPos[eType] = pos;
                }
                else
                {
                    dicRobPos.Add(eType, pos);
                }
            }
        }

        public void WriteRobotPos()
        {
            string strFile = m_strPath + "\\ExtXPos.ini";
            if (false == File.Exists(strFile))
            {
                System.Windows.Forms.MessageBox.Show("External X Position file does not exist!!", "Error");
                return;
            }
            CINIFile myIni = new CINIFile(strFile);
            foreach (string name in Enum.GetNames(typeof(enumPosition)))
            {
                enumPosition ePos = (enumPosition)Enum.Parse(typeof(enumPosition), name);
                if (dicRobPos.ContainsKey(ePos))
                {
                    RobPos pos = dicRobPos[ePos];

                    myIni.WriteIni(name, "SecsName", pos.SECSName);
                    myIni.WriteIni(name, "ARM1", pos.Pos_ARM1);
                    myIni.WriteIni(name, "ARM2", pos.Pos_ARM2);
                    myIni.WriteIni(name, "Moving_Distance", pos.Pos_AOIMovingDist);
                }
                else
                {
                    string str = string.Format("Write Robot Position fail, no find {0}!", name);
                    System.Windows.Forms.MessageBox.Show(str, "Error");
                }
            }
        }

        //  DIO IO name
        private void LoadDIOName_ini(int nBodyNo)
        {
            CINIFile myIni;
            if (false != File.Exists(m_strPath + "\\DIO" + nBodyNo + ".Ini"))
            {
                myIni = new CINIFile(m_strPath + "\\DIO" + nBodyNo + ".Ini");

                int nBit = 16;
                lock (m_lockINI)
                {
                    List<string> listSections = myIni.ReadSections();//ini Sections 000,001,002
                    for (int i = 0; i < listSections.Count; i++)
                    {
                        int nHCL = int.Parse(listSections[i]);
                        string[] strArryDI = new string[nBit];
                        string[] strArryDO = new string[nBit];
                        for (int j = 0; j < nBit; j++)//16 bit
                        {
                            strArryDI[j] = myIni.GetIni(listSections[i], "DI_" + j, "-----");
                            strArryDO[j] = myIni.GetIni(listSections[i], "DO_" + j, "-----");
                        }
                        switch (nBodyNo)
                        {
                            case 0:
                                m_dicDIO0_DIName.Add(nHCL, strArryDI);
                                m_dicDIO0_DOName.Add(nHCL, strArryDO);
                                break;
                            case 1:
                                m_dicDIO1_DIName.Add(nHCL, strArryDI);
                                m_dicDIO1_DOName.Add(nHCL, strArryDO);
                                break;
                            case 2:
                                m_dicDIO2_DIName.Add(nHCL, strArryDI);
                                m_dicDIO2_DOName.Add(nHCL, strArryDO);
                                break;
                            case 3:
                                m_dicDIO3_DIName.Add(nHCL, strArryDI);
                                m_dicDIO3_DOName.Add(nHCL, strArryDO);
                                break;
                            case 4:
                                m_dicDIO4_DIName.Add(nHCL, strArryDI);
                                m_dicDIO4_DOName.Add(nHCL, strArryDO);
                                break;
                            case 5:
                                m_dicDIO5_DIName.Add(nHCL, strArryDI);
                                m_dicDIO5_DOName.Add(nHCL, strArryDO);
                                break;
                        }

                    }
                }
            }
        }

        private void LoadAdamDIOName_ini(int nBodyNo)
        {
            CINIFile myIni;
            if (false != File.Exists(m_strPath + "\\ADAMIO_" + nBodyNo + ".Ini"))
            {
                myIni = new CINIFile(m_strPath + "\\ADAMIO_" + nBodyNo + ".Ini");

                int nBit = 6;
                lock (m_lockAdamINI)
                {
                    List<string> listSections = myIni.ReadSections();//ini Sections 000,001,002
                    for (int i = 0; i < listSections.Count; i++)
                    {
                        int nHCL = int.Parse(listSections[i]);
                        string[] strArryDI = new string[nBit];
                        string[] strArryDO = new string[nBit];
                        for (int j = 0; j < nBit; j++)//6 bit
                        {
                            strArryDI[j] = myIni.GetIni(listSections[i], "DI_" + j, "-----");
                            strArryDO[j] = myIni.GetIni(listSections[i], "DO_" + j, "-----");
                        }
                        switch (nBodyNo)
                        {
                            case 0:
                                m_dicAdamDIO0_DIName.Add(nHCL, strArryDI);
                                m_dicAdamDIO0_DOName.Add(nHCL, strArryDO);
                                break;
                            case 1:
                                m_dicAdamDIO1_DIName.Add(nHCL, strArryDI);
                                m_dicAdamDIO1_DOName.Add(nHCL, strArryDO);
                                break;
                        }

                    }
                }
            }
        }

        //  PLC IO name
        private void LoadPlcIOName_ini()
        {
            CINIFile myIni;
            if (false != File.Exists(m_strPath + "\\SafetyIO" + ".Ini"))
            {
                myIni = new CINIFile(m_strPath + "\\SafetyIO" + ".Ini");

                int nBit = 8;
                lock (m_lockINI)
                {
                    List<string> listSections = myIni.ReadSections();//ini Sections 000,001,002
                    for (int i = 0; i < listSections.Count; i++)
                    {
                        int nHCL = int.Parse(listSections[i]);
                        string[] strArryDI = new string[nBit];
                        string[] strArryDO = new string[nBit];
                        for (int j = 0; j < nBit; j++)//16 bit
                        {
                            strArryDI[j] = myIni.GetIni(listSections[i], "DI_" + j, "-----");
                            strArryDO[j] = myIni.GetIni(listSections[i], "DO_" + j, "-----");
                        }

                        m_dicPLC0_DIName.Add(nHCL, strArryDI);
                        m_dicPLC0_DOName.Add(nHCL, strArryDO);


                    }
                }
            }
        }
        //  OCR
        private void LoadOCRRecipeIniFile(string strPath, int num)
        {
            CINIFile myIni = new CINIFile(strPath);
            lock (m_lockINI)
            {
                OCRecipeData Data = new OCRecipeData();
                Data.Number = num;
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.GetIni("OCR Recipe", "Date", strTime);
                Data.Stored = myIni.GetIni("OCR Recipe", "Stored", 0);
                if (strPath.Contains("Front"))
                    Data.Name = myIni.GetIni("OCR Recipe", "Name", "M12_" + num);
                else
                    Data.Name = myIni.GetIni("OCR Recipe", "Name", "T7_" + num);
                Data.Angle_A = System.Convert.ToDouble(myIni.GetIni("OCR Recipe", "Angle_A", "265.5"));
                Data.Angle_B = System.Convert.ToDouble(myIni.GetIni("OCR Recipe", "Angle_B", "265.5"));
                Data.WaferIDLength = myIni.GetIni("OCR Recipe", "Wafer ID Length", 12);
                Data.LotIDFirstPosition = myIni.GetIni("OCR Recipe", "Lot ID First Position", 1);
                Data.LotIDLength = myIni.GetIni("OCR Recipe", "Lot ID Length", 10);
                Data.WaferNoFirstPosition = myIni.GetIni("OCR Recipe", "Wafer No First Position", 11);
                Data.WaferSize = 1; myIni.GetIni("OCR Recipe", "Wafer Size", 1);
                Data.Hyphen = 0; myIni.GetIni("OCR Recipe", "Hyphen", 0);
                Data.MaskLength = 0; myIni.GetIni("OCR Recipe", "Mask Length", 0);
                if (strPath.IndexOf("Front") != -1)
                    m_OCRecipeData_Front.Add(Data);
                else if (strPath.IndexOf("Back") != -1)
                    m_OCRecipeData_Back.Add(Data);
            }
        }
        //  Rbt address
        private void LoadPositionData()
        {
            m_LstPosRobotA = new List<RorzePosition>();
            m_LstPosRobotA.Clear();
            m_LstPosRobotB = new List<RorzePosition>();
            m_LstPosRobotB.Clear();

            string strFile = m_strPath + "\\RbtAddress.ini";
            CINIFile myIni = new CINIFile(strFile);

            foreach (var value in Enum.GetValues(typeof(enumRbtAddress)))
            {
                RorzePosition myPos1 = new RorzePosition();
                myPos1.strDefineName = ((enumRbtAddress)value);
                myPos1.strDisplayName = myIni.GetIni("DisplayName", ((enumRbtAddress)value).ToString(), ((enumRbtAddress)value).ToString());
                myPos1.Stge0to399 = myIni.GetIni("AddressRobotA", ((enumRbtAddress)value).ToString(), (int)value);
                m_LstPosRobotA.Add(myPos1);
                m_DicPosRobotA.Add(((enumRbtAddress)value), myPos1.Stge0to399);

                RorzePosition myPos2 = new RorzePosition();
                myPos2.strDefineName = ((enumRbtAddress)value);
                myPos2.strDisplayName = myIni.GetIni("DisplayName", ((enumRbtAddress)value).ToString(), ((enumRbtAddress)value).ToString());
                myPos2.Stge0to399 = myIni.GetIni("AddressRobotB", ((enumRbtAddress)value).ToString(), (int)value);
                m_LstPosRobotB.Add(myPos2);
                m_DicPosRobotB.Add(((enumRbtAddress)value), myPos2.Stge0to399);
            }
        }

        //---------------------------------------------------------------------------
        private bool IO_Flog = false;
        public bool GetIO_Flog()
        {
            return IO_Flog;
        }
        public void SetIO_Flog(bool bFlog)
        {
            IO_Flog = bFlog;
        }
        private string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if ((attributes != null) && (attributes.Length > 0))
                return attributes[0].Description;
            else
                return value.ToString();
        }
        //---------------------------------------------------------------------------
        #region Robot address position
        Dictionary<enumRbtAddress, int> m_DicPosRobotA = new Dictionary<enumRbtAddress, int>();
        List<RorzePosition> m_LstPosRobotA;

        Dictionary<enumRbtAddress, int> m_DicPosRobotB = new Dictionary<enumRbtAddress, int>();
        List<RorzePosition> m_LstPosRobotB;

        public List<RorzePosition> GetLisPosRobot(int nBody)
        {
            switch (nBody)
            {
                case 1: return m_LstPosRobotA;
                case 2: return m_LstPosRobotB;
                default: return null;
            }
        }
        public int GetDicPosRobot(int nBody, enumRbtAddress eRbtAddress)//0~399
        {
            int nValue = -1;
            switch (nBody)
            {
                case 1: nValue = m_DicPosRobotA[eRbtAddress]; break;
                case 2: nValue = m_DicPosRobotB[eRbtAddress]; break;
            }
            return nValue;
        }
        public int GetDicPosRobot(int nBody, SWafer.enumFromLoader eFromLoader, bool bAdapter = false)//0~399
        {
            int nValue = -1;
            enumRbtAddress eRbtAddress;
            switch (eFromLoader)
            {
                case SWafer.enumFromLoader.LoadportA: eRbtAddress = bAdapter ? enumRbtAddress.STG1_08 : enumRbtAddress.STG1_12; break;
                case SWafer.enumFromLoader.LoadportB: eRbtAddress = bAdapter ? enumRbtAddress.STG2_08 : enumRbtAddress.STG2_12; break;
                case SWafer.enumFromLoader.LoadportC: eRbtAddress = bAdapter ? enumRbtAddress.STG3_08 : enumRbtAddress.STG3_12; break;
                case SWafer.enumFromLoader.LoadportD: eRbtAddress = bAdapter ? enumRbtAddress.STG4_08 : enumRbtAddress.STG4_12; break;
                case SWafer.enumFromLoader.LoadportE: eRbtAddress = bAdapter ? enumRbtAddress.STG5_08 : enumRbtAddress.STG5_12; break;
                case SWafer.enumFromLoader.LoadportF: eRbtAddress = bAdapter ? enumRbtAddress.STG6_08 : enumRbtAddress.STG6_12; break;
                case SWafer.enumFromLoader.LoadportG: eRbtAddress = bAdapter ? enumRbtAddress.STG7_08 : enumRbtAddress.STG7_12; break;
                case SWafer.enumFromLoader.LoadportH: eRbtAddress = bAdapter ? enumRbtAddress.STG8_08 : enumRbtAddress.STG8_12; break;
                default: return -1;
            }
            nValue = GetDicPosRobot(nBody, eRbtAddress);
            return nValue;
        }
        public int GetDicPosRobot(int nBody, SWafer.enumPosition ePosition, bool bAdapter = false)//0~399
        {
            int nValue = -1;
            enumRbtAddress eRbtAddress;
            switch (ePosition)
            {
                case SWafer.enumPosition.Loader1: eRbtAddress = bAdapter ? enumRbtAddress.STG1_08 : enumRbtAddress.STG1_12; break;
                case SWafer.enumPosition.Loader2: eRbtAddress = bAdapter ? enumRbtAddress.STG2_08 : enumRbtAddress.STG2_12; break;
                case SWafer.enumPosition.Loader3: eRbtAddress = bAdapter ? enumRbtAddress.STG3_08 : enumRbtAddress.STG3_12; break;
                case SWafer.enumPosition.Loader4: eRbtAddress = bAdapter ? enumRbtAddress.STG4_08 : enumRbtAddress.STG4_12; break;
                case SWafer.enumPosition.Loader5: eRbtAddress = bAdapter ? enumRbtAddress.STG5_08 : enumRbtAddress.STG5_12; break;
                case SWafer.enumPosition.Loader6: eRbtAddress = bAdapter ? enumRbtAddress.STG6_08 : enumRbtAddress.STG6_12; break;
                case SWafer.enumPosition.Loader7: eRbtAddress = bAdapter ? enumRbtAddress.STG7_08 : enumRbtAddress.STG7_12; break;
                case SWafer.enumPosition.Loader8: eRbtAddress = bAdapter ? enumRbtAddress.STG8_08 : enumRbtAddress.STG8_12; break;
                case SWafer.enumPosition.AlignerA: eRbtAddress = enumRbtAddress.ALN1; break;
                case SWafer.enumPosition.AlignerB: eRbtAddress = enumRbtAddress.ALN2; break;
                case SWafer.enumPosition.BufferA: eRbtAddress = enumRbtAddress.BUF1; break;
                case SWafer.enumPosition.BufferB: eRbtAddress = enumRbtAddress.BUF2; break;
                case SWafer.enumPosition.EQM1: eRbtAddress = enumRbtAddress.EQM1; break;
                case SWafer.enumPosition.EQM2: eRbtAddress = enumRbtAddress.EQM2; break;
                case SWafer.enumPosition.EQM3: eRbtAddress = enumRbtAddress.EQM3; break;
                case SWafer.enumPosition.EQM4: eRbtAddress = enumRbtAddress.EQM4; break;
                case SWafer.enumPosition.AOI: eRbtAddress = enumRbtAddress.AOI; break;
                default: return -1;
            }
            nValue = GetDicPosRobot(nBody, eRbtAddress);
            return nValue;
        }

        #endregion
        //---------------------------------------------------------------------------
        #region OCR Recipe 修改
        string m_strPathOCR_Front, m_strPathOCR_Back;
        public void LoadOCRRecipeIniFile()
        {
            m_OCRecipeData_Front.Clear();
            m_OCRecipeData_Back.Clear();

            m_strPathOCR_Front = string.Format("{0}\\OCRecipe\\{1}", Directory.GetCurrentDirectory(), "Front");
            m_strPathOCR_Back = string.Format("{0}\\OCRecipe\\{1}", Directory.GetCurrentDirectory(), "Back");


            for (int n = 0; n < GetOCRRecipeMax; n++)
            {
                string strPathFront = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, n);
                string strPathBack = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, n);
                LoadOCRRecipeIniFile(strPathFront, n);
                LoadOCRRecipeIniFile(strPathBack, n);
            }
        }
        public List<OCRecipeData> GetOCRRecipeIniFile(bool isFront)
        {
            if (isFront)
                return m_OCRecipeData_Front;
            else
                return m_OCRecipeData_Back;

        }
        public void SetRecipeStored(int nItemIndex, int nStored, bool isFront)
        {
            string strPathName;

            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].Stored = nStored;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].Stored = nStored;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Stored", nStored);
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeName(int nItemIndex, string strName, bool isFront)
        {
            string strPathName;
            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].Name = strName;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].Name = strName;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Name", strName);
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeAngle_A(int nItemIndex, double dAngle, bool isFront)
        {
            string strPathName;

            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].Angle_A = dAngle;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].Angle_A = dAngle;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Angle_A", dAngle.ToString());
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeAngle_B(int nItemIndex, double dAngle, bool isFront)
        {
            string strPathName;

            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].Angle_B = dAngle;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].Angle_B = dAngle;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Angle_B", dAngle.ToString());
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeWaferIDLength(int nItemIndex, int nLength, bool isFront)
        {
            string strPathName;
            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].WaferIDLength = nLength;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].WaferIDLength = nLength;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Wafer ID Length", nLength);
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeLotIDFirstPosition(int nItemIndex, int nPosition, bool isFront)
        {
            string strPathName;

            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].LotIDFirstPosition = nPosition;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].LotIDFirstPosition = nPosition;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Lot ID First Position", nPosition);
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeLotIDLength(int nItemIndex, int nLength, bool isFront)
        {

            string strPathName;

            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].LotIDLength = nLength;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].LotIDLength = nLength;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Lot ID Length", nLength);
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        public void SetRecipeWaferNoFirstPosition(int nItemIndex, int nPosition, bool isFront)
        {
            string strPathName;
            if (isFront)
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Front, nItemIndex);
                m_OCRecipeData_Front[nItemIndex].WaferNoFirstPosition = nPosition;
            }
            else
            {
                strPathName = string.Format("{0}\\OCRRec{1:D2}.Ini", m_strPathOCR_Back, nItemIndex);
                m_OCRecipeData_Back[nItemIndex].WaferNoFirstPosition = nPosition;
            }

            lock (m_lockINI)
            {
                CINIFile myIni = new CINIFile(strPathName);
                myIni.WriteIni("OCR Recipe", "Wafer No First Position", nPosition);
                string strTime = DateTime.Now.ToString("ddd,MMM,dd,yyyy hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                myIni.WriteIni("OCR Recipe", "Date", strTime);
            }
        }
        #endregion

        //FreeStyle
        public Color ColorTitle { get { return Color.FromArgb(227, 138, 5); } }//深橘
        public Color ColorButton { get { return Color.FromArgb(255, 187, 85); } }//橙
        public Color ColorReadyGreen { get { return FreeStyle ? Color.FromArgb(97, 162, 79) : Color.Green; } }//ready綠
        public Color ColorWaitYellow { get { return FreeStyle ? Color.FromArgb(255, 218, 93)/*Color.Gold*/ : Color.Yellow; } }//waitting黃

        public Color ColorOrange5 { get { return Color.FromArgb(141, 85, 0); } }//深
        public Color ColorOrange4 { get { return Color.FromArgb(182, 109, 0); } }
        public Color ColorOrange3 { get { return Color.FromArgb(227, 138, 5); } }//中
        public Color ColorOrange2 { get { return Color.FromArgb(251, 170, 49); } }
        public Color ColorOrange1 { get { return Color.FromArgb(255, 190, 93); } }
        public Color ColorOrange0 { get { return Color.FromArgb(255, 204, 127); } }//淺


        //---------------------------------------------------------------------------
        Dictionary<string, string> m_DicAllLanguageTranfer = new Dictionary<string, string>();
        public string GetLanguage(string source)
        {
            string target = "";

            switch (GParam.theInst.SystemLanguage)
            {
                case enumSystemLanguage.Default:
                case enumSystemLanguage.zn_TW:
                    {
                        target = source;
                    }
                    break;
                case enumSystemLanguage.zh_CN:
                    {
                        if (m_DicAllLanguageTranfer.ContainsKey(source))
                        {
                            target = m_DicAllLanguageTranfer[source];
                        }
                        else
                        {
                            //LogExecute.theInst.WriteLog("GetLanguageTranslate : " + source);
                            target = source;
                        }
                    }
                    break;
            }

            return target;
        }


    }
}
