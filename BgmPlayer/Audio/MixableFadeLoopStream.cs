using System;

using NAudio.Extras;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace bgmPlayer
{
    public sealed class MixableFadeLoopStream : ISampleProvider, IDisposable
    {
        private readonly FadeInOutSampleProvider stream;
        private readonly LoopStream disposableStream;
        private const int FADE_MAX_DURATION_MS = 10000;
        private bool isStopped = false;

        private readonly System.Timers.Timer timer = new() { AutoReset = false };

        public MixableFadeLoopStream(string streamPath)
        {
            disposableStream = new LoopStream(new AudioFileReader(streamPath));
            var audioStream = new MediaFoundationResampler(disposableStream, new WaveFormat(48000, 2)).ToSampleProvider();
            stream = new(audioStream, true);
        }

        public WaveFormat WaveFormat => stream.WaveFormat;

        public void FadeIn(int fadeMilisecond)
        {
            stream.BeginFadeIn(Math.Clamp(fadeMilisecond, 0, FADE_MAX_DURATION_MS));
        }

        public void Start(int fadeMilisecond = 1000)
        {
            disposableStream.Position = 0;
            FadeIn(fadeMilisecond);
        }

        public void FadeOut(int fadeMilisecond)
        {
            stream.BeginFadeOut(Math.Clamp(fadeMilisecond, 0, FADE_MAX_DURATION_MS));
        }

        public void Stop(int fadeMilisecond = 1000)
        {
            FadeOut(fadeMilisecond);
            if (fadeMilisecond > 0)
            {
                timer.Elapsed += (sender, e) => isStopped = true;
                timer.Interval = fadeMilisecond;
                timer.Start();
            }
            else isStopped = false;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (isStopped)
            {
                return 0;
            }

            return stream.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            disposableStream.Dispose();
        }
    }


}
