using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.RC500.Event
{
    //==============================================================================
    public delegate void NotifyGDIOEventHandler(object sender, NotifyGDIOEventArgs e);
    public class NotifyGDIOEventArgs : EventArgs
    {
        string m_strInput, m_strOutput;
        int m_nHCLID;
        public int HCLID { get { return m_nHCLID; } }
        public bool[] Input;
        public bool[] Output;
        public NotifyGDIOEventArgs(int nHCLID, string strInput, string strOutput)
        {
            m_nHCLID = nHCLID;
            m_strInput = strInput;
            m_strOutput = strOutput;

            string binarydataDI = Convert.ToString(Int32.Parse(m_strInput, NumberStyles.HexNumber), 2).PadLeft(16, '0');
            string binarydataDO = Convert.ToString(Int32.Parse(m_strOutput, NumberStyles.HexNumber), 2).PadLeft(16, '0');
            string ReverseBinarydataDI = new string(binarydataDI.ToCharArray().Reverse().ToArray());
            string ReverseBinarydataDO = new string(binarydataDO.ToCharArray().Reverse().ToArray());
            Input = ReverseBinarydataDI.Select(i => Convert.ToBoolean(int.Parse(i.ToString()))).ToArray();
            Output = ReverseBinarydataDO.Select(i => Convert.ToBoolean(int.Parse(i.ToString()))).ToArray();
        }
    }
    //==============================================================================
    public delegate void NotifyGPIOEventHandler(object sender, NotifyGPIOEventArgs e);
    public class NotifyGPIOEventArgs : EventArgs
    {
        public bool[] Input;
        public bool[] Output;
        public NotifyGPIOEventArgs(bool[] bInput, bool[] bOutput)
        {
            try
            {
                Input = bInput;
                Output = bOutput;
            }
            catch (Exception ex)
            {
                throw new Exception("Error PIO string." + ex);
            }
        }
    }
    //==============================================================================
    public delegate void RorzeProtoclHandler(object sender, RorzeProtoclEventArgs e);
    public class RorzeProtoclEventArgs : EventArgs
    {
        public RC500Frame Frame { get; set; }
        public RorzeProtoclEventArgs(string frame)
        {
            Frame = new RC500Frame(frame);
        }
    }
    public class RC500Frame
    {
        private string _strFrame;   //aTBL1.STAT:00000/0000

        private char _charHeader;   //o,a,n,c,e
        private string _strID;      //TBL1.
        private string _strData;    //STAT:00000/0000

        private int _nBodyNo;       //1
        private string _strCommand; //STAT
        private string _strAttachCommand;
        private string _strValue;   //00000/0000

        public RC500Frame(string strFrame)
        {
            _strFrame = strFrame.Trim('\r', '\n');

            _charHeader = _strFrame[0];
            _strID = _strFrame.Substring(1, 5);
            _strData = _strFrame.Substring(6);

            _nBodyNo = _strID[3] >= 'A' ? _strID[3] - 'A' + 10 : _strID[3] - '0';

            string dataTemp = _strData.Split(':')[0];

            if (dataTemp.Split('.').Count() == 1)           //舉例 GPOS:
            {
                _strAttachCommand = string.Empty;
                _strCommand = dataTemp.Split(':')[0];       //GPOS
            }
            else if (dataTemp.Split('.').Count() == 2)      //舉例 AXS1.GPOS:
            {
                _strAttachCommand = dataTemp.Split('.')[0]; //AXS1
                _strCommand = dataTemp.Split('.')[1];       //GPOS             
            }

            _strValue = _strData.Contains(':') ? _strData.Split(':')[1] : "";
        }

        public char Header { get { return _charHeader; } }
        public string ID { get { return _strID; } }
        public string Data { get { return _strData; } }
        public int BodyNo { get { return _nBodyNo; } }
        public string Command { get { return _strCommand; } }
        public string Value { get { return _strValue; } }
        public string AttachCommand { get { return _strAttachCommand; } }
    }
    //==============================================================================

    /*public delegate void E84IOChangeStatHandler(object sender, E84IOChangeEventArgs e);
    public class E84IOChangeEventArgs : EventArgs
    {
        public int _nBodyNo { get; set; }//注意 loadport bodyno
        public E84IOChangeEventArgs(int nBody)
        {
            _nBodyNo = nBody;
        }
    }


    public delegate void RC500StatHandler(object sender, RC500StatEventArgs e);
    public class RC500StatEventArgs : EventArgs
    {
        public RC500StatEventArgs(string strErrCode)
        {
            Stat = "STAT:" + strErrCode;
        }
        public string Stat { get; set; }
    }


    public delegate void GPOSMessageHandler(object sender, GPOSMessageEventArgs e);
    public class GPOSMessageEventArgs : EventArgs
    {
        public double _value { get; set; }
        public GPOSMessageEventArgs(double Value)
        {
            _value = Value;
        }
    }


    public delegate void RC500MessageHandler(object sender, RC500MessageEventArgs e);
    public class RC500MessageEventArgs : EventArgs
    {
        public RC500MessageEventArgs(string strMessage)
        {
            Message = strMessage;
        }
        public string Message { get; set; }
    }*/
}
