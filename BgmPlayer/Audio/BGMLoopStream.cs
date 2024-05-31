using NAudio.Wave;
using NAudio.Extras;

namespace bgmPlayer
{
    public class BGMLoopStream(WaveStream introStream, WaveStream loopStream) : LoopStream(loopStream)
    {
        private readonly WaveStream loopStream = loopStream;

        public override int Read(byte[] buffer, int offset, int count)
        {
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

        protected override void Dispose(bool disposing)
        {
            loopStream.Dispose();
            base.Dispose(disposing);
        }
    }
}
