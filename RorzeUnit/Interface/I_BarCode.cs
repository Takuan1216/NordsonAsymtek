using System;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Interface
{
    public interface I_BarCode
    {
        bool Disable { get; }
        bool Connected { get; }
        string GetReply();
        string Read();
        void ReadTest();
        void Quit();
        void Open();
    }
}
