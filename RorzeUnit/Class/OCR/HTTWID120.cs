
using Rorze.Secs;
using RorzeComm.Log;
using RorzeUnit.Class.OCR.Enum;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using Wid110LibConstUser;
using Wid110LibUser;


namespace RorzeUnit.Class.OCR
{
    public class HTTWID120 : I_OCR
    {
        private const int STR_VERSION = 2000; //!< Version number server-software
        private const int STR_REVISION = 2005; //!< revision number 
        private const int STR_VC_TYPE = 2001; //!< System Board ID, defined by Hardware
        private const int STR_HW_VERSION = 2002; //!< System Hardware version
        private const int STR_FW_VERSION = 2020; //!< System Firmware version
        private const int STR_READER_COMMENT = 2059; //!< reader location string and custom comments     
        private const int STR_READER_HOSTNAME = 2060; //!< hostname for DHCP ident; empty for getting DHCP ident with MAC-Address, if name is given it's added to #IP file on reader

        private Wid110Lib m_WidLib;
        private SLogger m_logger = SLogger.GetLogger("CommunicationLog");
        private string m_strIP = "192.168.0.65";
        private bool m_bSimulate;
        private bool m_bConnected;
        private enumName m_eName;

        public HTTWID120(enumName eName, string strIP, bool bDisable, bool bSimulate)
        {

            m_eName = eName;
            m_strIP = strIP;
            Disable = bDisable;
            m_bSimulate = bSimulate;

            WriteLog("initialize...");

            m_WidLib = new Wid110Lib();

            string strLibVers = m_WidLib.FGetVersion();
            string strParVers = m_WidLib.FGetVersionParam();
            //string strWidVers = m_WidLib.FGetReaderInfo(STR_VERSION);
            //string strWidType = m_WidLib.FGetReaderInfo(STR_HW_VERSION);
            //string srtVers = string.Format("{0} {1} C# Library Version {2} compiled for Parameter {3}", strWidType, strWidVers, strLibVers, strParVers);
            string srtVers = string.Format("C# Library Version {0} compiled for Parameter {1}", strLibVers, strParVers);
            WriteLog(srtVers);

            if (m_eName == enumName.A2)
            {
                //Initial("");

                ////C:\Program Files (x86)\IOSS\WID120
                //SetRecipe("M12");

                //string str = "wafer id";
                //Read(ref str);

                //getRecipt();
                //Initial("");
            }


            if (m_bSimulate == false && Disable == false)
                Initial("");




        }
        ~HTTWID120()
        {
            Exit();
        }

