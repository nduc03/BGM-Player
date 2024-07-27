using System;
using System.Collections.Generic;

using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace bgmPlayer
{
    public class PhasedBgmStream : ISampleProvider, IDisposable
    {
        private readonly List<MixableFadeLoopStream> streams;
        private readonly MixingSampleProvider bgmStreams;

        private readonly WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        private readonly System.Timers.Timer timer = new() { AutoReset = false };

        private int currentStreamIndex = 0;

        public WaveFormat WaveFormat => waveFormat;

        public PhasedBgmStream(params string[] audioPaths)
        {
            streams = [];
            bgmStreams = new(waveFormat);
            foreach (var path in audioPaths)
            {
                var bgmStream = new MixableFadeLoopStream(path);
                if (bgmStream.WaveFormat.SampleRate != waveFormat.SampleRate ||
                    bgmStream.WaveFormat.Channels != waveFormat.Channels)
                {
                    throw new InvalidOperationException(
                        $"Audio with path {path} has an incompatible wave format " +
                        $"(sample rate: {waveFormat.SampleRate} channels: {waveFormat.Channels}. " +
                        $"Required wave format: sample rate = 48000Hz, channels = 2");
                }
                streams.Add(bgmStream);
            }
            bgmStreams.AddMixerInput(streams[currentStreamIndex]);
            streams[currentStreamIndex].Start(0);
        }

        public void NextCrossFade(int fadeMilisecond = 1000)
        {
            int prevStreamIndex = currentStreamIndex;
            streams[prevStreamIndex].Stop(fadeMilisecond);
            timer.Elapsed += (sender, e) => bgmStreams.RemoveMixerInput(streams[prevStreamIndex]);
            timer.Interval = fadeMilisecond;
            timer.Start();

            currentStreamIndex = (currentStreamIndex < streams.Count) ? (currentStreamIndex + 1) : 0;
            bgmStreams.AddMixerInput(streams[currentStreamIndex]);
            streams[currentStreamIndex].Start(fadeMilisecond);
        }

        public void Next()
        {
            streams[currentStreamIndex].Stop(0);
            bgmStreams.RemoveMixerInput(streams[currentStreamIndex]);

            currentStreamIndex = (currentStreamIndex < streams.Count) ? (currentStreamIndex + 1) : 0;
            bgmStreams.AddMixerInput(streams[currentStreamIndex]);
            streams[currentStreamIndex].Start(0);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return bgmStreams.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }
    }
}
