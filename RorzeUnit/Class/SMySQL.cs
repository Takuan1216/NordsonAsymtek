using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using Advantech.Adam;
using MySql.Data.MySqlClient;
using RorzeComm.Log;
using RorzeUnit.Class;

namespace RorzeApi.Class
{
    public class SMySQL
    {
        public enum enumUnit
        {
            TRB1Upper, TRB1Lower,
            TRB2Upper, TRB2Lower,
            ALN1, ALN2,
            BUF1_slot1, BUF1_slot2, BUF1_slot3, BUF1_slot4,
            BUF2_slot1, BUF2_slot2, BUF2_slot3, BUF2_slot4,
            EQM1, EQM2, EQM3, EQM4
        };

        protected string m_strServeIP = "127.0.0.1";
        protected string m_strDBName = "bwsdb-3200";
        protected string m_strUser = "root";
        //protected string m_strPassword = "84149596";
        protected string m_strPassword = "RORZE";

        protected MySqlConnection m_dbConnect = null;
        private SLogger m_logger = SLogger.GetLogger("ExecuteLog");

        public SMySQL(string ip, string name, string user, string password)
        {
            m_strServeIP = ip;
            m_strDBName = name;
            m_strUser = user;
            m_strPassword = password;

            string str = string.Format("server={0};uid={1};pwd={2};database={3};", m_strServeIP, m_strUser, m_strPassword, m_strDBName);
            m_dbConnect = new MySqlConnection(str);
        }

