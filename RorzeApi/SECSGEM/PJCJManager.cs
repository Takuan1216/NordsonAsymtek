using Rorze.Secs;
using RorzeComm.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RorzeApi.SECSGEM
{
    public class PJCJManager
    {
        public Dictionary<string, SProcessJobObject> PJlist;//PJ
        public Dictionary<string, SControlJobObject> CJlist;//CJ

        public PJCJManager()
        {
            PJCJReset();
        }

        public void CreateCJ(string strCJID)
        {
            if (CJlist.ContainsKey(strCJID))
            {
                CJlist.Remove(strCJID);
            }
            CJlist.Add(strCJID, new SControlJobObject(strCJID));
        }
        public void CreatePJ(string strPJID)
        {
            if (PJlist.ContainsKey(strPJID))
            {
                PJlist.Remove(strPJID);
            }
            PJlist.Add(strPJID, new SProcessJobObject(strPJID));
        }

        public void PJCJReset()
        {
            PJlist = new Dictionary<string, SProcessJobObject>();
            CJlist = new Dictionary<string, SControlJobObject>();
        }

        //判斷CJ中的PJ中的正在執行的那一筆 Job(SSourceTransInfo) 的 Sourec相同
        public bool IsCurrentExecutionLP(int nBodyNo)
        {

            foreach (SControlJobObject cj in CJlist.Values.ToArray())
            {
                if (cj.Status == JobStatus.COMPLETED) continue;

                foreach (SProcessJobObject pj in cj.PJList.Values.ToArray())
                {
                    if (pj.Status == JobStatus.COMPLETED) continue;

                    foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in pj.SourceTransInfoList.ToArray())
                    {
                        if (sourceTransInfo.Finish) continue;
                        if (sourceTransInfo.SourceSTG == -1) return false;
                        //目前要跑的那筆
                        if (sourceTransInfo.SourceSTG == nBodyNo)
                        {
                            return true;
                            //foreach (SProcessJobObject.TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                            //{
                            //    if (transferInfo.SourceSlot == nSlot)
                            //        return true;
                            //}
                            //return false;
                        }
                        else
                        {
                            return false;
                        }
                    }

                }
            }

            return false;
        }
     


        /// <summary>
        /// 判斷CJ中的PJ中還有未完成的 Job(SSourceTransInfo) 會影響此LP
        /// </summary>
        /// <param name="nBodyNo">STG</param>
        /// <returns></returns>
        public bool HasJobNotFinish(int nBodyNo)
        {
            foreach (SControlJobObject cj in CJlist.Values.ToArray())
            {
                if (cj.Status == JobStatus.COMPLETED) continue;

                foreach (SProcessJobObject pj in cj.PJList.Values.ToArray())
                {
                    if (pj.Status == JobStatus.COMPLETED) continue;

                    foreach (SProcessJobObject.SSourceTransInfo sourceTransInfo in pj.SourceTransInfoList.ToArray())
                    {
                        if (sourceTransInfo.Finish) continue;

                        //目前要跑的那筆
                        if (sourceTransInfo.SourceSTG == nBodyNo || sourceTransInfo.TargetSTG == nBodyNo)
                        {
                            return true;
                        }
                    }

                }
            }

            return false;
        }
     
    }
    public enum JobStatus { Non = -1, QUEUED = 0, FunctionSetup = 1, WaitFotHost = 2, EXECUTING = 3, COMPLETED = 4, PAUSING = 5, PAUSED = 6, ABORTING = 8, ABORT = 9, STOPPING = 7, STOP = 10, Destroy = 11, Select = 12 }
    //CJ
    public class SControlJobObject
    {
        public class ControlJobStatesEventArgs : EventArgs
        {
            public string CJID;
            public JobStatus Status;
            public JobStatus PreStatus;

            public ControlJobStatesEventArgs(string ID, JobStatus jobstat, JobStatus prejobstat)
            {
                CJID = ID;
                Status = jobstat;
                PreStatus = prejobstat;
            }
        }
        public delegate void ControlJobStatesHandler(object sender, ControlJobStatesEventArgs e);
        public event ControlJobStatesHandler OnControlJobStatesChange;


        public JobStatus _status;
        public JobStatus _prestatus;
        public JobStatus Status
        {
            get { return _status; }
            set
            {
                _prestatus = _status;
                _status = value;
                OnControlJobStatesChange?.Invoke(this, new ControlJobStatesEventArgs(this.ID, _status, _prestatus));
            }
        }

        public string ID { get; set; }

        public List<string> CarrierInputSpecID;
        public Dictionary<int, SProcessJobObject> PJList;

        public bool AutoStart = false;
        public int MTRLOUTSPECCount;
        public bool TargetCassEnd = false;
        public bool HostCompareResult = true;//210915 Ming Merge PTI dTEK V1.001
        public SControlJobObject(string CJID)
        {
            ID = CJID;
            _status = JobStatus.Non;
            _prestatus = JobStatus.Non;
            CarrierInputSpecID = new List<string>();
            PJList = new Dictionary<int, SProcessJobObject>();

            MTRLOUTSPECCount = 0;
        }
        public int PJCount { get { return PJList.Count; } }

        public bool AssignPJ(int N0, SProcessJobObject PJ)
        {
            if (PJList.ContainsKey(N0))
                return false;

            PJList.Add(N0, PJ);
            return true;
        }

    }
    //PJ
    public class SProcessJobObject
    {
        public class ProcessJobStatesEventArgs : EventArgs
        {
            public string PJID;
            public JobStatus Status;
            public JobStatus PreStatus;
            public List<string> CarrierList;
            public ProcessJobStatesEventArgs(string ID, JobStatus jobstat, JobStatus prejobstat, List<string> List)
            {
                PJID = ID;
                Status = jobstat;
                CarrierList = List;
                PreStatus = prejobstat;
            }
        }
        public delegate void ProcessJobStatesHandler(object sender, ProcessJobStatesEventArgs e);
        public event ProcessJobStatesHandler OnProcessJobStatesChange;

        public class TransferInfo
        {
            public int SourceSlot { get; private set; }
            public int TargetSlot { get; set; }
            public double NotchAngle { get; set; }
            public bool UseAligner { get; set; }
            public bool UseOCR { get; set; }
            public string OCRName { get; set; }

            public string WaferIDByHost { get; set; }
            public string LotIDByHost { get; set; }
            public bool[] ApplyEQ { get; set; }
            public TransferInfo(int sourceSlot)
            {
                SourceSlot = sourceSlot;
                //TargetSlot = sourceSlot;
                TargetSlot = -1;
            }
        }
        public class SSourceTransInfo
        {
            /// <summary>
            /// 1,2,3,4
            /// </summary>
            public int SourceSTG { get; private set; }//loadport
            /// <summary>
            /// 1,2,3,4
            /// </summary>
            public int TargetSTG { get; set; }//loadport
            /// <summary>
            /// 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16
            /// </summary>        
            public string SourceCarrierID { get; private set; }
            public string TargetCarrierID { get; set; }    

            public bool Finish = false;

            public List<TransferInfo> TransferList = new List<TransferInfo>();
            public SSourceTransInfo(int nSourceBodyNo, string strSourceCarrierID)
            {
                SourceSTG = nSourceBodyNo;//loadport 1,2,3,4
                TargetSTG = nSourceBodyNo;//loadport 1,2,3,4             
                SourceCarrierID = strSourceCarrierID;
                TargetCarrierID = strSourceCarrierID;              
            }
            public bool CreateSourceSlot(int sourceSlot)
            {
                foreach (TransferInfo transferInfo in TransferList.ToArray())
                {
                    if (transferInfo.SourceSlot == sourceSlot)
                        return false;//已經有這一片Slot
                }
                TransferList.Add(new TransferInfo(sourceSlot));
                return true;
            }
            public void ClearTransferInfo()
            {
                TransferList.Clear();
            }
            public int GetLastTargetSlot()
            {
                TransferInfo transferInfo = TransferList[TransferList.Count - 1];
                return transferInfo.TargetSlot;
            }
            public int GetLastSourceSlot()
            {
                TransferInfo transferInfo = TransferList[TransferList.Count - 1];
                return transferInfo.SourceSlot;
            }
        }

        JobStatus _status;
        JobStatus _prestatus;

        public JobStatus Status
        {
            get { return _status; }
            set
            {
                _prestatus = _status;
                _status = value;
                OnProcessJobStatesChange?.Invoke(this, new ProcessJobStatesEventArgs(this.ID, _status, _prestatus, GetSourceCarrierList()));
            }
        }
        public bool AutoStart { get; set; } = false;
        public string ID { get; set; }
        public string RecipeName { get; set; }
        public string OCR_Recipe { get; set; }
        public double Align_Angle { get; set; }

        public bool Use_OCR { get; set; }
        public bool Use_Align { get; set; }

        public BWSAction Action { get; set; }

        public List<SSourceTransInfo> SourceTransInfoList = new List<SSourceTransInfo>();

        //PJ Foup哪幾片要傳送
        public SProcessJobObject(string PJID)//建構子
        {
            ID = PJID;
            _status = JobStatus.Non;
            _prestatus = JobStatus.Non;

            OCR_Recipe = "";
            Align_Angle = -1;
            Use_OCR = false;
            Use_Align = false;
        }

        //建立CarrierID要傳送
        public int CreateSourceTransInfo(int nSourceLpBodyNo, string strSourceCarrierID)
        {
            SourceTransInfoList.Add(new SSourceTransInfo(nSourceLpBodyNo, strSourceCarrierID));
            return SourceTransInfoList.Count;
        }

        public bool CheckSourceTransInfoExist(string strSourceCarrierID)
        {
            for (int i = 0; i < SourceTransInfoList.Count; i++)// FOUPA FOUPB FOUPA 狀況會選擇後面的FOUPA
            {
                if (SourceTransInfoList[i].SourceCarrierID == strSourceCarrierID)
                {
                    return true;
                }
            }

            return false;
        }

        //建立CarrierID中哪一片slot要傳送
        public bool CreateSourceSlotInfo(string strSourceCarrierID, int nSourceSlot)
        {

            if (ContainsSourceSlot(strSourceCarrierID, nSourceSlot) == true)
                return false;//已經存在          

            int nIndex = -1;
            for (int i = 0; i < SourceTransInfoList.Count; i++)// FOUPA FOUPB FOUPA 狀況會選擇後面的FOUPA
            {
                if (SourceTransInfoList[i].SourceCarrierID == strSourceCarrierID)
                {
                    nIndex = i;
                }
            }
            if (nIndex == -1) return false;//找不到CarrierID

            return SourceTransInfoList[nIndex].CreateSourceSlot(nSourceSlot);
        }

        //分配CarrierID中哪一片slot要傳送到Target slot
        public bool AssginSourceSlotInfo(string strSourceCarrierID, int nSourceSlot, string strTargertCarrierID, int nTargetSlot, int nTargetStg, bool[] bApplyEQ, double dNotchAngle = 0, string WaferID = "", string lotID = "", bool UseAligner = false, bool UseOCR = false, string OCR_Recipe = "")
        {
            foreach (SSourceTransInfo sourceTransInfo in SourceTransInfoList)
            {
                foreach (TransferInfo transferInfo in sourceTransInfo.TransferList)
                {
                    if (sourceTransInfo.SourceCarrierID == strSourceCarrierID && transferInfo.SourceSlot == nSourceSlot)
                    {
                        sourceTransInfo.TargetSTG = nTargetStg;//1,2,3,4                   
                        sourceTransInfo.TargetCarrierID = strTargertCarrierID;
                        transferInfo.TargetSlot = nTargetSlot;
                        transferInfo.NotchAngle = dNotchAngle;
                        transferInfo.WaferIDByHost = WaferID;
                        transferInfo.LotIDByHost = lotID;
                        transferInfo.UseAligner = UseAligner;
                        transferInfo.UseOCR = UseOCR;
                        transferInfo.OCRName = OCR_Recipe;
                        transferInfo.ApplyEQ = bApplyEQ;
                    }
                }
            }
            return false;//找不到
        }

        public void CheckAssginSourceSlotInfoResult(string strSourceCarrierID) {
            //檢查哪些TargetSlot 是 -1，將其移除，發生原因為PJ有指定但CJ未給Target
            foreach (SSourceTransInfo sourceTransInfo in SourceTransInfoList.ToArray())
            {
                if (sourceTransInfo.SourceCarrierID  == strSourceCarrierID)
                {
                    sourceTransInfo.TransferList.RemoveAll(t => t.TargetSlot  == -1);
                }
            }



        }         
            


        //SourceTransInfoList全部的Job都完成
        public bool IsPJDone()
        {
            bool bOK = true;
            foreach (SSourceTransInfo sourceTransInfo in SourceTransInfoList.ToArray())
                bOK &= sourceTransInfo.Finish;

            return bOK;
        }

        //判斷PJ內部已經有此CarrierID Slot要傳送
        public bool ContainsSourceSlot(string strSourceCarrierID, int nSourceSlot)
        {
            foreach (SSourceTransInfo sourceTransInfo in SourceTransInfoList.ToArray())
            {
                if (sourceTransInfo.SourceCarrierID == strSourceCarrierID)
                {
                    foreach (TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                    {
                        if (transferInfo.SourceSlot == nSourceSlot)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool ContainsTargetSlot(string strCarrierID, int nTargetSlot)
        {
            foreach (SSourceTransInfo sourceTransInfo in SourceTransInfoList.ToArray())
            {
                if (sourceTransInfo.TargetCarrierID == strCarrierID)
                {
                    foreach (TransferInfo transferInfo in sourceTransInfo.TransferList.ToArray())
                    {
                        if (transferInfo.TargetSlot == nTargetSlot)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }



        public List<string> GetSourceCarrierList()
        {
            List<string> strList = new List<string>();
            foreach (SSourceTransInfo item in SourceTransInfoList)
            {
                strList.Add(item.SourceCarrierID);
            }

            return strList;
        }
        public void ClearSourceTransferInfo()
        {
            foreach (SSourceTransInfo sourceTransInfo in SourceTransInfoList)
            {
                sourceTransInfo.ClearTransferInfo();
            }
            SourceTransInfoList.Clear();
        }

    }

}
