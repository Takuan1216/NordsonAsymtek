using RorzeComm.Log;
using RorzeUnit.Interface;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RorzeApi
{
    public class GMotion
    {
        #region     ======================= Singleton ===========================
        private static readonly GMotion _instancce = new GMotion();
        public static GMotion theInst { get { return _instancce; } }
        #endregion  =============================================================

        //========== logger
        public SLogger _executeLog = SLogger.GetLogger("ExecuteLog");

        private enumTransfeStatus _eTransfeStatus = enumTransfeStatus.Idle;
        public enumTransfeStatus eTransfeStatus
        {
            get
            {
                return _eTransfeStatus;
            }
            set
            {
                if (_eTransfeStatus != value)
                {
                    _eTransfeStatus = value;
                }
            }
        }
        public bool InitOrgnDone { get; set; }
        private GMotion()
        {

        }

        //private List<I_Robot> RobotList = new List<I_Robot>();
        //private List<I_Loadport> LoadportList = new List<I_Loadport>();
        //private List<I_Aligner> AlignerList = new List<I_Aligner>();
        //private List<I_RC5X0_IO> RC5X0IOList = new List<I_RC5X0_IO>();
        //private List<I_RC5X0_Motion> RC5X0Motion = new List<I_RC5X0_Motion>();
        //private List<I_OCR> OCRList = new List<I_OCR>();
        //private List<I_E84> E84List = new List<I_E84>();
        //private List<I_RFID> RFIDList = new List<I_RFID>();
        //private List<I_BarCode> OCRBarCodeList = new List<I_BarCode>();
        //private List<I_BarCode> BarCodeList = new List<I_BarCode>();


        //public void Initialize(List<I_Robot> robot, List<I_Loadport> loader, List<I_Aligner> aligner, List<I_RC5X0_IO> io, List<I_RC5X0_Motion> motion, List<I_OCR> ocr)
        //{
        //    RobotList= robot;
        //    LoadportList = loader;
        //    AlignerList = aligner;
        //    RC5X0IOList = io;
        //    RC5X0Motion = motion;
        //}

        //發送訊息//


        public async Task SendWeChatMessageAsync(string message, string Title)//https://sct.ftqq.com/login    申請帳號
        {

            /*try
            {
                HttpClient httpClient = new HttpClient();
                //Server醬
                var response = await httpClient.GetAsync("https://sc.ftqq.com/SCT244623TIcVR0eIGgdiQJh90U0OSILKV.send"
                    + "?text=" + Title
                    + "&desp=" + message);
                string res = await response.Content.ReadAsStringAsync();
                //Console.WriteLine("Server醬發送狀態：" + response.StatusCode);
                //Console.WriteLine(res);             
            }
            catch
            {
            }*/
        }
    }
}
