using RorzeApi.SECSGEM;
using RorzeComm;
using RorzeComm.Log;
using RorzeComm.Threading;
using RorzeUnit.Class;
using RorzeUnit.Class.Loadport;
using RorzeUnit.Class.Loadport.Enum;
using RorzeUnit.Class.Loadport.Event;
using RorzeUnit.Class.Loadport.Type;
using RorzeUnit.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeUnit.Interface
{
    public interface I_Loadport
    {

        #region =========================== property ===========================================
        bool Simulate { get; }
        bool Connected { get; }
        int BodyNo { get; }
        bool Disable { get; }

        bool IsRobotExtend { get; }
        bool SetRobotExtend { set; }

        //STAT S1第1 bit
        enumLoadPortMode StatMode { get; }
        bool IsInitialized { get; }
        //STAT S1第2 bit
        bool IsOrgnComplete { get; }
        //STAT S1第3 bit
        bool IsProcessing { get; }
        //STAT S1第4 bit
        enumLoadPortStatus InPos { get; }
        bool IsMoving { get; }
        //STAT S1第5 bit
        int GetSpeed { get; }
        //STAT S2
        bool IsError { get; }
        int WaferTotal { get; }//Foup內部有幾層，並不是有幾片
        bool UseAdapter { get; }
        bool IsProtrude { get; }
        bool IsPresenceON { get; }
        bool IsPresenceleftON { get; }
        bool IsPresencerightON { get; }
        bool IsPresencemiddleON { get; }
        bool IsDoorOpen { get; }
        bool IsUnclamp { get; }//close 是勾住
        bool IsPSPL_AllOn { get; }
        bool IsPSPL_AllOf { get; }
        enumFoupType eFoupType { get; }//對應到Inforoad 16 組
        string FoupTypeName { get; }
        string[][] GetDPRMData { get; }  //  DPRM      
        string[] GetDMPRData { get; }  //  DMPR
        string[] GetDCSTData { get; } //  DCST
        SWafer.enumWaferSize[] LoadportWaferType { get; }//ini設定infopad對應的晶圓種類
        SWafer.enumWaferSize GetCurrentLoadportWaferType();
        enumStateMachine StatusMachine { get; set; }
        enumLoadPortPos GetYaxispos { get; }
        enumLoadPortPos GetZaxispos { get; }
        CarrierIDStats CarrierIDstatus { get; set; }
        CarrierSlotMapStats SlotMappingStats { get; set; }
        CarrierAccessStats CarrierAccessStats { get; set; }

        CarrierState  CarrierState { get; set; }

        bool FoupExist { get; set; }
        string FoupID { get; set; }
        string CJID { get; set; }
        string ExcutPJID { get; set; }
        int FoupArrivalIdleTimeout { get; set; }
        int FoupWaitTransferTimeout { get; set; }
        bool UndockQueueByHost { get; set; }//docking過程客戶想要退掉，keep住
        string[] GetRac2Data { get; }
        #endregion
        #region =========================== event ==============================================
        event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        event SlotEventHandler OnWaferDataDelete;

        event FoupExistChangEventHandler OnFoupExistChenge;

        event EventHandler<bool> OnORGNComplete;
        event EventHandler<bool> OnGetDataComplete;
        event EventHandler<LoadPortEventArgs> OnJigDockComplete;
        event EventHandler<LoadPortEventArgs> OnClmpComplete;
        event EventHandler<LoadPortEventArgs> OnClmp1Complete;
        event EventHandler<LoadPortEventArgs> OnUclmComplete;
        event EventHandler<LoadPortEventArgs> OnUclm1Complete;
        event EventHandler<LoadPortEventArgs> OnMappingComplete;

        event EventHandler OnProcessStart;
        event EventHandler OnProcessEnd;
        event EventHandler OnProcessAbort;

        event AutoProcessingEventHandler DoManualProcessing;
        event AutoProcessingEventHandler DoAutoProcessing;

        event RorzenumLoadportIOChangelHandler OnIOChange;

        event OccurStateMachineChangEventHandler OnStatusMachineChange;

        event MessageEventHandler OnReadData;

        event OccurErrorEventHandler OnOccurStatErr;
        event OccurErrorEventHandler OnOccurCancel;
        event OccurErrorEventHandler OnOccurCustomErr;
        event OccurErrorEventHandler OnOccurErrorRest;
        event OccurErrorEventHandler OnOccurWarning;
        event OccurErrorEventHandler OnOccurWarningRest;

        event EventHandler OnReadyRunningLot;

        event EventHandler OnFoupIDChange; //  更新UI

        event EventHandler<string> OnFoupTypeChange;//  更新UI

        event dlgv_n OnTakeWaferOutFoup;           //wafer從foup中被取出
        event dlgv_n OnTakeWaferInFoup;            //wafer被放回foup

        // ================= Simulate =================
        event EventHandler OnSimulateCLMP;
        event EventHandler OnSimulateUCLM;
        event EventHandler OnSimulateMapping;
        #endregion
        #region =========================== thread =============================================

        #endregion
        #region =========================== delegate ===========================================
        dlgb_Object dlgLoadInterlock { get; set; }                // 不可以load
        dlgb_Object dlgUnloadInterlock { get; set; }              // 不可以unload
        dlgv_wafer AssignToRobotQueue { get; set; }  //丟給robot作排程
        #endregion
        #region =========================== wafer ==============================================
        string MappingData { get; set; }
        string SimulateMappingData { set; }
        List<SWafer> Waferlist { get; }
        SWafer TakeWaferOutFoup(int nIndex);
        void TakeWaferInFoup(int nIndex, SWafer wafer);
        void TakeWaferSlotExchange(int nSlot1, int nSlot2);
        void SetWaferIDCompare(int nSlot, string WaferID_F, string WaferID_B);
        #endregion
        #region =========================== e84 ================================================    
        I_E84 E84Object { get; set; }
        E84PortStates E84Status { get; set; }
        bool IsE84Auto { get; }
        void SetE84AutoMode(bool bAuto);
        void SetE84TPtime(int[] nTime);
        #endregion

        void Open();
        void Close();
        //==============================================================================
        #region AutoProcess
        #region Run貨權
        bool IsRunning { get; }
        bool GetRunningPermission();
        void ReleaseRunningPermission();
        #endregion
        void AutoProcessStart();
        void AutoProcessEnd();
        List<SWafer> Getjobschedule();
        void Addjobschedule(SWafer Wafer);
        void deletejobschedule(SWafer Wafer);
        void Cleanjobschedule();
        bool AssignWafer(string strLotID, int nSlot, SWafer wafer);
        #endregion
        //==============================================================================
        #region OneThread 
        void INIT();
        void ORGN();
        void CLMP(bool NeedCheckFoupType = false);
        void CLMP1();
        void UCLM();
        void UCLM1();
        void WMAP();
        void RSTA(int nNum);
        void JigDock();
        void GetData();
        void CheckFoupExist();
        #endregion
        //==============================================================================
        void OrgnW(int nTimeout, int nVariable = 0);
        void ClmpW(int nTimeout, int nVariable = 0);
        void UclmW(int nTimeout, int nVariable = 0);
        void WmapW(int nTimeout);
        void LoadW(int nTimeout);
        void UnldW(int nTimeout);
        void EventW(int nTimeout);
        void ResetW(int nTimeout, int nReset = 0);
        void InitW(int nTimeout);
        void StopW(int nTimeout);
        void PausW(int nTimeout);
        void ModeW(int nTimeout, int nMode);
        void WtdtW(int nTimeout);
        void SspdW(int nTimeout, int nVariable);
        void SpotW(int nTimeout, int nBit, bool bOn);
        void StatW(int nTimeout);
        void GpioW(int nTimeout);
        void GmapW(int nTimeout);
        void Rca2W(int nTimeout, int nVariable);
        void GverW(int nTimeout);
        void StimW(int nTimeout);

        void GposW(int nTimeout);
        void GwidW(int nTimeout);
        void SwidW(int nTimeout, string strId);
        void YaxStepW(int nTimeout, string strStep);
        void ZaxStepW(int nTimeout, string strStep);
        void YaxHomeW(int nTimeout, int nHome);
        void ZaxHomeW(int nTimeout, int nHome);
        void GetDprmW(int nTimeout, int p1);
        void SetDprmW(int nTimeout, int p1, string strData);
        void GetDmprW(int nTimeout);
        void SetDmprW(int nTimeout, int p1, string strData);
        void GetDcstW(int nTimeout);
        void SetDCSTW(int nTimeout, string strData);
        //==============================================================================
        void ResetChangeModeCompleted();
        void WaitChangeModeCompleted(int nTimeout);
        void ResetProcessCompleted();
        void WaitProcessCompleted(int nTimeout);
        void ResetInPos();
        void WaitInPos(int nTimeout);

        //==============================================================================     
        #region =========================== CommandTable =======================================

        #endregion 
        #region =========================== Signals ============================================

        #endregion
        #region =========================== OnOccurError =======================================     
        //  發生警告
        void SendWarningMsg(enumLoadPortWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0);
        //  解除警告
        void RestWarningMsg(enumLoadPortWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0);
        void TriggerSException(enumLoadPortError eAlarm, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0);
        #endregion
        #region =========================== CreateMessage ======================================
        Dictionary<int, string> m_dicCancel { get; }
        Dictionary<int, string> m_dicController { get; }
        Dictionary<int, string> m_dicError { get; }
        #endregion

        #region 20220103新增 客戶要勾住Adapter        
        bool IsKeepClamp { get; }
        void KeepClamp(bool bClamp);
        #endregion

        void UpdateInfoPadEnable(List<bool> InfoPadList);
        bool IsInfoPadEnable();
        void UpdateTrbMapInfoEnable(List<bool> enableList);
        bool IsInfoPadTrbMapEnable();




        void SetFoupExistChenge();
        void SimulateFoupOn(bool bOn);

        //==============================================================================   
        void BarcodeOpen();//20240828
        bool IsBarcodeConnected { get; }//20240828
        bool IsBarcodeEnable { get; }//20240828
        string BarcodeRead();//20240828

        void ClmpW_WithoutMapping(int nTimeout, int nVariable = 0);
    }
}
