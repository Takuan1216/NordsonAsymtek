using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using RorzeComm.Log;
using System.IO;
using System.Data;

using System.Collections.Concurrent;
using RorzeComm.Threading;
using System.Runtime.CompilerServices;

namespace RorzeUnit.Data
{
    /// <summary>
    /// Compact DB automatically when mdb file size out of limit (1.5GB)
    /// </summary>
    public class SAccessDb
    {
        //========== variable
        protected SLogger _logger;
        private string _strDbPath;                  //Database完整路徑
        private string _strPassword;                //Database開啟密碼 3090-1647
        private bool _bCompactDB;                   //Enable/Disable database compact automatically function.
        private OleDbCommand _oleCmd;
        private OleDbConnection _oleConn;           //OleDb連線物件  
        private long _lngFileLimit = 1500000000;    //資料庫檔案容量上限 (default 1.5 GB)

        private DateTime _dtLastCompact;            //上一次壓縮資料庫的時間
        private bool _bCompacting = false;          //資料庫壓縮中
        private SSignal _signalCompact;             //資料庫壓縮中
        private string _strDBName;                  //Database name
        private SPollingThread _pollingSQLFile;
        private DateTime _dtLastBackup = DateTime.Now;
        private ConcurrentQueue<string> _queSQL = new ConcurrentQueue<string>();
        private bool _bIsOutOfLimit;                //是否已超過額定上限 1.8GB

        //========== property
        public bool IsOpen { get { return _oleConn.State != ConnectionState.Closed; } }
        public bool IsCompacting
        {
            get { return _bCompacting; }
            private set
            {
                _bCompacting = value;
                if (_bCompacting)
                {
                    WriteLog(string.Format("[Compact] Ready to compact database [{0}], file size = {1}.", _strDBName, GetFileSize()));
                    _signalCompact.Reset();   //設定壓縮中訊號 for hold reader function.
                    if (OnCompacting != null) OnCompacting(this, new EventArgs());
                }
                else
                {
                    WriteLog(string.Format("[Compact] Compact database completed [{0}], file size = {1}.", _strDBName, GetFileSize()));
                    _signalCompact.Set(); //Reset壓縮中訊號 for hold reader function.
                    if (OnCompacted != null) OnCompacted(this, new EventArgs());
                }
            }
        }
        public bool IsOutOfLimit
        {
            get { return _bIsOutOfLimit; }
            private set
            {
                _bIsOutOfLimit = value;
                if (_bIsOutOfLimit)
                {
                    if (OnOutOfLimitAlarm != null) OnOutOfLimitAlarm(this, new EventArgs());
                }
            }
        }

        //========== event
        public event StateChangeEventHandler OnConnectStateChange;
        public event EventHandler OnCompacting;                     //資料庫壓縮中
        public event EventHandler OnCompacted;                      //資料庫壓縮完成
        public event EventHandler OnOutOfLimitAlarm;                //異常事件, 超過額定大小 (1.8GB)

