using System;
using System.IO;
using System.Security.Cryptography;

namespace bgmPlayer
{
    public static class Utils
    {
        /// <summary>
        /// Get BGM name of intro and loop file.
        /// Only work correctly with correct pattern.
        /// Pattern: file_name_intro, file_name_loop
        /// </summary>
        /// <param name="path1">Full absolute path to intro or loop file</param>
        /// <param name="path2">Full absolute path to intro or loop file</param>
        /// <param name="invalidReturn">Set the return value when function cannot find pattern</param>
        /// <returns>If correct pattern return BGM name, else return <paramref name="invalidReturn"/></returns>
        public static string? GetBgmFileName(string? path1, string? path2, string? invalidReturn = null)
        {
            if (path1 == null && path2 == null) return invalidReturn;
            if (path1 == null) return Path.GetFileNameWithoutExtension(path2) ?? invalidReturn;
            if (path2 == null) return Path.GetFileNameWithoutExtension(path1) ?? invalidReturn;
            string p1 = Path.GetFileNameWithoutExtension(path1);
            string p2 = Path.GetFileNameWithoutExtension(path2);
            if ((p1.EndsWith(AppConstants.INTRO_END) && p2.EndsWith(AppConstants.LOOP_END)) || (p2.EndsWith(AppConstants.INTRO_END) && p1.EndsWith(AppConstants.LOOP_END)))
            {
                if (p1[..p1.LastIndexOf(AppConstants.INTRO_END)] == p2[..p2.LastIndexOf(AppConstants.LOOP_END)])
                    return p1[..p1.LastIndexOf(AppConstants.INTRO_END)];
                return invalidReturn;
            }
            return invalidReturn;
        }

#if ME
        public static OstInfo? GetArknightsOstName(string ParsedInGameFileName)
        {
            return ParsedInGameFileName switch
            {
                "demo" => new OstInfo("Demo"),

                "m_bat_abyssalhunters" => new OstInfo("Under Tides"),
                "m_bat_act12side_02"   => new OstInfo("Stop Breathing"),
                "m_bat_act20side_01"   => new OstInfo("滑梯衝浪 (Slide and Surf)"),
                "m_bat_ccs5"           => new OstInfo("Operation Spectrum Battle Theme"),
                "m_bat_ccs8_b1"        => new OstInfo("Fading Sky"),
                "m_bat_ccs9"           => new OstInfo("Surging Tide"),
                "m_bat_ccs10"          => new OstInfo("Crawling Forward!"),
                "m_bat_dsdevr"         => new OstInfo("愚人曲 (Stultifer Cantus)"),
                "m_bat_martyr"         => new OstInfo("殉道之人 (The Martyr)"),
                "m_bat_rglk2boss1"     => new OstInfo("大群之殤 (The Fall of We Many)", "Gareth Coker"),
                "m_bat_rglk2boss2"     => new OstInfo("旅者//征服者 (Voyager//Subjugator)", "Gareth Coker"),

                "m_avg_doubledragons"  => new OstInfo("雙龍 (Double Dragons)"),
                "m_avg_towerfierce"    => new OstInfo("高塔冲突 (Tower Fierce)"),

                "m_sys_act12side"      => new OstInfo("Dossoles Holiday"),
                "m_sys_ccs8"           => new OstInfo("Operation Dawnseeker"),
                "m_sys_ccs9"           => new OstInfo("Operation Deepness"),
                "m_sys_ccs10"          => new OstInfo("Operation Ashring"),
                "m_sys_fesready"       => new OstInfo("Ready?"),
                "m_sys_tech"           => new OstInfo("Vigilo"),
                _ => null,
            };
        }
#endif

        public static bool IsValidCache()
        {
            if (!File.Exists(AppConstants.THUMBNAIL_CACHE_LOCATION)) return false;
            var md5 = MD5.Create();
            var expectedHash = AppConstants.THUMBNAIL_HASH;
#if ME
            expectedHash = AppConstants.THUMBNAIL_ME_HASH;
#endif
            using var stream = File.OpenRead(AppConstants.THUMBNAIL_CACHE_LOCATION);
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "") == expectedHash;
        }
    }
}
