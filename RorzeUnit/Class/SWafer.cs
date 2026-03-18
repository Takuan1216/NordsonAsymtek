using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RorzeComm.Log;


namespace RorzeUnit.Class
{

    public class InspectData
    {
        public string Data_1 { get; set; }     // ex: 1, 2, 3...
        public string Data_2 { get; set; }     // ex: 0.156575555371953
        public string Data_3 { get; set; }     // ex: "gf"
        public string Data_4 { get; set; }     // ex: "RotatingShear 100 gf"
        public string Data_5 { get; set; }     // ex: "Shear 25 gm gold wire"
        public string Data_6 { get; set; }     // ex: "cyc test 25 shear"
    }

    public class SWafer
    {
        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        static public SStageData _dbWafer = new SStageData();//StageData.MDB
       

        #region enum
        public enum enumWaferSize : int { Unknow = 0, Inch12 = 1, Inch08 = 2, Inch06 = 3, Frame = 4, Panel = 5 };
        public enum enumProcessStatus : int
        {
            None = 0,
            Sleep = 1,
            WaitProcess,    //dirty wafer, robot必須卡arm wafer-in function
            //Transfer,
            Processing,
            Processed,      //clean wafer, robot必須卡arm wafer-out function 
            Cancel,         //211022 merge 庭瑋增加
            Abort,
            Error
        }
        public enum enumPosition : int
        {
            UnKnow = 0,
            Loader1, Loader2, Loader3, Loader4, Loader5, Loader6, Loader7, Loader8,
            AlignerA, AlignerB,
            UpperArm, LowerArm,
            BufferA, BufferB,
            EQM1, EQM2, EQM3, EQM4,
            AOI,
            HOME, ALEX,
        }
        public enum enumFromLoader : int
        {
            UnKnow = 0,
            LoadportA, LoadportB, LoadportC, LoadportD,
            LoadportE, LoadportF, LoadportG, LoadportH,
        }
        public enum enumWaferIDComparison : int { UnKnow = 0, IDAbort, IDAgree, }
        public enum enumMatchOrder : int { none = 0, SmalltoBig, BigToSmall }

        #endregion

        #region private
        private string m_strFoupID;                                              //  #01
        private string m_strLotID;                                               //  #02
        private string m_strCJID;                                                //  #03
        private string m_strPJID;                                                //  #04
        private string m_strRecipeID;                                            //  #05 GroupRecipe
        private int m_nSlot;                                                     //  #06
        private enumWaferSize m_eWaferSize;                                      //  #07
        private enumPosition m_ePosition;                                        //  #08 當下在哪裡
        private enumFromLoader m_eOwner;                                         //  #09 來自哪一個Load port
        private enumProcessStatus m_eProcessStatus;                              //  #10
        private bool m_bReadyToProcess;                                          //  #11 loadport判斷塞到robotQ
        private bool m_bAlgnComplete;                                            //  #12 Aligner 完成
        private enumWaferIDComparison m_eWaferIDComparison;                      //  #13 waferID比對
        private string[] m_strWaferID = new string[] { string.Empty, string.Empty };     //  #14,15 Front & Back
        private string[] m_strWaferInforID = new string[] { string.Empty, string.Empty };//  #16,17 比對waferID
        private enumFromLoader m_eToLoadport;                       //#18 要傳送到哪個Loadport
        private int m_nToSlot;                                      //#19 要傳送到哪個Slot
        private string m_strToFoupID;                               //#20 要傳送到哪個FOUPID(Loadport應該會對起來)
        private double m_dNotchAngle;                               //#21 Notch Angle
        private enumFromLoader m_eFromLoadport;                     //#22 Source Loadport

        private bool m_robotorder;                                  //#23 Kevin Robot order this wafer , So Finish need check
        private Dictionary<enumPosition, bool> m_bEqProcessComplete = new Dictionary<enumPosition, bool>();//#24 Equipment 完成


        Dictionary<string, string> m_dicMeasureData = new Dictionary<string, string>();   //#25 Equipment 結果
        List<InspectData> m_listInspectData = new List<InspectData>();  //#25 Equipment 結果
        #endregion

