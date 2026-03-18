using System;
using System.Text;
using System.Data.OleDb;
using System.IO;
using System.Data;
using RorzeComm.Log;

namespace RorzeUnit.Data
{
    public class SAccessDbv1
    {
        protected SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private string _strDbPath;      //Database完整路徑
        private string _strPassword;    //Database開啟密碼 3090-1647

        private OleDbCommand _oleCmd;
        private OleDbConnection _oleConn; //OleDb連線物件
        public bool IsOpen { get { return _oleConn.State != ConnectionState.Closed; } }
        public event StateChangeEventHandler OnConnectStateChange;

        public SAccessDbv1(string strDbPath, string strPassword)
        {
            _strDbPath = strDbPath;
            _strPassword = strPassword;

            _oleConn = new OleDbConnection();
            _oleCmd = new OleDbCommand();

            _oleConn.ConnectionString = string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;Data source={0};Jet OLEDB:Database Password={1};", _strDbPath, _strPassword);
            _oleConn.StateChange += new StateChangeEventHandler(_oleConn_StateChange);
        }

        void _oleConn_StateChange(object sender, StateChangeEventArgs e)
        {
            try
            {
                if (OnConnectStateChange != null) OnConnectStateChange(this, e);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        public void Open()
        {
            if (_oleConn.State == System.Data.ConnectionState.Closed)
            {
                if (!File.Exists(_strDbPath)) throw new Exception(string.Format("Database not found!! [{0}]", _strDbPath));
                _oleConn.Open();
                _oleCmd.Connection = _oleConn;
            }
        }

        public void Close()
        {
          //  if (_oleConn.State != System.Data.ConnectionState.Closed)
            {
                try
                {
                    _oleConn.Close();
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex);
                }
            }
        }

        public int SQLExec(string strSQL)
        {
            try
            {
                lock (this)
                {
                    _oleCmd.CommandText = strSQL;
                    return _oleCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return 0;
            }
        }
        public int SQLExec(string format, params object[] args)
        {
            return SQLExec(string.Format(format, args));
        }

        public DataSet Reader(string strSQL)
        {
            DataSet dsData = new DataSet();
            try
            {
                lock (this)
                {
                    _oleCmd.CommandText = strSQL;
                    OleDbDataAdapter oleAdapter = new OleDbDataAdapter(_oleCmd);
                    oleAdapter.Fill(dsData);
                    return dsData;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return new DataSet();
            }
        }
        public DataSet Reader(string format, params object[] args)
        {
            return Reader(string.Format(format, args));
        }

        public static string DateTimeFormat(DateTime dt)
        {
            return dt.ToString("yyy/MM/dd HH:mm:ss");
        }
        public static void ExportCSV(DataTable table, string path, bool bAppend = false, bool bPrintHeadRow = true)
        {
            if (table.Rows.Count <= 0)
            {
                SLogger.GetLogger("DataBase").WriteLog("[Export] export csv file failue. data table is empty.");
                return;
            }
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                SLogger.GetLogger("DataBase").WriteLog("[Export] export csv file failue. export path not exist. [{0}]", path);
                return;
            }
            StreamWriter sw = new StreamWriter(path, bAppend, Encoding.UTF8); //覆寫(false) or 附加(true)檔案
            //========== 寫欄位名
            if (bPrintHeadRow)
            {
                for (int nCol = 0; nCol < table.Columns.Count; nCol++)
                    sw.Write("{0},", table.Columns[nCol].ColumnName);
                sw.WriteLine();
            }
            //========== table資料
            for (int nRow = 0; nRow < table.Rows.Count; nRow++)
            {
                for (int nCol = 0; nCol < table.Columns.Count; nCol++)
                    sw.Write("{0},", table.Rows[nRow][nCol]);
                sw.WriteLine();
            }
            sw.Flush();
            sw.Close();
            SLogger.GetLogger("DataBase").WriteLog("[Export] export csv file completed. table=[{0}]. path=[{1}]", table.TableName, path);
        }
        public void CompactDB()
        {
            lock (this)
            {
                //關閉Db
                this.Close();
                //參數列
                object[] oParams = new object[]
                {
                    _oleConn.ConnectionString,
                    string.Format("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=D:\\Rorze\\{0};Jet OLEDB:Engine Type=5;Jet OLEDB:Database Password={1}",Path.GetFileName(_strDbPath),_strPassword)
                                                                                                    
                };
                //委派方法
                object objJRO = Activator.CreateInstance(Type.GetTypeFromProgID("JRO.JetEngine"));
                //執行委派 (壓縮中)
                objJRO.GetType().InvokeMember("CompactDatabase", System.Reflection.BindingFlags.InvokeMethod, null, objJRO, oParams);
                //刪除目前Db
                File.Delete(_strDbPath);
                File.Move(string.Format(@"D:\Rorze\{0}", Path.GetFileName(_strDbPath)), _strDbPath);
                //重新open Db
                this.Open();
            }
        }
    }
}