        protected void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("{0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        //========== constructor
        public SAccessDb(string strDbPath, string strPassword, bool bAutoCompact = false, int nFileSizeLimit = 1500000000)
        {
            _logger = SLogger.GetLogger("ExecuteLog");
            _strDbPath = strDbPath;
            _strPassword = strPassword;
            _strDBName = Path.GetFileNameWithoutExtension(_strDbPath);

            _oleConn = new OleDbConnection();
            _oleCmd = new OleDbCommand();

            _oleConn.ConnectionString = string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;Data source={0};Jet OLEDB:Database Password={1};", _strDbPath, _strPassword);

            _oleConn.StateChange += new StateChangeEventHandler(_oleConn_StateChange);







            //polling each SQL text file.
            _dtLastCompact = DateTime.Now.AddDays(-1);
            _bCompactDB = bAutoCompact;
            _lngFileLimit = nFileSizeLimit;
            _bCompacting = false;
            _signalCompact = new SSignal(true, System.Threading.EventResetMode.ManualReset);
            _pollingSQLFile = new SPollingThread(1000);
            _pollingSQLFile.DoPolling += RunCheckLimit;

            if (_bCompactDB)//要壓縮的才需要set
                _pollingSQLFile.Set();

        }

        //========== process
        void RunCheckLimit()
        {
            try
            {
                if ((_queSQL.Count > 0) && IsOpen) //buffer有資料且Database開啟
                {
                    string strSQL;
                    do
                    {
                        lock (this)
                        {
                            while (_queSQL.TryDequeue(out strSQL))
                            {
                                _SQLExec(strSQL);
                                System.Threading.Thread.Sleep(10); //wait database assign data.
                            }
                        }
                    } while (_queSQL.Count > 200); //buffer內資料超過200筆則繼續flush資料
                }

                if (!_bCompactDB) return; //disable to check file limit 


                long lngCurrSize = GetFileSize();
                IsOutOfLimit = lngCurrSize > 1800000000; //超過1.8GB
                if (lngCurrSize < _lngFileLimit) return; //檔案未超過limit size
                if ((DateTime.Now - _dtLastCompact).TotalMinutes < 5) return; //5min 前已做過壓縮

                this.CompactDB();

                _dtLastCompact = DateTime.Now;
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        //========== event handler
        void _oleConn_StateChange(object sender, StateChangeEventArgs e)
        {
            try
            {
                if (OnConnectStateChange != null) OnConnectStateChange(this, e);
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }

        //========== member function
        public void Open()
        {
            if (_oleConn.State == System.Data.ConnectionState.Closed)
            {
                if (!File.Exists(_strDbPath)) throw new Exception(string.Format("Database not found!! [{0}]", _strDbPath));
                _oleConn.Open();
                _oleCmd.Connection = _oleConn;
                // _pollingSQLFile.Set(); //
            }
        }
        public void Close()
        {
            try
            {
                _oleConn.Close();
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
        }
        public DataTable GetSchema( string tableName)
        {
            return _oleConn.GetSchema("Columns", new[] { null, null, tableName, null });
        }
        public int SQLExec(string strSQL)
        {
            try
            {
                if (_oleConn.State == ConnectionState.Closed) //資料庫關閉中則assign to buffer, 待寫入
                {
                    _queSQL.Enqueue(strSQL);
                    return 0;
                }
                else
                {
                    lock (this)
                    {
                        _oleCmd.CommandText = strSQL;
                        return _oleCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("strSQL:" + strSQL + " " + ex);
                WriteLog("[Exception] " + ex);
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
                _signalCompact.WaitOne(); //無窮等待至資料庫壓縮完成
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
                WriteLog("[Exception] " + ex);
                return new DataSet();
            }
        }
        public DataSet Reader(string format, params object[] args)
        {
            return Reader(string.Format(format, args));
        }
        public virtual void CompactDB()
        {
            IsCompacting = true;    //設定壓縮中旗標 for status

            lock (this)
            {
                //關閉Db
                this.Close();

                if (Directory.Exists(@"D:\temp\") == false)
                {
                    Directory.CreateDirectory(@"D:\temp\");
                }

                //參數列
                object[] oParams = new object[]
                {
                    _oleConn.ConnectionString,
                    string.Format("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=D:\\temp\\{0};" +
                    "Jet OLEDB:Engine Type=5;" +
                    "Jet OLEDB:Database Password={1}",Path.GetFileName(_strDbPath),_strPassword)
                };
                //委派方法
                object objJRO = Activator.CreateInstance(Type.GetTypeFromProgID("JRO.JetEngine"));
                //執行委派 (壓縮中)
                objJRO.GetType().InvokeMember("CompactDatabase",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null, objJRO, oParams);

                //刪除目前Db
                File.Delete(_strDbPath);
                File.Move(string.Format(@"D:\temp\{0}", Path.GetFileName(_strDbPath)), _strDbPath);
                //重新open Db
                this.Open();

            }
            IsCompacting = false;
        }
        public long GetFileSize()
        {
            FileInfo fileInfo = new FileInfo(_strDbPath);
            if (!fileInfo.Exists) return -2;
            return fileInfo.Length;
        }

        private void DeleteDB(string strPath)
        {
            int Retry = 0;
            while (Retry < 500)
            {
                try
                {
                    File.Delete(strPath);
                    WriteLog(string.Format("[{0}] Retry {1} times to delete Database.", _strDBName, Retry));
                    return; //成功刪除才return

                }
                catch (Exception ex)
                {
                    WriteLog("[Exception] " + ex);
                }
            }
        }
        private int _SQLExec(string strSQL)
        {
            try
            {
                //lock (this)
                {
                    _oleCmd.CommandText = strSQL;
                    return _oleCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }
            return 0;
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
        public static SAccessDb[] GetDBs(string path, string strDatabaseName, string strPassword, string strPassKeyWord = "")
        {
            List<SAccessDb> _lstDB = new List<SAccessDb>();

            foreach (string file in Directory.GetFiles(path))
            {
                if (!file.ToLower().EndsWith("mdb")) continue;
                if (file.Contains(strPassKeyWord)) continue;
                if (!file.Contains(strDatabaseName)) continue;

                _lstDB.Add(new SAccessDb(file, strPassword));
            }
            return _lstDB.ToArray();
        }
        public static string DateIndex(int groupSize)
        {
            return string.Format("{0}_{1}", DateTime.Now.Year, DateTime.Now.DayOfYear / groupSize);
        }

        private void DeleteTest()
        {

            try
            {


            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
            }

        }



        public string GetSqlType(Type type)
        {
            if (type == typeof(string))
                return "TEXT";
            if (type == typeof(int))
                return "INTEGER";
            if (type == typeof(DateTime))
                return "DATETIME";
            if (type == typeof(bool))
                return "BIT";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return "DECIMAL";

            // 默認為文本類型
            return "TEXT";
        }

    }
}
