using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace RorzeUnit.Class
{
    class CINIFile
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WritePrivateProfileString(string sectionName, string keyName, string keyValue, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetPrivateProfileString(string sectionName, string keyName, string defaultReturnString, StringBuilder returnString, int returnStringLength, string filePath);

        private string m_strFile;

        public CINIFile(string strPath)
        {
            m_strFile = strPath;
        }

        public bool WriteInt(string strSection, string strKey, int nValue)
        {
            return WriteString(strSection, strKey, nValue.ToString());
        }

        public bool WriteBool(string strSection, string strKey, bool bBOOL)
        {
            string strValue = "";
            strValue = (bBOOL) ? "1" : "0";
            return WriteString(strSection, strKey, strValue);
        }

        public bool WriteString(string strSection, string strKey, string strValue)
        {
            return WritePrivateProfileString(strSection, strKey, strValue, this.m_strFile);
        }

        public string GetString(string strSection, string strKey, string strDefault = "")
        {
            string strValue = "";
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(strSection, strKey, "NOTFIND", temp, 255, this.m_strFile);
            switch (temp.ToString())
            {
                case "":
                case "NOTFIND":
                    WritePrivateProfileString(strSection, strKey, strDefault, this.m_strFile);
                    strValue = strDefault;
                    break;
                default:
                    strValue = temp.ToString();
                    break;
            }
            return strValue;
        }

        public int GetInt(string strSection, string strKey, int nDefautl = 0)
        {
            int nValue = 0;
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(strSection, strKey, "NOTFIND", temp, 255, this.m_strFile);
            switch (temp.ToString())
            {
                case "":
                case "NOTFIND":
                    WritePrivateProfileString(strSection, strKey, nDefautl.ToString(), this.m_strFile);
                    nValue = nDefautl;
                    break;
                default:
                    nValue = int.Parse(temp.ToString());
                    break;
            }
            return nValue;
        }

        public bool GetBool(string strSection, string strKey, bool bDefault = false)
        {
            bool bValue = false;
            string strBool = bDefault ? "1" : "0";
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(strSection, strKey, "NOTFIND", temp, 255, this.m_strFile);
            switch (temp.ToString())
            {
                case "":
                case "NOTFIND":
                    WritePrivateProfileString(strSection, strKey, strBool, this.m_strFile);
                    bValue = bDefault;
                    break;
                default:
                    if (temp.ToString().ToUpper() == "TRUE" || temp.ToString().ToUpper() == "T" || temp.ToString() == "1")
                        bValue = true;
                    break;
            }
            return bValue;
        }

        public double GetDouble(string strSection, string strKey, double dDefault = 0.0)
        {
            double dValue;
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString(strSection, strKey, "NOTFIND", temp, 255, this.m_strFile);
            switch (temp.ToString())
            {
                case "":
                case "NOTFIND":
                    WritePrivateProfileString(strSection, strKey, dDefault.ToString(), this.m_strFile);
                    dValue = dDefault;
                    break;
                default:
                    dValue = double.Parse(temp.ToString());
                    break;
            }
            return dValue;
        }
    }
}
