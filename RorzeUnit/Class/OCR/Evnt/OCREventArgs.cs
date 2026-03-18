using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.OCR.Evnt
{

    public delegate void RorzeProtoclHandler(object sender, RorzeProtoclEventArgs e);
    public class RorzeProtoclEventArgs : EventArgs
    {
        public RecvFrame Frame { get; set; }
        public RorzeProtoclEventArgs(string frame)
        {
            Frame = new RecvFrame(frame);
        }
    }
    public class RecvFrame
    {
        private string _strFrame;   //aTBL1.STAT:00000/0000

        private char _charHeader;   //o,a,n,c,e
        private string _strID;      //TBL1.
        private string _strData;    //STAT:00000/0000

        private int _nBodyNo;       //1
        private string _strCommand; //STAT
        private string _strAttachCommand;
        private string _strValue;   //00000/0000

        public RecvFrame(string strFrame)
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

}
