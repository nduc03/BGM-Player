#if ME
using System.IO;
using System.Text.Json.Nodes;

namespace bgmPlayer
{
    // Json Format Sample
    // {
    //    "m_bat_act27side_1": {
    //      "Title": "Effervescence",
    //      "Artist": "Kirara Magic",
    //      "EventName": "So Long, Adele"
    //    }
    // }
    public static class OstList
    {
        public static readonly JsonNode? Data = GetContent(AppConstants.OST_INFO_PATH);
        private static JsonNode? GetContent(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonNode.Parse(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif