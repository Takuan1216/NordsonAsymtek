using Rorze.Secs;
using Rorze.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rorze.SocketObject;


namespace RorzeApi.SECSGEM
{ 
    public class CEIDManager
    {
        MainDB _db;
        public Dictionary<string, int> CEIDList;
        public CEIDManager(MainDB DB)
        {
            _db = DB;
            CEIDList = new Dictionary<string, int>();
            _db.GetSECSCEID(ref CEIDList);
        }
    }
}
