using Rorze.Secs;
using Rorze.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rorze.DB
{
  public  class MainDB : SAccessDb
    {
        public MainDB():base(System.Environment.CurrentDirectory +@"\"+ "RorzeSecs.MDB", "Rorze")
        {

        }
        public string GetECID(string strParameter)
        {
            try
            {
                DataTable dt = this.Reader("Select * From ECIDList Where ECName = '{0}'", strParameter).Tables[0];
                return dt.Rows.Count > 0 ? dt.Rows[0]["ECV"].ToString() : "";
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return string.Empty;
            }
        }

        public void GetGem300Config(ref string IP,ref int port)
        {
            DataTable _DataTable = this.Reader("Select * From ToolParameter").Tables[0];
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                switch (_DataTable.Rows[i]["ParameterName"].ToString())
                {
                    case "Gem300IP":
                        IP = _DataTable.Rows[i]["ParameterValue"].ToString();
                        break;
                    case "Gem300Port":
                        port = Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString());
                        break;
                }

            }

        }

        public void GetSECSParameter(ref SECSParameterConfig Parameter)
        {
            try
            {
                DataTable _DataTable = this.Reader("Select * From SECSParameter").Tables[0];
                string[] Value;
                string Function ;
                string Stream;
                for (int i=0;i<_DataTable.Rows.Count;i++)
                {
                    switch (_DataTable.Rows[i]["ParameterName"].ToString())
                    {
                        case "Config Alarm":
                            Value = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F');
                            Function = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[1];
                            Stream = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[0].Split('S')[1];
                            Parameter.AlarmFunction.Stream = (QsStream)Convert.ToInt16(Stream);
                            Parameter.AlarmFunction.Function = (QsFunction)Convert.ToInt16(Function);
                            break;
                        case "Config Connect":
                            Value = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F');
                            Function = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[1];
                            Stream = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[0].Split('S')[1];
                            Parameter.ConnectFunction.Stream = (QsStream)Convert.ToInt16(Stream);
                            Parameter.ConnectFunction.Function = (QsFunction)Convert.ToInt16(Function);
                            break;
                        case "Online Substatus":
                            Parameter.OnlineSubStats = (_DataTable.Rows[i]["ParameterValue"].ToString().ToUpper()== "ONLINELOCAL")? SECSStats.ONLINELOCAL: SECSStats.ONLINEREMOTE;
                            break;
                        case "WBITS10":
                            Parameter.S10Wbit = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0)?true:false;
                            break;
                        case "WBITS5":
                            Parameter.S5Wbit = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0) ? true : false;
                            break;
                        case "WBITS6":
                            Parameter.S6Wbit = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0) ? true : false;
                            break;
                        case "PJ Slotmap Bypass":
                            Parameter.PJSlotmapBypass = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0) ? true : false;
                            break;
                        case "CarrireIDByHost":
                            Parameter.CarrireIDByHost = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) >0) ? true : false;
                            break;
                        case "Continuous Run job":
                            Parameter.CanExcuteJob = Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;

                        case "SECS Driver":

                            Parameter.GemDriver = (_DataTable.Rows[i]["ParameterValue"].ToString().ToUpper() == "SDR") ? SECSDriver.SDR : SECSDriver.ITRI;


                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }

        }
        public void GetSECSConnectParameter(ref SECSConneetConfig Parameter)
        {
            try
            {
                DataTable _DataTable = this.Reader("Select * From SECSParameter").Tables[0];
               
                for (int i = 0; i < _DataTable.Rows.Count; i++)
                {
                    switch (_DataTable.Rows[i]["ParameterName"].ToString())
                    {
                        case "LocalIP":
                            Parameter.LocalIP = _DataTable.Rows[i]["ParameterValue"].ToString();
     
                            break;
                        case "LocalPort":
                            Parameter.LocalPort = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                           
                            break;
                        case "PortType":
                            Parameter.Mode = (_DataTable.Rows[i]["ParameterValue"].ToString().ToUpper() == "HSMS") ? SECSMODE.HSMS_MODE : SECSMODE.SECS_MODE;
                            break;
                        case "DeviceID":
                            Parameter.DDEVICEID = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;
                        case "T3":
                            Parameter.T3 = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;
                        case "T5":
                            Parameter.T5 = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;
                        case "T6":
                            Parameter.T6 = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;
                        case "T7":
                            Parameter.T7 = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;
                        case "T8":
                            Parameter.T8 = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }

        }


        public DataTable GetRecipeBobyDefine(string Table)
        {
            try
            {
                DataTable _DataTable = this.Reader(string.Format("Select * From {0}", Table)).Tables[0];
                return _DataTable;

            }
            catch (Exception ex)
            {

                return null;
            }
        }
        public void SetSECSParameter(SECSParameterConfig Parameter)
        {
            try
            {
                string Alarm = string.Format("{0}{1}", Parameter.AlarmFunction.Stream.ToString(), Parameter.AlarmFunction.Function.ToString());
                WriteSetting("SECSParameter", "Config Alarm", Alarm, "S5F1");
                string Connect = string.Format("{0}{1}", Parameter.ConnectFunction.Stream.ToString(), Parameter.ConnectFunction.Function.ToString());
                WriteSetting("SECSParameter", "Config Connect", Connect, "S1F13");
                string OnlineSubStats = (Parameter.OnlineSubStats == SECSStats.ONLINELOCAL) ? "ONLINELOCAL" : "ONLINEREMOTE";
                WriteSetting("SECSParameter", "Online Substatus", OnlineSubStats, "ONLINEREMOTE");
                string WBitS10 = (Parameter.S10Wbit) ? "1" : "0";
                string WBitS5 = (Parameter.S5Wbit) ? "1" : "0";
                string WBitS6 = (Parameter.S6Wbit) ? "1" : "0";
                string PJSlotmapBypass = (Parameter.PJSlotmapBypass) ? "1" : "0";
                string CarrierIDbyHost = (Parameter.CarrireIDByHost) ? "1" : "0";
                WriteSetting("SECSParameter", "WBITS10", WBitS10, "1");
                WriteSetting("SECSParameter", "WBITS5", WBitS5, "1");
                WriteSetting("SECSParameter", "WBITS6", WBitS6, "1");
                WriteSetting("SECSParameter", "PJ Slotmap Bypass", PJSlotmapBypass, "0");
                WriteSetting("SECSParameter", "CarrireIDByHost", CarrierIDbyHost, "0");
                WriteSetting("SECSParameter", "Continuous Run job", Parameter.CanExcuteJob.ToString(), "1");
                string Driver = (Parameter.GemDriver ==  SECSDriver.SDR) ? "SDR" : "ITRI";
                WriteSetting("SECSParameter", "SECS Driver", Driver.ToString(), "SDR");
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        public void SetSECSConnectParameter(SECSConneetConfig Parameter)
        {
            try
            {
               // string Alarm = string.Format("{0}{1}", Parameter.AlarmFunction.Stream.ToString(), Parameter.AlarmFunction.Function.ToString());
                WriteSetting("SECSParameter", "LocalIP", Parameter.LocalIP.ToString(), "127.0.0.1");
              //  string Connect = string.Format("{0}{1}", Parameter.ConnectFunction.Stream.ToString(), Parameter.ConnectFunction.Function.ToString());
                WriteSetting("SECSParameter", "LocalPort", Parameter.LocalPort.ToString(), "5000");
                // string OnlineSubStats = (Parameter.OnlineSubStats == SECSStats.ONLINELOCAL) ? "ONLINELOCAL" : "ONLINEREMOTE";
                string Mode = (Parameter.Mode == SECSMODE.HSMS_MODE) ? "HSMS" : "RS232";
                WriteSetting("SECSParameter", "PortType", Mode, "HSMS");
               
                WriteSetting("SECSParameter", "T3", Parameter.T3.ToString(), "45");
                WriteSetting("SECSParameter", "T5", Parameter.T5.ToString(), "10");
                WriteSetting("SECSParameter", "T6", Parameter.T6.ToString(), "5");
                WriteSetting("SECSParameter", "T7", Parameter.T7.ToString(), "10");
                WriteSetting("SECSParameter", "T8", Parameter.T8.ToString(), "5");
                WriteSetting("SECSParameter", "DeviceID", Parameter.DDEVICEID.ToString(), "0");

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        public bool WriteSetting(string TableName,string strSettingName, string strValue,params string[] str)
        {
            try
            {
                DataSet ds = Reader("Select * From {0} Where ParameterName='{1}'", TableName, strSettingName);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) //parameter exist.
                {
                    SQLExec("Update {0} Set ParameterValue='{1}' Where ParameterName='{2}'", TableName, strValue, strSettingName);
                    _logger.WriteLog("Set setting [{0}] from [{1}] to [{2}]", strSettingName, ds.Tables[0].Rows[0]["ParameterValue"], strValue);
                }
                else //parameter not found.
                {
                    this.SQLExec("Insert Into {0} (ParameterName, ParameterValue, Remark) Values ('{1}','{2}','{3}')", TableName, strSettingName, strValue, str[0]);
                    _logger.WriteLog("Add setting [{0}] value = [{1}]", strSettingName, strValue);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return false;
            }
            return true;
        }
       /*
        public void GetToolParameter(ref ToolParameterConfig _EQP)
        {
            try
            {
                DataSet dataTable = Reader(string.Format("Select * From ToolParameter"));
                for (int i = 0; i < dataTable.Tables[0].Rows.Count; i++)
                {
                    switch (dataTable.Tables[0].Rows[i]["ParameterName"].ToString())
                    {
                        case "EQP Type":
                            _EQP.Type = (dataTable.Tables[0].Rows[i]["ParameterValue"].ToString() == "FixBuffer") ? EQPType.FixBuffer
                                : (dataTable.Tables[0].Rows[i]["ParameterValue"].ToString() == "InternalBuffer") ? EQPType.InternalBuffer
                                : EQPType.LoadUnload;
                            break;
                        case "EQP Name":
                            _EQP.EQName = dataTable.Tables[0].Rows[i]["ParameterValue"].ToString();
                            break;
                        case "EQP IP":
                            _EQP.IPaddress = dataTable.Tables[0].Rows[i]["ParameterValue"].ToString();
                            break;
                        case "EQP_TCPIPPort":
                            _EQP.IPport = Convert.ToInt16(dataTable.Tables[0].Rows[i]["ParameterValue"].ToString());
                            break;
                        case "EQP Buffer Count":
                            _EQP.BufferCount = Convert.ToInt16(dataTable.Tables[0].Rows[i]["ParameterValue"].ToString());
                            break;
                        case "EQP Chamber":
                            _EQP.EQChamberCount = Convert.ToInt16(dataTable.Tables[0].Rows[i]["ParameterValue"].ToString());
                            break;
                        case "EQP PortNo":
                            _EQP.LoadPortCount = Convert.ToInt16(dataTable.Tables[0].Rows[i]["ParameterValue"].ToString());
                            break;
                       
                    }
                }
              

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);

            }
        }
        */
        /*
        public void GetEQTypeList(EQPType Type,ref List<string> ToolList)
        {
            try
            {
                string typestr = (Type == EQPType.FixBuffer)? "FixBufferList":(Type == EQPType.InternalBuffer)?"InternalBufferList": "LoadUnloadLlist";
                DataSet dataTable = Reader(string.Format("Select * From ToolParameter where ParameterName = '{0}'", typestr));
                ToolList = dataTable.Tables[0].Rows[0]["ParameterValue"].ToString().Split(',').ToList();

            }
            catch(Exception ex)
            {
                ToolList = new List<string>();
                _logger.WriteLog(ex);
            }
        }
        public void SetToolParameter(ToolParameterConfig Parameter)
        {
            try
            {
                string EQtype = (Parameter.Type == SEFEM.EQPType.FixBuffer) ? "FixBuffer" :
                (Parameter.Type == SEFEM.EQPType.InternalBuffer) ? "InternalBuffer" : "LoadUnload";
                WriteSetting("ToolParameter", "EQP Type", EQtype, "");
                WriteSetting("ToolParameter", "EQP Name", Parameter.EQName, "");
                WriteSetting("ToolParameter", "EQP IP", Parameter.IPaddress, "127.0.0.1");
                WriteSetting("ToolParameter", "EQP_TCPIPPort", Parameter.IPport.ToString(), "6000");
                WriteSetting("ToolParameter", "EQP Buffer Count", Parameter.BufferCount.ToString(), "1");
                WriteSetting("ToolParameter", "EQP Chamber", Parameter.EQChamberCount.ToString(), "1");
                WriteSetting("ToolParameter", "EQP PortNo", Parameter.LoadPortCount.ToString(), "1");

            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }
        */

    }
}
