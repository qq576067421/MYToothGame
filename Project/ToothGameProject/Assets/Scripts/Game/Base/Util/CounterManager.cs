using System;
using System.Collections.Generic;
using System.Text;

namespace GameDll
{
    public class Counter
    {
        private static long m_IdAll = 0;
        private long id;
        public System.Action m_PerCall;
        public int m_IntervalMMSeconds = 1000; //毫秒
        public int m_TotalCount = 1;
        public bool m_Loop = false;
        public int m_IntervalPastTime;
        public int m_Delay = 0;
        public System.Action m_FinishCall;

        public static Counter CreateClass()
        {
            var counter = new Counter();
            counter.Init();
            return counter;
        }
        public void Init()
        {
            id = ++m_IdAll;
            m_PerCall = null;
            m_IntervalMMSeconds = 1000; //毫秒
            m_TotalCount = 1;
            m_IntervalPastTime = 0;
            m_Delay = 0;
            m_FinishCall = null;
        }
        public long GetId()
        {
            return id;
        }
    }
    public class CounterManager
    {
        private static CounterManager m_Instance;
        public static CounterManager GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new CounterManager();
            }
            return m_Instance;
        }
        List<Counter> m_CounterList = new List<Counter>();
        private List<Counter> m_CounterUpdateList = new List<Counter>();
        public void Update()
        {
            int count = m_CounterList.Count;
            if (m_CounterUpdateList.Count > 0)
            {
                m_CounterUpdateList.Clear();
            }

            for (var i = 0; i < count; ++i)
            {
                m_CounterUpdateList.Add(m_CounterList[i]);
            }

            int dtmmseconds = (int)(UnityEngine.Time.deltaTime * 1000);
            for (int i = 0; i < count; ++i)
            {
                Counter counter = m_CounterUpdateList[i];
                if (counter.m_Delay > 0)
                {
                    counter.m_Delay -= dtmmseconds;
                    continue;
                }
                counter.m_IntervalPastTime += dtmmseconds;
                if (counter.m_IntervalPastTime >= counter.m_IntervalMMSeconds)
                {
                    counter.m_IntervalPastTime = 0;
                    if (counter.m_PerCall != null)
                    {
                        counter.m_PerCall();
                    }
                    counter.m_TotalCount--;

                    if (counter.m_Loop == false && counter.m_TotalCount <= 0)
                    {
                        m_CounterList.Remove(counter);
                        if (counter.m_FinishCall != null)
                        {
                            counter.m_FinishCall();
                        }
                    }


                }
            }
        }
        public long AddCounter(int intervalMMSec, int count, System.Action perCall, int delayMMSec = 0, System.Action finishCall = null)
        {
            Counter counter = Counter.CreateClass();
            counter.m_TotalCount = count;
            counter.m_Loop = count <= 0;
            counter.m_IntervalMMSeconds = intervalMMSec;
            counter.m_PerCall = perCall;
            counter.m_Delay = delayMMSec;
            counter.m_FinishCall = finishCall;
            m_CounterList.Add(counter);
            return counter.GetId();
        }
        public void RemoveCounter(long id)
        {
            int count = m_CounterList.Count;
            for (int i = 0; i < count; ++i)
            {
                var counter = m_CounterList[i];
                if (counter.GetId() == id)
                {
                    m_CounterList.RemoveAt(i);
                    return;
                }
            }
        }


        public void Destroy()
        {
            m_CounterList.Clear();
            UDebug.Log("CounterManager Destroy");
        }

    }
}
