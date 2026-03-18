using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace RorzeComm
{
    public class SLogger : IDisposable
    {
        private StreamWriter _sw;
        private string _strName;
        private string _strPath;
        private string _strRootPath = System.Environment.CurrentDirectory + @"\RorzeSecsLog";
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
            CreateLogger();
            lock (_lockLogger) // 新增：保護 Dictionary 操作
            {
                if (!_dicFiles.ContainsKey(_strName))
                    _dicFiles.Add(_strName, this);
            }
        }

        private void CreateLogger()
        {
            //驗證路徑
            _strPath = _strRootPath;
            if(_strPath[_strPath.Length-1] != '\\') _strPath += "\\";
            _strPath += DateTime.Now.ToString("yyy");
            _strPath += @"\" + DateTime.Now.ToString("yyyMM");
            _strPath += @"\" + DateTime.Now.ToString("yyyMMdd");
            //建立路徑
            Directory.CreateDirectory(_strPath + @"\");
            //建立檔案
            _sw = new StreamWriter(string.Format(@"{0}\{1}.log", _strPath, _strName), true);
            //畫押日期
            _nDay = DateTime.Now.Day;
        }

        public void WriteLog(string strMsg)
        {
            try
            {
                lock (this)
                {                    
                    if (strMsg == _strLastMsg) _nRepeat++;
                    else _nRepeat = 0;
                    _strLastMsg = strMsg;

                    if (_nRepeat > 3) return;

                    if (_nDay != DateTime.Now.Day) CreateLogger();

                    _sw.WriteLine(DateTime.Now.ToString("yyy/MM/dd HH:mm:ss.fff") + "\t" + strMsg);
                    _sw.Flush();
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
            ////========== 例外資訊不完整
            //StackTrace trace = new StackTrace(ex, true);
            //StackFrame frame = trace.GetFrame(trace.FrameCount - 1);
            //int errorLine = frame.GetFileLineNumber();
            //string strClassName = frame.GetMethod().ReflectedType.Name;
            //string strMathodName = frame.GetMethod().ToString();
            //WriteLog("<<<Exception>>> {0}_{1} at line {2} {3}", strClassName, strMathodName, errorLine, ex.Message);
        }

        public void WriteLog(string strFormName, Exception ex)
        {
            WriteLog("<<<Exception>>> [{0}] {1}", strFormName, ex.ToString());
            //========== 例外資訊不完整
            //StackTrace trace = new StackTrace(ex, true);
            //StackFrame frame = trace.GetFrame(trace.FrameCount - 1);
            //int errorLine = frame.GetFileLineNumber();
            //string strClassName = frame.GetMethod().ReflectedType.Name;
            //string strMathodName = frame.GetMethod().ToString();
            //WriteLog("<<<Exception>>> [{0}] {1}_{2} at line {3} {4}",strFormName, strClassName, strMathodName, errorLine, ex.Message);
        }

        private static Dictionary<string, SLogger> _dicFiles = new Dictionary<string, SLogger>();
        private static readonly object _lockLogger = new object(); // 新增：線程安全鎖

        public static SLogger GetLogger(string strName)
        {
            lock (_lockLogger) // 新增：鎖保護
            {
                if (_dicFiles.ContainsKey(strName)) return _dicFiles[strName];
                else
                {
                    SLogger newLogger = new SLogger(strName);
                    return newLogger;
                }
            }
        }

        public void Dispose()
        {
            _sw.Close(); 
        }
    }
}
