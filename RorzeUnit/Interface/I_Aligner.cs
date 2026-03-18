using RorzeComm;
using RorzeUnit.Class;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Class.Aligner.Event;
using RorzeUnit.Event;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using static RorzeUnit.Class.SWafer;

namespace RorzeUnit.Interface
{
    public interface I_Aligner
    {
        void Open();
        void StartManualFunction();

        //Motion method======================================
        void ORGN();
        void HOME();
        void CLMP();
        void UCLM();
        void ALGN(int nNum);
        void ALGN1();

        void Rot1EXTD(int nPos);
        void Rot1STEP(int nPos);
        void RSTA(int nNum);
        void INIT();
        void STOP();
        void PAUS();
        void MODE(int nNum);
        void WTDT();
        void SSPD(int nNum);
        void SSIZ(int nNum);
        void GSIZ();

        void ResetChangeModeCompleted();
        void WaitChangeModeCompleted(int nTimeout);
        void ResetOrgnSinal();
        void WaitOrgnCompleted(int TimeOut);
        void ResetInPos();
        void WaitInPos(int nTimeout);

        void AutoProcessStart();
        void AutoProcessEnd();

        ConcurrentQueue<SWafer> queCommand { get; set; }
        ConcurrentQueue<SWafer> quePreCommand { get; set; }


        //=========================================== 
 
        void InitW(int nTimeout);
 
        void OrgnW(int nTimeout, int nOrgn = 0);
        void HomeW(int nTimeout, int nP1 = 0, int nP2 = 0);
        void ClmpW(int nTimeout, bool bCheckVac = true);
        void UclmW(int nTimeout);
        void AlgnW(int nTimeout, int nMode = 1, int nPos = -1);
        void Algn1W(int nTimeout);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nTimeout"></param>
        /// <param name="strPos"></param>
        /// <param name="PhotographMethod">拍照次數，默認一次拍照</param>
        void AlgnDW(int nTimeout, string strPos , int PhotographMethod = 2);

        void RotationExtdW(int nTimeout, int nPos);
        //void RotStepW(int nTimeout, int nPos);

        void GposRW(int nTimeout);
        //property=========================================== 
        SWafer Wafer { get; set; }
        SRA320GPIO GPIO { get; }


        int BodyNo { get; }
        bool Disable { get; }
        bool Connected { get; }

        bool IsOrgnComplete { get; }
        bool IsMoving { get; }
        bool IsError { get; }

        bool AlignmentStart { get; set; }
        bool ProcessStart { get; set; }

        int Xaxispos { get; }
        int Yaxispos { get; }
        int Zaxispos { get; }
        int Raxispos { get; }
        

        void AssignQueue(SWafer wafer);
        void AligCompelet(SWafer wafer);
        bool IsZaxsInBottom();
        dlgv_wafer AssignToRobotQueue { get; set; }//丟給robot作排程

        // void SendWarningMsg(enumLoadPortWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0);


        //=============================================
        event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        event EventHandler<WaferDataEventArgs> Aligner_WaferChange;  //  更新UI
        event EventHandler<WaferDataEventArgs> OnAligCompelet;

        event EventHandler<bool> OnManualCompleted;
        event EventHandler<bool> OnORGNComplete;
        event EventHandler<bool> OnRot1StepComplete;

        event EventHandler OnProcessStart;
        event EventHandler OnProcessEnd;
        event EventHandler OnProcessAbort;

        event MessageEventHandler OnReadData;

        event AutoProcessingEventHandler DoManualProcessing;
        event AutoProcessingEventHandler DoAutoProcessing;

        event OccurErrorEventHandler OnOccurStatErr;
        event OccurErrorEventHandler OnOccurCancel;
        event OccurErrorEventHandler OnOccurCustomErr;
        event OccurErrorEventHandler OnOccurErrorRest;
        event OccurErrorEventHandler OnOccurWarning;
        event OccurErrorEventHandler OnOccurWarningRest;
        //=============================================

        Dictionary<int, string> m_dicCancel { get; }
        Dictionary<int, string> m_dicController { get; }
        Dictionary<int, string> m_dicError { get; }

        bool IsRobotExtend { get; }
        bool SetRobotExtend { set; }
        enumWaferSize WaferType { get; }
        I_BarCode Barcode { get; }

        bool WaferExists();
        bool IsClamp();
        bool IsUnClamp();
        bool IsAirOK();
        bool IsFanOK();

        void SendWarningMsg(enumAlignerWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0);
        void RestWarningMsg(enumAlignerWarning eWarning, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0);
        void Cleanjobschedule();//20240704

        bool IsHoldPermission();//20240809
        bool GetRunningPermission();//20240809
        void ReleaseRunningPermission();//20240809

        //==============================================================================   
        void BarcodeOpen();//20240828
        bool IsBarcodeConnected { get; }//20240828
        bool IsBarcodeEnable { get; }//20240828
        string BarcodeRead();//20240828


        int _AckTimeout { get; }
        int _MotionTimeout { get; }

        bool IsReadyToLoad();

        void TriggerSException(enumAlignerError eError);
      

    }
}
