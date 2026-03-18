using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace RorzeComm.Log
{
    public class SLogger : IDisposable
    {
        private StreamWriter _sw;
        private string _strName;
        private string _strPath;
        private string _strRootPath = System.Environment.CurrentDirectory + @"\Log";
        private int _nDay = 0;
        private string _strLastMsg = string.Empty;
        private int _nRepeat = 0;

        public SLogger(string Name, string strPath)
        {
            _strRootPath = strPath;
            _strName = Name;
            CreateLogger();
            lock (_lockLogger) // 新增：保護 Dictionary 操作
            {
                if (!_dicFiles.ContainsKey(_strName))
                    _dicFiles.Add(_strName, this);
            }
        }
        public SLogger(string Name)
        {
            _strName = Name;
            //CreateLogger();
            lock (_lockLogger) // 新增：保護 Dictionary 操作
            {
                if (!_dicFiles.ContainsKey(_strName))
                {
                    _dicFiles.Add(_strName, this);
                    _dicRJLog.Add(_strName, new RJServer(_strName));//MING
                }
            }
        }

        private void CreateLogger()
        {
            //驗證路徑
            _strPath = _strRootPath;

            _strPath += @"\" + DateTime.Now.ToString("yyyMMdd");

            //建立路徑
            Directory.CreateDirectory(_strPath + @"\");
            //建立檔案
            _sw = new StreamWriter(string.Format(@"{0}\{1}.log", _strPath, _strName), true);
            //畫押日期
            _nDay = DateTime.Now.Day;
        }


  
        public void WriteLog(string strMsg/*, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null*/)
        {
            try
            {
                lock (this)
                {

                    strMsg = strMsg.Replace('\r', ' ').Replace('\n', ' ');
                    //strMsg = strMsg + " at line " + lineNumber + " (" + caller + ")";

                    if (strMsg == _strLastMsg) _nRepeat++;
                    else _nRepeat = 0;
                    _strLastMsg = strMsg;

                    //  if (_nRepeat > 3) return;

                    if (_nDay != DateTime.Now.Day) CreateLogger();

                    _sw.WriteLine(DateTime.Now.ToString("yyy/MM/dd HH:mm:ss.fff") + "\t" + strMsg);
                    _sw.Flush();

                    if (_dicRJLog[_strName] != null && _dicRJLog[_strName].IsConnect)
                        _dicRJLog[_strName].SendData(DateTime.Now.ToString("yyy/MM/dd HH:mm:ss.fff") + "\t" + strMsg);//Ming
                }
            }
            catch (Exception ex)
            {
                _sw.Close();
                CreateLogger();
                Console.WriteLine("<<Exception>> write log failure!!!! {0}", ex.ToString());
            }
        }

        public void WriteLog(string strMsg, params object[] args)
        {
            WriteLog(string.Format(strMsg, args));
        }



        public void WriteLog(Exception ex)
        {
            WriteLog("<<<Exception>>> {0}", ex.ToString());
        }
        public void WriteLog(string strFormName, Exception ex)
        {
            WriteLog("<<<Exception>>> [{0}] {1}", strFormName, ex.ToString());
        }

        private static Dictionary<string, SLogger> _dicFiles = new Dictionary<string, SLogger>();
        private static Dictionary<string, RJServer> _dicRJLog = new Dictionary<string, RJServer>();//Ming
        private static readonly object _lockLogger = new object(); // 新增：線程安全鎖

        public static SLogger GetLogger(string strName)
        {
            lock (_lockLogger) // 新增：鎖保護整個方法
            {
                if (_dicFiles.ContainsKey(strName))
                {
                    return _dicFiles[strName];
                }
                else
                {
                    SLogger newLogger = new SLogger(strName);

                    string[] vn = System.Windows.Forms.Application.ProductVersion.Split('.');
                    string strVersionDate = System.Windows.Forms.Application.ProductName;
                    string strVersion = strVersionDate + " v" + vn[0] + '.' + vn[1] + vn[2] + vn[3];

                    newLogger.WriteLog("++++++++++++++++++++++++++++++++++++++");
                    newLogger.WriteLog("+++++++ " + strVersion + " +++++++");
                    newLogger.WriteLog("++++++++++++++++++++++++++++++++++++++");
                    return newLogger;
                }
            }
        }

        public void Dispose()
        {
            lock (_lockLogger) // 新增：保護 Dictionary 訪問
            {
                foreach (SLogger item in _dicFiles.Values)
                {
                    item._sw.Close();
                }
            }
        }
    }
}
