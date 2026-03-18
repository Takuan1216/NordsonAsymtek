using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rorze.Secs;
using Rorze.SocketObject;
namespace RorzeApi.SECSGEM
{
    public class SSECSParameter
    {
       
        public ToolProcessMode _processmode;
        public ToolProcessMode ProcessMode{ get { return _processmode; } set { _processmode = value; } }

        MainDB _db;
        public SECSConneetConfig _SecsConnectConfig;
        public SECSParameterConfig _SecsParameterConfig;
        List<SocketInfo> _SocketConnectConfig ;
        public SECSConneetConfig GetSecsConnectConfig { get { return _SecsConnectConfig; } }
        public SECSParameterConfig GetSECSParameterConfig { get { return _SecsParameterConfig; } set { _SecsParameterConfig = value; } }
       
        public List<SocketInfo> GetSocketConnectCofig { get { return _SocketConnectConfig; } }

        public SSECSParameter(MainDB db)
        {
            _db = db;
            _SocketConnectConfig = new List<SocketInfo>();
            _db.GetSECSConnectParameter(ref _SecsConnectConfig);
            _db.GetSECSParameter(ref _SecsParameterConfig);
            _db.GetSocketConnectConfig(ref _SocketConnectConfig);
            _processmode = _SecsParameterConfig.ProcessMode;
        }
       
        public void SetSECSConfig(bool IsSet, Rorze.Secs.GEMControlStats status)
        {
            _SecsParameterConfig.EnableSECS = IsSet;
            _SecsParameterConfig.OnlineSubStats = status;
               
            _db.SetSECSParameter(_SecsParameterConfig);
            _db.SetSECSConnectParameter(GetSecsConnectConfig);

        }


    }
   
}
