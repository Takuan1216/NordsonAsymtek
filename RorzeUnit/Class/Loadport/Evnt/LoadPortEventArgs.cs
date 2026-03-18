using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Type;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Loadport.Event
{
    public delegate void AutoProcessingEventHandler(object sender);


    public class LoadPortEventArgs : EventArgs
    {
        public LoadPortEventArgs(string strMappingData, int nBodyNo, bool bSucceed)
        {
            MappingData = strMappingData;
            BodyNo = nBodyNo;
            Succeed = bSucceed;
        }
        public string MappingData { get; set; }
        public int BodyNo { get; set; }
        public bool Succeed { get; set; }
    }



    public delegate void SlotEventHandler(object sender, SlotEventArgs e);
    public class SlotEventArgs : EventArgs
    {
        public SlotEventArgs(int slot) { Slot = slot; }
        public int Slot { get; set; }
    }


    #region FOUP 有無
    public delegate void FoupExistChangEventHandler(object sender, FoupExisteChangEventArgs e);
    public class FoupExisteChangEventArgs : EventArgs
    {
        public bool FoupExist;
        public FoupExisteChangEventArgs(bool OnFoup)
        {
            FoupExist = OnFoup;
        }
    }
    #endregion

    #region STG 狀態
    public delegate void OccurStateMachineChangEventHandler(object sender, OccurStateMachineChangEventArgs e);
    public class OccurStateMachineChangEventArgs : EventArgs
    {
        public enumStateMachine StatusMachine { get; }
        public OccurStateMachineChangEventArgs(enumStateMachine _StatusMachine)
        {
            StatusMachine = _StatusMachine;
        }
    }
    #endregion


    public class LoadPortGPIO
    {
        private string _strPi;
        private string _strPo;
        private long _nPi;
        private long _nPo;

        public enum LoadPortDI
        {
            _DIPreParationCmplet = 0,
            _DITemprorarilyStop,
            _DISignificanterror,
            _DILighterror,
            _DIExhaustFan1,
            _DIExhaustFan2,
            _DIProtrusion,
            _DINotSignalbit7,

            _DIFoupdoorleftclose,
            _DIFoupdoorleftopen,
            _DIFoupdoorrightclose,
            _DIFoupdoorrightopen,
            _DIMappingSensorcontaining,
            _DIMappingSensorpreparation,
            _DIUpperPressurelimit,
            _DILowerPressurelimit,

            _DICarrierClampOpen,
            _DICarrierClampClose,
            _DIPresenceleft,
            _DIPresenceright,
            _DIPresencemiddle,
            _DIinfopadA,
            _DIinfopadB,
            _DIinfopadC,

            _DIinfoPadD,
            _DIPresence,
            _DIFOSBidentificationsensor,
            _DIObstacledetectingsensor,
            _DIDoordetection,
            _DINotSignalbit29,
            _DIOpenCarrierdetectingSensor,
            _DINotSignalbit31,

            _DIStagerotarionBack,
            _DIStagerotarionFront,
            _DIBCRlifting,
            _DIBCRlowering,
            _DINotSignalbit36,
            _DINotSignalbit37,
            _DICarrierrotarionlowering,
            _DICarrierrotarionlifting,

            _DIExternalSW1,
            _DIExternalSW2,
            _DIExternalSW3,
            _DINotSignalbit43,
            _DINotSignalbit44,
            _DINotSignalbit45,
            _DIPFAL,
            _DIPFAR,

            _DI300mmDSC,
            _DI200mmDSC,
            _DI150mmDSC,
            _DICommon,
            _DI200mm,
            _DI150mm,
            _DIAdapter,
            _DINotSignalbit55,

            _DIValID,
            _DICS_0,
            _DICS_1,
            _DINotSignalbit56,
            _DITR_REQ,
            _DIBUSY,
            _DICOMPT,
            _DICONT,
        }
        public enum LoadPortDO
        {
            _DOPreParationCmplet = 0,
            _DOTemprorarilyStop,
            _DOSignificanterror,
            _DOLighterror,
            _DONotSignalbit4,
            _DONotSignalbit5,
            _DOAdapterPower,
            _DOObstacledetectioncanecl,

            _DONotSignalbit8,
            _DONotSignalbit9,
            _DOCarrierClmpClose,
            _DOCarrierClmpOpen,
            _DOFoupdoorlockopen,
            _DOFoupdoorlockclose,
            _DONotSignalbit14,
            _DONotSignalbit15,

            _DOMappingSensorpreparation,
            _DOMappingSensorcontaining,
            _DOChuckingON,
            _DOChuckingOFF,
            _DONotSignalbit20,
            _DONotSignalbit21,
            _DONotSignalbit22,
            _DONotSignalbit23,

            _DODoorOpen,
            _DOCarrierClamp,
            _DOCarrierDetecingNo,
            _DOPreparationComplt,
            _DOCarrierProperlyload,
            _DONotSignalbit29,
            _DONotSignalbit30,
            _DONotSignalbit31,

            _DOStagerotarionBack,
            _DOStagerotarionFront,
            _DOBCRlifting,
            _DOBCRlowering,
            _DONotSignalbit36,
            _DONotSignalbit37,
            _DOCarrierrotarionlowering,
            _DOCarrierrotarionlifting,

            _DOExternalSW1,
            _DOExternalSW3,
            _DOLOADLED,
            _DOUNLOADLED,
            _DOPRESENCELED,
            _DOPALCEMENTLED,
            _DOMANUALLED,
            _DOERRORLED,

            _DOCLAMPLED,
            _DODOCKLED,
            _DOBUSYLED,
            _DOAUTOLED,
            _DORESERVEDLED,
            _DONotSignalbit53,
            _DONotSignalbit54,
            _DONotSignalbit55,

            _DOL_REQ,
            _DOU_REQ,
            _DONotSignalbit58,
            _DOREADY,
            _DONotSignalbit60,
            _DONotSignalbit61,
            _DOHOAVBL,
            _DOES,
        }

        Dictionary<LoadPortDI, bool> _DIList = new Dictionary<LoadPortDI, bool>();

        Dictionary<LoadPortDO, bool> _D0List = new Dictionary<LoadPortDO, bool>();
        //DO
        public Dictionary<LoadPortDI, bool> GetDIList { get { return _DIList; } }
        public string GetDIstr { get { return _strPi; } }
        //DI
        public Dictionary<LoadPortDO, bool> GetD0List { get { return _D0List; } }
        public string GetDOstr { get { return _strPo; } }

        private bool isBitOn(Int64 nValue, int nBit)
        {
            Int64 nOne = 1;
            Int64 nV = nOne << nBit;
            return ((nValue & nV) == nV);
        }

        public LoadPortGPIO(string Pi, string Po)
        {
            _strPi = Pi;
            _strPo = Po;
            _nPi = Convert.ToInt64(_strPi, 16);
            _nPo = Convert.ToInt64(_strPo, 16);

            for (int i = 0; i < 64; i++)
            {
                if (_strPi.Length * 4 > i)//Loadport io lenght 8(32bit) or 16(64bit)
                {
                    _DIList.Add((LoadPortDI)i, isBitOn(_nPi, i));
                    _D0List.Add((LoadPortDO)i, isBitOn(_nPo, i));
                }
                else
                {
                    _DIList.Add((LoadPortDI)i, false);
                    _D0List.Add((LoadPortDO)i, false);
                }

            }
        }
    }
    public delegate void RorzenumLoadportIOChangelHandler(object sender, RorzenumLoadportIOChengeEventArgs e);
    public class RorzenumLoadportIOChengeEventArgs : EventArgs
    {
        public LoadPortGPIO Frame { get; set; }
        public RorzenumLoadportIOChengeEventArgs(LoadPortGPIO frame)
        {
            Frame = frame;
        }
    }



    public class LoadPortProtoclEventArgs : EventArgs
    {
        public StatFrame Frame { get; }
        public LoadPortProtoclEventArgs(string frame)
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


    public class LoadPortRC550GPIO
    {
        private string _strPi;
        private string _strPo;
        private long _nPi;
        private long _nPo;

        public enum LoadPortDI
        {
            _DIPlacementSensor1 = 0,
            _DIPlacementSensor2,
            _DIOperatePB,
            _DISignalNotConnected3,
            _DISignalNotConnected4,
            _DISignalNotConnected5,
            _DISignalNotConnected6,
            _DISignalNotConnected7,

            _DIFoupdoorleftclose,
            _DIFoupdoorleftopen,
            _DIFoupdoorrightclose,
            _DIFoupdoorrightopen,
            _DIMappingSensorcontaining,
            _DIMappingSensorpreparation,
            _DIUpperPressurelimit,
            _DILowerPressurelimit,

            _DICarrierClampOpen,
            _DICarrierClampClose,
            _DIPresenceleft,
            _DIPresenceright,
            _DIPresencemiddle,
            _DIinfopadA,
            _DIinfopadB,
            _DIinfopadC,

            _DIinfoPadD,
            _DIPresence,
            _DIFOSBidentificationsensor,
            _DIObstacledetectingsensor,
            _DIDoordetection,
            _DINotSignalbit29,
            _DIOpenCarrierdetectingSensor,
            _DINotSignalbit31,

            _DIStagerotarionBack,
            _DIStagerotarionFront,
            _DIBCRlifting,
            _DIBCRlowering,
            _DINotSignalbit36,
            _DINotSignalbit37,
            _DICarrierrotarionlowering,
            _DICarrierrotarionlifting,

            _DIExternalSW1,
            _DIExternalSW2,
            _DIExternalSW3,
            _DINotSignalbit43,
            _DINotSignalbit44,
            _DINotSignalbit45,
            _DIPFAL,
            _DIPFAR,

            _DI300mmDSC,
            _DI200mmDSC,
            _DI150mmDSC,
            _DICommon,
            _DI200mm,
            _DI150mm,
            _DIAdapter,
            _DINotSignalbit55,

            _DIValID,
            _DICS_0,
            _DICS_1,
            _DINotSignalbit56,
            _DITR_REQ,
            _DIBUSY,
            _DICOMPT,
            _DICONT,
        }

        public enum LoadPortDO
        {
            _DOPreParationCmplet = 0,
            _DOTemprorarilyStop,
            _DOSignificanterror,
            _DOLighterror,
            _DONotSignalbit4,
            _DONotSignalbit5,
            _DOAdapterPower,
            _DOObstacledetectioncanecl,

            _DONotSignalbit8,
            _DONotSignalbit9,
            _DOCarrierClmpClose,
            _DOCarrierClmpOpen,
            _DOFoupdoorlockopen,
            _DOFoupdoorlockclose,
            _DONotSignalbit14,
            _DONotSignalbit15,

            _DOMappingSensorpreparation,
            _DOMappingSensorcontaining,
            _DOChuckingON,
            _DOChuckingOFF,
            _DONotSignalbit20,
            _DONotSignalbit21,
            _DONotSignalbit22,
            _DONotSignalbit23,

            _DODoorOpen,
            _DOCarrierClamp,
            _DOCarrierDetecingNo,
            _DOPreparationComplt,
            _DOCarrierProperlyload,
            _DONotSignalbit29,
            _DONotSignalbit30,
            _DONotSignalbit31,

            _DOStagerotarionBack,
            _DOStagerotarionFront,
            _DOBCRlifting,
            _DOBCRlowering,
            _DONotSignalbit36,
            _DONotSignalbit37,
            _DOCarrierrotarionlowering,
            _DOCarrierrotarionlifting,

            _DOExternalSW1,
            _DOExternalSW3,
            _DOLOADLED,
            _DOUNLOADLED,
            _DOPRESENCELED,
            _DOPALCEMENTLED,
            _DOMANUALLED,
            _DOERRORLED,

            _DOCLAMPLED,
            _DODOCKLED,
            _DOBUSYLED,
            _DOAUTOLED,
            _DORESERVEDLED,
            _DONotSignalbit53,
            _DONotSignalbit54,
            _DONotSignalbit55,

            _DOL_REQ,
            _DOU_REQ,
            _DONotSignalbit58,
            _DOREADY,
            _DONotSignalbit60,
            _DONotSignalbit61,
            _DOHOAVBL,
            _DOES,
        }

        Dictionary<LoadPortDI, bool> _DIList = new Dictionary<LoadPortDI, bool>();

        Dictionary<LoadPortDO, bool> _D0List = new Dictionary<LoadPortDO, bool>();

        //DO
        public Dictionary<LoadPortDI, bool> GetDIList { get { return _DIList; } }
        //DI
        public Dictionary<LoadPortDO, bool> GetD0List { get { return _D0List; } }

        public LoadPortRC550GPIO(string Pi, string Po)
        {
            _strPi = Pi;
            _strPo = Po;
            _nPi = Convert.ToInt64(_strPi, 16);
            _nPo = Convert.ToInt64(_strPo, 16);

            for (int i = 0; i < 64; i++)
            {
                if (i == 0)
                {
                    _DIList.Add((LoadPortDI)i, (_nPi & 1) != 0);
                    _D0List.Add((LoadPortDO)i, (_nPo & 1) != 0);
                }
                else
                {
                    _DIList.Add((LoadPortDI)i, (_nPi & 1 << i) != 0);
                    _D0List.Add((LoadPortDO)i, (_nPo & 1 << i) != 0);
                }
            }
        }
    }
    public delegate void RorzenumLoadportRC550IOChangelHandler(object sender, RorzenumLoadportRC550IOChengeEventArgs e);
    public class RorzenumLoadportRC550IOChengeEventArgs : EventArgs
    {
        public LoadPortRC550GPIO Frame { get; set; }
        public RorzenumLoadportRC550IOChengeEventArgs(LoadPortRC550GPIO frame)
        {
            Frame = frame;
        }
    }







}
