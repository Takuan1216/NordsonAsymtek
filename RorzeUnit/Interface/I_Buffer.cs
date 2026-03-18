using RorzeComm;
using RorzeUnit.Class;
using RorzeUnit.Class.Buffer.Enum;
using RorzeUnit.Class.Buffer.Event;
using RorzeUnit.Event;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Interface
{
    public interface I_Buffer
    {
        bool Simulate { get; }
        int BodyNo { get; }
        bool Disable { get; }
        bool ProcessStart { get; }
        SWafer GetWafer(int nIndex);
        void SetWafer(int nIndex, SWafer wafer);
        int GetWaferInSlot(SWafer wafer);

        ConcurrentQueue<SWafer> queCommand { get; set; }
        ConcurrentQueue<SWafer> quePreCommand { get; set; }
        bool IsRobotExtend { get; }
        bool SetRobotExtend { set; }
        int HardwareSlot { get; }
        bool IsWaferDetectOn(int nIndex);

        event EventHandler<WaferDataEventArgs> OnAssignWaferData;
        event EventHandler OnProcessStart;
        event EventHandler OnProcessEnd;
        event EventHandler OnProcessAbort;
        event AutoProcessingEventHandler DoAutoProcessing;

        dlgb_v[] dlgSlotWaferExist { get; set; }
        dlgb_v dlgAroundTrigger { get; set; }
        dlgv_wafer AssignToRobotQueue { get; set; }//丟給robot作排程     
        void AutoProcessStart();
        void AutoProcessEnd();
        void AssignQueue(SWafer wafer);

        int GetEmptySlot();
        int GetWaferCount();
        bool AroundTrigger();
        bool AllowedLoad(int nIndex);
        bool AllowedUnld(int nIndex);
        SWafer.enumWaferSize WaferType { get; }
        bool AnySlotHasWafer();
        void Cleanjobschedule();//20240704
        bool IsSlotDisable(int nIndex);//20240704
    }
}
