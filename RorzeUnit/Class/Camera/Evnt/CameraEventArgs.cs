using System;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class.Camera.Evnt
{
    public class ProtoclEventArgs : EventArgs
    {
        public StatFrame _Frame { get; private set; }
        public ProtoclEventArgs(string frame)
        {
            _Frame = new StatFrame(frame);
        }
    }
    public class StatFrame
    {
        private string m_strFrame;   //aTRB1.STAT:00000/0000

        private char m_charHeader;   //o,a,n,c,e
        private string m_strID;      //TRB1.
        private string m_strData;    //STAT:00000/0000

        private int m_nBodyNo;       //1
        private string m_strCommand; //STAT
        private string m_strValue;   //00000/0000

        public StatFrame(string strFrame)
        {
            m_strFrame = strFrame.Trim('\r', '\n');

            m_charHeader = m_strFrame[0];
            m_strID = m_strFrame.Substring(1, 5);
            m_strData = m_strFrame.Substring(6);

            m_nBodyNo = m_strID[3] >= 'A' ? m_strID[3] - 'A' + 10 : m_strID[3] - '0';
            m_strCommand = m_strData.Split(':')[0];
            m_strValue = m_strData.Contains(':') ? m_strData.Split(':')[1] : "";
        }

        public char _Header { get { return m_charHeader; } }
        public string _ID { get { return m_strID; } }
        public string _Data { get { return m_strData; } }
        public int _BodyNo { get { return m_nBodyNo; } }
        public string _Command { get { return m_strCommand; } }
        public string _Value { get { return m_strValue; } }

    }
}
