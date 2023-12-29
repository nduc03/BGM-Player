using NAudio.Vorbis;

namespace bgmPlayer
{
    public class VorbisBGMLoopStream(string introPath, string loopPath) : VorbisLoopStream(loopPath)
    {
        private readonly VorbisWaveReader introStream = new(introPath);

        public override int Read(byte[] buffer, int offset, int count)
        {
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
