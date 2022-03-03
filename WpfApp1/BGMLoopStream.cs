using NAudio.Wave;
using NAudio.Extras;

namespace bgmPlayer
{
    public class BGMLoopStream : LoopStream
    {
        private readonly WaveStream? introStream;

        public BGMLoopStream(WaveStream introStream, WaveStream loopStream) : base(loopStream)
        {
            this.introStream = introStream;
        }

        public BGMLoopStream(WaveStream loopStream) : base(loopStream) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (introStream == null)
                return base.Read(buffer, offset, count);

            int introRead = introStream.Read(buffer, offset, count);
            int loopRead = 0;
            if (introRead < count)
            {
                // Problem: Some music appears overlapping issue between intro and loop 
                loopRead = base.Read(buffer, 0, count);
            }
            return introRead + loopRead;
        }
    }
}
