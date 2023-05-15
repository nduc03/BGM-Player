#if ME
namespace bgmPlayer
{
    public readonly struct OstInfo
    {
        public readonly string Title;
        public readonly string? Composer;

        public OstInfo(string title, string? artist = null)
        {
            Title = title;
            Composer = artist;
        }
    }
}
#endif