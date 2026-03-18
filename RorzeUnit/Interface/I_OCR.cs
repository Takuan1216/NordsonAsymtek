using RorzeUnit.Class;
using System.Collections.Generic;
using System.Text;

namespace RorzeUnit.Interface
{
    public interface I_OCR
    {
        //Motion method======================================
        bool Initial(string strRecipe);
        bool OnLine();
        bool OffLine();
        bool SetRecipe(string strRecipe);
        bool Read(ref string strResult, bool bOKSaveImage, string strCarrierID = "", string strLotID = "");
        bool SaveImage(string strFaileName);
        bool SaveImage(string strFolder, string strFaileName = "test.jpg");
        string[] getRecipt();

        //property=========================================== 
        bool Disable { get; }
        bool Connected { get; }
        string Name { get; }
        bool IsFront { get; }
        string SavePicturePath { get; }

    }
}
