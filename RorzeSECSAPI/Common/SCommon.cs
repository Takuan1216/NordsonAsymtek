using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RorzeComm
{
    public enum ToolUnit { Loader = 0, Unloader, EQ }

    //delegate callback
    public delegate void dlgv_v();
    public delegate void dlgv_n(int n1);
    public delegate void dlgv_n_n(int n1, int n2);
    public delegate void dlgv_d(double d);
    public delegate void dlgv_s(string s);
    public delegate void dlgv_slist(string s, params string[] slist);
    public delegate void dlgv_slistL(string s, params List<string>[] list);
    public delegate void dlgv_Object(object o);

    public delegate int dlgn_v();
    public delegate int dlgn_n(int n);
    public delegate int dlgn_d(double d);
    public delegate int dlgn_s(string s);

    public delegate bool dlgb_v();

    //event arguments
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string str, int alarmID)
        {
            Message = str;
            AlarmID = alarmID;
        }
        public string Message { get; set; }
        public int AlarmID { get; set; }
    }
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string strMessage)
        {
            Message = strMessage;
        }
        public string Message { get; set; }
    }





    //event handler
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
    public delegate void MessageEventHandler(object sender, MessageEventArgs e);
}
