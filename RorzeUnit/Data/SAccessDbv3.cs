using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using System.IO;
using System.Data;

using System.Collections.Concurrent;
using RorzeComm.Log;
using RorzeComm.Threading;

namespace RorzeUnit.Data
{
    /// <summary>
    /// Split DB automatically when mdb file size out of limit (1.5GB)
    /// </summary>
    public class SAccessDbv3
    {
        //========== variable
        protected SLogger _logger;
        private string _strDbPath;                  //Database完整路徑
        private string _strPassword;                //Database開啟密碼 3090-1647
        private bool _bSplitDB;                   //Enable/Disable database split automatically function.
        private OleDbCommand _oleCmd;
        private OleDbConnection _oleConn;           //OleDb連線物件  
        private long _lngFileLimit = 1500000000;    //資料庫檔案容量上限 (default 1.5 GB)

        //private DateTime _dtLastCompact;            //上一次壓縮資料庫的時間
        private DateTime _dtLastSplit;              //上一次分割資料庫時間
        private bool _bCompacting = false;          //資料庫壓縮中
        private SSignal _signalCompact;             //資料庫壓縮中
        private string _strDBName;                  //Database name
        private string _strSampleDBPath;            //sample database路徑
        private SPollingThread _pollingSQLFile;
        private DateTime _dtLastBackup = DateTime.Now;
        private ConcurrentQueue<string> _queSQL = new ConcurrentQueue<string>();
        private bool _bIsOutOfLimit;                //是否已超過額定上限 1.8GB

        //========== property
        public bool IsOpen { get { return _oleConn.State != ConnectionState.Closed; } }
        public bool IsCompacting {
            get { return _bCompacting; }
            private set
            {
                _bCompacting = value;
                if (_bCompacting)
                {
                    _logger.WriteLog("[Compact] Ready to compact database [{0}], file size = {1}.", _strDBName, GetFileSize());
                    _signalCompact.Reset();   //設定壓縮中訊號 for hold reader function.
                    if (OnCompacting != null) OnCompacting(this, new EventArgs());
                }
                else
                {
                    _logger.WriteLog("[Compact] Compact database completed [{0}], file size = {1}.", _strDBName, GetFileSize());
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

        //========== constructor
        public SAccessDbv3(string strDbPath, string strPassword, string strSampleDB = "", bool bAutoSplit = false, int nFileSizeLimit = 1500000000)
        {
            _logger = SLogger.GetLogger("DataBase");
            _strDbPath = strDbPath;
            _strPassword = strPassword;
            _strDBName = Path.GetFileNameWithoutExtension(_strDbPath);

            _strSampleDBPath = strSampleDB; //資料庫母片
            
            _oleConn = new OleDbConnection();
            _oleCmd = new OleDbCommand();

            _oleConn.ConnectionString = string.Format(@"Provider=Microsoft.Jet.OLEDB.4.0;Data source={0};Jet OLEDB:Database Password={1};", _strDbPath, _strPassword);
            _oleConn.StateChange += new StateChangeEventHandler(_oleConn_StateChange);

            //polling each SQL text file.
            _dtLastSplit = DateTime.Now.AddDays(-1);
            _bSplitDB = bAutoSplit;
            _lngFileLimit = nFileSizeLimit;
            _bCompacting = false;
            _signalCompact = new SSignal(true, System.Threading.EventResetMode.ManualReset);
            _pollingSQLFile = new SPollingThread(200);
            _pollingSQLFile.DoPolling += RunCheckLimit;
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
                
                if(!_bSplitDB) return; //disable to check file limit 
                long lngCurrSize = GetFileSize();
                IsOutOfLimit = lngCurrSize > 1800000000; //超過1.8GB
                if(lngCurrSize < _lngFileLimit) return; //檔案未超過limit size
                if ((DateTime.Now - _dtLastSplit).TotalSeconds < 10) return; //5min前已分割資料庫
                if (!File.Exists(_strSampleDBPath)) return; //母片不存在

                //分割資料庫
                SplitDB(); 

                _dtLastSplit = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
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
                _logger.WriteLog(ex);
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
                _pollingSQLFile.Set(); //
            }
        }
        public void Close(bool bDispose = false)
        {
            try
            {
                _oleConn.Close();
                if(bDispose) _oleConn.Dispose();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
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
                _logger.WriteLog(ex);
                return new DataSet();
            }
        }
        public DataSet Reader(string format, params object[] args)
        {
            return Reader(string.Format(format, args));
        }
        public void CompactDB()
        {
            IsCompacting = true;    //設定壓縮中旗標 for status
            
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
            IsCompacting = false;
        }

        public void SplitDB()
        {
            lock (this)
            {

                _logger.WriteLog("[Switch DB] start to switch database. current size = {0}", GetFileSize());
                this.Close();

                //搜尋目前檔案流水號
                int nFileIndex = 1;
                foreach (string file in Directory.GetFiles(Path.GetDirectoryName(_strDbPath)))
                {
                    string strDbName = Path.GetFileNameWithoutExtension(file);
                    if (strDbName.Contains(_strDBName))
                    {
                        int nCurr = 0;
                        if (int.TryParse(strDbName.Substring(strDbName.IndexOf('_') + 1), out nCurr))
                            if (nCurr > nFileIndex) nFileIndex = nCurr;
                    }
                }
                //遞增下一個檔案流水號
                nFileIndex++;
                //資料庫重新命名
                Rename(string.Format("{0}_{1}.mdb", _strDBName, nFileIndex));
                //複製sample database
                File.Copy(_strSampleDBPath, _strDbPath);
                _logger.WriteLog("[Switch DB] switch database completed. current size = {0}", GetFileSize());
                this.Open();
            }
        }
        
        public long GetFileSize()
        {
            FileInfo fileInfo = new FileInfo(_strDbPath);
            if(!fileInfo.Exists) return -2;
            return fileInfo.Length;
        }
        private void Rename(string strNewName)
        {
            if (this.IsOpen) throw new Exception("<<<Error>>> Rename failure due to database is open.");
            File.Move(_strDbPath, Path.GetDirectoryName(_strDbPath) +  @"\" +  strNewName);
        }
        
        private void DeleteDB(string strPath)
        {
            int Retry = 0;
            while (Retry < 500)
            {
                try
                {
                    File.Delete(strPath);
                    _logger.WriteLog("[{0}] Retry {1} times to delete Database.", _strDBName, Retry);
                    return; //成功刪除才return

                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} Exception caught.", ex);
                    System.Threading.Thread.Sleep(300);
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
                _logger.WriteLog(ex);
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
    }
}
