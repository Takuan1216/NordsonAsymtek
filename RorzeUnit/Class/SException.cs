using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RorzeUnit.Class
{
    public class SException : Exception
    {
        public int ErrorID { get; set; }
        public SException(int nErrorID, string strMsg) : base(strMsg)
        {
            ErrorID = nErrorID;
        }
    }
}