        public bool Disable { get; private set; }
        public bool Connected { get { return m_bConnected; } }
        public string Name { get { return m_eName.ToString(); } }
        public string SavePicturePath { get; private set; }
        public bool IsFront { get { return (m_eName == enumName.A1 || m_eName == enumName.B1); } }
        private void WriteLog(string strContent, [CallerMemberName] string meberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string strMsg = string.Format("[OCR]{0} : {1}  at line {2} ({3})", Name, strContent, lineNumber, meberName);
            m_logger.WriteLog(strMsg);
        }
        private void HandleWidLibError()
        {
            string strErrMsg;
            if (m_WidLib.CheckError(out strErrMsg))
            {
                WriteLog("ERROR: " + strErrMsg);
            }
        }
        private bool IsInitialized()
        {
            bool bOk = false;
            try
            {
                while (true)
                {
                    if (m_WidLib.FIsInitialized() == false)
                        break;
                    bOk = true;
                    break;
                }
                HandleWidLibError();
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bOk;
        }
        //-------------------------------------------------------------------------------------
        public bool Initial(string strRecipe)
        {
            bool bOk = false;
            try
            {
                while (true)
                {
                    // connect to IP address
                    if (m_WidLib.FInit(m_strIP) == false)
                        break;
                    m_bConnected = true;
                    bOk = true;
                    break;
                }
                HandleWidLibError();
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bOk;
        }
        public bool OnLine() { return true; }
        public bool OffLine() { return true; }
        public bool SetRecipe(string strJobName)
        {
            bool bOk = false;
            try
            {
                while (true)
                {
                    string strFolder = Directory.GetCurrentDirectory();
                    switch (m_eName)
                    {
                        case enumName.A1:
                            strFolder += "\\OCRecipe\\Job\\A1";
                            break;
                        case enumName.A2:
                            strFolder += "\\OCRecipe\\Job\\A2";
                            break;
                        case enumName.B1:
                            strFolder += "\\OCRecipe\\Job\\B1";
                            break;
                        case enumName.B2:
                            strFolder += "\\OCRecipe\\Job\\B2";
                            break;
                        default:
                            break;
                    }
                    if (Directory.Exists(strFolder) == false) { Directory.CreateDirectory(strFolder); }

                    if (IsInitialized() == false)
                        break;

                    WriteLog("Use Job:" + strJobName);

                    if (strJobName.IndexOf(".job") < 0) strJobName += ".job";
                    string strJobFilePath = Path.Combine(strFolder, strJobName);
                    if (File.Exists(strJobFilePath) == false)
                        break;
                    // read file and send to reader
                    if (m_WidLib.FLoadRecipes(strJobFilePath) == false)
                    {
                        if (m_WidLib.FInit(m_strIP) == false)
                            break;
                        if (m_WidLib.FLoadRecipes(strJobFilePath) == false)
                            break;
                    }

                    bOk = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bOk;
        }
        public bool Read(ref string strResult, bool bOKSaveImage, string strCarrierID = "", string strLotID = "")
        {
            bool bOk = false;
            try
            {
                while (true)
                {
                    if (IsInitialized() == false)
                    {
                        if (m_WidLib.FInit(m_strIP) == false)
                            break;
                        if (IsInitialized() == false)
                            break;
                    }
                    // perform process read
                    if (m_WidLib.FProcessRead() == false)
                    {
                        if (m_WidLib.FInit(m_strIP) == false)
                            break;
                        if (m_WidLib.FProcessRead() == false)
                            break;
                    }
                    // get read result
                    string result = m_WidLib.FGetWaferId();
                    int iQuality = m_WidLib.FGetCodeQualityLast();
                    WriteLog("WaferID: " + result + " Quality: " + iQuality);
                    bool bSuc;
                    if (m_WidLib.getReadOK() != 1)//失敗
                    {
                        int iRead = result.IndexOf(Wid110LibConst.rsltNOREAD);
                        if (iRead > -1)
                        {
                            result = result.Substring(iRead + Wid110LibConst.rsltNOREAD.Length);
                        }
                        bSuc = false;
                    }
                    else//成功
                    {
                        int iRead = result.IndexOf(Wid110LibConst.rsltREAD);
                        if (iRead > -1)
                        {
                            result = result.Substring(iRead + Wid110LibConst.rsltREAD.Length);
                        }
                        bSuc = true;
                    }
                    strResult = result;
                    //判斷失敗
                    if (result.IndexOf("fail") > -1 || bOKSaveImage)
                    {
                        //string strTime = DateTime.Now.ToString("yyyyMMdd HH_mm_ss");
                        //string strName = string.Format("{0} {1} {2} {3}", strTime, strCarrierID, m_eName, strLotID);
                        //WriteLog("Read Fail Save image:" + strName);
                        //SaveImage(strName, bSuc);
                        break;
                    }
                    bOk = true;
                    break;
                }
                HandleWidLibError();
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bOk;
        }
        public bool SaveImage(string strFileName)
        {
            bool bOk = false;
            try
            {
                string strFolder = Path.Combine(Directory.GetCurrentDirectory(), "OCR_Image");

                //日期是資料夾
                strFolder = Path.Combine(strFolder, DateTime.Now.ToString("yyyMMdd"));        


                SaveImage(strFolder, strFileName);
            }
            catch (Exception ex) { WriteLog("<<Exception>>" + ex); }
            return bOk;

        }
        public bool SaveImage(string strFolder, string strFileName)
        {
            bool bOk = false;
            try
            {
                if (Directory.Exists(strFolder) == false)
                {
                    Directory.CreateDirectory(strFolder);
                }
                if (strFileName.IndexOf(".jpg") > -1)
                {
                    strFileName.Replace(".jpg", ".bmp");
                }
                else if (strFileName.IndexOf(".bmp") < 0)
                {
                    strFileName += ".bmp";
                }

                string strPath = Path.Combine(strFolder, strFileName);
                SavePicturePath = strPath;
                while (true)
                {
                    if (IsInitialized() == false)
                        break;
                    if (m_WidLib.FProcessGetImage(strPath, 0) == false)
                        break;

                    bOk = true;
                    break;
                }
                HandleWidLibError();
            }
            catch (Exception ex) { WriteLog("<<Exception>>" + ex); }
            return bOk;

        }
        public string[] getRecipt()
        {
            string strFolder = Directory.GetCurrentDirectory();
            switch (m_eName)
            {
                case enumName.A1:
                    strFolder += "\\OCRecipe\\Job\\A1";
                    break;
                case enumName.A2:
                    strFolder += "\\OCRecipe\\Job\\A2";
                    break;
                case enumName.B1:
                    strFolder += "\\OCRecipe\\Job\\B1";
                    break;
                case enumName.B2:
                    strFolder += "\\OCRecipe\\Job\\B2";
                    break;
                default:
                    break;
            }
            if (Directory.Exists(strFolder) == false) { Directory.CreateDirectory(strFolder); }

            string[] files = Directory.GetFiles(strFolder, "*.job", SearchOption.AllDirectories);

            return files;
        }
        private bool Exit()
        {
            bool bOk = false;
            try
            {
                while (true)
                {
                    // disconnect from sensor
                    if (m_WidLib.FExit() == false)
                        break;
                    m_bConnected = false;
                    bOk = true;
                    break;
                }
                HandleWidLibError();
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bOk;
        }

        /*private bool Read(ref string strResult, string strRecipe)
        {
            bool bOk = false;
            try
            {
                while (true)
                {
                    if (IsInitialized() == false)
                        break;

                    // read file and send to reader
                    if (m_WidLib.FLoadRecipes(strRecipe) == false)
                        break;

                    if (Read(ref strResult) == false)
                        break;

                    bOk = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                WriteLog("<<Exception>>" + ex);
            }
            return bOk;
        }*/




    }
}
