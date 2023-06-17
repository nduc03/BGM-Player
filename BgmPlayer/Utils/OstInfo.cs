#if ME
namespace bgmPlayer
{
    public readonly struct OstInfo
    {
        public readonly string  Title;
        public readonly string? TranslatedTitle;
        public readonly string? Artist;
        public readonly string? EventName;

        public OstInfo(string title, string translatedTitle, string? artist = null, string? eventName = null)
        {
            Title = title;
            TranslatedTitle = translatedTitle;
            Artist = artist;
            EventName = eventName;
        }
        public OstInfo(string title, string? artist = null, string? eventName = null)
        {
            Title = title;
            Artist = artist;
            EventName = eventName;
        }

        public string GetParsedTitle(int? TitleOption = null)
        {
            return TitleOption switch
            {
                2 => Title,
                3 => TranslatedTitle ?? Title,
                _ => TranslatedTitle == null ? Title : Title + $" ({TranslatedTitle})"
            };
        }

        public string GetWindowTitle(int? TitleOption = null)
        {
            string result = GetParsedTitle(TitleOption);
            
            if (EventName != null && EventName != string.Empty)
            {
                result += $" [{EventName}]";
            }
            if (Artist != null && Artist != string.Empty)
            {
                result += $" - {Artist}";
            }
            return result;
        }
    }
}
#endif