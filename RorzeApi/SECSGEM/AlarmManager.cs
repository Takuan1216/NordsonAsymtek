using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Concurrent;
using RorzeApi.Class;
using RorzeComm.Threading;

namespace RorzeApi.SECSGEM
{
    public class AlarmManager
    {
        public delegate void AlarmEventHandler(AlarmEventArgs args); 
        //public event AlarmEventHandler OnAlarmOccurred;
        //public event AlarmEventHandler OnAlarmRemove;
        SPollingThread _exeDequeueAlarmMsg;
        MainDB _DB;
        Dictionary<int, string> _Alarmlist;

        Dictionary<int,CurrentAlarmItem> _CurrentAlarm;

        private ConcurrentQueue<AlarmEventArgs> Alarmqueue;
        public AlarmManager(MainDB MainDB)
        {
            _DB = MainDB;
             Alarmqueue = new ConcurrentQueue<AlarmEventArgs>();
            _Alarmlist = new Dictionary<int, string>();
            _CurrentAlarm = new Dictionary<int, CurrentAlarmItem>();
            //_EQ.OnAlarmHappen += _EQ_OnAlarmHappen;
            _exeDequeueAlarmMsg = new SPollingThread(500);
            _exeDequeueAlarmMsg.DoPolling += _exeDequeueAlarmMsg_DoPolling;
            _exeDequeueAlarmMsg.Set();
        }

        private void _exeDequeueAlarmMsg_DoPolling()
        {
            AlarmEventArgs AlarmMsg;
            if (!Alarmqueue.TryDequeue(out AlarmMsg))
                return;

            if (!_Alarmlist.ContainsKey(AlarmMsg.AlarmID))
                return;

            //if(AlarmMsg.IsSet)
            //{
            //    if (_CurrentAlarm.ContainsKey(AlarmMsg.AlarmID))
            //        return;

            //    _CurrentAlarm.Add(AlarmMsg.AlarmID, new CurrentAlarmItem() { ALID = AlarmMsg.AlarmID, CreateTime = DateTime.Now });

            //    if(OnAlarmOccurred!=null)
            //        OnAlarmOccurred(AlarmMsg);
            //}
            //else
            //{
            //    if (!_CurrentAlarm.ContainsKey(AlarmMsg.AlarmID))
            //        return;

            //    _CurrentAlarm.Remove(AlarmMsg.AlarmID);

            //    if (OnAlarmRemove != null)
            //        OnAlarmRemove(AlarmMsg);
            //}


        }
      

    }


    //public class AlarmEventArgs : EventArgs
    //{
    //    public int AlarmID { get; set; }
    //    public DateTime CreateTime { get; set; }
    //    public bool IsSet;
    //    public AlarmEventArgs(int _AlarmID, DateTime _CreateTime,bool Set)
    //    {
    //        this.AlarmID = _AlarmID;
    //        this.CreateTime = _CreateTime;
    //        IsSet = Set;
    //    }
    //}
   
    public struct CurrentAlarmItem
    {
        public int ALID;
        public DateTime CreateTime;
    }
}
