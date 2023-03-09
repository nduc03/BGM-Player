using NAudio.Vorbis;

namespace bgmPlayer
{
    public class VorbisBGMLoopStream : VorbisLoopStream
    {
        private readonly VorbisWaveReader? introStream;

        public VorbisBGMLoopStream(string introPath, string loopPath) : base(loopPath)
        {
            introStream = new VorbisWaveReader(introPath);
        }

        public VorbisBGMLoopStream(string loopPath) : base(loopPath)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (introStream == null)
                return base.Read(buffer, offset, count);
            if (introStream.Position < introStream.Length)
            {
                int introRead = introStream.Read(buffer, offset, count);
                if (introRead < count)
                    return introRead + base.Read(buffer, offset + introRead, count - introRead);
                return introRead;
            }
            else
                return base.Read(buffer, offset, count);
        }
    }
}
