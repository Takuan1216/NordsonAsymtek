using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rorze.Equipment.Unit
{
   public class SMaterial
    {
        public enum eSubStrate {Non=0,AtSource =1,AtWork =2, AtDestination =3}
        public enum eSubStrateProcess { Unkown=0,NeedProcessing=1,InProcess=2,Processed=3,Stop=4 ,Error=5}
        public enum RQStats{ Non = 0, WaitRead = 1, Reading = 2, ReadCompelete = 3, ReadFail = 4 }
        public enum eTransferSequence {Non=0,SourceToRobotA,RobotAToBuffer,BufferToRobotB,RobotBToEQ,EQToRobotB, RobotBToBuffer, BufferToRobotA,RobotAToDest}
        public class MaterialPositionChangeEventArgs : EventArgs
        {
           public SMaterial Material;
            
            public MaterialPositionChangeEventArgs(SMaterial sMaterial)
            {
                Material = sMaterial;
               
            }
        }
        public delegate void MaterialPositionChangeHandler(object sender, MaterialPositionChangeEventArgs e);
        public event MaterialPositionChangeHandler OnMaterialPositionChange;

        string _ID;
        string _LotID;
        int _slot;
        int TagetSlot;
        int _targetPortID;
        string _position;
        string _SorcePos;
        string _DestPos;
        string _recipe;
        string _carrierid;
        bool _IsEndMaterial;
        bool _IsMappingStatus;
        bool AssignTarget;
        public bool AssingPutEQ;
        public bool AssingTake;
        
        // SEMI E90
        public eSubStrate _subStrate;
        public eSubStrateProcess _subStrateProcess;

        RQStats _rqstats;

        public RQStats RQstats
        {
            get { return _rqstats; }
            set { _rqstats = value; }

        }


        public string Position
        {
            get { return _position; }
            set
            {
                _position = value;
                //if (OnMaterialPositionChange != null)
                //    OnMaterialPositionChange(this, new MaterialPositionChangeEventArgs(_ID, _LotID, _slot, _position));
             }
        }


        public string GetID { get { return _ID; } }
        public string SetID { set { _ID = value; } }
        public string GetLotID { get { return _LotID; } }
        public int GetslotID { get { return _slot; } }
        public string GetCarrierID { get { return _carrierid; } }
        public string GetDestPos { get { return _DestPos; } }
        public int GetTargetSlot { get { return TagetSlot; } }
        public bool GetAssignTarget { get { return AssignTarget; } }
        public string GetRecipe { get { return _recipe; } }
        public bool GetIsEndMaterial { get { return _IsEndMaterial; } }
        public int GetTargetPort { get { return _targetPortID; } }
        public bool GetMappingStatus { get { return _IsMappingStatus; } }
        public SMaterial(string CarrierID,string MeterialID,string LotID,int slot, int port,string SourcenumUnit,bool MappingResult =true)
        {
            _carrierid = CarrierID;
            _ID = MeterialID;
            _LotID = LotID;
            _slot = slot;
            _position = SourcenumUnit;
            TagetSlot = slot;
             _subStrate = eSubStrate.AtSource;
            if(MappingResult)
              _subStrateProcess = eSubStrateProcess.Unkown;
            else
               _subStrateProcess = eSubStrateProcess.Error;
            _SorcePos = SourcenumUnit;
            _DestPos = SourcenumUnit;
            _targetPortID = 0;
            _recipe = "No assign";
            _IsEndMaterial = false;
            AssingTake = false;
            _rqstats = RQStats.Non;
            _IsMappingStatus = MappingResult;
            AssingPutEQ = false;
        }
        public void AssingHostInfo (string HostMeterialID, string HostLotID)
        {
            _ID = HostMeterialID;
            _LotID = HostLotID;
        }
        public void AssingPosition(string DestPosition,int PortID,int TagetslotID)
        {
            _targetPortID = PortID;
              _DestPos = DestPosition;
            TagetSlot = TagetslotID;
            AssignTarget = true;
            _subStrateProcess = eSubStrateProcess.NeedProcessing;
            _rqstats = RQStats.WaitRead;
        }
        public void MaterialStatesChange(bool IsRobotTake, string Post)
        {
            _position = Post;
            if (!IsRobotTake)
            {
                if(_subStrate == eSubStrate.AtSource && Post!= _SorcePos)
                {
                    _subStrate = eSubStrate.AtWork;
                    _subStrateProcess = eSubStrateProcess.InProcess;
                  
                }
                else if(_subStrate == eSubStrate.AtWork && Post == _DestPos)
                {

                    _subStrate = eSubStrate.AtDestination;
                    _subStrateProcess = eSubStrateProcess.Processed;
                } 
                else if(Post == _SorcePos)
                {
                    _subStrateProcess = eSubStrateProcess.Stop; // Error!
                }
            }
          

            if (OnMaterialPositionChange != null)
                OnMaterialPositionChange(this, new MaterialPositionChangeEventArgs(this));
        }
        public void AssignRecip(string RecipeName)
        {
            _recipe = RecipeName;
        }
        public void AssignEnddMaterial(bool Isend)
        {
            _IsEndMaterial = Isend;
        }

    }
}
