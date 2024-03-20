using System;
using System.Diagnostics;

namespace bgmPlayer
{
    public class PlaybackTimer
    {
        public static PlaybackTimer Instance { get => lazyInstance.Value; }

        private readonly Stopwatch stopWatch;
        #region Singleton declaration
        private static readonly Lazy<PlaybackTimer> lazyInstance = new(() => new PlaybackTimer());
        private PlaybackTimer()
        {
            stopWatch = new Stopwatch();
        }
        #endregion

        public void Start()
        {
            stopWatch.Start();
        }

        public string GetParsedElapsed()
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
