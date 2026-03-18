using Rorze.Secs;
using Rorze.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rorze.SocketObject;

namespace RorzeApi.SECSGEM
{
    public class MainDB : SAccessDb
    {
        //public MainDB() : base(System.Environment.CurrentDirectory + @"\" + "DB_Secs.MDB", "Rorze")
        public MainDB() : base(AppDomain.CurrentDomain.BaseDirectory + "\\SettingFile" + "\\DB_Secs.MDB", "Rorze")
        {

        }
        public void ResetAllAlarm()
        {

            SQLExec("Update ALIDList Set Ocur =False");

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
                WriteLog("[Exception] " + ex);
                return string.Empty;
            }
        }

        public void GetGem300Config(ref string IP, ref int port)
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
                string Function;
                string Stream;
                for (int i = 0; i < _DataTable.Rows.Count; i++)
                {
                    switch (_DataTable.Rows[i]["ParameterName"].ToString())
                    {
                        case "Config Alarm":
                            Value = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F');
                            Function = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[1];
                            Stream = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[0].Split('S')[1];
                            // Parameter.AlarmFunction.Stream = (QsStream)Convert.ToInt16(Stream);
                            //  Parameter.AlarmFunction.Function = (QsFunction)Convert.ToInt16(Function);
                            Parameter.AlarmFunction = new SettingStreamFunction((QsStream)Convert.ToInt16(Stream), (QsFunction)Convert.ToInt16(Function));
                            break;
                        case "Config Connect":
                            Value = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F');
                            Function = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[1];
                            Stream = _DataTable.Rows[i]["ParameterValue"].ToString().Split('F')[0].Split('S')[1];
                            //  Parameter.ConnectFunction.Stream = (QsStream)Convert.ToInt16(Stream);
                            //  Parameter.ConnectFunction.Function = (QsFunction)Convert.ToInt16(Function);
                            Parameter.ConnectFunction = new SettingStreamFunction((QsStream)Convert.ToInt16(Stream), (QsFunction)Convert.ToInt16(Function));
                            break;
                        case "Online Substatus":
                            Parameter.OnlineSubStats = (_DataTable.Rows[i]["ParameterValue"].ToString().ToUpper() == "ONLINELOCAL") ? GEMControlStats.ONLINELOCAL : GEMControlStats.ONLINEREMOTE;
                            break;
                        case "WBITS10":
                            Parameter.S10Wbit = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0) ? true : false;
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
                            Parameter.CarrireIDByHost = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0) ? true : false;
                            break;
                        case "Continuous Run job":
                            Parameter.CanExcuteJob = Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString());
                            break;

                        case "SECS Driver":

                            Parameter.GemDriver = (_DataTable.Rows[i]["ParameterValue"].ToString().ToUpper() == "0") ? SECSDriver.SDR : SECSDriver.ITRI;
                            break;
                        case "DefProcessMode":
                            Parameter.ProcessMode = (_DataTable.Rows[i]["ParameterValue"].ToString() == "OfflineMode") ? ToolProcessMode.OfflineMode : ToolProcessMode.OnlineMode;
                            break;
                        case "Simulation":
                            Parameter.IsSimulation = (Convert.ToInt16(_DataTable.Rows[i]["ParameterValue"].ToString()) > 0) ? true : false;
                            break;
                        case "Enable SECS":
                            Parameter.EnableSECS = (_DataTable.Rows[i]["ParameterValue"].ToString() == "1") ? true : false;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
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
                        case "SECS Driver":
                            Parameter.Driver = (_DataTable.Rows[i]["ParameterValue"].ToString() == "0") ? SECSDriver.SDR : SECSDriver.ITRI;
                            break;
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
                WriteLog("[Exception] " + ex);
            }

        }

        public void GetSocketConnectConfig(ref List<SocketInfo> List)
        {
            DataTable _DataTable = this.Reader("Select * From ToolConnectParameter where ParameterName like 'EQP Connect Config%' or ParameterName = 'Gem300 Connect Config'").Tables[0];
            EFEMConfig _EFEM = new EFEMConfig();

            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                if (_DataTable.Rows[i]["ParameterValue"].ToString() == "")
                    continue;
                // info = new SocketInfo();
                string Name = _DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[0];
                string IP = _DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[1];
                int Port = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[2]);
                SocketTypes types = (_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[3] == "Server") ? SocketTypes.Server : SocketTypes.Clinet;
                if (_DataTable.Rows[i]["ParameterValue"].ToString().Split(',').Count() > 4)
                {
                    _EFEM._Type = (_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[4] == "0") ? EquipmentType.EFEM : (_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[4] == "1") ? EquipmentType.EQ : EquipmentType.EFEMAndEQ;
                    switch (_EFEM._Type)
                    {
                        case EquipmentType.EFEM:
                            _EFEM.LoadPortCount = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[5]);
                            _EFEM.RobotArmCount = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[6]);
                            _EFEM.AlingerCount = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[7]);

                            // Get Unit Pos
                            DataTable UnitPosTable = this.Reader(string.Format("Select * From ToolUnitPostionDefined where Model = '{0}'", Name)).Tables[0];
                            for (int j = 0; j < UnitPosTable.Rows.Count; j++)
                            {
                                _EFEM.UnitPos = new Dictionary<int, string>();
                                if (!_EFEM.UnitPos.ContainsKey(Convert.ToInt32(UnitPosTable.Rows[j]["UnitNo"].ToString())))
                                    _EFEM.UnitPos.Add(Convert.ToInt32(UnitPosTable.Rows[j]["UnitNo"].ToString()), UnitPosTable.Rows[j]["UnitName"].ToString());
                            }
                            break;

                        case EquipmentType.EQ:
                            _EFEM.ChamberCount = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[5]);
                            _EFEM.ChamberSpace = Convert.ToInt32(_DataTable.Rows[i]["ParameterValue"].ToString().Split(',')[6]);
                            _EFEM.ChanberSpacenumUnitPos = new Dictionary<int, string>();
                            // Get EQ Space pos
                            DataTable EQSpacePosTable = this.Reader(string.Format("Select * From ToolEQSpaceDefined where Model = '{0}'", Name)).Tables[0];
                            for (int j = 0; j < EQSpacePosTable.Rows.Count; j++)
                            {

                                if (!_EFEM.ChanberSpacenumUnitPos.ContainsKey(Convert.ToInt32(EQSpacePosTable.Rows[j]["EQSpaceNo"].ToString())))
                                    _EFEM.ChanberSpacenumUnitPos.Add(Convert.ToInt32(EQSpacePosTable.Rows[j]["EQSpaceNo"].ToString()), EQSpacePosTable.Rows[j]["EQSpaceName"].ToString());


                            }

                            break;
                        case EquipmentType.EFEMAndEQ:

                            break;


                    }


                }
                List.Add(new SocketInfo(Name, IP, Port, types, _EFEM));

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
                Console.WriteLine("{0} Exception caught.", ex);
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
                string OnlineSubStats = (Parameter.OnlineSubStats == GEMControlStats.ONLINELOCAL) ? "ONLINELOCAL" : "ONLINEREMOTE";
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
                string Driver = (Parameter.GemDriver == SECSDriver.SDR) ? "0" : "1";
                WriteSetting("SECSParameter", "SECS Driver", Driver.ToString(), "0");
                string Mode = (Parameter.ProcessMode == ToolProcessMode.OfflineMode) ? "OfflineMode" : "OnlineMode";
                WriteSetting("SECSParameter", "DefProcessMode", Mode.ToString(), "OfflineMode");
                string Enable = (Parameter.EnableSECS) ? "1" : "0";
                WriteSetting("SECSParameter", "Enable SECS", Enable.ToString(), "0");

            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
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
                WriteLog("[Exception] " + ex);
            }
        }
        public bool WriteSetting(string TableName, string strSettingName, string strValue, params string[] str)
        {
            try
            {
                DataSet ds = Reader("Select * From {0} Where ParameterName='{1}'", TableName, strSettingName);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) //parameter exist.
                {
                    SQLExec("Update {0} Set ParameterValue='{1}' Where ParameterName='{2}'", TableName, strValue, strSettingName);
                    WriteLog(string.Format("Set setting [{0}] from [{1}] to [{2}]", strSettingName, ds.Tables[0].Rows[0]["ParameterValue"], strValue));
                }
                else //parameter not found.
                {
                    this.SQLExec("Insert Into {0} (ParameterName, ParameterValue, Remark) Values ('{1}','{2}','{3}')", TableName, strSettingName, strValue, str[0]);
                    WriteLog(string.Format("Add setting [{0}] value = [{1}]", strSettingName, strValue));
                }
            }
            catch (Exception ex)
            {
                WriteLog("[Exception] " + ex);
                return false;
            }
            return true;
        }


        public void GetSECSECID(ref Dictionary<int, VIDObject> ECIDList, ref Dictionary<string, int> ECIDMapList)
        {

            DataTable _DataTable = this.Reader("Select * From ECIDList ").Tables[0];

            var Tempvalue = "";
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                if (ECIDList.ContainsKey(Convert.ToInt32(_DataTable.Rows[i]["ECID"].ToString())))
                    continue;
                int ECID = Convert.ToInt32(_DataTable.Rows[i]["ECID"].ToString());
                string ECIDName = _DataTable.Rows[i]["Name"].ToString();
                SecsFormateType ValueType = SecsFormateType.A;
                switch (_DataTable.Rows[i]["ValueType"].ToString())
                {
                    case "A":
                        ValueType = SecsFormateType.A;

                        break;
                    case "U1":
                        ValueType = SecsFormateType.U1;

                        break;

                    case "U2":
                        ValueType = SecsFormateType.U2;
                        break;
                    case "U4":
                        ValueType = SecsFormateType.U4;
                        break;
                    case "Bool":
                        ValueType = SecsFormateType.Bool;

                        break;
                    case "L":
                        ValueType = SecsFormateType.L;
                        break;

                }
                string Max = _DataTable.Rows[i]["Max"].ToString();
                string Min = _DataTable.Rows[i]["Min"].ToString();
                string Unit = _DataTable.Rows[i]["Unit"].ToString();
                Tempvalue = _DataTable.Rows[i]["ECValue"].ToString();

                ECIDList.Add(ECID, new VIDObject(VIDType.ECID, ValueType, ECID, ECIDName) { CurrentValue = Tempvalue });
                ECIDList[ECID].Max = Max;
                ECIDList[ECID].Min = Min;
                ECIDList[ECID].Unit = Unit;

                ECIDMapList.Add(ECIDName, ECID);
            }
        }
        public void GetSECSSVID(ref Dictionary<int, VIDObject> SVIDList)
        {

            DataTable _DataTable = this.Reader("Select * From VIDList Where VIDType ='SVID' AND Type = 'Nomal'").Tables[0];
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                if (SVIDList.ContainsKey(Convert.ToInt32(_DataTable.Rows[i]["VID"].ToString())))
                    continue;
                int VID = Convert.ToInt32(_DataTable.Rows[i]["VID"].ToString());
                string VIDName = _DataTable.Rows[i]["Name"].ToString();
                SecsFormateType ValueType = SecsFormateType.A;
                switch (_DataTable.Rows[i]["ValueType"].ToString())
                {
                    case "A":
                        ValueType = SecsFormateType.A;
                        break;
                    case "U1":
                        ValueType = SecsFormateType.U1;
                        break;

                    case "U2":
                        ValueType = SecsFormateType.U2;
                        break;
                    case "U4":
                        ValueType = SecsFormateType.U4;
                        break;
                    case "Bool":
                        ValueType = SecsFormateType.Bool;

                        break;
                    case "L":
                        ValueType = SecsFormateType.L;
                        break;
                    default:

                        break;
                }
                string Unit = _DataTable.Rows[i]["Unit"].ToString();
                SVIDList.Add(VID, new VIDObject(VIDType.SVID, ValueType, VID, VIDName));
                SVIDList[VID].Unit = Unit;
            }
        }
        public void GetSECSFDC(ref Dictionary<int, VIDObject> FDCList, ref Dictionary<string, int> FDCMapping)
        {

            DataTable _DataTable = this.Reader("Select * From VIDList Where VIDType ='SVID' AND Type = 'FDC'").Tables[0];
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                if (FDCList.ContainsKey(Convert.ToInt32(_DataTable.Rows[i]["VID"].ToString())))
                    continue;
                int VID = Convert.ToInt32(_DataTable.Rows[i]["VID"].ToString());
                string VIDName = _DataTable.Rows[i]["Name"].ToString();
                SecsFormateType ValueType = SecsFormateType.A;
                switch (_DataTable.Rows[i]["ValueType"].ToString())
                {
                    case "A":
                        ValueType = SecsFormateType.A;
                        break;
                    case "U1":
                        ValueType = SecsFormateType.U1;
                        break;

                    case "U2":
                        ValueType = SecsFormateType.U2;
                        break;
                    case "U4":
                        ValueType = SecsFormateType.U4;
                        break;
                    case "Bool":
                        ValueType = SecsFormateType.Bool;

                        break;
                    case "L":
                        ValueType = SecsFormateType.L;
                        break;
                    default:

                        break;
                }
                string Unit = _DataTable.Rows[i]["Unit"].ToString();
                FDCList.Add(VID, new VIDObject(VIDType.SVID, ValueType, VID, VIDName));
                FDCList[VID].Unit = Unit;
                FDCMapping.Add(VIDName.ToUpper(), VID);
            }
        }
        public void GetSECSDVID(ref Dictionary<string, VIDObject> DVIDList)
        {

            DataTable _DataTable = this.Reader("Select * From VIDList Where VIDType ='DVID'").Tables[0];
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                if (DVIDList.ContainsKey((_DataTable.Rows[i]["Name"].ToString())))
                    continue;
                int VID = Convert.ToInt32(_DataTable.Rows[i]["VID"].ToString());
                string VIDName = _DataTable.Rows[i]["Name"].ToString();
                SecsFormateType ValueType = SecsFormateType.A;
                switch (_DataTable.Rows[i]["ValueType"].ToString())
                {
                    case "A":
                        ValueType = SecsFormateType.A;
                        break;
                    case "U1":
                        ValueType = SecsFormateType.U1;
                        break;

                    case "U2":
                        ValueType = SecsFormateType.U2;
                        break;
                    case "U4":
                        ValueType = SecsFormateType.U4;
                        break;
                    case "Bool":
                        ValueType = SecsFormateType.Bool;

                        break;
                    case "L":
                        ValueType = SecsFormateType.L;
                        break;
                    default:

                        break;
                }
                string Unit = _DataTable.Rows[i]["Unit"].ToString();
                DVIDList.Add(VIDName, new VIDObject(VIDType.DVID, ValueType, VID, VIDName));
                DVIDList[VIDName].Unit = Unit;
            }
        }

        public void GetSECSCEID(ref Dictionary<string, int> CEIDList)
        {
            DataTable _DataTable = this.Reader("Select * From CEIDList ").Tables[0];
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                if (!CEIDList.ContainsKey(_DataTable.Rows[i]["CEID"].ToString()))
                    CEIDList.Add(_DataTable.Rows[i]["Name"].ToString(), Convert.ToInt32(_DataTable.Rows[i]["CEID"].ToString()));

            }

        }

    }
}
