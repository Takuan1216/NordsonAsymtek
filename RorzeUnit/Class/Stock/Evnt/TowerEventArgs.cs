using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using static RorzeAPI.Equipments.Combination.SScreen;

namespace RorzeUnit.Class.Stock.Evnt
{
    public class TowerEventArgs
    {
        public delegate void AutoProcessingEventHandler(object sender);
        public class TowerProtoclEventArgs : EventArgs
        {
            public StatFrame Frame { get; set; }
            public TowerProtoclEventArgs(string frame)
            {
                Frame = new StatFrame(frame);
            }
        }
        public class StatFrame
        {
            private string _strFrame;   //aSTK1.STAT:00000/0000

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
        public class TowerGPIO
        {
            private string _strPi;
            private string _strPo;
            private long _nPi;
            private long _nPo;

            public enum enumTowerDI
            {
                Zaxis_ORG = 0,
                Zaxis_RLS,
                Zaxis_FLS,
                N2_Source_Pressure,
                XCDA_Source_Pressure,
                Adj_Pre_1,
                Adj_Pre_2,
                Raxis_ORG,

                Raxis_FLS,
                Raxis_RLS,
                Robot_Check,
                Adj_Pre_3,
                Adj_Pre_4,
                Z_SF_POS,
                Reserve_14,
                DoorOpen,

                Z_Warfer_Mapping,
                Z_Warpage,
                Z_IRON_Mapping,
                L_CLAMP_1,//安全
                L_CLAMP_2,//夾
                R_CLAMP_1,//安全
                R_CLAMP_2,//夾
                L_UD_CY_1,

                L_UD_CY_2,
                R_UD_CY_1,
                R_UD_CY_2,
                Z_Warpage_2,//wafer exist  On: 有片 OFF 空片
                IonizerError,
                CST_Tilt_L,//檢查鐵框有開啟 On: 有片 OFF: 無片
                CST_Tilt_R,//檢查鐵框有開啟 On: 有片 OFF: 無片
                Reserve_31,

                Slot1_Flow_P1_011,
                Slot1_Flow_P2_011,
                Slot1_Flow_P1_031,
                Slot1_Flow_P2_031,
                Slot1_Flow_P1_051,
                Slot1_Flow_P2_051,
                Slot1_Flow_P1_071,
                Slot1_Flow_P2_071,

                Reserve_40,
                Reserve_41,
                Reserve_42,
                Reserve_43,
                Reserve_44,
                Reserve_45,
                Reserve_46,
                Reserve_47,

                Slot2_Flow_P1_011,
                Slot2_Flow_P2_011,
                Slot2_Flow_P1_031,
                Slot2_Flow_P2_031,
                Slot2_Flow_P1_051,
                Slot2_Flow_P2_051,
                Slot2_Flow_P1_071,
                Slot2_Flow_P2_071,

                Reserve_56,
                Reserve_57,
                Reserve_58,
                Reserve_59,
                Reserve_60,
                Reserve_61,
                Reserve_62,
                Reserve_63,

                Slot3_Flow_P1_011,
                Slot3_Flow_P2_011,
                Slot3_Flow_P1_031,
                Slot3_Flow_P2_031,
                Slot3_Flow_P1_051,
                Slot3_Flow_P2_051,
                Slot3_Flow_P1_071,
                Slot3_Flow_P2_071,

                Reserve_72,
                Reserve_73,
                Reserve_74,
                Reserve_75,
                Reserve_76,
                Reserve_77,
                Reserve_78,
                Reserve_79,

                Slot4_Flow_P1_011,
                Slot4_Flow_P2_011,
                Slot4_Flow_P1_031,
                Slot4_Flow_P2_031,
                Slot4_Flow_P1_051,
                Slot4_Flow_P2_051,
                Slot4_Flow_P1_071,
                Slot4_Flow_P2_071,

                Reserve_88,
                Reserve_89,
                Reserve_90,
                Reserve_91,
                Reserve_92,
                Reserve_93,
                Reserve_94,
                Reserve_95,

                Slot5_Flow_P1_011,
                Slot5_Flow_P2_011,
                Slot5_Flow_P1_031,
                Slot5_Flow_P2_031,
                Slot5_Flow_P1_051,
                Slot5_Flow_P2_051,
                Slot5_Flow_P1_071,
                Slot5_Flow_P2_071,

                Reserve_104,
                Reserve_105,
                Reserve_106,
                Reserve_107,
                Reserve_108,
                Reserve_109,
                Reserve_110,
                Reserve_111,

                Slot6_Flow_P1_011,
                Slot6_Flow_P2_011,
                Slot6_Flow_P1_031,
                Slot6_Flow_P2_031,
                Slot6_Flow_P1_051,
                Slot6_Flow_P2_051,
                Slot6_Flow_P1_071,
                Slot6_Flow_P2_071,

