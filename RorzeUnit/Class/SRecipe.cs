using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using RorzeUnit.Data;
using RorzeComm.Log;
using System.Drawing;


namespace RorzeUnit.Class
{
    public class SRecipe
    {
        private SMainDB _dbRecipe;

        private SLogger _logger = SLogger.GetLogger("ExecuteLog");

        public class SSChamberRecipeInfo
        {
            public string RecipeName, ArmMode, ArmNo, Dispense, FixedNozzle, Backside, Direction,
                           CreateTime, ModifyBy, ModifyTime;
            public int WaferSize, SpinSpeed, Step, AssignChamber, Flush, Cu_1Multiply, Cu_2Multiply, TiMultiply;
            public float StepTime;
        }
        public class SSArmRecipeInfo
        {
            public string WaferSize, ArmNo, RecipeName, SwingIsMoveMode, SwingIsSpeedMode, StartOffSet, EndOffSet, UnderPoint, PointNumber, ModifyTime, ModifyUser;
            public int[] ArmPosSpeed;
        }
        public class SSToolRecipeInfo
        {
            public string RecipeName, ModelName, UserID, CassetteType;
            public int nWaferSize, nSlot, nWaitTimePut, nWaitTimeTake;
            public double nAligne, nMagneticPut, nMagneticPutMax, nMagneticPutMin, nMagneticTake, nMagneticTakeMax, nMagneticTakeMin;
        }

        public event EventHandler OnAddArmRecipe;
        public event EventHandler OnDeleteArmRecipe;
        public event EventHandler OnModifiedArmRecipe;

        //public event EventHandler OnAddToolRecipe;
        public event EventHandler OnDeleteToolRecipe;
        //public event EventHandler OnModifiedToolRecipe;

        //public SRecipe(SMainDB db) { _dbRecipe = db; }

        //========== property
        //private int _nProfileRatio;
        //public int ProfileRatio
        //{
        //    get { return _nProfileRatio; }
        //    set
        //    {
        //        if (_nProfileRatio == value) return;
        //        _logger.WriteLog("[Recipe] Set Profile ratio from [{0}] to [{1}].", _nProfileRatio, value);
        //        _nProfileRatio = value;
        //    }
        //}

        //========== member function

        public bool IsPassToRunChamber(string recipe, int waferSize, params bool[] enableChamber)
        {
            int nMustChamber = 0; //紀錄此recipe要去哪些chamber
            int nEnableChamber = 0; //紀錄目前tool有哪些可用chamber

            //檢查機況
            for (int chamberNo = 0; chamberNo < enableChamber.Length; chamberNo++)
                nEnableChamber += enableChamber[chamberNo] ? (1 << chamberNo) : 0;

            //檢查recipe
            //SSChamberRecipeInfo[] steps = GetChamberRecipeStep(waferSize, recipe);
            SSToolRecipeInfo[] steps = GetRecipeData(waferSize, recipe);
            if (steps.Length <= 0) return false; //recipe不允許沒有任何步驟
            //nMustChamber = steps[0].AssignChamber;
            //nMustChamber = steps[0].station;
            //pass?
            return (nMustChamber & nEnableChamber) > 0;
        }

        public bool IsPassToRunTank(string recipe, int waferSize, params bool[] enableTank)
        {
            return true;
        }

        public bool IsChemicalExchange(string recipe, int waferSize)
        {
            return false;
        }


        public bool IsChemExMultiply(/*List<SWafer> recipe*/string recipe, int waferSize, string strTank)
        {
            return false;
        }

        public bool CheckRecipeExist(string strRecipeName)
        {
            bool ret = false;
            DataSet ds = _dbRecipe.Reader("Select * From RecipeTool Where RecipeName='{0}'", strRecipeName);
            if (ds != null)
            {
                if (ds.Tables[0].Rows.Count > 0)
                    ret = true;
            }
            return ret;
        }

