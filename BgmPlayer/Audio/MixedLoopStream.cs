using System;
using System.Collections.Generic;

using NAudio.Wave;

namespace bgmPlayer
{
    public class MixedLoopStream(WaveStream firstSection) : WaveStream
    {
        private readonly List<WaveStream> streams = [firstSection];
        private int playingSection = 0;
        private int loopSection = -1;

        private int PlayingSection
        {
            get => Math.Clamp(playingSection, 0, streams.Count);
            set
            {
                playingSection = Math.Clamp(value, 0, streams.Count);
            }
        }
        private WaveStream CurrentStream { get => streams[PlayingSection]; }

        public void AddSection(WaveStream streamSection)
        {
            streams.Add(streamSection);
        }

        public void NextStreamResetPosition()
        {
            CurrentStream.Position = 0;
            NextStream();
        }

        public void NextStream()
        {
            if (playingSection < streams.Count - 1)
            {
                PlayingSection++;
            }
            else
            {
                PlayingSection = 0;
            }
        }

        public int TotalSection { get => streams.Count; }

        public bool AutoNextSection { get; set; } = false;

        /// <summary>
        /// Getter returns the section that will be looped. 
        /// If AutoNextSection is false, this will alsways return the current section.
        /// <para>Setter: sets the section that will be looped (0-based index). Set to -1 to disable loop at any section.</para>
        /// </summary>
        public int LoopSection
        {
            get
            {
                if (!AutoNextSection) return playingSection;
                return loopSection;
            }
            set
            {
                loopSection = Math.Clamp(value, -1, streams.Count);
            }
        }

        public override long Length { get { return long.MaxValue / 32; } }

        public override long Position
        {
            get => CurrentStream.Position;
            set
            {
                if (CurrentStream != null)
                    CurrentStream.Position = value;
            }
        }

        public override WaveFormat WaveFormat => CurrentStream.WaveFormat;

        public override bool HasData(int count)
        {
            return true;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int required = count - read;
                int readThisTime = CurrentStream.Read(buffer, offset + read, required);
                if (readThisTime < required)
                {
                    CurrentStream.Position = 0;
                    if (LoopSection != PlayingSection) NextStream();
                }

                if (CurrentStream.Position >= CurrentStream.Length)
                {
                    CurrentStream.Position = 0;
                    if (LoopSection != PlayingSection) NextStream();
                }
                read += readThisTime;
            }
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var stream in streams)
            {
                stream.Dispose();
            }
            base.Dispose(disposing);
        }

        public static MixedLoopStream CreateBGMLoopStream(WaveStream introStream, WaveStream loopStream)
        {
            var stream = new MixedLoopStream(introStream);
            stream.AddSection(loopStream);
            return stream;
        }
    }
}
