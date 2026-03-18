using Advantech.Adam;
using RorzeApi;
using RorzeUnit.Class.Aligner.Enum;
using RorzeUnit.Class.RC500.RCEnum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Markup;
using static RorzeUnit.Class.Agito.SSPanelAlignerParm.SSAgitoParm;
using static RorzeUnit.Class.Agito.SSPanelAlignerParm.SSAlignerParm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace RorzeUnit.Class.Agito
{
    internal class SSPanelAlignerParm 
    {
        static object m_lockINI = new object();
        static string m_strFileIni = Directory.GetCurrentDirectory() + "\\ALNSetting.ini";
        public class SSAgitoParm
        {
            public class AxisData
            {
                public bool IsMoving;
                public bool IsServoOn;
                public string m_sAxisName;
                public int Encoder;
                public int InputAcc;
                public int InputDec;
                public int InputVel;
                public int RunAcc;//InputAcc*Resolution
                public int RunDec;//InputDec*Resolution
                public int RunVel;//InputVel*Resolution
                public int InputHomingVel;
                public int InputHomingAcc;
                public int InputHomingDec;
                public int RunHomingVel;//InputHomingVel*Resolution
                public int RunHomingAcc;//InputHomingAcc*Resolution
                public int RunHomingDec;//InputHomingDec*Resolution
                public int HomeOffset;
                public double Resolution;
                public string AxisName { get { return m_sAxisName; } }
                public AxisData(string sAxisName)
                {
                    m_sAxisName = sAxisName;
                }
            }
            int m_iAxisCnt;
       
            public string GetIP { get; private set; }
            public string BodyNo { get; private set; }
            List<AxisData> m_AxisList = new List<AxisData>();
            public SSAgitoParm(string bodyNo, int AxisCnt)
            {
                m_iAxisCnt = AxisCnt;
                BodyNo = bodyNo;
                for (int i = 0; i < m_iAxisCnt; i++)
                {
                    AxisData axisData = new AxisData("Axis" + (i).ToString());
                    m_AxisList.Add(axisData);
                }
                LoadIni();
            }
            public void LoadIni()
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                string AxisNo = "";
                string AlnNo = "ALN" + BodyNo;
                double Resolution;
                lock (m_lockINI)
                {
                    GetIP = myIni.GetIni(AlnNo, "IP", "172.1.1.101");
                    for (int i = 0; i < m_iAxisCnt; i++)
                    {
                        AxisNo = "Axis" + i.ToString() + "_";
                        AxisData axisData = m_AxisList[i];
                        axisData.Resolution = myIni.GetIni(AlnNo, AxisNo + "Resolution", 1.0);
                        Resolution = axisData.Resolution;
                        axisData.InputAcc = myIni.GetIni(AlnNo, AxisNo + "Acc", 20000);
                        axisData.RunAcc = (int)(axisData.InputAcc * Resolution);
                        axisData.InputDec = myIni.GetIni(AlnNo, AxisNo + "Dec", 20000);
                        axisData.RunDec = (int)(axisData.InputDec * Resolution);
                        axisData.InputVel = myIni.GetIni(AlnNo, AxisNo + "Vel", 2000);
                        axisData.RunVel = (int)(axisData.InputVel * Resolution);
                        axisData.InputHomingAcc = myIni.GetIni(AlnNo, AxisNo + "HomingAcc", 20000);
                        axisData.RunHomingAcc = (int)(axisData.InputHomingAcc * Resolution);
                        axisData.InputHomingDec = myIni.GetIni(AlnNo, AxisNo + "HomingDec", 20000);
                        axisData.RunHomingDec = (int)(axisData.InputHomingDec * Resolution);
                        axisData.InputHomingVel = myIni.GetIni(AlnNo, AxisNo + "HomingVel", 2000);
                        axisData.RunHomingVel = (int)(axisData.InputHomingVel * Resolution);
                        axisData.HomeOffset = myIni.GetIni(AlnNo, AxisNo + "HomeOffset", 0);
                    }
                }
            }

            public void WriteIni()
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                string AlnNo = "ALN" + BodyNo;
                lock (m_lockINI)
                {
                    myIni.WriteIni(AlnNo, "IP", GetIP);
                }
                for (int i = 0; i < m_iAxisCnt; i++)
                {
                    WriteIni_Axis(i);
                }
            }

            public void WriteIni_Axis(int AxisIdx)
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                string AlnNo = "ALN" + BodyNo;
                string AxisNo = "Axis" + AxisIdx.ToString() + "_";
                lock (m_lockINI)
                {
                    AxisData axisData = m_AxisList[AxisIdx];
                    myIni.WriteIni(AlnNo, AxisNo + "Acc", axisData.InputAcc);
                    myIni.WriteIni(AlnNo, AxisNo + "Dec", axisData.InputDec);
                    myIni.WriteIni(AlnNo, AxisNo + "Vel", axisData.InputVel);
                    myIni.WriteIni(AlnNo, AxisNo + "HomingAcc", axisData.InputHomingAcc);
                    myIni.WriteIni(AlnNo, AxisNo + "HomingDec", axisData.InputHomingDec);
                    myIni.WriteIni(AlnNo, AxisNo + "HomingVel", axisData.InputHomingVel);
                    myIni.WriteIni(AlnNo, AxisNo + "HomeOffset", axisData.HomeOffset);
                    myIni.WriteIni(AlnNo, AxisNo + "Resolution", axisData.Resolution);
                }
            }
            public int GetAxisCount()
            {
                return m_iAxisCnt;
            }
            public int GetAxisAcc(int AxisIdx)
            {
                return m_AxisList[AxisIdx].InputAcc;
            }
            public int GetAxisRunAcc(int AxisIdx)
            {
                //InputAcc*Resolution
                return m_AxisList[AxisIdx].RunAcc;
            }
            public int GetAxisDec(int AxisIdx)
            {
                return m_AxisList[AxisIdx].InputDec;
            }
            public int GetAxisRunDec(int AxisIdx)
            {
                //InputDec*Resolution
                return m_AxisList[AxisIdx].RunDec;
            }
            public int GetAxisVel(int AxisIdx)
            {
                return m_AxisList[AxisIdx].InputVel;
            }
            public int GetAxisRunVel(int AxisIdx)
            {
                //InputDec*Resolution
                return m_AxisList[AxisIdx].RunVel;
            }
            public int GetAxisHomingAcc(int AxisIdx)
            {
                return m_AxisList[AxisIdx].InputHomingAcc;
            }
            public int GetAxisRunHomingAcc(int AxisIdx)
            {
                //InputHomingAcc*Resolution
                return m_AxisList[AxisIdx].RunHomingAcc;
            }
            public int GetAxisHomingDec(int AxisIdx)
            {

                return m_AxisList[AxisIdx].InputHomingDec;
            }
            public int GetAxisRunHomingDec(int AxisIdx)
            {
                //InputHomingDec*Resolution
                return m_AxisList[AxisIdx].RunHomingDec;
            }

            public int GetAxisHomingVel(int AxisIdx)
            {
                return m_AxisList[AxisIdx].InputHomingVel;
            }
            public int GetAxisRunHomingVel(int AxisIdx)
            {
                //InputHomingVel*Resolution
                return m_AxisList[AxisIdx].RunHomingVel;
            }
            public int GetAxisHomeOffset(int AxisIdx)
            {
                return m_AxisList[AxisIdx].HomeOffset;
            }
            public double GetAxisResolution(int AxisIdx)
            {
                //pulse = mm * 1000 * Resolution
                return m_AxisList[AxisIdx].Resolution;
            }
            public bool IsMoving(int AxisIdx)
            {
                return m_AxisList[AxisIdx].IsMoving;
            }
            public bool IsServoOn(int AxisIdx)
            {
                return m_AxisList[AxisIdx].IsServoOn;
            }
            public int GetPosition(int AxisIdx)
            {
                return m_AxisList[AxisIdx].Encoder;
            }

            public void SetAxisAcc(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].InputAcc = data;
                m_AxisList[AxisIdx].RunAcc = (int)(data * m_AxisList[AxisIdx].Resolution);
            }
            public void SetAxisDec(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].InputDec = data;
                m_AxisList[AxisIdx].RunDec = (int)(data * m_AxisList[AxisIdx].Resolution);
            }
            public void SetAxisVel(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].InputVel = data;
                m_AxisList[AxisIdx].RunVel = (int)(data * m_AxisList[AxisIdx].Resolution);
            }
            public void SetAxisHomingAcc(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].InputHomingAcc = data;
                m_AxisList[AxisIdx].RunHomingAcc = (int)(data * m_AxisList[AxisIdx].Resolution);
            }
            public void SetAxisHomingDec(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].InputHomingDec = data;
                m_AxisList[AxisIdx].RunHomingDec = (int)(data * m_AxisList[AxisIdx].Resolution);
            }
            public void SetAxisHomingVel(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].InputHomingVel = data;
                m_AxisList[AxisIdx].RunHomingVel = (int)(data * m_AxisList[AxisIdx].Resolution);
            }
            public void SetAxisHomeOffset(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].HomeOffset = data;

            }
            public void SetAxisResolution(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].Resolution = data;
            }
            public void SetMoving(int AxisIdx, bool data)
            {
                m_AxisList[AxisIdx].IsMoving = data;
            }
            public void SetServoOnStatus(int AxisIdx, bool data)
            {
                m_AxisList[AxisIdx].IsServoOn = data;
            }
            public void SetPosition(int AxisIdx, int data)
            {
                m_AxisList[AxisIdx].Encoder = data;
            }


        }

        public class SSAlignerParm
        {
            public class HOMEPosition
            {
                public int X = 0;
                public int Y = 0;
                public int Z = 0;
                public int R = 0;
            }

            public Dictionary<int, string[]> m_dicDALNData = new Dictionary<int, string[]>();
            private Dictionary<int,List<HOMEPosition>> m_DicHOMEPosition;
            public string BodyNo { get; private set; }
            public SSAlignerParm(string bodyNo)
            {
                BodyNo = bodyNo;
                m_DicHOMEPosition = new Dictionary<int, List<HOMEPosition>>();
                for (int i = 0; i < 8; i++)
                {
                    List<HOMEPosition> Temp = new List<HOMEPosition>();
                    m_DicHOMEPosition.Add(i, Temp);
                }
                LoadIni();
            }
            public string[] GetDALN(int P1)
            {
                if (P1 > 7)
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Input parameter outrange. [{0}]", "GALN"));
                return m_dicDALNData[P1];
            }
            public void SetDALN(int P1, string[] Data)
            {
                if (P1 > 7 )
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Input parameter outrange. [{0}]", "GALN"));
                m_dicDALNData[P1] = Data;
               RefreshHomeData(P1, Data);
            }
            public void SetDALN(int P1,int P2,string Data)
            {
                if (P1 > 7 || P2 > 31)
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Input parameter outrange. [{0}]", "GALN"));
                m_dicDALNData[P1][P2] = Data;
                if(P2 < 18)
                    RefreshHomeData(P1, m_dicDALNData[P1]);
            }

            public void WriteIni()
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                string AlnNo = "ALN" + BodyNo + "_Data";
                string SavingDada = "";
                string[] RunningData;
                //Write ALGN
                for (int i = 0; i < 8; i++)
                {
                    SavingDada = "";
                    RunningData = m_dicDALNData[i];
                    for (int j = 0;j < 32; j++)
                    {
                        SavingDada += RunningData[j] + ",";
                    }
                    myIni.WriteIni(AlnNo, $"DALN[{i}]", SavingDada);
                }
            }
            public void LoadIni()
            {
                CINIFile myIni = new CINIFile(m_strFileIni);
                string AlnNo = "ALN" + BodyNo + "_Data";
                string LoadingData = "";
                //Load ALGN
                for (int i = 0; i < 8; i++)
                {
                    LoadingData = "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0";
                    LoadingData = myIni.GetIni(AlnNo, $"DALN[{i}]", LoadingData);
                    m_dicDALNData.Add(i, LoadingData.Split(','));
                    RefreshHomeData(i, m_dicDALNData[i]);
                }
            }
            private void RefreshHomeData(int P1, string[] data)
            {
                m_DicHOMEPosition[P1].Clear();
                for (int j = 0; j < 4; j++)
                {
                    HOMEPosition classPos = new HOMEPosition();
                    classPos.X = int.Parse(data[0 + j]);
                    classPos.Y = int.Parse(data[4 + j]);
                    classPos.Z = int.Parse(data[8 + j]);
                    classPos.R = int.Parse(data[14 + j]);
                    m_DicHOMEPosition[P1].Add(classPos);
                }
            }
            public HOMEPosition GetHomePos(int P1,int HomeIndex)
            {
                if (P1 > 7 || HomeIndex > 3)
                    throw new SException((int)enumAlignerError.SendCommandFailure, string.Format("Input parameter outrange. [{0}]", "GALN"));
                return m_DicHOMEPosition[P1][HomeIndex];
            }



        }


    }
}