        public bool Check()
        {
            bool bSuc = false;
            lock (this)
            {
                try
                {
                    string strSQL = "show databases";

                    MySqlCommand myCmd = new MySqlCommand(strSQL, m_dbConnect);
                    m_dbConnect.Open();
                    myCmd.ExecuteNonQuery();
                    m_dbConnect.Close();
                    bSuc = true;
                }
                catch (Exception ex)
                {
                    WriteLog(string.Format("Exception : {0}", ex));
                    m_dbConnect.Close();
                }
            }
            return bSuc;
        }
        protected bool SQLExecute(string strSQL)
        {
            bool bSuc = false;
            lock (this)
            {
                try
                {
                    //MySqlCommand myCmd = m_dbConnect.CreateCommand();
                    //myCmd.CommandText = strSQL;
                    //myCmd.ExecuteNonQuery();

                    MySqlCommand myCmd = new MySqlCommand(strSQL, m_dbConnect);
                    m_dbConnect.Open();
                    myCmd.ExecuteNonQuery();
                    bSuc = true;
                }
                catch (Exception ex) { WriteLog("<Exception>:" + ex); }
                m_dbConnect.Close();
            }
            return bSuc;
        }
        protected DataTable SQLQuery(string strSQL)
        {
            DataTable dataTable = null;
            DataSet dataSet = new DataSet();
            lock (this)
            {
                try
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(strSQL, m_dbConnect);
                    m_dbConnect.Open();
                    da.Fill(dataSet);
                }
                catch (Exception ex)
                {
                    WriteLog(string.Format("Exception : {0}", ex));
                }
                if (dataSet.Tables.Count > 0)
                    dataTable = dataSet.Tables[0];
                m_dbConnect.Close();
            }
            return dataTable;
        }
        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[SQL] : {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }
    }

    
    public class SStockerSQL : SMySQL
    {
        /*
        gradedata
        towersetdata
        areasetdata
        slotsetdata

        processstatus
        slotstatus
        unitstatus
        waferlog
        wafertransfer
        */

      

        public enum enumGradeType { Tower, Area, Slot, Status }
        public enum enumChangeType { Add, Del }

        private int m_nTC;    //暫存塔柱數量 (Tower Count)16
        private int m_nSTC;  //暫存單一塔柱數量 (Single Tower Count)200
        private int m_nSAC; //暫存單一區塊數量 (Single Area Count)
        public bool TowerQuerySet { get; set; }
        public bool AreaQuerySet { get; set; }

        private string m_strTowerEnable;

        public SStockerSQL(string ip, string name, string user, string password, int nTC, int nSTC, int nSAC, string strTowerEnable)
            : base(ip, name, user, password)
        {
            m_nTC = nTC;
            m_nSTC = nSTC;
            m_nSAC = nSAC;
            m_strTowerEnable = strTowerEnable;
            Check_slotstatus();//考慮要判斷沒有的資料建置進去

            #region 詢問資料表slotstatus內欄位有沒有名稱叫Particle      
            try
            {
                string strSQL = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'slotstatus' AND COLUMN_NAME = 'Particle'";
                DataTable dt = SQLQuery(strSQL);
                if (dt == null || dt.Rows.Count == 0)
                {
                    //沒有此欄位需要進行新增
                    strSQL = "ALTER TABLE `slotstatus` ADD `Particle` BOOLEAN NOT NULL DEFAULT FALSE AFTER `VCL`";
                    dt = SQLQuery(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }

            try
            {
                string strSQL = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'view_slotstatus' AND COLUMN_NAME = 'Particle'";
                DataTable dt = SQLQuery(strSQL);
                if (dt == null || dt.Rows.Count == 0)
                {
                    //select
                    //`bwsdb - 3200`.`slotstatus`.`No` AS `No`,
                    //`bwsdb - 3200`.`slotstatus`.`SlotName` AS `SlotName`,
                    //`bwsdb - 3200`.`slotstatus`.`WaferStatus` AS `WaferStatus`,
                    //`bwsdb - 3200`.`slotstatus`.`WaferID` AS `WaferID`,
                    //`bwsdb - 3200`.`slotstatus`.`WaferGrade` AS `WaferGrade`,
                    //`bwsdb - 3200`.`slotstatus`.`M12ID` AS `M12ID`,
                    //`bwsdb - 3200`.`slotstatus`.`LotID` AS `LotID`,
                    //`bwsdb - 3200`.`slotstatus`.`VCL` AS `VCL` 
                    //`bwsdb - 3200`.`slotstatus`.`Particle` AS `Particle` 
                    //from `bwsdb - 3200`.`slotstatus` where(1 = 1)


                    //select `bwsdb-3200`.`slotstatus`.`No` AS `No`,`bwsdb-3200`.`slotstatus`.`SlotName` AS `SlotName`,`bwsdb-3200`.`slotstatus`.`WaferStatus` AS `WaferStatus`,`bwsdb-3200`.`slotstatus`.`WaferID` AS `WaferID`,`bwsdb-3200`.`slotstatus`.`WaferGrade` AS `WaferGrade`,`bwsdb-3200`.`slotstatus`.`M12ID` AS `M12ID`,`bwsdb-3200`.`slotstatus`.`LotID` AS `LotID`,`bwsdb-3200`.`slotstatus`.`VCL` AS `VCL` from `bwsdb-3200`.`slotstatus` where (1 = 1)

                    strSQL = "ALTER ALGORITHM = UNDEFINED DEFINER =`root`@`localhost` SQL SECURITY DEFINER VIEW `view_slotstatus` AS SELECT * FROM slotstatus";
                    dt = SQLQuery(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            #endregion

            #region 詢問資料表slotstatus內欄位有沒有名稱叫Disable  
            try
            {
                string strSQL = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'slotstatus' AND COLUMN_NAME = 'Disable'";
                DataTable dt = SQLQuery(strSQL);
                if (dt == null || dt.Rows.Count == 0)
                {
                    //沒有此欄位需要進行新增
                    strSQL = "ALTER TABLE `slotstatus` ADD `Disable` BOOLEAN NOT NULL DEFAULT FALSE AFTER `Particle`";
                    dt = SQLQuery(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }

            try
            {
                string strSQL = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'view_slotstatus' AND COLUMN_NAME = 'Disable'";
                DataTable dt = SQLQuery(strSQL);
                if (dt == null || dt.Rows.Count == 0)
                {
                    //select
                    //`bwsdb - 3200`.`slotstatus`.`No` AS `No`,
                    //`bwsdb - 3200`.`slotstatus`.`SlotName` AS `SlotName`,
                    //`bwsdb - 3200`.`slotstatus`.`WaferStatus` AS `WaferStatus`,
                    //`bwsdb - 3200`.`slotstatus`.`WaferID` AS `WaferID`,
                    //`bwsdb - 3200`.`slotstatus`.`WaferGrade` AS `WaferGrade`,
                    //`bwsdb - 3200`.`slotstatus`.`M12ID` AS `M12ID`,
                    //`bwsdb - 3200`.`slotstatus`.`LotID` AS `LotID`,
                    //`bwsdb - 3200`.`slotstatus`.`VCL` AS `VCL` 
                    //`bwsdb - 3200`.`slotstatus`.`Particle` AS `Particle` 
                    //`bwsdb - 3200`.`slotstatus`.`Disable` AS `Disable` 
                    //from `bwsdb - 3200`.`slotstatus` where(1 = 1)


                    //select `bwsdb-3200`.`slotstatus`.`No` AS `No`,`bwsdb-3200`.`slotstatus`.`SlotName` AS `SlotName`,`bwsdb-3200`.`slotstatus`.`WaferStatus` AS `WaferStatus`,`bwsdb-3200`.`slotstatus`.`WaferID` AS `WaferID`,`bwsdb-3200`.`slotstatus`.`WaferGrade` AS `WaferGrade`,`bwsdb-3200`.`slotstatus`.`M12ID` AS `M12ID`,`bwsdb-3200`.`slotstatus`.`LotID` AS `LotID`,`bwsdb-3200`.`slotstatus`.`VCL` AS `VCL` from `bwsdb-3200`.`slotstatus` where (1 = 1)

                    strSQL = "ALTER ALGORITHM = UNDEFINED DEFINER =`root`@`localhost` SQL SECURITY DEFINER VIEW `view_slotstatus` AS SELECT * FROM slotstatus";
                    dt = SQLQuery(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            m_strTowerEnable = strTowerEnable;
            #endregion

        }



        public int GetGradeSetCount(string tmpGradeName)
        {
            DataTable dt;
            string strCondition = "";
            string strSQL = "";
            //-----------------20170921 wei modify-----------------Tower
            if (TowerQuerySet == true)
            {
                for (int nTowerNo = 0; nTowerNo < m_nTC; nTowerNo++)
                {
                    //If aryTowerDisableList(nTowerNo -1) = True Then
                    if (strCondition == "")
                        strCondition = "and TowerName <> 'Tower" + nTowerNo + "'";
                    else
                        strCondition = strCondition + "and TowerName <> 'Tower" + nTowerNo + "'";
                }

                strSQL = "select count(*) as GCount from TowerSetData where TowerGrade='" + tmpGradeName + "'" + strCondition;
                dt = SQLQuery(strSQL);

                if (dt.Rows.Count > 0)
                    return dt.Rows.Count * m_nSTC;
                else
                    return 0;
            }
            //------------------------------------------------------Area
            if (AreaQuerySet == true)
            {
                int nTowerOfArea = m_nSTC / m_nSAC;
                for (int nTowerNo = 0; nTowerNo < m_nTC; nTowerNo++)
                {
                    //If aryTowerDisableList(nTowerNo -1) = True Then
                    for (int nAreaCount = 0; nAreaCount < nTowerOfArea; nAreaCount++)
                    {
                        int nAreaNo = (nTowerNo * nTowerOfArea) + nAreaCount;
                        if (strCondition == "")
                            strCondition = "and AreaName <> 'Area" + nAreaNo + 1 + "'";
                        else
                            strCondition = strCondition + "and AreaName <> 'Area" + nAreaNo + 1 + "'";
                    }
                }

                strSQL = "Select count(*) as GCount from areasetdata where AreaGrade='" + tmpGradeName + "'" + strCondition;
                dt = SQLQuery(strSQL);

                if (dt.Rows.Count > 0)
                    return dt.Rows.Count * m_nSAC;
                else
                    return 0;
            }
            //------------------------------------------------------No select
            //if (dt.Rows.Count == 0)
            //{
            return 0;
            //}
        }

        #region ===== table (towersetdata/areasetdata/slotsetdata) ==========
        public DataTable SelectDistinctGradeSetList(enumGradeType eGradeType, string tmpStr)
        {
            try
            {
                string strSQL = "";
                switch (eGradeType)
                {
                    case enumGradeType.Tower:
                        strSQL = string.Format("select distinct TowerName from TowerSetData where " + tmpStr);
                        break;
                    case enumGradeType.Area:
                        strSQL = string.Format("select distinct AreaName from AreaSetData where " + tmpStr);
                        break;
                    case enumGradeType.Slot:
                        strSQL = string.Format("select distinct SlotName from SlotSetData where " + tmpStr);
                        break;
                    case enumGradeType.Status:
                        strSQL = string.Format("select distinct SlotName from SlotStatus where " + tmpStr);
                        break;
                }
                DataTable dt = SQLQuery(strSQL);
                return dt;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 查詢GradeName對應的位置(Tower/Area/Slot)
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <param name="strGrade"></param>
        /// <returns></returns>
        public List<string> LoadGradeSetListGetName(enumGradeType eGradeType, string strGrade)
        {
            List<string> listName = new List<string>();
            try
            {
                string strSQL = "";
                switch (eGradeType)
                {
                    case enumGradeType.Tower:
                        strSQL = string.Format("SELECT TowerName FROM TowerSetData WHERE TowerGrade = '{0}'", strGrade);
                        break;
                    case enumGradeType.Area:
                        strSQL = string.Format("SELECT AreaName FROM AreaSetData WHERE AreaGrade = '{0}' ", strGrade);
                        break;
                    case enumGradeType.Slot:
                        strSQL = string.Format("SELECT SlotName FROM SlotSetData WHERE Grade = '{0}'", strGrade);
                        break;
                }
                DataTable dt = SQLQuery(strSQL);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow item in dt.Rows)
                    {
                        listName.Add(item.ItemArray[0].ToString());
                    }
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return listName;
        }
        /// <summary>
        /// 查詢位置(Tower/Area/Slot)對應那些GradeName
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <param name="strName"></param>
        /// <returns></returns>
        public List<string> LoadGradeSetListGetGrade(enumGradeType eGradeType, string strName)
        {
            List<string> listName = new List<string>();
            try
            {
                string strSQL = "";
                switch (eGradeType)
                {
                    case enumGradeType.Tower:
                        strSQL = string.Format("SELECT TowerGrade FROM TowerSetData WHERE TowerName = '{0}'", strName);
                        break;
                    case enumGradeType.Area:
                        strSQL = string.Format("SELECT AreaGrade FROM AreaSetData WHERE AreaName = '{0}' ", strName);
                        break;
                    case enumGradeType.Slot:
                        strSQL = string.Format("SELECT Grade FROM AreaSetData WHERE SlotName = '{0}' ", strName);
                        break;
                }
                DataTable dt = SQLQuery(strSQL);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow item in dt.Rows)
                    {
                        listName.Add(item.ItemArray[0].ToString());
                    }
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return listName;
        }
        /// <summary>
        /// 查詢位置(Tower/Area/Slot)與GradeName是否存在DB內
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <param name="strStockName"></param>
        /// <param name="strGradeName"></param>
        /// <returns></returns>
        private bool CheckGradeExist(enumGradeType eGradeType, string strStockName, string strGradeName)
        {
            string strSQL;
            switch (eGradeType)
            {
                case enumGradeType.Tower:
                    strSQL = string.Format("select * from TowerSetData where TowerName='{0}' and TowerGrade='{1}'", strStockName, strGradeName);
                    break;
                case enumGradeType.Area:
                    strSQL = string.Format("select * from AreaSetData where AreaName='{0}' and AreaGrade='{1}'", strStockName, strGradeName);
                    break;
                case enumGradeType.Slot:
                    strSQL = string.Format("select * from SlotSetData where SlotName='{0}' and Grade='{1}'", strStockName, strGradeName);
                    break;
                default:
                    return false;
            }

            DataTable dt = SQLQuery(strSQL);

            if (dt != null && dt.Rows.Count > 0)
                return true;
            else
                return false;
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <param name="strStockName"></param>
        /// <param name="strGradeName"></param>
        public bool InsertGradeNameToStocker(enumGradeType eGradeType, string strStockName, string strGradeName)
        {
            bool bSuc = false;
            try
            {
                string strSQL = "";
                if (CheckGradeExist(eGradeType, strStockName, strGradeName) == false)//已存在
                {
                    switch (eGradeType)
                    {
                        case enumGradeType.Tower:
                            strSQL = string.Format("INSERT INTO TowerSetData ({0},{1}) values ('{2}','{3}')", "TowerName", "TowerGrade", strStockName, strGradeName);
                            break;
                        case enumGradeType.Area:
                            strSQL = string.Format("INSERT INTO AreaSetData ({0},{1}) values ('{2}','{3}')", "AreaName", "AreaGrade", strStockName, strGradeName);
                            break;
                        case enumGradeType.Slot:
                            strSQL = string.Format("INSERT INTO SlotSetData ({0},{1}) values ('{2}','{3}')", "SlotName", "Grade", strStockName, strGradeName);
                            break;
                    }
                    bSuc = SQLExecute(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 刪除
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <param name="strStockName"></param>
        /// <param name="strGradeName"></param>
        public bool DeleteGradeNameInStocker(enumGradeType eGradeType, string strStockName, string strGradeName)
        {
            bool bSuc = false;
            try
            {
                string strSQL = "";
                if (CheckGradeExist(eGradeType, strStockName, strGradeName) == true)//不存在
                {
                    switch (eGradeType)
                    {
                        case enumGradeType.Tower:
                            strSQL = string.Format("delete from TowerSetData where TowerName='{0}' and  TowerGrade ='{1}'", strStockName, strGradeName);
                            break;
                        case enumGradeType.Area:
                            strSQL = string.Format("delete from AreaSetData where AreaName='{0}' and  AreaGrade ='{1}'", strStockName, strGradeName);
                            break;
                        case enumGradeType.Slot:
                            strSQL = string.Format("delete from SlotSetData where SlotName='{0}' and  Grade ='{1}'", strStockName, strGradeName);
                            break;
                    }
                    bSuc = SQLExecute(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        #region ===== table towersetdata ======
        /// <summary>
        /// 查詢 towersetdata table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectTowerSetData()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "SELECT * from towersetdata order by TowerName ASC";
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 新增沒有在內容的新增
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool InsertTowerSetData(DataTable table)
        {
            bool bSuc = false;
            try
            {
                foreach (DataRow datarow in table.Rows)
                {
                    string No = datarow["No"].ToString();
                    string TowerName = datarow["TowerName"].ToString();
                    string TowerGrade = datarow["TowerGrade"].ToString();

                    if (CheckGradeExist(enumGradeType.Tower, TowerName, TowerGrade)) continue;
                    //DB內沒有這項->新增
                    string strName = string.Format("No,TowerName,TowerGrade");
                    string strValue = string.Format("'{0}','{1}','{2}'", No, TowerName, TowerGrade);
                    string strSQL = string.Format("INSERT INTO towersetdata ({0}) values ({1})", strName, strValue);
                    bSuc = SQLExecute(strSQL);
                    if (bSuc == false) break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        #region ===== table areasetdata =======
        /// <summary>
        /// 查詢 areasetdata table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectAreaSetData()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "SELECT * from areasetdata order by AreaName ASC";
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 新增沒有在內容的新增
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool InsertAreaSetData(DataTable table)
        {
            bool bSuc = false;
            try
            {
                foreach (DataRow datarow in table.Rows)
                {
                    string No = datarow["No"].ToString();
                    string AreaName = datarow["AreaName"].ToString();
                    string AreaGrade = datarow["AreaGrade"].ToString();

                    if (CheckGradeExist(enumGradeType.Area, AreaName, AreaGrade)) continue;
                    //DB內沒有這項->新增
                    string strName = string.Format("No,AreaName,AreaGrade");
                    string strValue = string.Format("'{0}','{1}','{2}'", No, AreaName, AreaGrade);
                    string strSQL = string.Format("INSERT INTO areasetdata ({0}) values ({1})", strName, strValue);
                    bSuc = SQLExecute(strSQL);
                    if (bSuc == false) break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        //
        #region ===== table gradedata =========
        /// <summary>
        /// 查詢 gradedata table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectGradeData()
        {
            DataTable dt = null;
            try
            {
                dt = SQLQuery("select * from GradeData order by GradeName ASC");
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 gradedata table by GradeName
        /// </summary>
        /// <returns></returns>
        public DataTable SelectGradeData(string strGradeName)
        {
            DataTable dt = null;
            try
            {
                string strSQL = string.Format("select * from GradeData where GradeName ='{0}'", strGradeName);
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 gradedata table
        /// </summary>
        /// <returns></returns>
        public List<string> SelectGradeNameList()
        {
            List<string> listName = new List<string>();
            try
            {
                DataTable dt = SelectGradeData();
                if (dt != null)
                    foreach (System.Data.DataRow item in dt.Rows)
                    {
                        listName.Add(item["GradeName"].ToString());
                    }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return listName;
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="name"></param>
        /// <param name="hight"></param>
        /// <param name="low"></param>
        /// <param name="foupid"></param>
        /// <returns></returns>
        public bool InsertGradeName(string name, int hight, int low, string foupid)
        {
            bool bSuc = false;
            try
            {
                string strName = string.Format("GradeName,HightLimit,LowLimit,FoupID3");
                string strValue = string.Format("'{0}','{1}','{2}','{3}'", name, hight, low, foupid);
                string strSQL = string.Format("insert into GradeData ({0}) values ({1})", strName, strValue);
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="no"></param>
        /// <param name="name"></param>
        /// <param name="hight"></param>
        /// <param name="low"></param>
        /// <param name="foupid"></param>
        /// <returns></returns>
        public bool UpdateGradeName(int no, string name, int hight, int low, string foupid)
        {
            bool bSuc = false;
            try
            {
                string strSQL = string.Format("update GradeData SET GradeName = '{0}', HightLimit = '{1}', LowLimit = '{2}', FoupID3 = '{3}' where No = '{4}'", name, hight, low, foupid, no);
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 更新 All
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool UpdateGradeName(DataTable table)
        {
            bool bSuc = false;
            try
            {
                foreach (DataRow datarow in table.Rows)
                {
                    string GradeName = datarow["GradeName"].ToString();
                    string HightLimit = datarow["HightLimit"].ToString();
                    string LowLimit = datarow["LowLimit"].ToString();
                    string FoupID3 = datarow["FoupID3"].ToString();
                    string No = datarow["No"].ToString();

                    DataTable dt = SelectGradeData(GradeName);

                    if (dt == null) continue;

                    if (dt.Rows.Count > 0)//DB內沒有這項->新增
                        continue;

                    int nHightLimit, nLowLimit;
                    int.TryParse(HightLimit, out nHightLimit);
                    int.TryParse(LowLimit, out nLowLimit);

                    //string strSQL = string.Format("update GradeData SET  GradeName ='{0}', HightLimit ='{1}', LowLimit ='{2}', FoupID3 ='{3}' where No='{4}'"
                    //    , GradeName, HightLimit, LowLimit, FoupID3, No);

                    bSuc = InsertGradeName(GradeName, nHightLimit, nLowLimit, FoupID3);
                    if (bSuc == false) break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 刪除
        /// </summary>
        /// <param name="no"></param>
        /// <returns></returns>
        public bool DeleteGradeName(int no)
        {
            bool bSuc = false;
            try
            {
                string strSQL = string.Format("delete from GradeData where No='{0}'", no);
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 判斷名稱是否重複
        /// </summary>
        /// <returns></returns>
        public DataTable SelectGradeDataNameRepeat()
        {
            DataTable dt = null;
            try
            {
                dt = SQLQuery("select GradeName,count(*) as qty from GradeData group by GradeName having count(*)>1");
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 刪除重複的GradeName
        /// </summary>
        /// <param name="no"></param>
        /// <returns></returns>
        public bool DeleteRepeatGradeName(string strGradeName)
        {
            bool bSuc = false;
            try
            {
                string strSQL = string.Format("select * from GradeData where GradeName ='{0}' order by No ASC", strGradeName);
                DataTable dt = SQLQuery(strSQL);

                if (dt != null && dt.Rows.Count > 1)
                {
                    for (int i = 0; i < dt.Rows.Count - 1; i++)
                    {
                        string strNo = dt.Rows[i]["No"].ToString();
                        DeleteGradeName(int.Parse(strNo));
                    }
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 清除 slotstatus table
        /// </summary>
        /// <returns></returns>
        public bool TruncateGradeData()
        {
            bool bSuc = false;
            try
            {
                while (true)
                {
                    bSuc = SQLExecute("TRUNCATE TABLE gradedata");
                    break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        //
        #region ===== table slotstatus ========
        /// <summary>
        /// 考慮要判斷沒有的資料建置進去
        /// </summary>
        public bool Check_slotstatus()
        {
            bool bSuc = false;
            try
            {
                int nNo = 0;
                for (int i = 0; i < m_nTC; i++)//0~15
                {
                    for (int j = 0; j < m_nSTC; j++)//0~199
                    {
                        nNo++;
                        string strSQL1 = string.Format("select * from slotstatus where No = '{0}'", nNo);
                        string strSlotName = string.Format("T{0:D2}-S{1:D3}", i + 1, j + 1);
                        DataTable dt = SQLQuery(strSQL1);
                        if (dt == null || dt.Rows.Count == 0)//DB內沒有這項->新增
                        {
                            string strName = string.Format("No,SlotName,WaferStatus,WaferID,WaferGrade,M12ID,LotID,VCL");
                            string strValue = string.Format("'{0}','{1}','{2}',null,null,null,null,null", nNo, strSlotName, 0);
                            string strSQL = string.Format("insert into slotstatus ({0}) values ({1})", strName, strValue);

                            bSuc = SQLExecute(strSQL);
                            if (bSuc == false) break;
                        }
                        else if (dt.Rows[0]["SlotName"].ToString() != strSlotName)//No有但SlotName名稱不對->編輯
                        {
                            string strSQL = string.Format("update slotstatus set SlotName = '{0}' where No = '{1}'", strSlotName, nNo);
                            bSuc = SQLExecute(strSQL);
                            if (bSuc == false) break;
                        }
                    }
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 查詢 slotstatus table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectSlotStatus()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "SELECT * from SlotStatus Order by No";
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 slotstatus table 1~16
        /// </summary>
        /// <returns></returns>
        public DataTable SelectSlotStatus(int nTower)
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select * from SlotStatus where SlotName like '%T" + String.Format("{0:00}", nTower) + "%'  Order by No";
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 slotstatus table 用 WaferID
        /// </summary>
        /// <param name="strWaferID"></param>
        /// <returns></returns>
        public DataTable SelectSlotStatusByWaferID(string strWaferID)
        {
            //撰寫SQL 語法環境 Access SQL Server
            //比對一個字元     「?」    「_」
            //比對多個字元     「*」    「%」
            //比對一個數字     「#」    「#」
            //包含指定範圍[A - C]代表包含A到C的任何單一字元
            //排除包含指定範圍[^A - C]代表排除A到C的任何單一字元
            DataTable dt = null;
            try
            {
                //Like是找相似的
                strWaferID = strWaferID.Replace('*', '%');
                string strSQL = string.Format("SELECT SlotName,WaferID FROM SlotStatus WHERE WaferID  LIKE '{0}' AND WaferStatus = '1' ", strWaferID);
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 slotstatus table 用 GradeName
        /// </summary>
        /// <param name="strGradeName"></param>
        /// <param name="bExist"></param>
        /// <returns></returns>
        public DataTable SelectSlotStatusByGradeName(string strGradeName, bool bExist, bool descending)
        {
            DataTable dt = null;
            try
            {

                string strSQL = bExist == true ? string.Format("select SlotName,WaferID,WaferGrade from view_slotstatus where WaferStatus='1' and WaferGrade ='{0}' AND Particle = '0' AND Disable = '0'", strGradeName) :
                                                 string.Format("select SlotName,WaferID,WaferGrade from view_slotstatus where WaferStatus='0' AND Particle = '0' AND Disable = '0'");
                string strTowerQuerySlot = "";//'暫存要查詢的槽位名稱
                string strAreaQuerySlot = ""; //'暫存要查詢的槽位名稱

                //=====塔柱層===============================================================================
                if (TowerQuerySet == true)
                {
                    if (strGradeName != "") //判斷等級非空值
                    {
                        List<string> NameList = LoadGradeSetListGetName(SStockerSQL.enumGradeType.Tower, strGradeName);
                        int nTowerNo = 0;
                        //If IsNothing(aryTowerName) = False Then
                        for (int i = 0; i < NameList.Count; i++)                  //依塔柱名稱取槽位
                        {
                            nTowerNo = Convert.ToInt32(NameList[i].Replace("Tower", ""));

                            if (m_strTowerEnable[nTowerNo - 1] != '1') continue;//塔被禁用

                            if (strTowerQuerySlot == "")
                                strTowerQuerySlot = "SlotName like '%T" + String.Format("{0:00}", nTowerNo) + "%'";
                            else
                                strTowerQuerySlot = strTowerQuerySlot + " or SlotName like '%T" + String.Format("{0:00}", nTowerNo) + "%'";
                        }
                    }
                }

                //=====區塊層===============================================================================
                if (AreaQuerySet == true)    //判斷是否開啟區塊查詢
                {
                    if (strGradeName != "") //判斷等級非空值
                    {
                        List<string> NameList = LoadGradeSetListGetName(SStockerSQL.enumGradeType.Area, strGradeName);
                        int nTowerNo = 0;
                        int nAreaNo = 0;
                        //If IsNothing(aryTowerName) = False Then
                        for (int i = 0; i < NameList.Count; i++)                  //依塔柱名稱取槽位
                        {
                            nAreaNo = Convert.ToInt32(NameList[i].Replace("Area", ""));

                            nTowerNo = (nAreaNo % (m_nSTC / m_nSAC)) == 0 ? (nAreaNo / (m_nSTC / m_nSAC)) : (nAreaNo / (m_nSTC / m_nSAC) + 1);


                            if (m_strTowerEnable[nTowerNo - 1] != '1') continue;//塔被禁用


                            for (int j = 1; j < m_nSAC + 1; j++)                  //依塔柱名稱取槽位
                            {
                                int a = ((nAreaNo - 1) % (m_nSTC / m_nSAC)) * m_nSAC + j;

                                if (strAreaQuerySlot == "")
                                    strAreaQuerySlot = strAreaQuerySlot + "SlotName='" + "T" + String.Format("{0:00}", nTowerNo) + "-S" + String.Format("{0:000}", ((nAreaNo - 1) % (m_nSTC / m_nSAC)) * m_nSAC + j) + "'";
                                else
                                    strAreaQuerySlot = strAreaQuerySlot + " or " + "SlotName='" + "T" + String.Format("{0:00}", nTowerNo) + "-S" + String.Format("{0:000}", ((nAreaNo - 1) % (m_nSTC / m_nSAC)) * m_nSAC + j) + "'";
                            }
                        }
                    }
                }

                if (strTowerQuerySlot != "" || strAreaQuerySlot != "")
                {
                    strSQL = strSQL + " and" + " (" + (strTowerQuerySlot != "" ? strTowerQuerySlot : "") + ((strTowerQuerySlot != "" && strAreaQuerySlot != "") ? " or " : "") + (strAreaQuerySlot != "" ? strAreaQuerySlot : "") + ")";

                    if (descending)
                        strSQL += "ORDER BY SlotName DESC";
                    else
                        strSQL += "ORDER BY SlotName ASC";


                    dt = SQLQuery(strSQL);
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 更新 slotstatus table Wafer移除
        /// </summary>
        /// <param name="SlotName"></param>
        public bool UpdateSlotStatus(string SlotName)
        {
            bool bSuc = false;
            try
            {
                //UPDATE `slotstatus` SET `WaferStatus` = '0' WHERE `slotstatus`.`No` = 178;
                string strSQL;
                strSQL = "UPDATE slotstatus SET  WaferStatus = '0', WaferID = NULL, WaferGrade = NULL, M12ID = NULL, LotID = NULL, VCL = NULL" +
                                         " where SlotName = '" + SlotName + "'";
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 更新 slotstatus table Wafer加入
        /// </summary>
        /// <param name="SlotName"></param>
        /// <param name="WaferID"></param>
        /// <param name="WaferGrade"></param>
        /// <param name="M12ID"></param>
        /// <param name="LotID"></param>
        public bool UpdateSlotStatus(string SlotName, string WaferID, string WaferGrade, string M12ID, string LotID)
        {
            bool bSuc = false;
            try
            {
                string strSQL;
                strSQL = "update slotstatus SET  WaferStatus = '1', WaferID ='" + WaferID + "', WaferGrade = '" + WaferGrade + "', M12ID = '" + M12ID + "', LotID = '" + LotID + "', VCL = NULL" +
                                         " where SlotName='" + SlotName + "'";
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }

        /// <summary>
        /// 更新 slotstatus table Wafer加入
        /// </summary>
        public bool UpdateSlotStatus(string SlotName, bool bParticle, bool bDisable)
        {
            bool bSuc = false;
            try
            {
                string strSQL;
                strSQL = "update slotstatus SET  Particle ='" + (bParticle ? 1 : 0) + "'" + ",Disable ='" + (bDisable ? 1 : 0) + "'" +
                                         " where SlotName='" + SlotName + "'";
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }

        /// <summary>
        /// 更新 slotstatus All
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool UpdateSlotStatus(DataTable table)
        {
            bool bSuc = false;
            try
            {
                foreach (DataRow datarow in table.Rows)
                {
                    string WaferID = datarow["WaferID"].ToString();
                    string WaferGrade = datarow["WaferGrade"].ToString();
                    string M12ID = datarow["M12ID"].ToString();
                    string LotID = datarow["LotID"].ToString();
                    string SlotName = datarow["SlotName"].ToString();
                    string WaferStatus = datarow["WaferStatus"].ToString();

                    string Particle = datarow["Particle"].ToString().ToUpper() == "TRUE" ? "1" : "0";
                    string Disable = datarow["Disable"].ToString().ToUpper() == "TRUE" ? "1" : "0";

                    string strSQL = string.Format("update slotstatus SET  WaferStatus ='{0}', WaferID ='{1}', WaferGrade ='{2}', M12ID ='{3}', LotID = '{4}', VCL = NULL , Particle = '{6}' , Disable = '{7}'  where SlotName='{5}'"
                        , WaferStatus, WaferID, WaferGrade, M12ID, LotID, SlotName, Particle, Disable);

                    bSuc = SQLExecute(strSQL);
                    if (bSuc == false) break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 清除 slotstatus table
        /// </summary>
        /// <returns></returns>
        public bool TruncateSlotStatus()
        {
            bool bSuc = false;
            try
            {
                while (true)
                {
                    bSuc = SQLExecute("TRUNCATE TABLE slotstatus");
                    if (bSuc == false) break;
                    bSuc = Check_slotstatus();
                    break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        //
        #region ===== table wafertransfer =====
        /// <summary>
        /// 查詢 wafertransfer table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectWaferTransfer()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select * from wafertransfer";
                dt = SQLQuery(strSQL);

                //考慮只有P->T或T->P則啟動Undo
                DataTable dataTable = dt.Copy();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();

                        if (strSource[0] == strTarget[0])
                        {
                            dataTable.Rows.RemoveAt(i);
                        }
                    }
                }
                dt = dataTable;
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 wafertransfer table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectWaferTransferWithoutPtoP_TtoT()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select * from wafertransfer";
                dt = SQLQuery(strSQL);

                //考慮只有P->T或T->P則啟動Undo
                DataTable dataTable = dt.Copy();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();

                        if (strSource[0] == strTarget[0])
                        {
                            //dataTable.Rows.RemoveAt(i);

                            for (int j = 0; j < dataTable.Rows.Count; j++)
                            {
                                string strSource2 = dataTable.Rows[j]["Source"].ToString();
                                string strTarget2 = dataTable.Rows[j]["Target"].ToString();
                                if (strSource == strSource2 && strTarget == strTarget2)
                                    dataTable.Rows.RemoveAt(j);
                            }


                        }



                    }
                }
                dt = dataTable;
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 wafertransfer table 有資料
        /// </summary>
        /// <returns></returns>
        public bool SelectWaferTransferHasJob()
        {
            bool bHasJob = false;
            try
            {
                string strSQL = "select * from wafertransfer";
                DataTable dt = SQLQuery(strSQL);
                bHasJob = (dt != null && dt.Rows.Count > 0);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bHasJob;
        }
        /// <summary>
        /// 查詢 wafertransfer table 有資料
        /// </summary>
        /// <returns></returns>
        public bool SelectWaferTransferHasJobWithoutPtoP_TtoT()
        {
            bool bHasJob = false;
            try
            {
                string strSQL = "select * from wafertransfer";
                DataTable dt = SQLQuery(strSQL);
                //考慮只有P->T或T->P則啟動Undo
                DataTable dataTable = dt.Copy();
                List<int> listDelect = new List<int>();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();

                        if (strSource[0] == strTarget[0])
                        {
                            //這筆需要刪除
                            listDelect.Insert(0, i);


                        }
                    }
                }

                for (int i = 0; i < listDelect.Count; i++)
                {
                    dataTable.Rows.RemoveAt(listDelect[i]);
                }



                bHasJob = (dataTable != null && dataTable.Rows.Count > 0);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bHasJob;
        }
        /// <summary>
        /// 新增 wafertransfer table 當下傳到目標的紀錄(UNDO使用)
        /// </summary>
        /// <param name="WaferData"></param>
        /// <returns></returns>
        public bool InsertWaferTransfer(SWafer WaferData)
        {
            bool bSuc = false;
            try
            {
                // No Date    Time Source  Target ActionType  WaferID  Commander M12ID   LotID VCL
                string strSource = "";
                string strTarget = "";
                string strType = "";
                //Source
                strSource = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.Owner, WaferData.Slot, WaferData.FoupID);
                //Target
                strTarget = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.ToLoadport, WaferData.ToSlot, WaferData.ToFoupID);
                strType = "OUT";


                string strName = string.Format("Date,Time,Source,Target,ActionType,WaferID,Commander,M12ID,LotID,VCL");
                string strValue = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    DateTime.Now.ToString("yyyyMMdd"),
                    DateTime.Now.ToString("HHmmss"),
                    strSource,
                    strTarget,
                    strType,
                    WaferData.WaferID_B,
                    null,
                    WaferData.WaferID_F,
                    WaferData.LotID,
                    null);

                string strSQL = string.Format("insert into wafertransfer ({0}) values ({1})", strName, strValue);

                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 清除 wafertransfer table
        /// </summary>
        /// <returns></returns>
        public bool TruncateWaferTransfer()
        {
            bool bSuc = false;
            try
            {
                bSuc = SQLExecute("TRUNCATE TABLE wafertransfer");
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        //
        #region ===== table unitstatus ========
        /// <summary>
        /// 查詢 unitstatus table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectUnitStatus()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select* from unitstatus";
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 unitstatus table 單獨
        /// </summary>
        /// <param name="eUnit"></param>
        /// <returns></returns>
        public string GetUnitStatus(enumUnit eUnit)
        {
            string str = string.Empty;
            try
            {
                DataTable dt = SelectUnitStatus();
                if (dt == null || dt.Rows.Count == 0) return str;
                switch (eUnit)
                {
                    case enumUnit.ALN1: str = dt.Rows[0]["AlgAFrom"].ToString(); break;
                    case enumUnit.ALN2: str = dt.Rows[0]["AlgBFrom"].ToString(); break;
                    case enumUnit.TRB1Upper: str = dt.Rows[0]["RbAUpFrom"].ToString(); break;
                    case enumUnit.TRB1Lower: str = dt.Rows[0]["RbALoFrom"].ToString(); break;
                    case enumUnit.TRB2Upper: str = dt.Rows[0]["RbBUpFrom"].ToString(); break;
                    case enumUnit.TRB2Lower: str = dt.Rows[0]["RbBLoFrom"].ToString(); break;
                    case enumUnit.BUF1_slot1: str = dt.Rows[0]["Buf1From"].ToString(); break;
                    case enumUnit.BUF1_slot2: str = dt.Rows[0]["Buf2From"].ToString(); break;
                    //case enumUnit.BUF1_slot3: str = dt.Rows[0]["Buf3From"].ToString(); break;
                    //case enumUnit.BUF1_slot4: str = dt.Rows[0]["Buf4From"].ToString(); break;
                    case enumUnit.BUF2_slot1: str = dt.Rows[0]["Buf3From"].ToString(); break;
                    case enumUnit.BUF2_slot2: str = dt.Rows[0]["Buf4From"].ToString(); break;
                        //case enumUnit.BUF2_slot3: str = dt.Rows[0]["Buf3From"].ToString(); break;
                        //case enumUnit.BUF2_slot4: str = dt.Rows[0]["Buf4From"].ToString(); break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return str;
        }
        /// <summary>
        /// 查詢 unitstatus table 單獨
        /// </summary>
        /// <param name="eUnit"></param>
        /// <returns></returns>
        public string GetUnitWaferID(enumUnit eUnit)
        {
            string str = string.Empty;
            try
            {
                DataTable dt = SelectUnitStatus();
                if (dt == null || dt.Rows.Count == 0) return str;
                switch (eUnit)
                {
                    case enumUnit.ALN1: str = dt.Rows[0]["AlgAWaferID"].ToString(); break;
                    case enumUnit.ALN2: str = dt.Rows[0]["AlgBWaferID"].ToString(); break;
                    case enumUnit.TRB1Upper: str = dt.Rows[0]["RbAUpWaferID"].ToString(); break;
                    case enumUnit.TRB1Lower: str = dt.Rows[0]["RbALoWaferID"].ToString(); break;
                    case enumUnit.TRB2Upper: str = dt.Rows[0]["RbBUpWaferID"].ToString(); break;
                    case enumUnit.TRB2Lower: str = dt.Rows[0]["RbBLoWaferID"].ToString(); break;
                    case enumUnit.BUF1_slot1: str = dt.Rows[0]["Buf1WaferID"].ToString(); break;
                    case enumUnit.BUF1_slot2: str = dt.Rows[0]["Buf2WaferID"].ToString(); break;
                    case enumUnit.BUF2_slot1: str = dt.Rows[0]["Buf3WaferID"].ToString(); break;
                    case enumUnit.BUF2_slot2: str = dt.Rows[0]["Buf4WaferID"].ToString(); break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return str;
        }
        /// <summary>
        /// 更新 unitstatus table
        /// </summary>
        /// <param name="eUnit"></param>
        /// <param name="nBody"></param>
        /// <param name="nSlot"></param>
        /// <param name="WaferData"></param>
        /// <returns></returns>
        public bool UpdateUnitStatus(enumUnit eUnit, SWafer WaferData = null)
        {
            bool bSuc = false;
            try
            {
                string tmpFrom = WaferData != null ? WaferData.Owner + "_" + WaferData.Slot + "_" + WaferData.FoupID : null;      //暫存Source
                string tmpID = WaferData != null ? WaferData.WaferID_F + "_" + WaferData.WaferID_B : null; //暫存WaferID

                string strSQL = "";

                switch (eUnit)
                {
                    case enumUnit.TRB1Upper:
                        strSQL = "update UnitStatus set RbAUpFrom='" + tmpFrom + "',RbAUpWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.TRB1Lower:
                        strSQL = "update UnitStatus set RbALoFrom='" + tmpFrom + "',RbALoWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.TRB2Upper:
                        strSQL = "update UnitStatus set RbBUpFrom='" + tmpFrom + "',RbBUpWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.TRB2Lower:
                        strSQL = "update UnitStatus set RbBLoFrom='" + tmpFrom + "',RbBLoWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.ALN1:
                        strSQL = "update UnitStatus set AlgAFrom='" + tmpFrom + "',AlgAWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.ALN2:
                        strSQL = "update UnitStatus set AlgBFrom='" + tmpFrom + "',AlgBWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF1_slot1:
                        strSQL = "update UnitStatus set Buf1From='" + tmpFrom + "',Buf1WaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF1_slot2:
                        strSQL = "update UnitStatus set Buf2From='" + tmpFrom + "',Buf2WaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF2_slot1:
                        strSQL = "update UnitStatus set Buf3From='" + tmpFrom + "',Buf3WaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF2_slot2:
                        strSQL = "update UnitStatus set Buf4From='" + tmpFrom + "',Buf4WaferID='" + tmpID + "'";
                        break;
                }
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 清除 unitstatus table
        /// </summary>
        /// <returns></returns>
        public bool TruncateUnitStatus()
        {
            bool bSuc = false;
            try
            {
                string strSQL = "truncate table UnitStatus";
                bSuc = SQLExecute(strSQL);
                strSQL = "INSERT INTO UnitStatus (" +
                        "RbAUpFrom,RbAUpWaferID,RbAUpWaferGrade" +
                        ",RbALoFrom,RbALoWaferID,RbALoWaferGrade" +
                        ",RbBUpFrom,RbBUpWaferID,RbBUpWaferGrade" +
                        ",RbBLoFrom,RbBLoWaferID,RbBLoWaferGrade" +
                        ",AlgAFrom,AlgAWaferID,AlgAWaferGrade" +
                        ",AlgBFrom,AlgBWaferID,AlgBWaferGrade" +
                        ",AlgABuFrom,AlgABuWaferID,AlgABuWaferGrade" +
                        ",AlgBBuFrom,AlgBBuWaferID,AlgBBuWaferGrade" +
                        ",Buf1From,Buf1WaferID,Buf1WaferGrade" +
                        ",Buf2From,Buf2WaferID,Buf2WaferGrade" +
                        ",Buf3From,Buf3WaferID,Buf3WaferGrade" +
                        ",Buf4From,Buf4WaferID,Buf4WaferGrade" +
                        ",CoinStkFrom,CoinStkWaferID,CoinStkWaferGrade)" +
                        " values (NULL, NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL)";
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }

        #endregion
        //
        #region ===== table processstatus =====
        /// <summary>
        /// 查詢 processstatus table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectProcessStatus()
        {
            try
            {
                string strSQL = "SELECT * from ProcessStatus";
                DataTable dt = SQLQuery(strSQL);
                return dt;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 清除 processstatus table
        /// </summary>
        /// <returns></returns>
        public void TruncateProcessStatus()
        {
            string strSQL = "";
            strSQL = "TRUNCATE TABLE ProcessStatus";
            SQLExecute(strSQL);
            strSQL = "insert into ProcessStatus (Date,Time,ProcessEndStatus) values (NULL, NULL, '1')";
            SQLExecute(strSQL);
        }

        #endregion
        //
        #region ===== table changelog =========
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <param name="strName"></param>
        /// <param name="tmpChangeID"></param>
        public void InsertChangeLog(enumGradeType eGradeType, string strName, int tmpChangeID)
        {
            try
            {
                string strSQL = "INSERT INTO ChangeLog (ChangeID,Date,Time,SlotName,ChangeFlag)" +
                             " values ('" + tmpChangeID + "','" + DateTime.Now.ToString("yyyyMMdd") + "'" +
                             ",'" + DateTime.Now.ToString("HHmmss") + "','" + strName + "','" +
                             (eGradeType == enumGradeType.Tower ? "3" :
                              eGradeType == enumGradeType.Area ? "2" : "1") + "')";
                SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
        }
        /// <summary>
        /// 查詢 changelog table
        /// </summary>
        /// <param name="eGradeType"></param>
        /// <returns></returns>
        public List<string> SelectChangeLog(enumGradeType eGradeType)
        {
            try
            {
                List<string> strList = new List<string>();
                string strSQL = "";

                //'讀取Tower Set異動記錄

                strSQL = "SELECT a.Date,a.Time,a.SlotName,b.ChangeType,b.ChangeGrade" +
                         " from ChangeLog AS a, ChangeHistory AS b" +
                         " where a.ChangeID = b.ChangeID" +
                         " and a.ChangeFlag='" +
                         (eGradeType == enumGradeType.Tower ? "3" :
                          eGradeType == enumGradeType.Area ? "2" : "1") + "'" +
                         " order by Date desc,Time desc,SlotName desc";// +
                                                                       //" limit " + strTowerLog + "";

                DataTable dt = SQLQuery(strSQL);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow item in dt.Rows)
                    {
                        strList.Add(item["Date"].ToString() + "          " + item["Time"].ToString() + "          " + item["SlotName"].ToString() + "          " + item["ChangeType"].ToString() + "          " + item["ChangeGrade"].ToString());
                    }
                }

                return strList;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 查詢 changelog table
        /// </summary>
        /// <returns></returns>
        public int SelectChangeLogMaxChangeID() // 取異動編號
        {
            DataTable dt = new DataTable();
            string strSQL = "Select Max(ChangeID) from ChangeLog";
            dt = SQLQuery(strSQL);

            if (dt.Rows[0][0].GetType().ToString() == "System.DBNull")
            {
                return 1;
            }
            else
            {
                return Convert.ToInt32(dt.Rows[0][0].ToString()) + 1;
            }
        }
        #endregion
        //
        #region ===== table ChangeHistory =====
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="eChangeType"></param>
        /// <param name="strName"></param>
        /// <param name="tmpChangeID"></param>
        public void InsertChangeHistoryLog(enumChangeType eChangeType, string strName, int tmpChangeID)
        {
            try
            {
                string strSQL = "insert into ChangeHistory (ChangeID,ChangeType,ChangeGrade)" +
                                " values ('" + tmpChangeID + (eChangeType == enumChangeType.Add ? "','add','" : "','del','") + strName + "')";

                SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
        }
        #endregion
        //
        #region ===== table waferlog=====
        /// <summary>
        /// 查詢 waferlog table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectWaferlog(DateTime dateStart, DateTime dateEnd,
            string txbWaferLogQuerySource, string txbWaferLogQueryTarget, string txbWaferLogQueryActionType,
            string txbWaferLogQueryWaferID, string txbWaferLogQueryWaferGrade, string txbWaferLogQueryCommander,
            string txbWaferLogQueryM12ID, string txbWaferLogQueryLotID, string txbWaferLogQueryVCL, string nLogCount)
        {
            Dictionary<string, string> strNameSubstitutionList = new Dictionary<string, string>();
            strNameSubstitutionList["WaferID"] = "T7";
            strNameSubstitutionList["M12ID"] = "M12";

            DataTable dt = null;
            try
            {
                string strSQL = "select No,Date,Time,Source,Target,ActionType"
                    + ",WaferID As " + strNameSubstitutionList["WaferID"]
                    + ",WaferGrade,Commander"
                    + ",M12ID As " + strNameSubstitutionList["M12ID"]
                    + ",LotID,VCL From WaferLog"
                    + " where Date>='" + dateStart.ToString("yyyyMMdd") + "'"
                    + " and Date<='" + dateEnd.ToString("yyyyMMdd") + "'";

                if (txbWaferLogQuerySource != "") strSQL = strSQL + " and Source like '%" + txbWaferLogQuerySource + "%'";
                if (txbWaferLogQueryTarget != "") strSQL = strSQL + " and Target like '%" + txbWaferLogQueryTarget + "%'";
                if (txbWaferLogQueryActionType != "") strSQL = strSQL + " and ActionType like '%" + txbWaferLogQueryActionType + "%'";
                if (txbWaferLogQueryWaferID != "") strSQL = strSQL + " and WaferID like '%" + txbWaferLogQueryWaferID + "%'";
                if (txbWaferLogQueryWaferGrade != "") strSQL = strSQL + " and WaferGrade like '%" + txbWaferLogQueryWaferGrade + "%'";
                if (txbWaferLogQueryCommander != "") strSQL = strSQL + " and Commander like '%" + txbWaferLogQueryCommander + "%'";
                if (txbWaferLogQueryM12ID != "") strSQL = strSQL + " and M12ID like '%" + txbWaferLogQueryM12ID + "%'";
                if (txbWaferLogQueryLotID != "") strSQL = strSQL + " and LotID like '%" + txbWaferLogQueryLotID + "%'";
                if (txbWaferLogQueryVCL != "") strSQL = strSQL + " and VCL like '%" + txbWaferLogQueryVCL + "%'";
                strSQL = strSQL + " limit " + nLogCount;

                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 新增 waferlog table 當下傳到目標的紀錄
        /// </summary>
        /// <param name="WaferData"></param>
        /// <returns></returns>
        public bool InsertWaferlog(SWafer WaferData)
        {
            bool bSuc = false;
            try
            {
                // No Date    Time Source  Target ActionType  WaferID Commander M12ID   LotID VCL
                string strSource = "";
                string strTarget = "";
                string strType = "";
                //Source
                strSource = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.Owner, WaferData.Slot, WaferData.FoupID);
                //Target
                strTarget = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.ToLoadport, WaferData.ToSlot, WaferData.ToFoupID);

                strType = "Sotr";

                string strName = string.Format("Date,Time,Source,Target,ActionType,WaferID,Commander,M12ID,LotID,VCL");
                string strValue = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    DateTime.Now.ToString("yyyyMMdd"),
                    DateTime.Now.ToString("HHmmss"),
                    strSource,
                    strTarget,
                    strType,
                    WaferData.WaferID_B,
                    null,
                    WaferData.WaferID_F,
                    WaferData.LotID,
                    null);

                string strSQL = string.Format("insert into waferlog ({0}) values ({1})", strName, strValue);

                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }


        #endregion

    }

    public class SSSorterSQL : SMySQL
    {
        /*
              processstatus
        slotstatus
        unitstatus
        waferlog
        wafertransfer
        */
      
        public enum enumChangeType { Add, Del }


        public SSSorterSQL(string ip, string name, string user, string password) : base(ip, name, user, password)
        {

        }


        #region ===== table wafertransfer =====
        /// <summary>
        /// 查詢 wafertransfer table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectWaferTransfer()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select * from wafertransfer";
                dt = SQLQuery(strSQL);

                //考慮只有P->T或T->P則啟動Undo
                DataTable dataTable = dt.Copy();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();

                        if (strSource[0] == strTarget[0])
                        {
                            dataTable.Rows.RemoveAt(i);
                        }
                    }
                }
                dt = dataTable;
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 wafertransfer table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectWaferTransferWithoutPtoP_TtoT()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select * from wafertransfer";
                dt = SQLQuery(strSQL);

                //考慮只有P->T或T->P則啟動Undo
                DataTable dataTable = dt.Copy();
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();

                        if (strSource[0] == strTarget[0])
                        {
                            //dataTable.Rows.RemoveAt(i);

                            for (int j = 0; j < dataTable.Rows.Count; j++)
                            {
                                string strSource2 = dataTable.Rows[j]["Source"].ToString();
                                string strTarget2 = dataTable.Rows[j]["Target"].ToString();
                                if (strSource == strSource2 && strTarget == strTarget2)
                                    dataTable.Rows.RemoveAt(j);
                            }


                        }



                    }
                }
                dt = dataTable;
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 wafertransfer table 有資料
        /// </summary>
        /// <returns></returns>
        public bool SelectWaferTransferHasJob()
        {
            bool bHasJob = false;
            try
            {
                string strSQL = "select * from wafertransfer";
                DataTable dt = SQLQuery(strSQL);
                bHasJob = (dt != null && dt.Rows.Count > 0);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bHasJob;
        }
        /// <summary>
        /// 查詢 wafertransfer table 有資料
        /// </summary>
        /// <returns></returns>
        public bool SelectWaferTransferHasJobWithoutPtoP_TtoT()
        {
            bool bHasJob = false;
            try
            {
                string strSQL = "select * from wafertransfer";
                DataTable dt = SQLQuery(strSQL);
                //考慮只有P->T或T->P則啟動Undo
                DataTable dataTable = dt.Copy();
                List<int> listDelect = new List<int>();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string strSource = dt.Rows[i]["Source"].ToString();
                        string strTarget = dt.Rows[i]["Target"].ToString();

                        if (strSource[0] == strTarget[0])
                        {
                            //這筆需要刪除
                            listDelect.Insert(0, i);


                        }
                    }
                }

                for (int i = 0; i < listDelect.Count; i++)
                {
                    dataTable.Rows.RemoveAt(listDelect[i]);
                }



                bHasJob = (dataTable != null && dataTable.Rows.Count > 0);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bHasJob;
        }
        /// <summary>
        /// 新增 wafertransfer table 當下傳到目標的紀錄(UNDO使用)
        /// </summary>
        /// <param name="WaferData"></param>
        /// <returns></returns>
        public bool InsertWaferTransfer(SWafer WaferData)
        {
            bool bSuc = false;
            try
            {
                // No Date    Time Source  Target ActionType  WaferID  Commander M12ID   LotID VCL
                string strSource = "";
                string strTarget = "";
                string strType = "";
                //Source
                strSource = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.Owner, WaferData.Slot, WaferData.FoupID);
                //Target
                strTarget = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.ToLoadport, WaferData.ToSlot, WaferData.ToFoupID);
                strType = "OUT";


                string strName = string.Format("Date,Time,Source,Target,ActionType,WaferID,Commander,M12ID,LotID,VCL");
                string strValue = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    DateTime.Now.ToString("yyyyMMdd"),
                    DateTime.Now.ToString("HHmmss"),
                    strSource,
                    strTarget,
                    strType,
                    WaferData.WaferID_B,
                    null,
                    WaferData.WaferID_F,
                    WaferData.LotID,
                    null);

                string strSQL = string.Format("insert into wafertransfer ({0}) values ({1})", strName, strValue);

                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 清除 wafertransfer table
        /// </summary>
        /// <returns></returns>
        public bool TruncateWaferTransfer()
        {
            bool bSuc = false;
            try
            {
                bSuc = SQLExecute("TRUNCATE TABLE wafertransfer");
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        #endregion
        //
        #region ===== table unitstatus ========
        /// <summary>
        /// 查詢 unitstatus table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectUnitStatus()
        {
            DataTable dt = null;
            try
            {
                string strSQL = "select* from unitstatus";
                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 查詢 unitstatus table 單獨
        /// </summary>
        /// <param name="eUnit"></param>
        /// <returns></returns>
        public string GetUnitStatus(enumUnit eUnit)
        {
            string str = string.Empty;
            try
            {
                DataTable dt = SelectUnitStatus();
                if (dt == null || dt.Rows.Count == 0) return str;
                switch (eUnit)
                {
                    case enumUnit.ALN1: str = dt.Rows[0]["AlgAFrom"].ToString(); break;
                    case enumUnit.ALN2: str = dt.Rows[0]["AlgBFrom"].ToString(); break;
                    case enumUnit.TRB1Upper: str = dt.Rows[0]["RbAUpFrom"].ToString(); break;
                    case enumUnit.TRB1Lower: str = dt.Rows[0]["RbALoFrom"].ToString(); break;
                    case enumUnit.TRB2Upper: str = dt.Rows[0]["RbBUpFrom"].ToString(); break;
                    case enumUnit.TRB2Lower: str = dt.Rows[0]["RbBLoFrom"].ToString(); break;
                    case enumUnit.BUF1_slot1: str = dt.Rows[0]["Buf1From"].ToString(); break;
                    case enumUnit.BUF1_slot2: str = dt.Rows[0]["Buf2From"].ToString(); break;
                    //case enumUnit.BUF1_slot3: str = dt.Rows[0]["Buf3From"].ToString(); break;
                    //case enumUnit.BUF1_slot4: str = dt.Rows[0]["Buf4From"].ToString(); break;
                    case enumUnit.BUF2_slot1: str = dt.Rows[0]["Buf3From"].ToString(); break;
                    case enumUnit.BUF2_slot2: str = dt.Rows[0]["Buf4From"].ToString(); break;
                        //case enumUnit.BUF2_slot3: str = dt.Rows[0]["Buf3From"].ToString(); break;
                        //case enumUnit.BUF2_slot4: str = dt.Rows[0]["Buf4From"].ToString(); break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return str;
        }
        /// <summary>
        /// 查詢 unitstatus table 單獨
        /// </summary>
        /// <param name="eUnit"></param>
        /// <returns></returns>
        public string GetUnitWaferID(enumUnit eUnit)
        {
            string str = string.Empty;
            try
            {
                DataTable dt = SelectUnitStatus();
                if (dt == null || dt.Rows.Count == 0) return str;
                switch (eUnit)
                {
                    case enumUnit.ALN1: str = dt.Rows[0]["AlgAWaferID"].ToString(); break;
                    case enumUnit.ALN2: str = dt.Rows[0]["AlgBWaferID"].ToString(); break;
                    case enumUnit.TRB1Upper: str = dt.Rows[0]["RbAUpWaferID"].ToString(); break;
                    case enumUnit.TRB1Lower: str = dt.Rows[0]["RbALoWaferID"].ToString(); break;
                    case enumUnit.TRB2Upper: str = dt.Rows[0]["RbBUpWaferID"].ToString(); break;
                    case enumUnit.TRB2Lower: str = dt.Rows[0]["RbBLoWaferID"].ToString(); break;
                    case enumUnit.BUF1_slot1: str = dt.Rows[0]["Buf1WaferID"].ToString(); break;
                    case enumUnit.BUF1_slot2: str = dt.Rows[0]["Buf2WaferID"].ToString(); break;
                    case enumUnit.BUF2_slot1: str = dt.Rows[0]["Buf3WaferID"].ToString(); break;
                    case enumUnit.BUF2_slot2: str = dt.Rows[0]["Buf4WaferID"].ToString(); break;
                }
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return str;
        }
        /// <summary>
        /// 更新 unitstatus table
        /// </summary>
        /// <param name="eUnit"></param>
        /// <param name="nBody"></param>
        /// <param name="nSlot"></param>
        /// <param name="WaferData"></param>
        /// <returns></returns>
        public bool UpdateUnitStatus(enumUnit eUnit, SWafer WaferData = null)
        {
            bool bSuc = false;
            try
            {
                string tmpFrom = WaferData != null ? WaferData.Owner + "_" + WaferData.Slot + "_" + WaferData.FoupID : null;      //暫存Source
                string tmpID = WaferData != null ? WaferData.WaferID_F + "_" + WaferData.WaferID_B : null; //暫存WaferID

                string strSQL = "";

                switch (eUnit)
                {
                    case enumUnit.TRB1Upper:
                        strSQL = "update UnitStatus set RbAUpFrom='" + tmpFrom + "',RbAUpWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.TRB1Lower:
                        strSQL = "update UnitStatus set RbALoFrom='" + tmpFrom + "',RbALoWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.TRB2Upper:
                        strSQL = "update UnitStatus set RbBUpFrom='" + tmpFrom + "',RbBUpWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.TRB2Lower:
                        strSQL = "update UnitStatus set RbBLoFrom='" + tmpFrom + "',RbBLoWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.ALN1:
                        strSQL = "update UnitStatus set AlgAFrom='" + tmpFrom + "',AlgAWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.ALN2:
                        strSQL = "update UnitStatus set AlgBFrom='" + tmpFrom + "',AlgBWaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF1_slot1:
                        strSQL = "update UnitStatus set Buf1From='" + tmpFrom + "',Buf1WaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF1_slot2:
                        strSQL = "update UnitStatus set Buf2From='" + tmpFrom + "',Buf2WaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF2_slot1:
                        strSQL = "update UnitStatus set Buf3From='" + tmpFrom + "',Buf3WaferID='" + tmpID + "'";
                        break;
                    case enumUnit.BUF2_slot2:
                        strSQL = "update UnitStatus set Buf4From='" + tmpFrom + "',Buf4WaferID='" + tmpID + "'";
                        break;
                }
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }
        /// <summary>
        /// 清除 unitstatus table
        /// </summary>
        /// <returns></returns>
        public bool TruncateUnitStatus()
        {
            bool bSuc = false;
            try
            {
                string strSQL = "truncate table UnitStatus";
                bSuc = SQLExecute(strSQL);
                strSQL = "INSERT INTO UnitStatus (" +
                        "RbAUpFrom,RbAUpWaferID,RbAUpWaferGrade" +
                        ",RbALoFrom,RbALoWaferID,RbALoWaferGrade" +
                        ",RbBUpFrom,RbBUpWaferID,RbBUpWaferGrade" +
                        ",RbBLoFrom,RbBLoWaferID,RbBLoWaferGrade" +
                        ",AlgAFrom,AlgAWaferID,AlgAWaferGrade" +
                        ",AlgBFrom,AlgBWaferID,AlgBWaferGrade" +
                        ",AlgABuFrom,AlgABuWaferID,AlgABuWaferGrade" +
                        ",AlgBBuFrom,AlgBBuWaferID,AlgBBuWaferGrade" +
                        ",Buf1From,Buf1WaferID,Buf1WaferGrade" +
                        ",Buf2From,Buf2WaferID,Buf2WaferGrade" +
                        ",Buf3From,Buf3WaferID,Buf3WaferGrade" +
                        ",Buf4From,Buf4WaferID,Buf4WaferGrade" +
                        ",CoinStkFrom,CoinStkWaferID,CoinStkWaferGrade)" +
                        " values (NULL, NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL" +
                        ",NULL,NULL,NULL)";
                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }

        #endregion
        //
        #region ===== table processstatus =====
        /// <summary>
        /// 查詢 processstatus table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectProcessStatus()
        {
            try
            {
                string strSQL = "SELECT * from ProcessStatus";
                DataTable dt = SQLQuery(strSQL);
                return dt;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 清除 processstatus table
        /// </summary>
        /// <returns></returns>
        public void TruncateProcessStatus()
        {
            string strSQL = "";
            strSQL = "TRUNCATE TABLE ProcessStatus";
            SQLExecute(strSQL);
            strSQL = "insert into ProcessStatus (Date,Time,ProcessEndStatus) values (NULL, NULL, '1')";
            SQLExecute(strSQL);
        }

        #endregion
        
      
        //
        #region ===== table ChangeHistory =====
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="eChangeType"></param>
        /// <param name="strName"></param>
        /// <param name="tmpChangeID"></param>
        public void InsertChangeHistoryLog(enumChangeType eChangeType, string strName, int tmpChangeID)
        {
            try
            {
                string strSQL = "insert into ChangeHistory (ChangeID,ChangeType,ChangeGrade)" +
                                " values ('" + tmpChangeID + (eChangeType == enumChangeType.Add ? "','add','" : "','del','") + strName + "')";

                SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
        }
        #endregion
        //
        #region ===== table waferlog=====
        /// <summary>
        /// 查詢 waferlog table
        /// </summary>
        /// <returns></returns>
        public DataTable SelectWaferlog(DateTime dateStart, DateTime dateEnd,
            string txbWaferLogQuerySource, string txbWaferLogQueryTarget, string txbWaferLogQueryActionType,
            string txbWaferLogQueryWaferID, string txbWaferLogQueryWaferGrade, string txbWaferLogQueryCommander,
            string txbWaferLogQueryM12ID, string txbWaferLogQueryLotID, string txbWaferLogQueryVCL, string nLogCount)
        {
            Dictionary<string, string> strNameSubstitutionList = new Dictionary<string, string>();
            strNameSubstitutionList["WaferID"] = "T7";
            strNameSubstitutionList["M12ID"] = "M12";

            DataTable dt = null;
            try
            {
                string strSQL = "select No,Date,Time,Source,Target,ActionType"
                    + ",WaferID As " + strNameSubstitutionList["WaferID"]
                    + ",WaferGrade,Commander"
                    + ",M12ID As " + strNameSubstitutionList["M12ID"]
                    + ",LotID,VCL From WaferLog"
                    + " where Date>='" + dateStart.ToString("yyyyMMdd") + "'"
                    + " and Date<='" + dateEnd.ToString("yyyyMMdd") + "'";

                if (txbWaferLogQuerySource != "") strSQL = strSQL + " and Source like '%" + txbWaferLogQuerySource + "%'";
                if (txbWaferLogQueryTarget != "") strSQL = strSQL + " and Target like '%" + txbWaferLogQueryTarget + "%'";
                if (txbWaferLogQueryActionType != "") strSQL = strSQL + " and ActionType like '%" + txbWaferLogQueryActionType + "%'";
                if (txbWaferLogQueryWaferID != "") strSQL = strSQL + " and WaferID like '%" + txbWaferLogQueryWaferID + "%'";
                if (txbWaferLogQueryWaferGrade != "") strSQL = strSQL + " and WaferGrade like '%" + txbWaferLogQueryWaferGrade + "%'";
                if (txbWaferLogQueryCommander != "") strSQL = strSQL + " and Commander like '%" + txbWaferLogQueryCommander + "%'";
                if (txbWaferLogQueryM12ID != "") strSQL = strSQL + " and M12ID like '%" + txbWaferLogQueryM12ID + "%'";
                if (txbWaferLogQueryLotID != "") strSQL = strSQL + " and LotID like '%" + txbWaferLogQueryLotID + "%'";
                if (txbWaferLogQueryVCL != "") strSQL = strSQL + " and VCL like '%" + txbWaferLogQueryVCL + "%'";
                strSQL = strSQL + " limit " + nLogCount;

                dt = SQLQuery(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return dt;
        }
        /// <summary>
        /// 新增 waferlog table 當下傳到目標的紀錄
        /// </summary>
        /// <param name="WaferData"></param>
        /// <returns></returns>
        public bool InsertWaferlog(SWafer WaferData)
        {
            bool bSuc = false;
            try
            {
                // No Date    Time Source  Target ActionType  WaferID Commander M12ID   LotID VCL
                string strSource = "";
                string strTarget = "";
                string strType = "";
                //Source
                strSource = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.Owner, WaferData.Slot, WaferData.FoupID);
                //Target
                strTarget = string.Format("P{0:D2}-S{1:D3}-{2}", (int)WaferData.ToLoadport, WaferData.ToSlot, WaferData.ToFoupID);

                strType = "Sotr";

                string strName = string.Format("Date,Time,Source,Target,ActionType,WaferID,Commander,M12ID,LotID,VCL");
                string strValue = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'",
                    DateTime.Now.ToString("yyyyMMdd"),
                    DateTime.Now.ToString("HHmmss"),
                    strSource,
                    strTarget,
                    strType,
                    WaferData.WaferID_B,
                    null,
                    WaferData.WaferID_F,
                    WaferData.LotID,
                    null);

                string strSQL = string.Format("insert into waferlog ({0}) values ({1})", strName, strValue);

                bSuc = SQLExecute(strSQL);
            }
            catch (Exception ex) { WriteLog("<Exception>:" + ex); }
            return bSuc;
        }


        #endregion

    }
}