                Reserve_120,
                Reserve_121,
                Reserve_122,
                Reserve_123,
                Reserve_124,
                Reserve_125,
                Reserve_126,
                Reserve_127,

                Slot7_Flow_P1_011,
                Slot7_Flow_P2_011,
                Slot7_Flow_P1_031,
                Slot7_Flow_P2_031,
                Slot7_Flow_P1_051,
                Slot7_Flow_P2_051,
                Slot7_Flow_P1_071,
                Slot7_Flow_P2_071,

                Reserve_136,
                Reserve_137,
                Reserve_138,
                Reserve_139,
                Reserve_140,
                Reserve_141,
                Reserve_142,
                Reserve_143,

                Slot8_Flow_P1_011,
                Slot8_Flow_P2_011,
                Slot8_Flow_P1_031,
                Slot8_Flow_P2_031,
                Slot8_Flow_P1_051,
                Slot8_Flow_P2_051,
                Slot8_Flow_P1_071,
                Slot8_Flow_P2_071,

                Reserve_152,
                Reserve_153,
                Reserve_154,
                Reserve_155,
                Reserve_156,
                Reserve_157,
                Reserve_158,
                Reserve_159,

                Protrude_UP_T11,
                Protrude_UP_T12,
                Protrude_UP_T13,
                Protrude_UP_T21,
                Protrude_UP_T22,
                Protrude_UP_T23,
                Protrude_UP_T31,
                Protrude_UP_T32,
                Protrude_UP_T33,
                Protrude_UP_T41,
                Protrude_UP_T42,
                Protrude_UP_T43,

                Reserve_172,
                Reserve_173,
                Reserve_174,
                Reserve_175,

                Protrude_Low_T11,
                Protrude_Low_T12,
                Protrude_Low_T13,
                Protrude_Low_T21,
                Protrude_Low_T22,
                Protrude_Low_T23,
                Protrude_Low_T31,
                Protrude_Low_T32,
                Protrude_Low_T33,
                Protrude_Low_T41,
                Protrude_Low_T42,
                Protrude_Low_T43,

                Reserve_188,
                Reserve_189,
                Reserve_190,
                Reserve_191,
            }
            public enum enumTowerDO
            {
                Reserve_00,
                Reserve_01,
                Reserve_02,
                Reserve_03,
                Reserve_04,
                Reserve_05,
                Reserve_06,
                Reserve_07,

                Reserve_08,
                Reserve_09,
                Reserve_10,
                Reserve_11,
                Reserve_12,
                Reserve_13,
                Reserve_14,
                DoorOpenUnlock,

                OpenerUclm,
                OpenerClmp,
                Reserve_18,
                Reserve_19,
                Reserve_20,
                Reserve_21,
                Reserve_22,
                Reserve_23,

                Reserve_24,
                Reserve_25,
                Reserve_26,
                Reserve_27,
                Reserve_28,
                Reserve_29,
                Reserve_30,
                Reserve_31,

                Slot1Valve11,
                Slot1Valve31,
                Slot1Valve51,
                Slot1Valve71,
                Reserve_36,
                Reserve_37,
                Reserve_38,
                Reserve_39,

                Reserve_40,
                Reserve_41,
                Reserve_42,
                Reserve_43,
                Reserve_44,
                Reserve_45,
                Reserve_46,
                Reserve_47,

                Slot2Valve11,
                Slot2Valve31,
                Slot2Valve51,
                Slot2Valve71,
                Reserve_52,
                Reserve_53,
                Reserve_54,
                Reserve_55,

                Reserve_56,
                Reserve_57,
                Reserve_58,
                Reserve_59,
                Reserve_60,
                Reserve_61,
                Reserve_62,
                Reserve_63,


                Slot3Valve11,
                Slot3Valve31,
                Slot3Valve51,
                Slot3Valve71,
                Reserve_68,
                Reserve_69,
                Reserve_70,
                Reserve_71,

                Reserve_72,
                Reserve_73,
                Reserve_74,
                Reserve_75,
                Reserve_76,
                Reserve_77,
                Reserve_78,
                Reserve_79,

                Slot4Valve11,
                Slot4Valve31,
                Slot4Valve51,
                Slot4Valve71,
                Reserve_84,
                Reserve_85,
                Reserve_86,
                Reserve_87,

                Reserve_88,
                Reserve_89,
                Reserve_90,
                Reserve_91,
                Reserve_92,
                Reserve_93,
                Reserve_94,
                Reserve_95,

                Slot5Valve11,
                Slot5Valve31,
                Slot5Valve51,
                Slot5Valve71,
                Reserve_100,
                Reserve_101,
                Reserve_102,
                Reserve_103,

