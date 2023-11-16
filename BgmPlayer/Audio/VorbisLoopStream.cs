using NAudio.Vorbis;

namespace bgmPlayer
{
    // Inspired by NAudio.Extras.LoopStream but modified for VorbisWave
    // Modified for always enable looping and removed some unnecessary field
    public class VorbisLoopStream : VorbisWaveReader
    {
        public VorbisLoopStream(string path) : base(path)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = base.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    base.Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}
