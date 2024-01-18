#if ME
using Microsoft.VisualBasic.FileIO;
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
        // the list file is saved at user's documents folder instead of app folder
        // so the list will be kept even when the app is deleted
        private static readonly string ostInfoPath = Path.Combine(SpecialDirectories.MyDocuments, AppConstants.OST_INFO_RELATIVE_PATH);
        private static JsonNode? data = GetContent();
        public static JsonNode? Data
        {
            get { return data; }
        }
        private static JsonNode? GetContent()
        {
            if (!File.Exists(ostInfoPath)) return null;

            try
            {
                return JsonNode.Parse(File.ReadAllText(ostInfoPath));
            }
            catch
            {
                return null;
            }
        }
        // Currently it only creates empty folder
        public static bool Init()
        {
            try
            {
                var folder = Path.GetDirectoryName(ostInfoPath);
                if (folder != null) Directory.CreateDirectory(folder);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ReloadContent()
        {
            data = GetContent();
        }

        public static void AddOrReplaceContent(
            string InGameName, 
            string Title, 
            string? TranslatedTitle = null, 
            string? Artist = null, 
            string? EventName = null
            )
        {
            data ??= GetContent() ?? new JsonObject();

            var node = new JsonObject
            {
                ["Title"] = Title,
            };
            if (TranslatedTitle != null) node["TranslatedTitle"] = TranslatedTitle;
            if (Artist != null) node["Artist"] = Artist;
            if (EventName != null) node["EventName"] = EventName;

            data[InGameName] = node;
            File.WriteAllText(ostInfoPath, data.ToJsonString());
        }
    }
}
#endif