                Reserve_104,
                Reserve_105,
                Reserve_106,
                Reserve_107,
                Reserve_108,
                Reserve_109,
                Reserve_110,
                Reserve_111,

                Slot6Valve11,
                Slot6Valve31,
                Slot6Valve51,
                Slot6Valve71,
                Reserve_116,
                Reserve_117,
                Reserve_118,
                Reserve_119,

                Reserve_120,
                Reserve_121,
                Reserve_122,
                Reserve_123,
                Reserve_124,
                Reserve_125,
                Reserve_126,
                Reserve_127,

                Slot7Valve11,
                Slot7Valve31,
                Slot7Valve51,
                Slot7Valve71,
                Reserve_132,
                Reserve_133,
                Reserve_134,
                Reserve_135,

                Reserve_136,
                Reserve_137,
                Reserve_138,
                Reserve_139,
                Reserve_140,
                Reserve_141,
                Reserve_142,
                Reserve_143,

                Slot8Valve11,
                Slot8Valve31,
                Slot8Valve51,
                Slot8Valve71,
                Reserve_148,
                Reserve_149,
                Reserve_150,
                Reserve_151,

                Reserve_152,
                Reserve_153,
                Reserve_154,
                Reserve_155,
                Reserve_156,
                Reserve_157,
                Reserve_158,
                Reserve_159,

                Reserve_160,
                Reserve_161,
                Reserve_162,
                Reserve_163,
                Reserve_164,
                Reserve_165,
                Reserve_166,
                Reserve_167,

                Reserve_168,
                Reserve_169,
                Reserve_170,
                Reserve_171,
                Reserve_172,
                Reserve_173,
                Reserve_174,
                Reserve_175,

                Reserve_176,
                Reserve_177,
                Reserve_178,
                Reserve_179,
                Reserve_180,
                Reserve_181,
                Reserve_182,
                Reserve_183,

                Reserve_184,
                Reserve_185,
                Reserve_186,
                Reserve_187,
                Reserve_188,
                Reserve_189,
                Reserve_190,
                Reserve_191,
            }

            Dictionary<enumTowerDI, bool> _DIList = new Dictionary<enumTowerDI, bool>();

            Dictionary<enumTowerDO, bool> _DOList = new Dictionary<enumTowerDO, bool>();

            //DO
            public Dictionary<enumTowerDI, bool> GetDIList { get { return _DIList; } }
            //DI
            public Dictionary<enumTowerDO, bool> GetDOList { get { return _DOList; } }

            private bool isBitOn(Int64 nValue, int nBit)
            {
                Int64 nOne = 1;
                Int64 nV = nOne << nBit;
                return ((nValue & nV) == nV);
            }

            public TowerGPIO(string Pi, string Po)
            {
                _strPi = Pi;
                _strPo = Po;
                //_nPi = Convert.ToInt64(_strPi, 16);
                //_nPo = Convert.ToInt64(_strPo, 16);

                //for (int i = 0; i < 64; i++)
                //{
                //    if (_strPi.Length * 4 > i)//Loadport io lenght 8(32bit) or 16(64bit)
                //    {
                //        _DIList.Add((enumTowerDI)i, isBitOn(_nPi, i));
                //        _DOList.Add((enumTowerDO)i, isBitOn(_nPo, i));
                //    }
                //    else
                //    {
                //        _DIList.Add((enumTowerDI)i, false);
                //        _DOList.Add((enumTowerDO)i, false);
                //    }
                //}


                foreach (enumTowerDI item in System.Enum.GetValues(typeof(enumTowerDI)))
                {
                    int nIndx = (int)item;
                    int nCharIndx = nIndx / 4;
                    int nBitIndx = nIndx % 4;
                    int nStrLength = _strPi.Length;
                    string strChar = _strPi.Substring(nStrLength - nCharIndx - 1, 1);
                    long n = Convert.ToInt64(strChar, 16);
                    _DIList[item] = isBitOn(n, nBitIndx);

                }
                foreach (enumTowerDO item in System.Enum.GetValues(typeof(enumTowerDO)))
                {
                    int nIndx = (int)item;
                    int nCharIndx = nIndx / 4;
                    int nBitIndx = nIndx % 4;
                    int nStrLength = _strPo.Length;
                    string strChar = _strPo.Substring(nStrLength - nCharIndx - 1, 1);
                    long n = Convert.ToInt64(strChar, 16);
                    _DOList[item] = isBitOn(n, nBitIndx);
                }


            }
        }
        public class TowerGMAP_EventArgs : EventArgs
        {
            public TowerGMAP_EventArgs(int nFaceIndx, string strMappingData)
            {
                FaceIndx = nFaceIndx;
                MappingData = strMappingData;
            }
            public int FaceIndx { get; set; }
            public string MappingData { get; set; }

        }
    }
}
