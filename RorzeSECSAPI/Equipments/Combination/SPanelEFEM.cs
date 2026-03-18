using System;
using System.Collections.Generic;
using System.Text;
using RorzeAPI.Equipments;
using Rorze.Equipments;
using Rorze.SocketObject;

using System.Threading;
using System.Linq;
using Rorze.Equipment.Unit;
using RorzeComm.Threading;

namespace RorzeAPI.Equipments.Combination
{
   public class SPanelEFEM :SEFEMType
   {
        string _EFEMName = string.Empty;
        int _portcount;
        
        public string EFEMName { get { return _EFEMName; } }
        public int LoadPortCount { get { return _portcount; } }
        public event EventHandler OnEFEMINIT;
        public enum PanelEFEMCommand
        {
            Putwafer,
            TakeWafer,
            CaneclWafer,
            Stop,
            Max,
        }
        Dictionary<string, SSignal> _signalPanelAck = new Dictionary<string, SSignal>();
        public SPanelEFEM(string Name, SocketControl Control,int LoadPortMax,int RobotArmCount,int AlingerCount,Dictionary<int,string> unitpos):
            base(Name, Control, LoadPortMax, RobotArmCount, AlingerCount, unitpos)
        {
            _EFEMName = Name;
            _portcount = LoadPortMax;
            for (int nCnt = 0; nCnt < (int)PanelEFEMCommand.Max; nCnt++)
                _signalPanelAck.Add(_dicCmdsTable[(PanelEFEMCommand)nCnt], new SSignal(false, EventResetMode.ManualReset));

            this.OnEFENINIT += SPanelEFEM_OnEFENINIT;
        }

        private void SPanelEFEM_OnEFENINIT(object sender, EventArgs e)
        {
            try
            {
                // Reset robot and loadPort Data
                if (this._robot.Material[ArmSelect.LowArm] != null)
                    this._robot.Material[ArmSelect.LowArm] = null;
                if (this._robot.Material[ArmSelect.UpArm] != null)
                    this._robot.Material[ArmSelect.UpArm] = null;

                this._robot.CurrentPos = Rorze.Equipments.Unit.SRobot.RobotPos.Home;
                this._robot.IsRobotMovingFromEQ = false;
                if (OnEFEMINIT != null)
                    OnEFEMINIT(this, new EventArgs());



            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }


        }

        Dictionary<PanelEFEMCommand, string> _dicCmdsTable = new Dictionary<PanelEFEMCommand, string>()
        {
            {PanelEFEMCommand.Putwafer,"PutWafer"},
            {PanelEFEMCommand.TakeWafer,"TakeWafer"},
            {PanelEFEMCommand.CaneclWafer,"CancelPanel"},
            {PanelEFEMCommand.Stop,"Stop" }

        };

        public override bool CheckCmdexist(string Msg)
        {
            if (base.CheckCmdexist(Msg))
                return true;

            foreach (string scmd in _dicCmdsTable.Values) //查字典
            {
                if (Msg.Contains(string.Format("{0}", scmd)))
                    return true;

            }
            return false;
        }

        public override bool ParseCmdMsg(EFEMFrame frame)
        {
            if (base.ParseCmdMsg(frame))
                return true;
            if (!_dicCmdsTable.ContainsValue(frame.Command))
                return false;
            PanelEFEMCommand cmd = PanelEFEMCommand.Max;
            cmd = _dicCmdsTable.FirstOrDefault(x => x.Value == frame.Command).Key;
            switch (cmd)
            {
                case PanelEFEMCommand.Putwafer:
                case PanelEFEMCommand.TakeWafer:
                case PanelEFEMCommand.CaneclWafer:
                case PanelEFEMCommand.Stop:
                    if (frame.Parameter.Split(',')[0] != "0")
                        _signalPanelAck[_dicCmdsTable[cmd]].bAbnormalTerminal = true;
                    _signalPanelAck[_dicCmdsTable[cmd]].Set();
                    return true;


            }
            return false;
        }

        public void PutPanel(string PanelID,bool IsPut)
        {
            int Putaction = (IsPut) ? 0 : 1;
            SendOrder(_dicCmdsTable[PanelEFEMCommand.Putwafer], Putaction.ToString(), PanelID);
            if (!_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.Putwafer]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[PanelEFEMCommand.Putwafer]));
            if (_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.Putwafer]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[PanelEFEMCommand.Putwafer]));

        }
        public void TakePanel(string PanelID, int TargetPort,int TargetSlot)
        {
            
            SendOrder(_dicCmdsTable[PanelEFEMCommand.TakeWafer], PanelID, TargetPort.ToString(), TargetSlot.ToString());
            if (!_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.TakeWafer]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[PanelEFEMCommand.TakeWafer]));
            if (_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.TakeWafer]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[PanelEFEMCommand.TakeWafer]));

        }
        public void CaneclPanel(int Port)
        {
            SendOrder(_dicCmdsTable[PanelEFEMCommand.CaneclWafer], Port.ToString());
            if (!_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.CaneclWafer]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[PanelEFEMCommand.CaneclWafer]));
            if (_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.CaneclWafer]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[PanelEFEMCommand.CaneclWafer]));
        }
        public void StopPanel()
        {
            SendOrder(_dicCmdsTable[PanelEFEMCommand.Stop]);
            if (!_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.Stop]].WaitOne(CMDTimeOut))
                throw new EFEMException(string.Format("{0} is receive time out", _dicCmdsTable[PanelEFEMCommand.Stop]));
            if (_signalPanelAck[_dicCmdsTable[PanelEFEMCommand.Stop]].bAbnormalTerminal)
                throw new EFEMException(string.Format("{0} is Abnormal ack", _dicCmdsTable[PanelEFEMCommand.Stop]));
        }

    }
}
