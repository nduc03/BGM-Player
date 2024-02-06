﻿#if ME
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
        // it also has auto backup by OneDrive
        public static readonly string FilePath = Path.Combine(SpecialDirectories.MyDocuments, AppConstants.OST_INFO_RELATIVE_PATH);
        private static JsonNode? data = GetContent();
        public static JsonNode? Data
        {
            get { return data; }
        }
        private static JsonNode? GetContent()
        {
            if (!File.Exists(FilePath)) return null;

            try
            {
                return JsonNode.Parse(File.ReadAllText(FilePath));
            }
            catch
            {
                return null;
            }
        }
        // Currently it only creates empty folder
        public static bool CreateFolder()
        {
            try
            {
                var folder = Path.GetDirectoryName(FilePath);
                if (folder != null)
                {
                    Directory.CreateDirectory(folder);
                    return true;
                }
                return false;
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
            File.WriteAllText(FilePath, data.ToJsonString());
        }
    }
}
#endif