        #region public
        //  #01
        public string FoupID { get { return m_strFoupID; } set { m_strFoupID = value; } }
        //  #02
        public string LotID { get { return m_strLotID; } set { m_strLotID = value; } }
        //  #03
        public string CJID { get { return m_strCJID; } set { m_strCJID = value; } }
        //  #04
        public string PJID { get { return m_strPJID; } set { m_strPJID = value; } }
        //  #05
        public string RecipeID { get { return m_strRecipeID; } set { m_strRecipeID = value; } }
        //  #06
        public int Slot
        {
            get { return m_nSlot; }
            set
            {
                _logger.WriteLog("[Wafer] Remove loader [{0}].Slot[{1}] wafer data due to slot position be changed.", m_eOwner, m_nSlot);
                RemoveWaferData(m_eOwner, m_nSlot); //Slot被改變時先刪除目前slot的所有資料
                m_nSlot = value;
                DataSync(); //新slot同步所有資料
            }
        }
        public void SetSlot(int slot) { m_nSlot = slot; }
        //  #07
        public enumWaferSize WaferSize { get { return m_eWaferSize; } }

        object m_oLockPos = new object();

        //  #08
        public enumPosition Position
        {
            get
            {
                lock (m_oLockPos)
                {
                    return m_ePosition;
                }
            }
            set
            {
                lock (m_oLockPos)
                {
                    m_ePosition = value;
                    _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "WaferPosition", this.Position.ToString());
                }
            }
        }
        //  #09
        public enumFromLoader Owner { get { return m_eOwner; } }
        public void SetOwner(enumFromLoader owner) { m_eOwner = owner; }
        //  #10
        public enumProcessStatus ProcessStatus
        {
            get { return m_eProcessStatus; }
            set
            {
                m_eProcessStatus = value;
                if (m_eProcessStatus == enumProcessStatus.Processed)
                    this.Robotorder = false;
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "ProcessStatus", this.ProcessStatus.ToString());
            }
        }
        //  #11
        public bool ReadyToProcess
        {
            get { return m_bReadyToProcess; }
            set
            {
                m_bReadyToProcess = value;
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "ReadyToProcess", this.ReadyToProcess);
            }
        }
        //  #12
        public bool AlgnComplete { get { return m_bAlgnComplete; } set { m_bAlgnComplete = value; } }
        //  #13 waferID比對
        public enumWaferIDComparison WaferIDComparison { get { return m_eWaferIDComparison; } set { m_eWaferIDComparison = value; } }
        //  #14
        public string WaferID_F { get { return m_strWaferID[0]; } set { m_strWaferID[0] = value; } }
        //  #15
        public string WaferID_B { get { return m_strWaferID[1]; } set { m_strWaferID[1] = value; } }
        //  #16
        public string WaferInforID_F { get { return m_strWaferInforID[0]; } set { m_strWaferInforID[0] = value; } }
        //  #17
        public string WaferInforID_B { get { return m_strWaferInforID[1]; } set { m_strWaferInforID[1] = value; } }
        //  #18
        public enumFromLoader ToLoadport { get { return m_eToLoadport; } set { m_eToLoadport = value; } }
        //  #19
        public int ToSlot { get { return m_nToSlot; } set { m_nToSlot = value; } }
        //  #20
        public string ToFoupID { get { return m_strToFoupID; } set { m_strToFoupID = value; } }
        //  #21
        public double NotchAngle { get { return m_dNotchAngle; } set { m_dNotchAngle = value; } }
        //  #22
        public enumFromLoader FromLoadport { get { return m_eFromLoadport; } set { m_eFromLoadport = value; } }

        //  #23
        public bool Robotorder { get { return m_robotorder; } set { m_robotorder = value; } }
        //  #24 Equipment 完成
        public bool Eqm1Complete
        {
            get
            {
                // 若不存在就視為false或丟錯誤，看你需求
                bool value;
                if (m_bEqProcessComplete.TryGetValue(enumPosition.EQM1, out value))
                {
                    return value;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                m_bEqProcessComplete[enumPosition.EQM1] = value;
            }
        }
        public bool IsEqProcessComplete()
        {
            bool allTrue = m_bEqProcessComplete.All(item => item.Value);
            return allTrue;
        }
        public bool IsEqProcessComplete(enumPosition ePos)
        {
            bool bComplete = false;
            if (m_bEqProcessComplete.ContainsKey(ePos))
                bComplete = m_bEqProcessComplete[ePos];

            return bComplete;
        }
        public void SetEqProcessComplete(enumPosition ePos)
        {
            if (m_bEqProcessComplete.ContainsKey(ePos))
                m_bEqProcessComplete[ePos] = true;
        }




        //  #25 Equipment 結果
        public Dictionary<string, string> GetMeasureData()
        {
            Dictionary<string, string> dicMeasureData = new Dictionary<string, string>();
            foreach (var item in m_dicMeasureData)
                dicMeasureData.Add(item.Key, item.Value);
            return dicMeasureData;
        }
        public void SetMeasureData(Dictionary<string, string> dicReportData)
        {
            m_dicMeasureData = new Dictionary<string, string>();
            foreach (var item in dicReportData)
            {
                m_dicMeasureData.Add(item.Key, item.Value);
            }
        }



        public List <InspectData> GetInspectData()
        {
            return m_listInspectData;
        }

        #endregion

        public SWafer(string strFoupID, string lotID, string cjID, string pjID, string recipeID, int slot,
            enumWaferSize wafersize,
            enumPosition pos,
            enumFromLoader owner,
            bool[] isEQDisable,
            enumProcessStatus processStatus = enumProcessStatus.Sleep,
            string[] waferID = null)
        {
            try
            {
                m_strFoupID = strFoupID;                             //  #01
                m_strLotID = lotID;                                  //  #02
                m_strCJID = cjID;                                    //  #03
                m_strPJID = pjID;                                    //  #04
                m_strRecipeID = recipeID;                            //  #05
                m_nSlot = slot;                                      //  #06
                m_eWaferSize = wafersize;                            //  #07
                m_ePosition = pos;                                   //  #08
                m_eOwner = owner;                                    //  #09
                m_eProcessStatus = processStatus;                    //  #10
                m_bReadyToProcess = false;                           //  #11
                m_bAlgnComplete = false;                             //  #12
                m_eWaferIDComparison = enumWaferIDComparison.UnKnow; //  #13
                m_strWaferID[0] = waferID == null ? "" : waferID[0]; //  #14
                m_strWaferID[1] = waferID == null ? "" : waferID[1]; //  #15

                m_eToLoadport = owner;                               //  #18 預設跟取的地方一樣
                m_nToSlot = slot;                                    //  #19 預設跟取的地方一樣
                m_strToFoupID = strFoupID;                           //  #20 預設跟取的地方一樣
                m_dNotchAngle = -1;                                  //  #21          
                m_eFromLoadport = owner;                             //#22 預設跟取的地方一樣
                m_bEqProcessComplete[enumPosition.EQM1] = false;     //  #24 預設wafer尚未進入EQ1
                m_bEqProcessComplete[enumPosition.EQM2] = false;     //  #24 預設wafer尚未進入EQ2
                m_robotorder = false;
                DataSync();

                for (int i = 0; i < 4; i++)
                {
                    if (isEQDisable.Length > i)
                        m_bEqProcessComplete.Add(enumPosition.EQM1 + i, isEQDisable[i]);
                    else
                        m_bEqProcessComplete.Add(enumPosition.EQM1 + i, true);
                }


            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        public override string ToString()
        {
            return string.Format("Owner=[{0}], Foup=[{1}], Lot=[{2}], Recipe=[{3}], Slot=[{4}], Pos=[{5}], Size=[{6}], Status=[{7}]",
                Owner, FoupID, LotID, RecipeID, Slot, Position, WaferSize, ProcessStatus);
        }
        private void DataSync()
        {
            try
            {
                _logger.WriteLog("[Wafer] sync wafer data [{0}].", this.ToString());
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "FoupID", this.FoupID);
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "LotID", this.LotID);
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "CJID", this.CJID);
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "RecipeID", this.RecipeID.ToString());
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "WaferSize", this.WaferSize.ToString());
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "WaferPosition", this.Position.ToString());
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "ProcessStatus", this.ProcessStatus.ToString());
                _dbWafer.SetWafderData(m_eOwner.ToString(), Slot, "ReadyToProcess", this.ReadyToProcess);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        static public void RemoveWaferData(enumFromLoader loader, int nSlot)
        {
            _dbWafer.RemoveWafderData(loader.ToString(), nSlot);
        }

        public void Update_XYZ_Result(List<string[]> Result_content_list )
        {
            for (int i = 0; i < Result_content_list.Count; i++)
            {

                InspectData temp = new InspectData();
                temp.Data_1 = Result_content_list[i][0];
                temp.Data_2 = Result_content_list[i][1];
                temp.Data_3 = Result_content_list[i][2];
                temp.Data_4 = Result_content_list[i][3];
                temp.Data_5 = Result_content_list[i][4];
                temp.Data_6 = Result_content_list[i][5];
                m_listInspectData.Add(temp);
            }
        }

    }
}
