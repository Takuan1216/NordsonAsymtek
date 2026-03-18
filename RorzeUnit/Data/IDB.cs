using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Scientech.Data
{
    public interface IDB
    {
        void Open();
        void Close();
        int SQLExec(string sql);
        DataSet Reader(string sql);
        bool IsOpen { get; }

    }
}
