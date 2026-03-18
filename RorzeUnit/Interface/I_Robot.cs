using RorzeComm;
using RorzeUnit.Class;
using RorzeUnit.Class.RC500;
using RorzeUnit.Class.Robot;
using RorzeUnit.Class.Robot.Enum;
using RorzeUnit.Class.Robot.Event;
using RorzeUnit.Event;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static RorzeUnit.Class.SWafer;

namespace RorzeUnit.Interface
{
    public interface I_Robot
    {
        void Open();

        //Motion method======================================
        void StartManualFunction();
        void ORGN();
        void PAUS();
        void RSTA(int nReset);
        void STOP();
        void MODE(int nMode);
        void STEP(object Axis, int Step);
        void EXTD(object Axis, int Step);
        void SSPD(int nSsped);
        void WMAP(int nStage);
        void GetTeachData(int nStage);
        void SetTeachData(int nStage);
        void GetDMPRData(int nStage);//robot mapping
        void SetDMPRData(int nStage);//robot mapping
        void HOME();
        void GetDAPMData(int nStage);//Alignment
        void SetDAPMData(int nStage);//Alignment

        string GetMappingData { get; }//robot mapping
        string[] GetRac2Data { get; }//robot mapping

        /// <summary>
        /// Robot Home
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="HaveWafer"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStg0to399:0~399</remarks>
        void MoveToStandbyByInterLockW(int nTimeout, bool HaveWafer, enumRobotArms eRobotArms, int nStg0to399, int nSlot);
        void MoveToStandbyByInterLockW_ExtXaxis(int nTimeout, bool HaveWafer, enumPosition ePosition, enumRobotArms eRobotArms, int nStg0to399, int nSlot);
        /// <summary>
        /// Robot Unld
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <param name="WaferData"> Default = null. For gRPC data update. </param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStg0to399:0~399</remarks>
        void PutWaferByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null);
        void PutWaferByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null);
        /// <summary>
        /// Robot Load
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <param name="nCheckTime"></param>
        /// <param name="WaferData"> Default = null. For gRPC data update. </param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStg0to399:0~399</remarks>
        void PutWaferByInterLockClampCheckW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, int nCheckTime, SWafer WaferData = null);
        void PutWaferByInterLockClampCheckW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, int nCheckTime, SWafer WaferData = null);
        /// <summary>
        /// Robot Load
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="eRobotArms"></param>
        /// <param name="nStg0to399"></param>
        /// <param name="nSlot"></param>
        /// <param name="WaferData"> Default = null. For gRPC data update. </param>
        /// <exception cref="SException"></exception>
        /// <remarks>nStg0to399:0~399</remarks>
        void TakeWaferByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null);
        void TakeWaferByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null);

        void TakeWaferExchangeByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot);

        void TwoStepTakeWaferW(int nTimeout, enumRobotArms eRobotArms, int nFrameArm, int nStg0to399, int nSlot);

        void TakeWaferAlignmentByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null);
        void TakeWaferAlignmentByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null);

        void PutWaferAlignmentByInterLockW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot, SWafer WaferData = null);
        void PutWaferAlignmentByInterLockW_ExtXaxis(int nTimeout, enumRobotArms eRobotArms, enumPosition ePosition, int nStg0to399, int nSlot, SWafer WaferData = null);
        void FlipByInterLockW(int nTimeout, int nSide, enumRobotArms eRobotArms, int nStg0to399, int nSlot);

        void ArmExtendShiftWaferW(int nTimeout, enumRobotArms eRobotArms, int nStg0to399, int nSlot);

        //Manual Function=========================================== 

        void OrgnW(int nTimeout);

        void ClmpW(int nTimeout, enumRobotArms armSelect, int nCheckTime = 0);
        void UclmW(int nTimeout, enumRobotArms armSelect, int nCheckTime = 0);

        void ClmpW(int nTimeout, int nVariable);
        void UclmW(int nTimeout, int nVariable);

        void LoadW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0);
        void UnldW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg = 0);

        void ExtdW(int nTimeout, int id, enumRobotArms armSelect, int nStage, int nSlot, int flg = 0);

        void HomeW(int nTimeout, int id, enumRobotArms armSelect, int nStage, int nSlot);
        void AlexW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg, int naf);
        void AlldW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg, int naf);
        void AlulW(int nTimeout, enumRobotArms armSelect, int nStg0to399, int nSlot, int nflg);
        void GaldW(int nTimeout, enumRobotArms armSelect);

        void MoveToStandbyPosW(int nTimeout);
        void MoveToStandbyPosW(int nTimeout, bool HaveWafer, enumRobotArms armSelect, int nStage, int nSlot);
        void MoveToStandbyPosW_Ext_Xaxis(int nTimeout, bool HaveWafer, enumPosition ePosition, enumRobotArms armSelect, int nStage, int nSlot);

        void SspdW(int nTimeout, int nSpeed);
        void ModeW(int nTimeout, int nMode);
        void WmapW(int nTimeout, int nStg0to399);
        void GmapW(int nTimeout, int nStg0to399);//robot mapping
        void Rca2W(int nTimeout, int nVariable);//robot mapping
        void StdtW(int nTimeout, enumRobotDataType eType, int nStage, int nPos, string Value);
        void GtdtW(int nTimeout, enumRobotDataType eType, int nStage);

        void WtdtW(int nTimeout);

        void MtpnW(int nTimeout, int n);//2024.4.10針對TRB2新增控制PIN伸縮,0是縮
        void GtpnW(int nTimeout);//2024.4.10針對TRB2新增詢問PIN狀態

        void ResetProcessCompleted();
        void WaitProcessCompleted(int nTimeout);
        void ResetInPos();
        void WaitInPos(int nTimeout);
        void ResetOrgnSinal();
        void WaitOrgnCompleted(int TimeOut);



        //Fashion=========================================== 
        enumRobotArms GetAvailableArm(enumArmFunction armFunc);

        RobotPos GetCurrePos { get; }
        RobotPos SetCurrePos { set; }


        void AutoProcessStart();
        void AutoProcessEnd();
        void AssignQueue(SWafer wafer);

        //property=========================================== 
        SWafer UpperArmWafer { get; set; }
        SWafer LowerArmWafer { get; set; }

        SWafer PrepareUpperWafer { get; set; }
        SWafer PrepareLowerWafer { get; set; }

        SRR757Axis UpperArm { get; set; }
        SRR757Axis LowerArm { get; set; }
        SRR757Axis Rotater { get; set; }
        SRR757Axis Lifter { get; set; }
        SRR757Axis Lifter2 { get; set; }
        SRR757Axis Traverse { get; set; }
        SSRC560_Motion TBL_560 { get; set; }


        SRR757GPIO GPIO { get; }

        ConcurrentQueue<SWafer> queCommand { get; set; }
        ConcurrentQueue<SWafer> quePreCommand { get; set; }

        object objLockQueue { get; }

        string[] DEQUData { get; set; }
        string[] DTRBData { get; set; }
        string[] DTULData { get; set; }
        string[] DMPRData { get; set; }//robot mapping
        string[] DCFGData { get; set; }
        Dictionary<int, string[]> DAPMData { get; set; }
        int GetAckTimeout { get; }
        int GetMotionTimeout { get; }
        int BodyNo { get; }
        bool Disable { get; }
        bool XaxsDisable { get; }
        bool ExtXaxisDisable { get; }
        bool UseArmSameMovement { get; }
        bool Connected { get; }
        bool IsOrgnComplete { get; }
        bool IsMoving { get; }
        bool ProcessStart { get; }
        bool IsError { get; }
        int GetSpeed { get; }
        enumArmFunction UpperArmFunc { get; }
        enumArmFunction LowerArmFunc { get; }
        int FrameArmBackPulse { get; }
        enumRobotMode StatMode { get; }
        bool PinExtend { get; }//2024.4.10針對NPD TRB2新增
        bool PinSafety { get; }//2024.4.10針對NPD TRB2新增
        bool EnableMap { get; }
        enumDEQU_15_waferSearch MappingType { get; }
        bool EnableUpperAlignment { get; }
        bool EnableLowerAlignment { get; }
        //event=============================================
        event EventHandler<WaferDataEventArgs> OnAssignUpperArmWaferData;
        event EventHandler<WaferDataEventArgs> OnAssignLowerArmWaferData;

        event EventHandler<WaferDataEventArgs> OnLeaveUpperArmWaferData;
        event EventHandler<WaferDataEventArgs> OnLeaveLowerArmWaferData;

        event EventHandler<WaferDataEventArgs> UpperArmWaferChange;  //  手臂上 Wafer 取或放
        event EventHandler<WaferDataEventArgs> LowerArmWaferChange;  //  手臂上 Wafer 取或放

        event EventHandler<LoadUnldEventArgs> OnLoadExchangeComplete;//load finish
        event EventHandler<LoadUnldEventArgs> OnLoadComplete;//load finish
        event EventHandler<LoadUnldEventArgs> OnUnldComplete;//unld finish

        event EventHandler OnProcessStart;
        event EventHandler OnProcessEnd;
        event EventHandler OnProcessAbort;

        event EventHandler<bool> OnManualCompleted;
        event EventHandler<bool> OnORGNComplete;
        event EventHandler<bool> OnPAUSComplete;
        event EventHandler<bool> OnRSTAComplete;
        event EventHandler<bool> OnSTOPComplete;
        event EventHandler<bool> OnMODEComplete;
        event EventHandler<bool> OnJobFunctionCompleted;//Step
        event EventHandler<bool> OnExtdFunctionCompleted;//Absolute
        event EventHandler<bool> OnSSPDComplete;
        event EventHandler<bool> OnWmapFunctionCompleted;//mapping
        event EventHandler<bool> OnGetTeachDataCompleted;
        event EventHandler<bool> OnSetTeachDataCompleted;
        event EventHandler<bool> OnGetDmprDataCompleted;//robot mapping
        event EventHandler<bool> OnSetDmprDataCompleted;////robot mapping
        event EventHandler<bool> OnHOMEComplete;

        event EventHandler<bool> OnGetAlignmentDataCompleted;//Alignment
        event EventHandler<bool> OnSetAlignmentDataCompleted;//Alignment

        event AutoProcessingEventHandler DoManualProcessing;
        event AutoProcessingEventHandler DoAutoProcessing;

        event EventHandler<WaferDataEventArgs> OnWaferStart;
        event EventHandler<WaferDataEventArgs> OnWaferEnd;
        event EventHandler<WaferDataEventArgs> OnWaferMeasureEnd;

        event OccurErrorEventHandler OnOccurStatErr;
        event OccurErrorEventHandler OnOccurCancel;
        event OccurErrorEventHandler OnOccurCustomErr;
        event OccurErrorEventHandler OnOccurErrorRest;

        event MessageEventHandler OnReadData;

        event EventHandler<string> OnNotifyVibration;

        //delegate=============================================Robot 與 Unit 間關係
        void AddInterlock(int nStg0to399, dlgb_o_o_o_o interlock);
        dlgb_v LoadEQ_BeforeOK { get; set; }  //手臂取Wafer前要做的事情
        dlgb_v LoadEQ_AfterOK { get; set; }   //手臂取Wafer後要做的事情
        dlgb_v UnldEQ_BeforeOK { get; set; }  //手臂放Wafer前要做的事情
        dlgb_v UnldEQ_AfterOK { get; set; }   //手臂放Wafer後要做的事情
        dlgb_v GetEQExtendFlag { get; set; }  //true手臂伸入EQ
        dlgv_b SetEQExtendFlag { set; }       //true手臂伸入EQ

        /// <summary>
        /// FromLoader & slot
        /// </summary>
        dlgn_o_n GetFromLoaderStagIndx { get; set; }//獲取stage indx 0~399
        /// <summary>
        /// Position & slot
        /// </summary>
        dlgn_o_n GetPositionStagIndx { get; set; }//獲取stage indx 0~399

        dlgb_Enum DlgPanelMisalign { get; set; }//委派外層
        bool _IsPanelMisalign(enumPosition e);

        //=============================================
        Dictionary<int, string> m_dicCancel { get; }
        Dictionary<int, string> m_dicController { get; }
        Dictionary<int, string> m_dicError { get; }

        //=============================================


        bool RobotHardwareAllow(SWafer.enumFromLoader ePos);
        bool RobotHardwareAllow(SWafer.enumPosition ePos);
        bool RobotHardwareAllowBarcode();


        //搶Align      
        bool GetRunningPermissionForALN(int nBody);
        void ReleaseRunningPermissionForALN(int nBody);
        //搶EQ
        bool GetRunningPermissionForEQ(int nBody);
        void ReleaseRunningPermissionForEQ(int nBody);

        //搶Buffer
        bool GetRunningPermissionForBUF(int nBody);
        void ReleaseRunningPermissionForBUF(int nBody);
        //搶Robot
        bool GetRunningPermissionForStgMap(int nStgBody);
        void ReleaseRunningPermissionForStgMap(int nStgBody);
        bool GetRunningPermissionForStgMap();
        void ReleaseRunningPermissionForStgMap();
        //-------------------------------------------------------------

        I_BarCode Barcode { get; }

        void TriggerSException(enumRobotError eRobotError);
        void Cleanjobschedule();//20240704
    }
}
