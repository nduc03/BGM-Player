using NAudio.Wave;
using NAudio.Extras;

namespace bgmPlayer
{
    public class BGMLoopStream : LoopStream
    {
        private readonly WaveStream? introStream;
        private readonly WaveStream loopStream;

        public BGMLoopStream(WaveStream introStream, WaveStream loopStream) : base(loopStream)
        {
            this.introStream = introStream;
            this.loopStream = loopStream;
        }

        public BGMLoopStream(WaveStream loopStream) : base(loopStream)
        {
            this.loopStream = loopStream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (introStream == null)
                return base.Read(buffer, offset, count);
            if (introStream.Position < introStream.Length)
            {
                int introRead = introStream.Read(buffer, offset, count);
                if (introRead < count)
                    return introRead + loopStream.Read(buffer, offset + introRead, count - introRead);
                return introRead;
            }
            else
                return base.Read(buffer, offset, count);
        }
    }
}
