using System.IO;
using System.Text.Json;
using NAudio.Wave;
using NAudio.Extras;

namespace WpfApp1
{
    public class BGMLoopStream : LoopStream
    {
        WaveStream startStream;
        public BGMLoopStream(WaveStream startStream, WaveStream loopStream) : base(loopStream)
        {
            this.startStream = startStream;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (startStream.Position < startStream.Length)
                return startStream.Read(buffer, offset, count);
            else
                return base.Read(buffer, offset, count);
        }
    }
    public class PathData
    {
        public string? StartPath { get; set; }
        public string? LoopPath { get; set; }
    }

    public static class ConfigManager
    {
        public static PathData? LoadPath()
        {
            if (!File.Exists(AppConstants.CONFIG_LOCATION)) return null;
            else
            {
                string fileContent = File.ReadAllText(AppConstants.CONFIG_LOCATION);
                return JsonSerializer.Deserialize<PathData>(fileContent);
            }
        }

        public static void SavePath(string? startPath, string? loopPath)
        {
            PathData? data = LoadPath();
            if (data == null)
            {
                Directory.CreateDirectory(AppConstants.DATA_LOCATION);
                File.Create(AppConstants.CONFIG_LOCATION).Close();
                data = new PathData();
            }
            if (startPath != null) data.StartPath = startPath;
            if (loopPath != null) data.LoopPath = loopPath;
            File.WriteAllText(AppConstants.CONFIG_LOCATION, JsonSerializer.Serialize(data));
        }
    }


}