        public string[] GetRecipeNameList(int nSize)
        {
            List<string> lstRecipe = new List<string>();

            DataSet ds;
            if (nSize != 99)
                ds = _dbRecipe.Reader("Select * From RecipeTool Where WaferSize={0}", nSize);
            else
                ds = _dbRecipe.Reader("Select * From RecipeTool");

            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());
            }
            return lstRecipe.ToArray();
        }

        public string[] GetRecipeList(int nWaferSize)
        {
            List<string> lstRecipe = new List<string>();
            DataSet ds = _dbRecipe.Reader("Select distinct RecipeName From RecipeChamber Where WaferSize={0} Order By RecipeName", nWaferSize);
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());
            }
            return lstRecipe.ToArray();
        }
        public string[] GetToolRecipeList(int nWaferSize)
        {
            List<string> lstRecipe = new List<string>();
            DataSet ds = _dbRecipe.Reader("Select distinct RecipeName From RecipeTool Where WaferSize={0} Order By RecipeName", nWaferSize);
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());
            }
            return lstRecipe.ToArray();
        }

        public SSToolRecipeInfo[] GetRecipeData(int nWaferSize)
        {
            List<SSToolRecipeInfo> lstRecipeStep = new List<SSToolRecipeInfo>();
            DataSet ds = _dbRecipe.Reader("Select * From RecipeTool Where WaferSize={0}", nWaferSize);
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                {
                    SSToolRecipeInfo step = new SSToolRecipeInfo();
                    step.RecipeName = ds.Tables[0].Rows[nRow]["RecipeName"].ToString();
                    step.ModelName = ds.Tables[0].Rows[nRow]["ModelName"].ToString();
                    step.nAligne = Convert.ToDouble(ds.Tables[0].Rows[nRow]["Aligne"].ToString());
                    step.UserID = ds.Tables[0].Rows[nRow]["UserID"].ToString();
                    step.CassetteType = ds.Tables[0].Rows[nRow]["CassetteType"].ToString();
                    step.nWaferSize = int.Parse(ds.Tables[0].Rows[nRow]["WaferSize"].ToString());
                    step.nSlot = int.Parse(ds.Tables[0].Rows[nRow]["Slot"].ToString());
                    step.nMagneticPut = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPut"].ToString());
                    step.nMagneticPutMax = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPutMax"].ToString());
                    step.nMagneticPutMin = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPutMin"].ToString());
                    step.nWaitTimePut = int.Parse(ds.Tables[0].Rows[nRow]["WaitTimePut"].ToString());
                    step.nMagneticTake = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTake"].ToString());
                    step.nMagneticTakeMax = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTakeMax"].ToString());
                    step.nMagneticTakeMin = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTakeMin"].ToString());
                    step.nWaitTimeTake = int.Parse(ds.Tables[0].Rows[nRow]["WaitTimeTake"].ToString());
                    lstRecipeStep.Add(step);
                }
            }
            return lstRecipeStep.ToArray();
        }
        public SSToolRecipeInfo[] GetRecipeData(string strRecipe)
        {
            List<SSToolRecipeInfo> lstRecipeStep = new List<SSToolRecipeInfo>();
            DataSet ds = _dbRecipe.Reader("Select * From RecipeTool Where RecipeName='{0}'", strRecipe);
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                {
                    SSToolRecipeInfo step = new SSToolRecipeInfo();
                    step.RecipeName = ds.Tables[0].Rows[nRow]["RecipeName"].ToString();
                    step.ModelName = ds.Tables[0].Rows[nRow]["ModelName"].ToString();
                    step.nAligne = Convert.ToDouble(ds.Tables[0].Rows[nRow]["Aligne"].ToString());
                    step.UserID = ds.Tables[0].Rows[nRow]["UserID"].ToString();
                    step.CassetteType = ds.Tables[0].Rows[nRow]["CassetteType"].ToString();
                    step.nWaferSize = int.Parse(ds.Tables[0].Rows[nRow]["WaferSize"].ToString());
                    step.nSlot = int.Parse(ds.Tables[0].Rows[nRow]["Slot"].ToString());
                    step.nMagneticPut = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPut"].ToString());
                    step.nMagneticPutMax = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPutMax"].ToString());
                    step.nMagneticPutMin = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPutMin"].ToString());
                    step.nWaitTimePut = int.Parse(ds.Tables[0].Rows[nRow]["WaitTimePut"].ToString());
                    step.nMagneticTake = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTake"].ToString());
                    step.nMagneticTakeMax = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTakeMax"].ToString());
                    step.nMagneticTakeMin = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTakeMin"].ToString());
                    step.nWaitTimeTake = int.Parse(ds.Tables[0].Rows[nRow]["WaitTimeTake"].ToString());
                    lstRecipeStep.Add(step);
                }
            }
            return lstRecipeStep.ToArray();
        }
        public SSToolRecipeInfo[] GetRecipeData(int nWaferSize, string strRecipe)
        {
            List<SSToolRecipeInfo> lstRecipeStep = new List<SSToolRecipeInfo>();
            DataSet ds = _dbRecipe.Reader("Select * From RecipeTool Where RecipeName='{0}' and WaferSize = {1}", strRecipe, nWaferSize);
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                {
                    SSToolRecipeInfo step = new SSToolRecipeInfo();
                    step.RecipeName = ds.Tables[0].Rows[nRow]["RecipeName"].ToString();
                    step.ModelName = ds.Tables[0].Rows[nRow]["ModelName"].ToString();
                    step.nAligne = Convert.ToDouble(ds.Tables[0].Rows[nRow]["Aligne"].ToString());
                    step.UserID = ds.Tables[0].Rows[nRow]["UserID"].ToString();
                    step.CassetteType = ds.Tables[0].Rows[nRow]["CassetteType"].ToString();
                    step.nWaferSize = int.Parse(ds.Tables[0].Rows[nRow]["WaferSize"].ToString());
                    step.nSlot = int.Parse(ds.Tables[0].Rows[nRow]["Slot"].ToString());
                    step.nMagneticPut = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPut"].ToString());
                    step.nMagneticPutMax = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPutMax"].ToString());
                    step.nMagneticPutMin = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticPutMin"].ToString());
                    step.nWaitTimePut = int.Parse(ds.Tables[0].Rows[nRow]["WaitTimePut"].ToString());
                    step.nMagneticTake = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTake"].ToString());
                    step.nMagneticTakeMax = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTakeMax"].ToString());
                    step.nMagneticTakeMin = Convert.ToDouble(ds.Tables[0].Rows[nRow]["MagneticTakeMin"].ToString());
                    step.nWaitTimeTake = int.Parse(ds.Tables[0].Rows[nRow]["WaitTimeTake"].ToString());
                    lstRecipeStep.Add(step);
                }
            }
            return lstRecipeStep.ToArray();
        }


        public bool ModifyToolRecipe(int nSize, params SSToolRecipeInfo[] args)
        {
            try
            {
                //DataSet ds = _dbRecipe.Reader("Select * From RecipeTool Where RecipeName='{0}'", strRecipe);
                //if (ds != null)
                //{
                EditRecipeTool(nSize, args);
                //if (ds.Tables[0].Rows.Count > 0) //recipe已存在
                //{
                //    if (OnModifiedToolRecipe != null) OnModifiedToolRecipe(this, new EventArgs());
                //}
                //else
                //{
                //    if (OnAddToolRecipe != null) OnAddToolRecipe(this, new EventArgs());
                //}
                //}
                //else
                //{
                //    EditRecipeTool(args);
                //    if (OnAddToolRecipe != null) OnAddToolRecipe(this, new EventArgs());
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                return false;
            }
            return true;
        }
        private void EditRecipeTool(int nSize, params SSToolRecipeInfo[] args)
        {
            //win 20151026
            string strDateTime = SAccessDb.DateTimeFormat(DateTime.Now);

            _dbRecipe.SQLExec("Delete * From RecipeTool Where WaferSize ={0}", nSize);

            foreach (SSToolRecipeInfo step in args)
            {
                _dbRecipe.SQLExec("INSERT INTO RecipeTool(HistoryTime, RecipeName, ModelName, Aligne, UserID, CassetteType, WaferSize, Slot, MagneticPut, MagneticPutMax, MagneticPutMin, WaitTimePut, MagneticTake, MagneticTakeMax, MagneticTakeMin, WaitTimeTake) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}')",
                        strDateTime, step.RecipeName, step.ModelName, step.nAligne, step.UserID, step.CassetteType, step.nWaferSize, step.nSlot,
                        step.nMagneticPut, step.nMagneticPutMax, step.nMagneticPutMin, step.nWaitTimePut,
                        step.nMagneticTake, step.nMagneticTakeMax, step.nMagneticTakeMin, step.nWaitTimeTake);
            }
        }

        public bool DeleteToolRecipe(string strRecipe)
        {
            try
            {
                _dbRecipe.SQLExec("Delete * From RecipeTool Where RecipeName='{0}'", strRecipe);
                if (OnDeleteToolRecipe != null) OnDeleteToolRecipe(this, new EventArgs());
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[Recipe] Delete recipe [{0}] failure. [{1}]", strRecipe, ex.ToString());
                return false;
            }
            return true;
        }

        //========== 應用函式 for arm
        public string[] GetArmRecipeList(int nArmNo, int nWaferSize = 12)
        {
            List<string> lstArmRecipe = new List<string>();
            DataSet ds = _dbRecipe.Reader("Select * From RecipeArmPar Where ArmNo='{0}' And WaferSize='{1}'", nArmNo, nWaferSize);
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstArmRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());
            }
            return lstArmRecipe.ToArray();
        }
        public string[] GetArmRecipeList(int nArmNo, string strDispense, int nWaferSize = 12)
        {
            List<string> lstArmRecipe = new List<string>();
            DataSet ds;

            ds = _dbRecipe.Reader("Select * From RecipeArmPar Where ArmNo='{0}' and SwingIsMoveMode = '{1}' and WaferSize = '{2}'", nArmNo, strDispense, nWaferSize);

            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstArmRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());
            }
            return lstArmRecipe.ToArray();
        }
        public string[] GetArmRecipeList(string strArmNo, string strSize, string strMode)
        {
            List<string> lstArmRecipe = new List<string>();
            DataSet ds = _dbRecipe.Reader(string.Format("Select * From RecipeArmPar where PointNumber = '50' AND ArmNo = '{0}' AND WaferSize = '{1}' AND SwingIsMoveMode = '{2}'", strArmNo, strSize, strMode));
            if (ds != null)
            {
                for (int nRow = 0; nRow < ds.Tables[0].Rows.Count; nRow++)
                    lstArmRecipe.Add(ds.Tables[0].Rows[nRow]["RecipeName"].ToString());
            }
            return lstArmRecipe.ToArray();
        }

        public void ModifyArmRecipe()
        {
            OnModifiedArmRecipe(this, new EventArgs());
        }
        public void AddArmRecipe()
        {
            OnAddArmRecipe(this, new EventArgs());
        }
        public void DelArmRecipe()
        {
            OnDeleteArmRecipe(this, new EventArgs());
        }

        public bool ExportArmRecipe(string strRecipe, string path)
        {
            try
            {
                DataSet ds = _dbRecipe.Reader("Select * Form RecipeArmPar Where RecipeName='{0}'", strRecipe);
                if (ds == null) throw new Exception(string.Format("Cannot export arm recipe, the Arm recipe [{0}] not found.", strRecipe));
                if (ds.Tables[0].Rows.Count <= 0) throw new Exception(string.Format("Cannot export arm recipe, the Arm recipe [{0}] not found.", strRecipe));
                SAccessDb.ExportCSV(ds.Tables[0], path);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("[Recipe] Export arm recipe [{0}] was failure. [{1}]", strRecipe, ex.ToString());
                return false;
            }
            return true;
        }
        public SSArmRecipeInfo ImportArmRecipe(string path)
        {
            SSArmRecipeInfo armRecipe = new SSArmRecipeInfo();
            try
            {
                //========== 驗證路徑
                if (!File.Exists(path))
                {
                    _logger.WriteLog("[Recipe] Cannot import arm recipe due to CSV file [{0}] not found.", path);
                    return armRecipe;
                }
                if (!Path.GetExtension(path).ToLower().Contains("csv"))
                {
                    _logger.WriteLog("[Recipe] Cannot import arm recipe due to file format is wrong. [{0}]", path);
                    return armRecipe;
                }
                StreamReader sr = new StreamReader(path);
                sr.ReadLine(); //第一行是欄位名稱, 直接丟掉
                string str = sr.ReadLine();
                string[] cells = str.Split(',');
                if (cells.Length < 20)
                {
                    _logger.WriteLog("[Recipe] Cannot import arm recipe due to the count of field is wrong. cells = [{0}]", cells.Length);
                    return armRecipe;
                }
                armRecipe.WaferSize = cells[0];
                armRecipe.ArmNo = cells[1];
                armRecipe.RecipeName = cells[2];
                armRecipe.SwingIsMoveMode = cells[3];
                armRecipe.SwingIsSpeedMode = cells[4];
                armRecipe.StartOffSet = cells[5];
                armRecipe.EndOffSet = cells[6];
                armRecipe.UnderPoint = cells[7];
                armRecipe.PointNumber = cells[8];

                armRecipe.ArmPosSpeed = new int[50];
                for (int nCnt = 0; nCnt < 50; nCnt++)
                {
                    armRecipe.ArmPosSpeed[nCnt] = Convert.ToInt32(cells[10 + nCnt]);
                }
                sr.Close();
            }
            catch (Exception ex)
            {
                _logger.WriteLog("Recipe", ex);
            }

            return armRecipe;
        }



        public double GetArmCenterPos(string strChamberNo, string strArmNo, string strNozzle, string strPos)
        {
            double doubleCenter = 0;

            DataSet ds = _dbRecipe.Reader(string.Format("Select * From ArmTune where ChamberNo = '{0}' and ArmNo = '{1}' and ChemicalNo = '{2}'", strChamberNo, strArmNo, strNozzle));

            if (ds != null)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i]["Position"].ToString() == strPos)
                    {
                        doubleCenter = double.Parse(ds.Tables[0].Rows[i]["PulseValue"].ToString());
                    }
                }
            }
            return doubleCenter;
        }

        //Daniel 20141210
        public Dictionary<string, ArmItem> ArmRange;

        //定義結構
        public class ArmItem
        {
            public long ArmRangeStart;
            public long ArmRangeEnd;
            public long ArmRangeCenter;
        }

        public ArmItem GetArmRange(int intChamberNo, int intArmNo, int intNo)
        {
            ArmItem armItem = new ArmItem();

            int ChamberStart = 1150 + ((intChamberNo - 1) * 200);
            int ArmNo = ChamberStart + ((intArmNo - 1) * 40);
            int Number = ArmNo + (intNo + 1); //EM1152 double word
            int start = ArmNo + 21; //range-
            int end = ArmNo + 20; //range+


            return armItem;
        }
        public ArmItem GetArmRange(int nChamberNo, int nArmNo, string strLiquid)
        {
            int nLiquid = 0;

            switch (strLiquid)
            {
                case "Chemical#1":
                case "Chemical#2":
                    nLiquid = 1;
                    break;
                case "Chemical#3":
                    nLiquid = 2;
                    break;
                //case "Chemical#2":
                //case "Chemical#4":   

                case "Rinse":
                    nLiquid = 2;
                    break;
                case "N2":
                    nLiquid = 3;
                    break;
                case "DIW":
                    nLiquid = 4;
                    break;
                default:
                    break;
            }
            //case "Chemical#1": aintChamberRecipe[9 + (step * 20) + 20] = 1; break;
            //                case "Chemical#2": aintChamberRecipe[9 + (step * 20) + 20] = 1; break;
            //                case "Chemical#3": aintChamberRecipe[9 + (step * 20) + 20] = 2; break;
            //                case "Rinse": aintChamberRecipe[9 + (step * 20) + 20] = 2; break;
            //                case "DIW": aintChamberRecipe[9 + (step * 20) + 20] = 4; break;
            //                case "N2": aintChamberRecipe[9 + (step * 20) + 20] = 5; break;
            //                case "APC": aintChamberRecipe[9 + (step * 20) + 20] = 6; break;
            //                case "Not Use":

            return GetArmRange(nChamberNo, nArmNo, nLiquid);
        }
    }
}
