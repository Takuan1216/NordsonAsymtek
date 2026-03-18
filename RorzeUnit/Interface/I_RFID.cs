using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Interface
{
    public interface I_RFID
    {
        bool IsConnect();
        string GetCommand();

        string ReadMID();
        bool CheckReader();
        string GetReply();

    
    }
}
