using NAudio.Wave;
using NAudio.Extras;

namespace bgmPlayer
{
    public class BGMLoopStream : LoopStream
    {
        private readonly WaveStream? startStream;

        public BGMLoopStream(WaveStream startStream, WaveStream loopStream) : base(loopStream)
        {
            this.startStream = startStream;
        }

        public BGMLoopStream(WaveStream loopStream) : base(loopStream) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (startStream == null)
                return base.Read(buffer, offset, count);

            int startRead = startStream.Read(buffer, offset, count);
            int loopRead = 0;
            if (startRead < count)
            {
                // startRead < count when the startStream near the end of stream.
                // when near the end, block the code until startStream finish read
                // then call loopStream read forever
                while (startStream.Position < startStream.Length)
                {
                    // do nothing
                    // just block loopStream read until startStream is fully read
                    // and avoid overlap between the end of startStream and the start of loopStream
                }
                // loopStream started loop here and loop until stop or close the app.
                loopRead = base.Read(buffer, 0, count);
            }
            return startRead + loopRead;
        }
    }
}
