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
    public enum VIDType { DVID = 0, SVID, ECID }
    public class VIDManager
    {
        MainDB _db;
        public  Dictionary<int, VIDObject> SVIDList;
        public Dictionary<string, VIDObject> DVIDList;
        public Dictionary<int, VIDObject> ECIDList;
        public Dictionary<int, VIDObject> FDCList;
        public Dictionary<string, int> FDCMappingList; // for Screen ....  
        public Dictionary<string, int> ECIDMappingList; // for Screen ....  
        public VIDManager(MainDB DB)
        {
            _db = DB;
            SVIDList = new Dictionary<int, VIDObject>();
            DVIDList = new Dictionary<string, VIDObject>();
            ECIDList = new Dictionary<int, VIDObject>();
            FDCList = new Dictionary<int, VIDObject>();
            FDCMappingList = new Dictionary<string, int>();
            ECIDMappingList = new Dictionary<string, int>();
            _db.GetSECSECID(ref ECIDList,ref ECIDMappingList);
            _db.GetSECSDVID(ref DVIDList);
            _db.GetSECSSVID(ref SVIDList);
            _db.GetSECSFDC(ref FDCList,ref FDCMappingList);
        }
    }
    public class VIDObject
    {
        public VIDType _type;
        public SecsFormateType ValueType;
        public int VID;
        public string VIDName;
        public string Min;
        public string Max;
        public string Unit;
        public object CurrentValue;
      //  public List<SecsFormateType> ValueTypeList;
      //  public int SubValueCount = 0;
      //  public List<SecsFormateType> SubValueTypeList;

        public VIDObject(VIDType types, SecsFormateType valuetypes, int No,string Name)
        {
            _type = types;
            ValueType = valuetypes;
            VID = No;
            VIDName = Name;
            Min = "";
            Max = "";
            switch (valuetypes)
            {
                case SecsFormateType.A:
                    CurrentValue = string.Empty;

                    break;
                case SecsFormateType.Bool:
                    CurrentValue = false;
                    break;
                case SecsFormateType.U1:
                case SecsFormateType.U2:
                case SecsFormateType.U4:
                    CurrentValue = 0;

                    break;
                case SecsFormateType.F4:
                case SecsFormateType.F8:
                    CurrentValue = double.NaN;
                    break;
            }
            
        }
    }



    
    public class DVID_Obj
    {

        public string Name;
        public object Value;

        public DVID_Obj(string _Name, object _Value)
        {
            Name = _Name;
            Value = _Value;
        }

    }

    public class CEID_Obj
    {

        public string Name;

        public CEID_Obj(string _Name)
        {
            Name = _Name;
 
        }

    }



}
