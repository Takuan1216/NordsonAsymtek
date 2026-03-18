using System;
using System.Collections.Generic;
using RorzeUnit.Data;
using System.Data;
using RorzeComm.Log;
using RorzeUnit.Class;
using System.Runtime.CompilerServices;


namespace RorzeApi.Class
{
    public class SPermission
    {
        public event EventHandler OnLogin;
        public event EventHandler OnLogout;


        private SMainDB _dbMain;                                        //  DB  
        private string _strUserID = string.Empty;
        private int _nUserLevel = 9;
        private bool _bIsLogin = false;

        private bool _bTeachingEnable = false;
        private bool _bInitialenEnable = false;
        private bool _bMaintenanceEnable = false;
        private bool _bSetupEnable = false;
        private bool _bLogRecordEnable = false;

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[MDI] {0}  at line {1} ({2})", strContent, lineNumber, meberName);
            _logger.WriteLog(strMsg);
        }

        public string UserID { get { return _strUserID; } }
        public int Level { get { return _nUserLevel; } }
        public bool IsLogin { get { return _bIsLogin; } }




        public bool TeachingEnable { get { return _bTeachingEnable; } }
        public bool InitialenEnable { get { return _bInitialenEnable; } }
        public bool MaintenanceEnable { get { return _bMaintenanceEnable; } }
        public bool SetupEnable { get { return _bSetupEnable; } }
        public bool LogRecordEnable { get { return _bLogRecordEnable; } }



        public SPermission(SMainDB db)
        {
            _dbMain = db;
        }

        //  使用者已登入
        public bool Login(string strUserID, string strPassword)
        {
            DataSet ds1 = _dbMain.Reader("Select * from UserLogin Where UserPassword = '{0}'", strPassword);

            DataSet ds = _dbMain.Reader("Select * from UserLogin Where UserName = '{0}' And UserPassword = '{1}'", strUserID, strPassword);
            //DataSet dt = _dbMain.Reader(string.Format("Select * from UserLogin where UserName = '{0}'", strUserID));

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                _strUserID = ds.Tables[0].Rows[0]["UserName"].ToString();
                _nUserLevel = (int)ds.Tables[0].Rows[0]["UserLevel"];

                _bTeachingEnable = Convert.ToBoolean(ds.Tables[0].Rows[0]["Teaching"]);
                _bInitialenEnable = Convert.ToBoolean(ds.Tables[0].Rows[0]["Initialen"]);
                _bMaintenanceEnable = Convert.ToBoolean(ds.Tables[0].Rows[0]["Maintenance"]);
                _bSetupEnable = Convert.ToBoolean(ds.Tables[0].Rows[0]["Setup"]);
                _bLogRecordEnable = Convert.ToBoolean(ds.Tables[0].Rows[0]["Log"]);

                _bIsLogin = true;
                if (OnLogin != null) OnLogin(this, new EventArgs());
                WriteLog(string.Format("LogIn user:{0} level:{1}", _strUserID, _nUserLevel));
                return true;
            }
            else if (ds1.Tables.Count > 0 && ds1.Tables[0].Rows.Count > 0 && strUserID == "smartrfid")
            {
                _strUserID = ds1.Tables[0].Rows[0]["UserName"].ToString();
                _nUserLevel = (int)ds1.Tables[0].Rows[0]["UserLevel"];

                _bTeachingEnable = Convert.ToBoolean(ds1.Tables[0].Rows[0]["Teaching"]);
                _bInitialenEnable = Convert.ToBoolean(ds1.Tables[0].Rows[0]["Initialen"]);
                _bMaintenanceEnable = Convert.ToBoolean(ds1.Tables[0].Rows[0]["Maintenance"]);
                _bSetupEnable = Convert.ToBoolean(ds1.Tables[0].Rows[0]["Setup"]);
                _bLogRecordEnable = Convert.ToBoolean(ds1.Tables[0].Rows[0]["Log"]);

                _bIsLogin = true;
                if (OnLogin != null) OnLogin(this, new EventArgs());
                WriteLog(string.Format("RFID LogIn user:{0} level:{1}", _strUserID, _nUserLevel));
                return true;
            }
            else
            {
         
                Logout();
                return false;
            }
        }
        //  使用者已登出
        public void Logout()
        {
            WriteLog(string.Format("Logout user:{0} level:{1}", _strUserID, _nUserLevel));
            _strUserID = string.Empty;
            _nUserLevel = 9;
            _bTeachingEnable = false;
            _bInitialenEnable = false;
            _bMaintenanceEnable = false;
            _bSetupEnable = false;
            _bLogRecordEnable = false;
            _bIsLogin = false;
            if (OnLogout != null) OnLogout(this, new EventArgs());
        }
        //  取得所有使用者名稱
        public string[] GetPermissionUser()
        {
            DataTable dt = _dbMain.Reader("Select * From UserLogin").Tables[0];
            List<string> lstGroup = new List<string>();
            for (int nCnt = 0; nCnt < dt.Rows.Count; nCnt++)
                lstGroup.Add(dt.Rows[nCnt]["UserName"].ToString());
            return lstGroup.ToArray();
        }







    }
}
