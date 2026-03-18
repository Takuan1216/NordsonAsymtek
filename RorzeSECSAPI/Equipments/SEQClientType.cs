using System;
using System.Collections.Generic;
using System.Text;
using Rorze.SocketObject;
using Rorze.Threading;
using Rorze.Equipment.Unit;
using System.Linq;
using System.Threading;
using Rorze.Equipments.Unit;
namespace Rorze.Equipments
{
  public abstract class SEQClientType: SEFEM
  {
        string _eqname = string.Empty;
        Dictionary<string, Dictionary<string,SUnitModel>> _chamberlist;
        List<string> _recipeList;
        Dictionary<string, string> _recipebody;
        public SEQClientType(string name, SocketControl control, List<string> ChamberName,List<string> ChamberSpaceName)
            :base(name,control)
        {
            _eqname = name;
            _recipeList = new List<string>();
            _chamberlist = new Dictionary<string, Dictionary<string, SUnitModel>>();
            _recipebody = new Dictionary<string, string>();
            foreach (string Name in ChamberName)
            {
                if(!_chamberlist.ContainsKey(Name))
                {
                    _chamberlist.Add(Name, new Dictionary<string, SUnitModel>());
                    foreach(string SpaceName in ChamberName)
                    {
                        if(!_chamberlist[Name].ContainsKey(SpaceName))
                            _chamberlist[Name].Add(SpaceName, new SUnitModel(SpaceName));
                        
                    }
                }
            }

        }


    }
}
