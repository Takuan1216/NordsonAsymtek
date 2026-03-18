using RorzeUnit.Class;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RorzeAPI.Equipments.Combination.SScreen;
using static RorzeUnit.Net.Sockets.sClient;

namespace RorzeApi.Class
{
    public class SGroupRecipeManager
    {
        SMainDB _dbMain;

        Dictionary<string, SGroupRecipe> m_dicGroupReicpe = new Dictionary<string, SGroupRecipe>();

        public Dictionary<string, SGroupRecipe> GetRecipeGroupList { get { return m_dicGroupReicpe; } }
        public SGroupRecipeManager(SMainDB DB)
        {

            _dbMain = DB;
            GetAllRecipe();

        }
        private void GetAllRecipe()
        {

            DataSet ds = _dbMain.Reader("Select * from GroupRecipe");

            string strTableName = "GroupRecipe";

            #region -------------------- 檢查 創建

            //判斷如果沒有db裡面沒有table
            if (ds.Tables.Count == 0)
            {

                _dbMain.SQLExec($"CREATE TABLE {strTableName} (ID AUTOINCREMENT PRIMARY KEY)");

                //_dbMain.SQLExec("create table GroupRecipe (" +
                //    "HistoryTime datetime," +
                //    "RecipeName varchar(50)," +
                //    "EQRecipe varchar(50)," +
                //    "M12 varchar(50)," +
                //    "T7 varchar(50)," +
                //    "HistoryUser varchar(50)" +
                //    ");");
            }

            DataTable _DataTable = _dbMain.Reader("Select * from GroupRecipe").Tables[0];

            // 獲取現有欄位
            var existingColumns = _DataTable.Columns;

            // 獲取類的屬性作為應有的欄位
            var requiredColumns = typeof(SGroupRecipe).GetProperties();

            // 檢查並添加缺少的欄位
            foreach (var prop in requiredColumns)
            {
                string columnName = prop.Name.Substring(1); // 去掉前面的下劃線
                if (!existingColumns.Contains(columnName))
                {
                    string sqlType = _dbMain.GetSqlType(prop.PropertyType);
                    _dbMain.SQLExec($"ALTER TABLE {strTableName} ADD COLUMN {columnName} {sqlType}");
                }
            }

            #endregion

            foreach (DataRow row in _DataTable.Rows)//每一列
            {
                var groupRecipe = new SGroupRecipe();
                for (int i = 0; i < _DataTable.Columns.Count; i++)//每一行
                {
                    var property = typeof(SGroupRecipe).GetProperty("_" + _DataTable.Columns[i].ColumnName);//屬性多一_但Table沒有

                    if (property != null && row[i] != null)
                    {
                        object value = row[i];
                        // 處理可能的類型轉換
                        if (value.GetType() != property.PropertyType)
                        {
                            value = Convert.ChangeType(value, property.PropertyType);
                        }
                        property.SetValue(groupRecipe, value, null);
                    }
                }
                m_dicGroupReicpe[groupRecipe._RecipeName] = groupRecipe;
            }




        }
        public void UpdateRecipe(string GroupRecipe)
        {
            _dbMain.SQLExec("Update GroupRecipe Set EQ_ProcessEnable = '{0}', EQRecipe = '{1}', M12 = '{2}' , T7 = '{3}' , HistoryUser = '{4}', HistoryTime = '{5}' Where RecipeName = '{6}'"
                , m_dicGroupReicpe[GroupRecipe]._EQ_ProcessEnable
                , m_dicGroupReicpe[GroupRecipe]._EQRecipe
                , m_dicGroupReicpe[GroupRecipe]._M12
                , m_dicGroupReicpe[GroupRecipe]._T7
                , m_dicGroupReicpe[GroupRecipe]._HistoryUser
                , m_dicGroupReicpe[GroupRecipe]._HistoryTime
                , GroupRecipe);
        }
        public void InsertRecipe(string GroupRecipe)
        {
            _dbMain.SQLExec("INSERT INTO GroupRecipe (RecipeName, EQ_ProcessEnable, EQRecipe, M12, T7, HistoryUser, HistoryTime) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                GroupRecipe
                , m_dicGroupReicpe[GroupRecipe]._EQ_ProcessEnable
                , m_dicGroupReicpe[GroupRecipe]._EQRecipe
                , m_dicGroupReicpe[GroupRecipe]._M12
                , m_dicGroupReicpe[GroupRecipe]._T7
                , m_dicGroupReicpe[GroupRecipe]._HistoryUser
                , m_dicGroupReicpe[GroupRecipe]._HistoryTime
                );
        }
        public void DeleteRecipe(string GroupRecipe)
        {
            _dbMain.SQLExec("Delete * From  GroupRecipe Where RecipeName = '{0}'", GroupRecipe);
            if (m_dicGroupReicpe.ContainsKey(GroupRecipe))
                m_dicGroupReicpe.Remove(GroupRecipe);
        }

        public void ModifyRecipe(string GroupName, List<bool> listEQProcessEnable, List<string> listEQRecipe, string M12Recipe, string T7Recipe, string user)
        {
            SGroupRecipe Temprecipe = new SGroupRecipe(GroupName, listEQProcessEnable, listEQRecipe, M12Recipe, T7Recipe, user, DateTime.Now);

            if (!m_dicGroupReicpe.ContainsKey(GroupName))
            {
                m_dicGroupReicpe.Add(GroupName, Temprecipe);
                InsertRecipe(GroupName);
            }
            else
            {
                m_dicGroupReicpe[GroupName] = Temprecipe;
                UpdateRecipe(GroupName);
            }
        }

    }


    public class SGroupRecipe
    {
        public string _RecipeName { get; set; }
        //[Browsable(false)]
        public string _EQ_ProcessEnable { get; set; }
        public string _EQRecipe { get; set; }
        public string _M12 { get; set; }
        public string _T7 { get; set; }
        public string _HistoryUser { get; set; }
        public DateTime _HistoryTime { get; set; }


        public string[] GetEQ_Recipe() { return _EQRecipe.Split(','); }

        public bool[] GetEQ_ProcessEnable()
        {
            if (_EQ_ProcessEnable == null) return null;

            return _EQ_ProcessEnable.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s =>
                    {
                        if (int.TryParse(s, out int num))
                            return num != 0;

                        if (bool.TryParse(s, out bool b))
                            return b;

                        return s.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }).ToArray();
        }

        public SGroupRecipe()
        {

        }

        public SGroupRecipe(string strRecipeName, List<bool> listEQProcessEnable, List<string> listEQRecipe, string M12, string T7, string strHistoryUser, DateTime time)
        {
            _RecipeName = strRecipeName;
            _EQ_ProcessEnable = string.Join(",", listEQProcessEnable.Select(b => b ? "true" : "false"));
            _EQRecipe = string.Join(",", listEQRecipe); ;
            _M12 = M12;
            _T7 = T7;
            _HistoryUser = strHistoryUser;
            _HistoryTime = time;
        }


    }

}
