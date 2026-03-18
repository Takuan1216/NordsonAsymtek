using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rorze.Equipment.Unit
{
    //public enum CarrierIDStats
    //{
    //    Create =-2,
    //    NoStatus=-1,
    //    IDNotRead =0,
    //    IDRead =1,
    //    IDReadFail = 2,
    //    IDVerificationok =3,
    //    IDVerificationFail =4,

    //}
    //public enum CarrierSlotMapStats
    //{
    //    NoStatus = -1,
    //    NotSlotMap =0,
    //    SlotMappingOK=1,
    //    SlotMappingVerificationok = 2,
    //    SlotMappingVerificationFail = 3,

    //}
    //public enum CarrierAccessStats
    //{
    //    NoStatus = -1,
    //    NotAccess = 0,
    //    Accessing = 1,
    //    CarrierComplete = 2,
    //    CarrierStop = 3,

    //}

    public class SCarrier
    {
        public class CarrierStatusChangeEventArgs : EventArgs
        {
            public string UnitName;
            public string CarrierID;
            public int PortID;
           
            //public CarrierIDStats CarrierIDstatus;
            //public CarrierIDStats PreCarrierIDstatus;
            //public CarrierSlotMapStats SlotMappingStats;
            //public CarrierSlotMapStats PreSlotMappingStats;
            //public CarrierAccessStats CarrierAccessStats;
            //public CarrierAccessStats PreCarrierAccessStats;
            
            //public string Mapping;
            //public CarrierStatusChangeEventArgs(string Name,string ID, int PortNo, string mappingdata,
            //    CarrierIDStats FoupIDstatus, CarrierSlotMapStats MappingStats, CarrierAccessStats AccessingStats,
            //    CarrierIDStats PreFoupIDstatus, CarrierSlotMapStats PreMappingStats, CarrierAccessStats PreAccessingStats
            //    )
            //{
            //    UnitName = Name;
            //    CarrierID = ID;
            //    PortID = PortNo;
            //    Mapping = mappingdata;
            //    CarrierIDstatus = FoupIDstatus;
            //    SlotMappingStats = MappingStats;
            //    CarrierAccessStats = AccessingStats;
            //    PreCarrierIDstatus = PreFoupIDstatus;
            //    PreSlotMappingStats = PreMappingStats;
            //    PreCarrierAccessStats = PreAccessingStats;
               

            //}
        }
        public delegate void CarrierStatusChangeHandler(object sender, CarrierStatusChangeEventArgs e);

        public class CreateMaterialEventArgs : EventArgs
        {
            public int PortID;
            public int Slot;
            public SMaterial Material;
            public CreateMaterialEventArgs(int port , int slotID, SMaterial materials)
            {
                PortID = port;
                Slot = slotID;
                Material = materials;

            }
        }
        public delegate void CreateMaterialHandler(object sender, CreateMaterialEventArgs e);

        //public event CarrierStatusChangeHandler OnCarrierIDStatsChange;
        //public event CarrierStatusChangeHandler OnSlotmapStatsChange;
        //public event CarrierStatusChangeHandler OnAccessStatsChange;

        //public event CreateMaterialHandler OnMaterialCreate;
        public string GetMappingdata { get {return MappingData; } }
        public int Capacity { set; get; }
        public int SubstrateCount{ set; get; }
        public string Usage { set; get; }
       
        public string ID;
        public int PortID;
        public string UnitName;
        string MappingData;
       // bool _IsMappingResult;
        
        public bool UseReadID { set; get; }
        public Dictionary<int, SMaterial> MaterialList;

        //CarrierIDStats _IDstatus= CarrierIDStats.Create;
        //CarrierIDStats _preIDstatus;
        //CarrierSlotMapStats _slotMapStats= CarrierSlotMapStats.NoStatus;
        //CarrierSlotMapStats _preslotMapStats;
        //CarrierAccessStats _accessStats = CarrierAccessStats.NoStatus;
        //CarrierAccessStats _preaccessStats;

        //public CarrierIDStats IDStatus
        //{
        //    get { return _IDstatus; }
        //    set
        //    {
        //        _preIDstatus = _IDstatus;
        //        _IDstatus = value;
        //        if (OnCarrierIDStatsChange != null)
        //            OnCarrierIDStatsChange(this, new CarrierStatusChangeEventArgs(UnitName,ID, PortID, MappingData, _IDstatus, _slotMapStats, _accessStats,_preIDstatus,_preslotMapStats,_preaccessStats));
        //    }
        //}
        //public CarrierSlotMapStats SlotMapStats
        //{
        //    get { return _slotMapStats; }
        //    set
        //    {
        //        _preslotMapStats = _slotMapStats;
        //        _slotMapStats = value;
        //        if (OnCarrierIDStatsChange != null)
        //            OnSlotmapStatsChange(this, new CarrierStatusChangeEventArgs(UnitName,ID, PortID, MappingData, _IDstatus, _slotMapStats, _accessStats, _preIDstatus, _preslotMapStats, _preaccessStats));
        //    }
        //}
        //public CarrierAccessStats AccessStats
        //{
        //    get { return _accessStats; }
        //    set
        //    {
        //        _preaccessStats = _accessStats;
        //        _accessStats = value;
        //        if (OnCarrierIDStatsChange != null)
        //            OnAccessStatsChange(this, new CarrierStatusChangeEventArgs(UnitName,ID, PortID, MappingData, _IDstatus, _slotMapStats, _accessStats, _preIDstatus, _preslotMapStats, _preaccessStats));
        //    }
        //}
      //  public bool GetMappingResult{ get { return _IsMappingResult; } }


        public SCarrier(string Name,int portNo)
        {
            UnitName = Name;
            PortID = portNo;
            MappingData = "9999999999999999999999999"; // not defind
           
            MaterialList = new Dictionary<int, SMaterial>();
            UseReadID = false;
            Capacity = 6;
            Usage = "True";
        }
        //public void CarrierCreate()
        //{
        //    IDStatus = CarrierIDStats.NoStatus;
        //    SlotMapStats = CarrierSlotMapStats.NotSlotMap;
        //    AccessStats = CarrierAccessStats.NotAccess;
        //}
        //public void AssignCarrierID(string CarrierID)
        //{
        //    ID = CarrierID;
        //    if (ID == "")
        //        IDStatus = CarrierIDStats.IDReadFail;
        //    else
        //        IDStatus = CarrierIDStats.IDRead;


        //}
        public void AssignCarrierIDToHost(string CarrierID)
        {
            ID = CarrierID;
        }
        //public void CreateMaterialObject(string Mapping)
        //{
        //    MappingData = Mapping;
        //    for(int i=0;i< MappingData.Length;i++)
        //    {
        //        switch (MappingData[i])
        //        {
        //            case '1':
        //                if (!MaterialList.ContainsKey(i + 1))
        //                {
        //                    //  MaterialList.Add(i + 1, new SMaterial("waferID" + (i + 1).ToString(), "lotID"+ID, i + 1, (EFEMUnit)PortID));
        //                    MaterialList.Add(i + 1, new SMaterial(ID, string.Format("Port{0}_waferIDSlot{1}", PortID,i+1), string.Format("Port{0}_LotID", PortID), i + 1, PortID,string.Format("Port{0}", PortID)));
        //                    if (OnMaterialCreate != null)
        //                        OnMaterialCreate(this, new CreateMaterialEventArgs(PortID, i + 1, MaterialList[i+1]));
        //                }
        //                break;
        //            case '0':
        //                break;
        //            default:
        //                if (!MaterialList.ContainsKey(i + 1))
        //                {
        //                    //  MaterialList.Add(i + 1, new SMaterial("waferID" + (i + 1).ToString(), "lotID"+ID, i + 1, (EFEMUnit)PortID));
        //                    MaterialList.Add(i + 1, new SMaterial(ID, string.Format("Port{0}_waferIDSlot{1}", PortID, i + 1), string.Format("Port{0}_LotID", PortID), i + 1, PortID, string.Format("Port{0}", PortID),false));
        //                    if (OnMaterialCreate != null)
        //                        OnMaterialCreate(this, new CreateMaterialEventArgs(PortID, i + 1, MaterialList[i + 1]));
        //                }

        //                // alarm ?? 
        //                break;
        //        }
        //    }
        //    if (MaterialList.Where(x => x.Value.GetMappingStatus == false).Count() > 0)
        //        _IsMappingResult = false;
        //    else
        //        _IsMappingResult = true;
        //    SlotMapStats = CarrierSlotMapStats.SlotMappingOK;
        //}

    }
}
