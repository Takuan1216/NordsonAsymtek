using RorzeComm;
using RorzeUnit.Class;
using RorzeUnit.Class.Stock.Enum;
using RorzeUnit.Event;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static RorzeUnit.Class.Stock.Evnt.TowerEventArgs;

namespace RorzeUnit.Interface
{
    public interface I_Stock
    {
        bool Connected { get; }
        bool Disable { get; }
        int BodyNo { get; }
        int StageNo { get; }
        Dictionary<int, List<SWafer>> WaferData { get; }

        /// <summary>
        /// Tower1to16,Slot1to200/400
        /// </summary>
        /// <param name="nTower1to16">Tower:1~16</param>
        /// <param name="nTowerSlot">Slot:1~200/400</param>
        /// <returns>WaferData</returns>
        SWafer GetWafer(int nTower1to16, int nTowerSlot);
        /// <summary>
        /// Stge1to400,Slot1to25
        /// </summary>
        /// <param name="nStg1to400">Stage:1~400</param>
        /// <param name="nSlot1to25">Slot:1~25</param>
        /// <returns>WaferData</returns>
        SWafer GetWaferByRobotStgSlot(int nStg1to400, int nSlot1to25);
        /// <summary>
        /// Slot:1~800/1600
        /// </summary>
        /// <param name="nStockSlot">Slot1to800/1600</param>
        /// <returns>WaferData</returns>
        SWafer GetWaferByStockSlot(int nStockSlot);
        /// <summary>
        /// Transfer Wafer Count
        /// </summary>
        /// <param name="nTower1to16">Tower:1~16</param>
        /// <returns></returns>
        int GetTransferWaferCount(int nTower1to16);
        /// <summary>
        /// GMAP result 4*200/400
        /// </summary>
        /// <returns>1111....1111...1111(800/1600)</returns>
        string GetMapDataAll();
        /// <summary>
        /// GMAP result 200/400
        /// </summary>
        /// <returns>1111....1111...1111(200/400)</returns>
        string GetMapDataOneTower(int nFaceIndx);
        int GetMapDataWaferCount();
        void SetMapDataTower(int nFaceIndx, string str);
        /// <summary>
        /// 設定那些位置被鎖定傳送中
        /// </summary>
        /// <param name="nFaceIndx"></param>
        /// <param name="nSlot1to400"></param>
        void SetSlotOrder(int nFaceIndx, int nSlot1to400, bool bOrder);

        bool IsTowerSlotOrder(int nTower1to16, int nSlot1to400);


        event EventHandler<bool> OnManualCompleted;
        event EventHandler<bool> OnORGNComplete;
        event EventHandler<bool> OnGetDataComplete;
        //event EventHandler<bool> OnLOADComplete;
        event EventHandler<bool> OnHOMEComplete;
        event EventHandler<TowerGMAP_EventArgs> OnMappingComplete;
        event EventHandler<string> OnMappingCompleteAll;
        event EventHandler OnMappingError;

        event EventHandler OnProcessStart;
        event EventHandler OnProcessEnd;
        event EventHandler OnProcessAbort;

        event AutoProcessingEventHandler DoManualProcessing;
        event AutoProcessingEventHandler DoAutoProcessing;

        event MessageEventHandler OnReadData;

        event OccurErrorEventHandler OnOccurStatErr;
        event OccurErrorEventHandler OnOccurCancel;
        event OccurErrorEventHandler OnOccurCustomErr;
        event OccurErrorEventHandler OnOccurErrorRest;
        event OccurErrorEventHandler OnOccurWarning;
        event OccurErrorEventHandler OnOccurWarningRest;

        /// <summary>
        /// wafer從tower中取出，通知外部更改UI
        /// </summary>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        event dlgv_n_n OnTakeWaferOutFoup;//wafer從foup中被取出
        /// <summary>
        /// wafer被放回tower，通知外部更改UI
        /// </summary>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        event dlgv_n_n OnTakeWaferInFoup;

        dlgv_wafer AssignToRobotQueue { get; set; }   //丟給robot作排程

