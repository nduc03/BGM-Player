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
        /// <returns>If correct pattern return BGM name. Return null when function cannot find the pattern</returns>
        public static string? GetBgmFileName(string path1, string path2)
        {
            string p1 = Path.GetFileNameWithoutExtension(path1);
            string p2 = Path.GetFileNameWithoutExtension(path2);
            if ((p1.EndsWith(AppConstants.INTRO_END) && p2.EndsWith(AppConstants.LOOP_END)) || (p2.EndsWith(AppConstants.INTRO_END) && p1.EndsWith(AppConstants.LOOP_END)))
            {
                if (p1[..p1.LastIndexOf(AppConstants.INTRO_END)] == p2[..p2.LastIndexOf(AppConstants.LOOP_END)])
                    return p1[..p1.LastIndexOf(AppConstants.INTRO_END)];
                return null;
            }
            return null;
        }

#if ME
        public static string GetArknightsOstName(string ParsedInGameFileName)
        {
            return ParsedInGameFileName switch
            {
                "m_bat_abyssalhunters" => "Under Tides",
                "m_bat_act12side_02"   => "Stop Breathing",
                "m_bat_act20side_01"   => "滑梯衝浪 (Slide and Surf)",
                "m_bat_ccs5"           => "Operation Spectrum Battle Theme",
                "m_bat_ccs8_b1"        => "Fading Sky",
                "m_bat_ccs9"           => "Surging Tide",
                "m_bat_ccs10"          => "Crawling Forward!",
                "m_bat_martyr"         => "殉道之人 (The Martyr)",
                "m_bat_dsdevr"         => "愚人曲 (Stultifer Cantus)",
                "m_avg_doubledragons"  => "雙龍 (Double Dragons)",
                "m_avg_towerfierce"    => "高塔冲突 (Tower Fierce)",
                "m_sys_act12side"      => "Dossoles Holiday",
                "m_sys_ccs8"           => "Operation Dawnseeker",
                "m_sys_ccs9"           => "Operation Deepness",
                "m_sys_ccs10"          => "Operation Ashring",
                "m_sys_fesready"       => "Ready?",
                "m_sys_tech"           => "Vigilo",
                _ => ParsedInGameFileName,
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
