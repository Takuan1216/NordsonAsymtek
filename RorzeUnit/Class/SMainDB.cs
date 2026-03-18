using RorzeUnit.Data;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RorzeComm.Log;
using System.IO;
using System.Windows.Forms;




namespace RorzeUnit.Class
{
    public class SMainDB : SAccessDb
    {
        private SLogger _loggers = SLogger.GetLogger("ExecuteLog");

        //public SMainDB() : base(AppDomain.CurrentDomain.BaseDirectory + @"\" + "DB_Main.MDB", "1")
        public SMainDB() : base(AppDomain.CurrentDomain.BaseDirectory + "\\SettingFile" + "\\DB_Main.MDB", "1")
        {        

        }

        public string GetSetting(string strSettingName)
        {
            try
            {
                DataSet ds = this.Reader("Select * From Settings Where DataName = '{0}'", strSettingName);
                if (ds == null) return string.Empty;
                return ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0]["DataValue"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }

        public string GetSetting(string strFunction, string strSettingName, string strDefault)
        {
            try
            {
                DataSet ds = this.Reader("Select * From Settings Where GroupName = '{1}' And DataName = '{0}'", strSettingName, strFunction);
                if (ds == null)
                {
                    this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strDefault);
                    return strDefault;
                }
                if (ds.Tables[0].Rows.Count <= 0)
                {
                    this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strDefault);
                    return strDefault;
                }
                return ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0]["DataValue"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }

        public List<List<SFuncCell>> GetFunctionList(string strModule, SFuncCell.FuncType typeFunc)
        {
            List<List<SFuncCell>> lstFunc = new List<List<SFuncCell>>();
            try
            {
                DataSet ds = this.Reader("Select * From Func Where ModuleName='{0}' And {1}=True Order By PS1", strModule, typeFunc.ToString());
                //distinct group name 
                List<string> _lstGroupName = new List<string>();

                //
                string strGroup = "";
                int nGroupIdx = -1;
                for (int row = 0; row < ds.Tables[0].Rows.Count; row++)
                {
                    strGroup = ds.Tables[0].Rows[row]["FuncGroup"].ToString();
                    if (_lstGroupName.IndexOf(strGroup) < 0)
                    {
                        _lstGroupName.Add(strGroup);

                        //nGroupIdx++;
                        lstFunc.Add(new List<SFuncCell>());
                        //lstFunc[nGroupIdx] = new List<SFuncCell>();
                        //strGroup = ds.Tables[0].Rows[row]["FuncGroup"].ToString();
                    }
                    nGroupIdx = _lstGroupName.IndexOf(strGroup);
                    lstFunc[nGroupIdx].Add(new SFuncCell()
                    {
                        ModuleName = ds.Tables[0].Rows[row]["ModuleName"].ToString(),
                        FuncGroup = ds.Tables[0].Rows[row]["FuncGroup"].ToString(),
                        FuncName = ds.Tables[0].Rows[row]["FuncName"].ToString(),
                        DmAddress = Convert.ToInt32(ds.Tables[0].Rows[row]["DMAddr"]),
                        DmBit = Convert.ToInt32(ds.Tables[0].Rows[row]["DMBit"]),
                        MrAddress = Convert.ToInt32(ds.Tables[0].Rows[row]["MRAddr"]),
                    });
                }
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
            }
            return lstFunc;
        }

        public List<List<string>> GetFunctionListString(string strModule, SFuncCell.FuncType typeFunc)
        {
            List<List<string>> lstGroup = new List<List<string>>();
            List<string> lstFunc = new List<string>();
            foreach (var group in GetFunctionList(strModule, typeFunc))
            {
                lstFunc = new List<string>();
                foreach (SFuncCell cell in group)
                    lstFunc.Add(string.Join(",", cell.FuncGroup, cell.FuncName, cell.DmAddress, cell.DmBit, cell.MrAddress));
                lstGroup.Add(lstFunc);
            }
            return lstGroup;
        }

