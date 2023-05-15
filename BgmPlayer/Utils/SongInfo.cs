#if ME
namespace bgmPlayer
{
    public readonly struct OstInfo
    {
        public readonly string Title;
        public readonly string? Artist;

        public OstInfo(string title, string? artist = null)
        {
            Title = title;
            Artist = artist;
        }
    }
}
#endif