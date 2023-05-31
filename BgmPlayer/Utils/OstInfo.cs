#if ME
namespace bgmPlayer
{
    public readonly struct OstInfo
    {
        public readonly string Title;
        public readonly string? Artist;
        public readonly string? EventName;

        public OstInfo(string title, string? artist = null, string? eventName = null)
        {
            Title = title;
            Artist = artist;
            EventName = eventName;
        }

        public string GetWindowTitle()
        {
            string result = Title;
            if (EventName != null || EventName != string.Empty)
            {
                result += $" [{EventName}]";
            }
            if (Artist != null || Artist != string.Empty)
            {
                result += $" - {Artist}";
            }
            return result;
        }
    }
}
#endif