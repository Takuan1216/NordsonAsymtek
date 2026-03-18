using Rorze.Equipment.Unit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rorze.Equipments.Unit
{
   public class SUnitModel
   {
        public enum eSubStrateLoation { UNOCCUPIED = 0, OCCUPIED = 1 }
        eSubStrateLoation _subStrateLoationStats;
        string _unitname;
        // Dictionary<int, SMaterial> _materiallist;
        SMaterial _material ;
        SMaterial _prematerial;
        // Event 
        public class StrateLoationStatusChangeEventArgs : EventArgs
        {
            //public SMaterial Material;
            SMaterial Material;
            public eSubStrateLoation LoationStats;
            public StrateLoationStatusChangeEventArgs(eSubStrateLoation stats, SMaterial materials) 
            {

                Material = materials;
                LoationStats = stats;
            }
        }
        public delegate void StrateLoationStatusChangeHandler(object sender, StrateLoationStatusChangeEventArgs e);
        public event StrateLoationStatusChangeHandler OnStrateLoationStatusChange;

        // GET SET
        public eSubStrateLoation StrateLoationStats
        {
            get { return _subStrateLoationStats; }
            set
            {
                _subStrateLoationStats = value;
                if (OnStrateLoationStatusChange != null)
                    OnStrateLoationStatusChange(this, new StrateLoationStatusChangeEventArgs(_subStrateLoationStats, _material));
            }
        }
      //  public Dictionary<int, SMaterial> Materiallist { get { return _materiallist; } set { Materiallist = value; } }
        public SMaterial Material { get { return _material; } set { _prematerial = _material; _material = value; } }
        public SMaterial PreMaterial { get { return _prematerial; } set { _prematerial = value; } }
        public string GetUnitName { get { return _unitname; } }
        public SUnitModel(string Name)
        {
            _unitname = Name;
            _subStrateLoationStats = eSubStrateLoation.UNOCCUPIED;
            _material = null;
            _prematerial = null;
        }
    }
}