        /// <summary>
        /// BodyNo:1->Tower01,Tower02,Tower03,Tower04
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns>Tower01,Tower02,Tower03,Tower04</returns>
        string TowerName(int nIndex);
        /// <summary>
        /// Robot take wafer out
        /// </summary>
        /// <param name="nStge">stage number 1~400</param>
        /// <param name="nSlot">slot number 1~25</param>
        /// <returns>nStge:1~400, slot:1~25</returns>
        SWafer TakeWaferOut(int nStge, int nSlot);
        /// <summary>
        /// Robot take wafer out.
        /// </summary>
        /// <param name="nFaceIndx">index 0,1,2,3</param>
        /// <param name="nTowerSlot">slot number 1~200/400</param>
        /// <returns>wafer data</returns>
        /// <remarks>index:0~3, slot:1~200/400</remarks>
        SWafer TakeWaferOut2(int nFaceIndx, int nTowerSlot);
        /// <summary>
        /// Robot take wafer in.
        /// </summary>
        /// <param name="nStge">stage number 1~400</param>
        /// <param name="nSlot">slot number 1~25</param>
        /// <param name="wafer">wafer data</param>
        /// <remarks>nStge:1~400, slot:1~25</remarks>
        void TakeWaferIn(int nStge, int nSlot, SWafer wafer);






        bool IsRobotExtend { get; }
        bool SetRobotExtend { set; }
        int GetAckTimeout { get; }//Timeout
        int GetMotionTimeout { get; }//Timeout

        void AutoProcessStart();
        void AutoProcessEnd();

        enumStateMachine StatusMachine { get; set; }
        void SetCJID(int nIndx, string strCJID);
        string GetCJID(int nIndx);

        void StartManualFunction();
        void ORGN();
        void RSTA(int nNum);
        void WMAP();
        //void LOAD(int nStg, int nSlot);
        void HOME(int nStg);
        void Close();

        int TowerCount { get; }
        bool TowerEnable(int nIndx);
        int TheTowerSlotNumber { get; }
        int TheTowerStgeNumber { get; }
        void ResetInPos();
        void WaitInPos(int nTimeout);
        void ResetProcessCompleted();
        void WaitProcessCompleted(int nTimeout);


        void OrgnW(int nTimeout, int nVariable = 0);
        void HomeW(int nTimeout, int nStg, int nSlot, int nFlag);
        void HomeForTowerSlotW(int nTimeout, int nFaceIndx, int nTowerSlot1to400);
        void HomeForTowerTopW(int nTimeout, int nFaceIndx, int nTowerSlot1to400);
        void LoadW(int nTimeout, int nStg, int nSlot, int nFlag);//Opens the shutter 
        void LoadForTowerSlotW(int nTimeout, int nFaceIndx, int nTowerSlot1to400);
        void UnldW(int nTimeout, int nFlg);//Closes the shutter.
        void ClmpW(int nTimeout);//Opens the bar for retaining the shutter.
        void UclmW(int nTimeout);//Contains the bar for retaining the shutter.       
        void SpotW(int nTimeout, int nBit, bool bOn);

        void ResetW(int nTimeout, int nReset = 0);

        void GposW(int nTimeout);


        bool IsAreaSesorTrigger();//interlock
        bool IsWaferExist();//interlock
        bool IsOpenDetectFrame();//interlock
        bool IsOpenZaxisDownSensor();//interlock

        SWafer.enumWaferSize WaferType { get; }

        enumTowerStatus InPos { get; }
        bool IsMoving { get; }
        bool IsError { get; }
        bool PassPurge { get; set; }
        Dictionary<int, string> m_dicCancel { get; }
        Dictionary<int, string> m_dicController { get; }
        Dictionary<int, string> m_dicError { get; }

        TowerGPIO GPIO { get; }

        int GetRaxisPos { get; }
        int GetAaxisPos { get; }
        int GetClampPos { get; }
        int GetZaxisPos { get; }


        bool IsClampOpenSlotNotSafety { get; }

        List<SWafer> Getjobschedule();


        void Addjobschedule(SWafer Wafer);


        void deletejobschedule(SWafer Wafer);


        void Cleanjobschedule();

        int GetStockerTotalSlot();

    }
}

