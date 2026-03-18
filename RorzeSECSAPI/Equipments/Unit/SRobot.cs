using Rorze.Equipment.Unit;
using System;
using System.Collections.Generic;
using System.Text;
using static Rorze.Equipments.SEFEMType;

namespace Rorze.Equipments.Unit
{
   public class SRobot
   {
        public enum RobotPos {Home=0,Port1,Port2,OCR,Buffer}
        public bool IsRobotMovingFromEQ;
        Dictionary<ArmSelect, SMaterial> _material = new Dictionary<ArmSelect, SMaterial>
        {
            {ArmSelect.UpArm,null },
            {ArmSelect.LowArm,null },
        };
        RobotPos _pos;

        public RobotPos CurrentPos { get { return _pos; }  set { _pos = value; } }
        public Dictionary<ArmSelect, SMaterial> Material { get { return _material; } set { _material = value; } }

       public SRobot()
       {
            _pos = RobotPos.Home;
            IsRobotMovingFromEQ = false;
       }
    }
}
