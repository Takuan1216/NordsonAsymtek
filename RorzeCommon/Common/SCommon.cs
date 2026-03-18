using RorzeUnit.Class;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static RorzeUnit.Class.SWafer;

namespace RorzeComm
{
    //delegate callback
    public delegate void dlgv_v();
    public delegate void dlgv_b(bool b);
    public delegate void dlgv_n(int n1);
    public delegate void dlgv_n_n(int n1, int n2);
    public delegate void dlgv_d(double d);
    public delegate void dlgv_s(string s);
    public delegate void dlgv_s_n(string s, int n);
    public delegate void dlgv_slist(string s, params string[] slist);
    public delegate void dlgv_slistL(string s, params List<string>[] list);
    public delegate void dlgv_Object(object o);
    public delegate void dlgv_Object_INT(object o, int n);

    public delegate void dlgv_wafer(SWafer w);

    public delegate int dlgn_v();
    public delegate int dlgn_n(int n);
    public delegate int dlgn_d(double d);
    public delegate int dlgn_s(string s);
    public delegate int dlgn_o_n(object o, int n);

    public delegate bool dlgb_v();
    public delegate bool dlgb_b(bool b);
    public delegate bool dlgb_n(int n);
    public delegate bool dlgb_Object(object o);
    public delegate bool dlgb_o_b(object o, bool b);
    public delegate bool dlgb_o_o_o_o(object o1, object o2, object o3, object o4);
    public delegate bool dlgb_Enum(enumPosition e);

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
        public MessageEventArgs(string[] strMessage)
        {
            Message = strMessage;
        }
        public string[] Message { get; set; }
    }

    //event handler
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
    public delegate void MessageEventHandler(object sender, MessageEventArgs e);
}
