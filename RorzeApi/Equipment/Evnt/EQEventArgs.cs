using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using RorzeUnit.Class.EQ.Enum;
using RorzeUnit.Interface;

namespace RorzeUnit.Class.EQ.Event
{

    public delegate void AutoProcessingEventHandler(object sender);


    public delegate void EQProtoclHandler(object sender, EQProtoclEventArgs e);
    //  TCP收到字串
    public class EQProtoclEventArgs : EventArgs
    {
        public EQFrame Frame { get; set; }
        public EQProtoclEventArgs(string frame)
        {
            Frame = new EQFrame(frame);
        }
    }
    //  解析TCP收到字串
    public class EQFrame
    {
        private char _char1 = '\x02';//起始碼
        private char _char2 = '\r';

        //  #CanLoad$   #SaveName,FileName:Name$    #Report,DataName:DataValue,DataName:DataValue$
        // XYZ EX: #Status,0,W,M
        private char _charHeader;   //o,a,n,c,e
        private string _strReceive; //  CanLoad   SaveName,FileName:Name    Report,DataName:DataValue,DataName:DataValue
        private string _strCommand; //  CanLoad   SaveName    Report
        private string _result;
        private string _strValue;
        private string[] _straData;  //    FileName:Name   DataName:DataValue,DataName:DataValue

        /*public EQFrame(string Receive)
        {
            _strReceive = Receive.Trim('\r', '\n', _char1, _char2);

            if (Receive.Length > 0)
            {
                _charHeader = _strReceive[0];
                _strReceive = _strReceive.Substring(1);//去頭
            }

            string[] strArrary = _strReceive.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); // 
            if (strArrary.Length < 2) return;//沒有資料直接完成
            _strCommand = strArrary[0];
            _result = strArrary[1];
            _straData = strArrary.Skip(2).ToArray();
        }*/

        public EQFrame(string Receive)
        {
            _strReceive = Receive.Trim('\r', '\n', _char1, _char2);

            if (Receive.Length > 0)
            {
                _charHeader = _strReceive[0];
                _strReceive = _strReceive.Substring(1);//去頭
            }

            string[] strArrary = _strReceive.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            _strCommand = strArrary[0];
            if (strArrary.Length == 1) return;//沒有資料直接完成
            _strValue = strArrary[1];
            _straData = strArrary[1].Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public char Header { get { return _charHeader; } }
        public string Command { get { return _strCommand; } }
        public string Result { get { return _result; } }
        public string Value { get { return _strValue; } }
        public string[] Data { get { return _straData; } }
    }
}
