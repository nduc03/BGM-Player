using System;
using System.Diagnostics;

namespace bgmPlayer
{
    public class TimeCount
    {
        #region Singleton declaration
        private TimeCount() { }
        private static TimeCount? instance = null;
        public static TimeCount Instance
        {
            get
            {
                if (instance == null) { instance = new TimeCount(); }
                return instance;
            }
        }
        #endregion
        private readonly Stopwatch stopWatch = new();

        public void Start()
        {
            stopWatch.Start();
        }

        public string GetTimer()
        {
            TimeSpan time = stopWatch.Elapsed;
            if (time.Hours == 0)
                return string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
            return string.Format("{0:00}:{1:00}:{2:00}", time.Hours, time.Minutes, time.Seconds);
        }

        public void Stop()
        {
            stopWatch.Stop();
        }

        public void Reset()
        {
            stopWatch.Reset();
        }
    }
}
