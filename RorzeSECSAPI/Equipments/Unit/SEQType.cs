using System;
using System.Collections.Generic;
using System.Text;
using Rorze.SocketObject;

using Rorze.Equipment.Unit;
using System.Linq;
using System.Threading;
using Rorze.Equipments.Unit;
using static RorzeAPI.Equipments.Combination.SScreen;

namespace Rorze.Equipments
{
  public class SEQType
  {
      string _eqname = string.Empty;
      Dictionary<int, Chamber> _chamberlist;
      RcipeManage _recipeobject;
      public SEQType(string name, int ChamberCount, Dictionary<int,string> ChamberSpaceName) 
      {
            _eqname = name;
            _recipeobject = new RcipeManage();
            _chamberlist = new Dictionary<int, Chamber>();
            for (int i = 0; i < ChamberCount; i++)
                _chamberlist.Add(i + 1, new Chamber(i + 1, ChamberSpaceName.Count, ChamberSpaceName));
        }

        public Dictionary<int, Chamber> ChamberList { get { return _chamberlist; } set { _chamberlist = value; } }
        public RcipeManage RecipeObject { get { return _recipeobject; } set { _recipeobject = value; } }
    }


    public class Chamber
    {
        public enum ChamberStats
        { Disable = 0, Idle = 1, ProcessStart = 2, ProcessEnd = 3, Stop = 4, Error = 5 }
        int _num;
        int _spaceno;
        Dictionary<int, SUnitModel> _spacenumUnitlist;
        ChamberStats _stats;

        public Chamber(int CHNo,int space, Dictionary<int,string> unitlist)
        {
            _num = CHNo;
            _spaceno = space;
            _spacenumUnitlist = new Dictionary<int, SUnitModel>();
            foreach(int UnitNo in unitlist.Keys)
            {
                if(!_spacenumUnitlist.ContainsKey(UnitNo))
                 _spacenumUnitlist.Add(UnitNo, new SUnitModel(unitlist[UnitNo]));
            }
            _stats = ChamberStats.Idle;
        }

        public int GetNum { get { return _num; } }

        public ChamberStats Stats { get { return _stats; } set { _stats = value; } }
        public Dictionary<int, SUnitModel> SpaceList { get { return _spacenumUnitlist; } set { _spacenumUnitlist = value; } }



    }
    public class RcipeManage
    {
        List<string> _recipelist;
        List<string> _currentrecipbody; // Name,value

        public RcipeManage()
        {
            _recipelist = new List<string>();
            _currentrecipbody = new List<string>();
        }
        public List<string> RecpieList { get { return _recipelist; } set { _recipelist = value; } }
        public List<string> ReccipBody { get { return _currentrecipbody; } set { _currentrecipbody = value; } }


    }

    public class EFEMVIDUpdateEventArgs : EventArgs
    {
        public List<string> VIDValue;
        public DataType Datatype;
        public EFEMVIDUpdateEventArgs(DataType Type, List<string> DataValue)
        {
            Datatype = Type;
            VIDValue = DataValue;
        }
    }
    public delegate void EFEMVIDUpdateHandler(object sender, EFEMVIDUpdateEventArgs e);


}
