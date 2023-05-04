using NAudio.Vorbis;
using NAudio.Wave;

namespace bgmPlayer
{
    // Inspired by NAudio.Extras.LoopStream but modified for VorbisWave
    public class VorbisLoopStream : VorbisWaveReader
    {
        public VorbisLoopStream(string path) : base(path)
        {
            EnableLooping = true;
        }

        public bool EnableLooping { get; set; }

        public override WaveFormat WaveFormat
        {
            get { return base.WaveFormat; }
        }

        public override long Length
        {
            get { return base.Length; }
        }

        public override long Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = base.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    if (base.Position == 0 || !EnableLooping)
                    {
                        break;
                    }
                    base.Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}
