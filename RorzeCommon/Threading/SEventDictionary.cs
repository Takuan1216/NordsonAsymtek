using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace RorzeComm.Threading
{
    public class SEventDictionary<T>
    {
        public EventHandlerList listEventDelegates;
        public EventHandlerList listEventFisrst;
        public EventHandlerList listEventEnd;
        public Dictionary<string, object> listKeys;
        string Event_Name;
        public SEventDictionary(string UnitName)
        {
            listEventDelegates = new EventHandlerList();
            listEventFisrst = new EventHandlerList();
            listEventEnd = new EventHandlerList();
            listKeys = new Dictionary<string, object>();
            Event_Name = UnitName;
        }
     
        public event EventHandler<T> EventAction
        {
            add
            {
                listEventDelegates.AddHandler(Event_Name, value);
            }
            remove
            {
                listEventDelegates.RemoveHandler(Event_Name, value);
            }
        }
        public event EventHandler EventFirst
        {
            add
            {
                listEventFisrst.AddHandler(Event_Name, value);
            }
            remove
            {
                listEventFisrst.RemoveHandler(Event_Name, value);
            }
        }
        public event EventHandler EventEnd
        {
            add
            {
                listEventEnd.AddHandler(Event_Name, value);
            }
            remove
            {
                listEventEnd.RemoveHandler(Event_Name, value);
            }
        }


        protected void OnEventOccur(string UnitName,T e)
        {
            var eh = listEventDelegates[UnitName];
            if (eh == null)
                return;

            foreach (var d in eh.GetInvocationList())
            {
               var h = (EventHandler<T>)d;
               try
                {
                    var args = e;
                    h.Invoke(this, args);
                }
                 catch (Exception ex)
                {
                    Console.WriteLine("{0} Exception caught.", ex);
                } 
            }
        }
        public void ChangeEvent(string UnitName , T e)
        {
            this.OnEventOccur(UnitName, e);
        }

        public void FistEvent(string UnitName)
        {
            this.OnFistEvent(UnitName);
        }
        protected void OnFistEvent(string UnitName)
        {
            var eh = listEventFisrst[UnitName];
            if (eh == null)
                return;

            foreach (var d in eh.GetInvocationList())
            {
                var h = (EventHandler)d;
                try
                {
                    h.Invoke(this, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} Exception caught.", ex);
                }
            }
        }

        public void EndEvent(string UnitName)
        {
            this.OnEndEvent(UnitName);
        }
        protected void OnEndEvent(string UnitName)
        {
            var eh = listEventEnd[UnitName];
            if (eh == null)
                return;

            foreach (var d in eh.GetInvocationList())
            {
                var h = (EventHandler)d;
                try
                {
                    h.Invoke(this, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} Exception caught.", ex);
                }
            }
        }
    }
}