        public bool WriteSetting(string strFunction, string strSettingName, string strValue)
        {
            try
            {
                DataSet ds = Reader("Select * From Settings Where GroupName='{0}' And DataName='{1}'", strFunction, strSettingName);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) //parameter exist.
                {
                    SQLExec("Update Settings Set DataValue='{0}' Where GroupName='{1}' And DataName='{2}'", strValue, strFunction, strSettingName);
                    WriteLog(string.Format("Set setting [{0}][{1}] from [{2}] to [{3}]", strFunction, strSettingName, ds.Tables[0].Rows[0]["DataValue"], strValue));
                }
                else //parameter not found.
                {
                    this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strValue);
                    WriteLog(string.Format("Add setting [{0}][{1}] value = [{2}]", strFunction, strSettingName, strValue));
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                return false;
            }
            return true;
        }

        public string[] GetToolRecipe()
        {
            try
            {
                List<string> lstRecipe = new List<string>();

                DataSet ds = this.Reader("Select distinct RecipeName From RecipeChamber Order By RecipeName");
                if (ds == null) return lstRecipe.ToArray();

                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());

                return lstRecipe.ToArray();
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return null;
            }
        }
        public string Get_SECS_Par(string strParameter)
        {
            try
            {
                DataTable dt = this.Reader("Select * From SECSPar Where ParName = '{0}'", strParameter).Tables[0];
                return dt.Rows.Count > 0 ? dt.Rows[0]["ParValue"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }
        public string GetECID(string strParameter)
        {
            try
            {
                DataTable dt = this.Reader("Select * From ECID Where ECName = '{0}'", strParameter).Tables[0];
                return dt.Rows.Count > 0 ? dt.Rows[0]["ECV"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }
        public bool GetAlarmAddressByCode(string strCode, out int nDValue, out int nBit)
        {
            nDValue = 0;
            nBit = 0;
            try
            {
                DataSet ds = Reader("Select * From AlarmSV Where Code='{0}'", strCode);
                if (ds.Tables[0].Rows.Count <= 0) return false;

                nDValue = Convert.ToInt32(ds.Tables[0].Rows[0]["DValue"]);
                nBit = Convert.ToInt32(ds.Tables[0].Rows[0]["BValue"]);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return false;
            }
            return true;
        }

    }

    public class SFuncCell
    {
        public enum FuncType
        {
            IsChamber, IsTeach, IsCaliFlow
        }
        public string ModuleName { get; set; }
        public string FuncGroup { get; set; }
        public string FuncName { get; set; }
        public int DmAddress { get; set; }
        public int DmBit { get; set; }
        public int MrAddress { get; set; }
        public override string ToString()
        {
            return string.Join(",", ModuleName, FuncGroup, FuncName, DmAddress, DmBit, MrAddress);
        }
    }

    public class SStageData : SAccessDb
    {
        private SLogger _loggers = SLogger.GetLogger("ExecuteLog");

        public SStageData() : base(AppDomain.CurrentDomain.BaseDirectory + "\\SettingFile" + "\\DB_StageData.MDB", "1")
        {

        }

        public string GetSetting(string strSettingName)
        {
            try
            {
                DataSet ds = this.Reader("Select * From Settings Where DataName = '{0}'", strSettingName);
                if (ds == null) return string.Empty;
                return ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0]["DataValue"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }

        public string GetSetting(string strFunction, string strSettingName, string strDefault)
        {
            try
            {
                DataSet ds = this.Reader("Select * From Settings Where GroupName = '{1}' And DataName = '{0}'", strSettingName, strFunction);
                if (ds == null)
                {
                    this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strDefault);
                    return strDefault;
                }
                if (ds.Tables[0].Rows.Count <= 0)
                {
                    this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strDefault);
                    return strDefault;
                }
                return ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0]["DataValue"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }

        public void SetWafderData(string strLoad, int nSlot, string strTitle, string strDefault)
        {
            try
            {
                this.SQLExec("Update WaferData Set " + strTitle + "='{0}' Where Loadport='{1}' And Slot={2}", strDefault, strLoad, nSlot);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                //return string.Empty;
            }
        }
        public void SetWafderData(string strLoad, int nSlot, string strTitle, bool bDefault)
        {
            try
            {
                this.SQLExec("Update WaferData Set " + strTitle + "={0} Where Loadport='{1}' And Slot={2}", bDefault.ToString(), strLoad, nSlot);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                //return string.Empty;
            }
        }
        public string GetWafderData(string strLoad, int nSlot, string strTitle, string strDefault)
        {
            try
            {
                DataSet ds = this.Reader("Select * From WaferData Where Loadport = '{0}' And Slot = {1}", strLoad, nSlot);

                if (ds == null)
                {
                    //this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strDefault);
                    return strDefault;
                }
                if (ds.Tables[0].Rows.Count <= 0)
                {
                    //this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strDefault);
                    return strDefault;
                }
                if (ds.Tables[0].Rows[0][strTitle].ToString() == "")
                {
                    return strDefault;
                }

                return ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0][strTitle].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }

        public void RemoveWafderData(string strLoad, int nSlot)
        {
            try
            {
                this.SQLExec("Update WaferData Set" +
                    " FoupID=''" +
                    ",LotID=''" +
                    ",WaferID=''" +
                    ",CJID=''" +
                    ",RecipeID=''" +
                    ",WaferSize=''" +
                    ",WaferPosition=''" +
                    ",ProcessStatus=''" +
                    ",MagneticPut=''" +
                    ",MagneticPutStatus=FALSE" +
                    ",MagneticTake=''" +
                    ",MagneticTakeStatus=FALSE" +
                    ",ReadyToProcess=FALSE" +
                    " Where Loadport='{0}' And Slot={1}", strLoad, nSlot);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                //return string.Empty;
            }
        }

        public bool WriteSetting(string strFunction, string strSettingName, string strValue)
        {
            try
            {
                DataSet ds = Reader("Select * From Settings Where GroupName='{0}' And DataName='{1}'", strFunction, strSettingName);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) //parameter exist.
                {
                    SQLExec("Update Settings Set DataValue='{0}' Where GroupName='{1}' And DataName='{2}'", strValue, strFunction, strSettingName);
                    WriteLog(string.Format("Set setting [{0}][{1}] from [{2}] to [{3}]", strFunction, strSettingName, ds.Tables[0].Rows[0]["DataValue"], strValue));
                }
                else //parameter not found.
                {
                    this.SQLExec("Insert Into Settings (GroupName, DataName, DataValue, DataDefault) Values ('{0}','{1}','{2}','{2}')", strFunction, strSettingName, strValue);
                    WriteLog(string.Format("Add setting [{0}][{1}] value = [{2}]", strFunction, strSettingName, strValue));
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                return false;
            }
            return true;
        }


    }

    public class SProcessDB : SAccessDb
    {
        private SLogger _loggers = SLogger.GetLogger("ExecuteLog");

        public SProcessDB(int nDay) : base(AppDomain.CurrentDomain.BaseDirectory + "\\SettingFile" + "\\DB_RecordsLog.MDB", "1", true)
        {

        }

        public void Insert(string strOccurTime, string strFoupID, string strWaferID, string strEQ, string strMessage)
        {
            try
            {
                this.SQLExec("INSERT INTO ProcessLog(OccurTime, FoupID , WaferID, Equipment, Message) VALUES('{0}', '{1}', '{2}', '{3}', '{4}')", strOccurTime, strFoupID, strWaferID, strEQ, strMessage);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
            }
        }
        public void DeleteOccurTime()
        {
            this.SQLExec("Delete * From  ProcessLog");
        }
        public void DeleteOccurTime(string Day)
        {
            this.SQLExec("Delete * from ProcessLog where OccurTime <= #" + Day + "#)");
        }


        // 1.ProcessLot => CarrierID,CJ,PJ,StartTime,EndTime
        public void CreateProcessLotInfo(DateTime StartTime, string SourceCarrierID, string CJID, string PJID)
        {
            SQLExec(string.Format("Insert Into ProcessLot (ProcessStartTime, SourceCarrierID, CJID,PJID) Values (#{0:yyy/MM/dd HH:mm:ss}#,'{1}','{2}','{3}')", StartTime, SourceCarrierID, CJID, PJID));
        }
        public void UpdateProcessLotInfo(DateTime EndTime, string CJID, string PJID)
        {
            SQLExec(string.Format("Update ProcessLot Set ProcessEndTime = #{0}# Where CJID='{1}' AND PJID='{2}'", EndTime.ToString("yyy/MM/dd HH:mm:ss"), CJID, PJID));
        }
        public DataSet GetProcessLotInfoSearchLike(string strStartTime, string strEndTime)
        {
            try
            {
                DataSet ds = this.Reader(@"SELECT SourceCarrierID as [Foup ID],
                                         CJID,
                                         PJID,
                                         FORMAT(ProcessStartTime,'yyyy-MM-dd HH:mm:ss') as [Start Time],
                                         FORMAT(ProcessEndTime,'yyyy-MM-dd HH:mm:ss') as [End Time] 
                                         FROM ProcessLot Where ProcessStartTime >= #{0} 00:00:00# AND ProcessStartTime <= #{1} 23:59:59# 
                                         Order by ProcessStartTime asc",
                                         strStartTime, strEndTime);
                if (ds == null) return null;
                return ds;
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return null;
            }
        }
        public DataSet GetProcessWafer(string CarrierID, string CJID, string PJID)
        {
            try
            {
                //DataSet ds = this.Reader(@"SELECT  PanelID as [Wafer ID], 
                //                         RecipeID, 
                //                         SourceSlot as [Slot], 
                //                         CJID, 
                //                         PJID, 
                //                         FORMAT(WaferStartTime,'yyyy-MM-dd HH:mm:ss') as [Start Time],
                //                         FORMAT(WaferEndTime,'yyyy-MM-dd HH:mm:ss') as [End Time] 
                //                         FROM ProcessWafer Where CJID ='{0}' AND PJID ='{1}' AND SourceCarrierID = '{2}' Order by WaferStartTime asc",
                //                         CJID, PJID, soruceID);
                DataSet ds = this.Reader(@"SELECT SourceCarrierID as [Foup ID],
                                         CJID, 
                                         PJID,
                                         RecipeID as [Recipe],
                                         SourceSlot as [Slot], 
                                         M12, 
                                         T7, 
                                         FORMAT(WaferStartTime,'yyyy-MM-dd HH:mm:ss') as [Start Time],
                                         FORMAT(WaferEndTime,'yyyy-MM-dd HH:mm:ss') as [End Time]
                                         FROM ProcessWafer Where CJID ='{0}' AND PJID ='{1}' AND SourceCarrierID = '{2}' 
                                         Order by WaferStartTime asc",
                                         CJID, PJID, CarrierID);
                if (ds == null) return null;
                return ds;
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return null;
            }
        }
        public DataSet GetProcessWaferStation(string CJID, string PJID, string RecipeID, int SourceSlot)
        {
            try
            {
                DataSet ds = this.Reader(@"SELECT FORMAT(WaferInTime,'yyyy-MM-dd HH:mm:ss') as [Time],
                                         RecipeID,
                                         M12, 
                                         T7,
                                         Station                                       
                                         FROM ProcessWaferByStation Where CJID ='{0}' AND PJID ='{1}' AND RecipeID = '{2}' AND SourceSlot ='{3}' 
                                         Order by WaferInTime asc",
                                         CJID, PJID, RecipeID, SourceSlot);
                if (ds == null) return null;
                return ds;
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return null;
            }
        }

        // 2.ProcessWafer => CarrierID,CJ,PJ,Recipe,Slot,M12,T7,StartTime,EndTime
        public void CreateProcessWafer(DateTime StartTime, string SourceCarrierID, string CJID, string PJID, string RecipeID, int SourceSlot, string M12, string T7)
        {
            SQLExec(string.Format("Insert Into ProcessWafer (WaferStartTime, SourceCarrierID, CJID, PJID, RecipeID, SourceSlot, M12, T7) Values (#{0:yyy/MM/dd HH:mm:ss}#,'{1}','{2}','{3}','{4}','{5}','{6}','{7}')", StartTime, SourceCarrierID, CJID, PJID, RecipeID, SourceSlot, M12, T7));
        }
        public void UpdateProcessWafer_M12(string M12, string CJID, string PJID, int SourceSlot)
        {
            SQLExec(string.Format("Update ProcessWafer Set M12 = '{0}' Where CJID='{1}' AND PJID='{2}' AND SourceSlot='{3}'", M12, CJID, PJID, SourceSlot));
        }
        public void UpdateProcessWafer_T7(string T7, string CJID, string PJID, int SourceSlot)
        {
            SQLExec(string.Format("Update ProcessWafer Set T7 = '{0}' Where CJID='{1}' AND PJID='{2}' AND SourceSlot='{3}'", T7, CJID, PJID, SourceSlot));
        }
        public void UpdateProcessWafer_EndTime(DateTime EndTime, string CJID, string PJID, int SourceSlot)
        {
            SQLExec(string.Format("Update ProcessWafer Set WaferEndTime = #{0:yyy/MM/dd HH:mm:ss}# Where CJID='{1}' AND PJID='{2}' AND SourceSlot='{3}'", EndTime, CJID, PJID, SourceSlot));
        }
        // 3.ProcessWaferByStation => CarrierID,CJ,PJ,Recipe,Slot,M12,T7,Time,Station
        public void CreateProcessWaferbyStation(DateTime WaferInTime, string SourceCarrierID, string CJID, string PJID, string RecipeID, int SourceSlot, string M12, string T7, string Station)
        {
            SQLExec(string.Format("Insert Into ProcessWaferByStation (WaferInTime, SourceCarrierID, CJID, PJID, RecipeID, SourceSlot, M12, T7, Station) Values (#{0:yyy/MM/dd HH:mm:ss}#,'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", WaferInTime, SourceCarrierID, CJID, PJID, RecipeID, SourceSlot, M12, T7, Station));
        }




        //EvntLog
        public DataSet SearchEvntLogOccurTime(string Start, string End)
        {
            DataSet dt = this.Reader("Select * from EvntLog where OccurTime >= #" + Start + " 00:00:00# and OccurTime<=  #" + End + " 23:59:59#");
            return dt;
        }
        public void InsertEvntLog(string strTime, string strForm, string strUserName, string strEquipment, string strMessage)
        {
            try
            {
                this.SQLExec("INSERT INTO EvntLog(OccurTime, FormName, UserName, Equipment, Message) VALUES('{0}', '{1}', '{2}', '{3}', '{4}')", strTime, strForm, strUserName, strEquipment, strMessage);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
            }
        }
        //AlarmLog
        public DataSet SelectAlarmLogOccurTime(string Start, string End)
        {
            DataSet dt = this.Reader("Select * from AlarmLog where OccurTime >= #" + Start + " 00:00:00# and OccurTime<=  #" + End + " 23:59:59#");

            return dt;
        }
        public void InsertAlarmLog(string strTime, string strType, string strUnit, string strAlarmCode, string strMessage)
        {
            try
            {
                this.SQLExec("INSERT INTO AlarmLog(OccurTime, Type, UnitType , AlarmCode, Message) VALUES('{0}', '{1}', '{2}', '{3}', '{4}')", strTime, strType, strUnit, strAlarmCode, strMessage);

            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);

            }
        }



        public override void CompactDB()
        {
            DeleteDeleteDaysAgo(90);
            base.CompactDB();
        }

        public void DeleteDeleteDaysAgo(int nDay)//刪除幾天前的紀錄
        {
            DateTime today = DateTime.Now;
            DateTime thirtyDaysAgo = today.AddDays(-nDay);

            #region 匯出時間的資料
            string strFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            //日期是資料夾
            strFolder = Path.Combine(strFolder, DateTime.Now.ToString("yyyMMddHHmmss"));

            string[] strPageName = new string[]
            {
                "ProcessLot.csv","ProcessWafer.csv","ProcessWaferByStation.csv","EvntLog.csv","AlarmLog.csv"
            };
            string[] strSQLselect = new string[]
            {
                string.Format("Select * from ProcessLot WHERE ProcessStartTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo),
                string.Format("Select * from ProcessWafer WHERE WaferStartTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo),
                string.Format("Select * from ProcessWaferByStation WHERE WaferInTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo),
                string.Format("Select * from EvntLog WHERE OccurTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo),
                string.Format("Select * from AlarmLog WHERE OccurTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo),
            };
            for (int i = 0; i < strPageName.Length; i++)
            {
                DataSet dataSet = this.Reader(strSQLselect[i]);
                // 取得 DataSet 中的第一個 DataTable
                DataTable dataTable = dataSet.Tables[0];

                if (dataTable != null && dataTable.Rows.Count == 0) continue;
                //建立檔案路徑
                string strPath = Path.Combine(strFolder, strPageName[i]);
                //資料夾不存在要產生
                if (Directory.Exists(strFolder) == false) { Directory.CreateDirectory(strFolder); }
                SAccessDb.ExportCSV(dataTable, strPath);
            }
            #endregion

            #region 刪除時間的資料
            this.SQLExec(string.Format("DELETE * FROM ProcessLot WHERE ProcessStartTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo));

            this.SQLExec(string.Format("DELETE * FROM ProcessWafer WHERE WaferStartTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo));

            this.SQLExec(string.Format("DELETE * FROM ProcessWaferByStation WHERE WaferInTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo));

            this.SQLExec(string.Format("DELETE * FROM EvntLog WHERE OccurTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo));

            this.SQLExec(string.Format("DELETE * FROM AlarmLog WHERE OccurTime <= #{0:yyy/MM/dd HH:mm:ss}#", thirtyDaysAgo));
            #endregion
        }


    }

    /*public class SEvntLogDB : SAccessDb
    {
        private SLogger _loggers = SLogger.GetLogger("ExecuteLog");

        public SEvntLogDB() : base(AppDomain.CurrentDomain.BaseDirectory + @"\" + "DB_RecordsLog.MDB", "1")
        {

        }

        public DataSet SearchOccurTime(string Start, string End)
        {
            DataSet dt = this.Reader("Select * from EvntLog where OccurTime >= #" + Start + " 00:00:00# and OccurTime<=  #" + End + " 23:59:59#");
            return dt;
        }
        public DataSet SearchForm(string strFormName)
        {
            DataSet dt = this.Reader("Select * from EvntLog Where Form = '{0}'", strFormName);
            return dt;
        }
        public DataSet SearchUserName(string strUserName)
        {
            DataSet dt = this.Reader("Select * From EvntLog Where UserName = '{0}'", strUserName);
            return dt;
        }
        public DataSet SearchEquipment(string strEquipment)
        {
            DataSet dt = this.Reader("Select * From EvntLog Where Equipment = '{0}'", strEquipment);
            return dt;
        }
        public DataSet SearchMessage(string strMessage)
        {
            DataSet dt = this.Reader("Select * From EvntLog Where Message = '{0}'", strMessage);
            return dt;
        }

        public void Insert(string strTime, string strForm, string strUserName, string strEquipment, string strMessage)
        {
            try
            {
                this.SQLExec("INSERT INTO EvntLog(OccurTime, FormName, UserName, Equipment, Message) VALUES('{0}', '{1}', '{2}', '{3}', '{4}')", strTime, strForm, strUserName, strEquipment, strMessage);
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
            }
        }

        public DataSet ReadEvntLog()
        {
            DataSet dt = this.Reader("Select * From EvntLog");
            return dt;
        }

        public void DeleteOccurTime(string Day)
        {
            this.SQLExec("DELETE * from EvntLog where OccurTime <= #" + Day + "#)");
        }


    }*/

    /*public class SAlarmLogDB : SAccessDb
    {
        private SLogger _loggers = SLogger.GetLogger("ExecuteLog");

        public SAlarmLogDB() : base(AppDomain.CurrentDomain.BaseDirectory + @"\" + "DB_RecordsLog.MDB", "1")
        {

        }

        public DataSet SelectAlarmLog(DateTime OccurTime, int AlarmCode)
        {
            DataSet dt = this.Reader("Select * from AlarmLog where OccurTime = #" + OccurTime.ToString("yyyy/MM/dd HH:mm:ss") + "# And AlarmCode ='" + AlarmCode.ToString() + "'");
            return dt;
        }

        public DataSet SelectAlarmLog(string Start, string End)
        {
            DataSet dt = this.Reader("Select * from AlarmLog where OccurTime >= #" + Start + " 00:00:00# and OccurTime<=  #" + End + " 23:59:59#");

            return dt;
        }

        public string SearchTime(string strParameter)
        {
            try
            {
                DataTable dt = this.Reader("Select * From AlarmLog Where ECName = '{0}'", strParameter).Tables[0];
                return dt.Rows.Count > 0 ? dt.Rows[0]["ECV"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }
        public string SearchFoupID(string strParameter)
        {
            try
            {
                DataTable dt = this.Reader("Select * From AlarmLog Where ECName = '{0}'", strParameter).Tables[0];
                return dt.Rows.Count > 0 ? dt.Rows[0]["ECV"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }
        public string SearchUsrtNumber(string strParameter)
        {
            try
            {
                DataTable dt = this.Reader("Select * From AlarmLog Where ECName = '{0}'", strParameter).Tables[0];
                return dt.Rows.Count > 0 ? dt.Rows[0]["ECV"].ToString() : "";
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);
                return string.Empty;
            }
        }

        public void Insert(string strTime, string strType, string strUnit, string strAlarmCode, string strMessage)
        {
            try
            {
                this.SQLExec("INSERT INTO AlarmLog(OccurTime, Type, UnitType , AlarmCode, Message) VALUES('{0}', '{1}', '{2}', '{3}', '{4}')", strTime, strType, strUnit, strAlarmCode, strMessage);

            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);

            }
        }

        public DataSet ReadAlarmLog()
        {
            DataSet dt = this.Reader("Select * From AlarmLog");
            return dt;
        }

        public void DeleteMessage(string Day)
        {
            this.SQLExec("DELETE * from AlarmLog where OccurTime <= #" + Day + "#)");
        }
        public void DeleteMessage()
        {
            this.SQLExec("Delete * From  AlarmLog");
        }


    }*/

    public class SAlarmListDB : SAccessDb
    {
        private SLogger _loggers = SLogger.GetLogger("ExecuteLog");

        public SAlarmListDB() : base(AppDomain.CurrentDomain.BaseDirectory + "\\SettingFile" + "\\DB_AlarmList.MDB", "1")
        {

        }

        public void SetAlarmOcur(int AlarmID, bool bOcur)
        {
            try
            {
                if (bOcur)
                    this.SQLExec("UPDATE AlarmList SET Ocur = TRUE where AlarmID = '{0}'", AlarmID.ToString());
                else
                    this.SQLExec("UPDATE AlarmList SET Ocur = FALSE where AlarmID = '{0}'", AlarmID.ToString());
            }
            catch (Exception ex)
            {
                _loggers.WriteLog(ex);

            }
        }

        public DataSet UpdateMessage()
        {
            DataSet dt = this.Reader("Select * From AlarmLog");
            return dt;
        }

        public DataSet SelectAlarmOcur(bool bOcur)
        {
            DataSet dt;

            if (bOcur)
                dt = this.Reader("Select * from AlarmList where Ocur = TRUE");
            else
                dt = this.Reader("Select * from AlarmList where Ocur = FALSE");

            return dt;
        }
        public DataSet SelectAlarmOcur(int AlarmID, bool bOcur)
        {
            DataSet dt;

            if (bOcur)
                dt = this.Reader("Select * from AlarmList where AlarmID = '{0:D9}' and Ocur = TRUE", AlarmID.ToString());
            else
                dt = this.Reader("Select * from AlarmList where AlarmID = '{0:D9}' and Ocur = FALSE", AlarmID.ToString());

            return dt;
        }
        public DataSet SelectAlarmList(int AlarmID)
        {
            DataSet dt = this.Reader("Select * from AlarmList where AlarmID = '{0:D9}'", AlarmID.ToString());

            return dt;
        }

        public DataSet SelectCancelOcur(bool bOcur)
        {
            DataSet dt;

            if (bOcur)
                dt = this.Reader("Select * from AlarmList where Type = 'Cancel' and Ocur = TRUE");
            else
                dt = this.Reader("Select * from AlarmList where Type= 'Cancel' and Ocur = FALSE");

            return dt;
        }
        public DataSet SelectCancelOcur(int AlarmID, bool bOcur)
        {
            DataSet dt;

            if (bOcur)
                dt = this.Reader("Select * from AlarmList where Type = 'Cancel' and AlarmID = '{0}' and Ocur = TRUE", AlarmID.ToString());
            else
                dt = this.Reader("Select * from AlarmList where Type = 'Cancel' and AlarmID = '{0}' and Ocur = FALSE", AlarmID.ToString());

            return dt;
        }

        public void DeleteMessage(string Day)
        {
            this.SQLExec("DELETE * from AlarmLog where OccurTime <= #" + Day + "#)");
        }
        public void DeleteMessage()
        {
            this.SQLExec("Delete * From  AlarmLog");
        }

        //用於確認AlarmList之中有"OcurTime"行
        public void AddColumnToDB(string strName, string strType, int nLength)
        {
            try
            {
                var schema = GetSchema("AlarmList");
                bool bGetNoColumn = true;
                foreach (DataRow row in schema.Rows)
                {
                    if (row["COLUMN_NAME"].ToString().Equals(strName, StringComparison.OrdinalIgnoreCase))
                    {
                        bGetNoColumn = false;
                        break;
                    }
                }
                if (bGetNoColumn)
                {
                    this.SQLExec(string.Format("ALTER TABLE AlarmList ADD COLUMN {0} {1}({2}) ", strName, strType, (int)nLength));
                }
            }
            catch(Exception ex)
            {
                _loggers.WriteLog(ex);
            }
        }

        //用於初始化一次性更新所有的ErrorCode
        public void AddAlarmCodeToDB(int nCode, string strType, string strUnit, string strContent)//將code加入到DB，如果已經有了會更新
        {
            string strAlarmID = nCode.ToString("D9");
            string strMsg = strContent.Replace('_', ' ');

            DataSet dtSelectAlarmCode = this.Reader(string.Format("Select * from AlarmList where AlarmID = '{0:D9}'", (int)nCode));
            //  沒有這個alarm code就加入
            if (dtSelectAlarmCode.Tables[0].Rows.Count < 1)
            {
                this.SQLExec(string.Format("Insert Into AlarmList (AlarmID, Type, UnitType, AlarmMsg, Ocur, Enable) Values ('{0}','{1}','{2}','{3}',{4},{5})", strAlarmID, strType, strUnit, strMsg, false, true));
            }
            else
            {
                this.SQLExec(string.Format("Update AlarmList Set Type='{1}', UnitType='{2}', AlarmMsg='{3}', Ocur={4} Where AlarmID='{0}'", strAlarmID, strType, strUnit, strMsg, false));
            }
        }
        //  更新DB中的異常代碼，用於異常發生檢查有沒有ErrorCode
        public void UpdataAlarmList(int code, string strType, string strUnit, string msg)
        {
            //  解析 Alarm code
            string strAlarmID = code.ToString("D9");
            DataSet dtSelectAlarmCode = this.Reader(string.Format("Select * from AlarmList where AlarmID = '{0:D9}'", code));
            if (dtSelectAlarmCode.Tables[0].Rows.Count < 1)
            {
                this.SQLExec(string.Format("Insert Into AlarmList (AlarmID, Type, UnitType, AlarmMsg, Ocur) Values ('{0}','{1}','{2}','{3}',{4})", strAlarmID, strType, strUnit, msg, false));
            }
        }



    }
}
