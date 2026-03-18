using RorzeComm.Log;
using RorzeUnit.Class.E84;
using RorzeUnit.Class.ElectrostaticDetect;
using RorzeUnit.Class.EQ;
using RorzeUnit.Class.Robot;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RorzeApi
{
    public class SystemContext
    {
        private static SystemContext _instance;
        private static readonly object _lock = new object();
        private SLogger ExeLog = SLogger.GetLogger("ExecuteLog");
        public void WriteExeLog(string msg)
        {
            ExeLog.WriteLog(msg);
        }

        public List<I_Robot> ListTRB { get; private set; }
        public List<I_Loadport> ListSTG { get; private set; }
        public List<I_Aligner> ListALN { get; private set; }
        public List<I_Buffer> ListBUF { get; private set; }
        public List<SSEquipment> ListEQM { get; private set; }
        public List<I_RC5X0_IO> ListDIO { get; private set; }
        public List<I_RC5X0_Motion> ListTBL { get; private set; }
        public Keyence_DL_RS1A DL_RS1A { get; private set; }
        public bool Simulate { get; private set; }

        private SystemContext() { }

        public static SystemContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SystemContext();
                    }
                }
                return _instance;
            }
        }

        public void Initialize(List<I_Robot> listTRB = null, List<I_Loadport> listSTG = null,
                              List<I_Aligner> listALN = null, List<I_Buffer> listBUF = null,
                              List<SSEquipment> listEQM = null,
                              List<I_RC5X0_IO> listDIO = null, List<I_RC5X0_Motion> listTBL = null,
                              Keyence_DL_RS1A dl_RS1A = null,
                              bool simulate = false)
        {
            ListTRB = listTRB;
            ListSTG = listSTG;
            ListALN = listALN;
            ListBUF = listBUF;
            ListEQM = listEQM;
            ListDIO = listDIO;
            ListTBL = listTBL;
            DL_RS1A = dl_RS1A;

            Simulate = simulate;
        }

        
    }
}
