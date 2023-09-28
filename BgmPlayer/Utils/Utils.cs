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
        /// <summary>
        /// All the unofficial translation from arknights.fandom.com.
        /// All the official name from Hypergryph's public OST or Arknights global version by Yostar.
        /// </summary>
        /// <param name="ParsedInGameFileName"></param>
        /// <returns></returns>
        public static OstInfo? GetArknightsOstInfo(string? ParsedInGameFileName)
        {
            return ParsedInGameFileName switch
            {
                "m_bat_abyssalhunters"   => new OstInfo("Under Tides", "Steven Grove", "Under Tides"),
                "m_bat_act12side_02"     => new OstInfo("Stop Breathing", "BaoUner", "Dossoles Holiday"),
                "m_bat_act16side_01"     => new OstInfo("潛在危機", "Latent Threat", "Erik Castro", "Guide Ahead"),
                "m_bat_act19side_01"     => new OstInfo("因變量", "Dependent Variables", "Gareth Coker", "Dorothy's Vision"),
                "m_bat_act20side_01"     => new OstInfo("滑梯衝浪", "Slide and Surf", "PMP Music", "Ideal City"),
                "m_bat_act22side_01"     => new OstInfo("火與灰", "Cinders and Ashes", "PMP Music", "What the Firelight Casts"),
                "m_bat_act23side_01"     => new OstInfo("度關山", "Crossing the Mountain Passes", "Adam Gubman", "Where Vernal Winds Will Never Blow"),
                "m_bat_act25side_01"     => new OstInfo("The Coming of the Future", "Steven Grove", "Lone Trail"),
                "m_bat_act26side"        => new OstInfo("裁決日", "Judgement Day", "Erik Castro", "Hortus de Escapismo"),
                "m_bat_ccs5"             => new OstInfo("Operation Spectrum Battle Theme", "Cybermiso, DOT96, & Tigerlily", "Contingency Contract Spectrum (CC#5)"),
                "m_bat_bbrain"           => new OstInfo("夢境蘇醒", "Awaken From Dreamland", "Gareth Coker", "Dorothy's Vision"),
                "m_bat_ccs8_b1"          => new OstInfo("Fading Sky", "AJURIKA", "Contingency Contract Dawnseeker (CC#8)"),
                "m_bat_ccs9"             => new OstInfo("湧潮急奏", "Surging Tide", "BaoUner", "Contingency Contract Deepness (CC#9)"),
                "m_bat_ccs10"            => new OstInfo("Crawling Forward!", "LJCH", "Contingency Contract Ashring (CC#10)"),
                "m_bat_cledub"           => new OstInfo("無罪之人", "The Sinless", "Erik Castro", "Hortus de Escapismo"),
                "m_bat_cstlrs"           => new OstInfo("Control's Wishes", "Steven Grove", "Lone Trail"),
                "m_bat_dsdevr"           => new OstInfo("愚人曲", "Stultifer Cantus", "Steven Grove", "Stultifera Navis"),
                "m_bat_manfri_02"        => new OstInfo("提卡茲之根", "Teekazwurtzen", "BaoUner", "Episode 10: Shatterpoint"),
                "m_bat_martyr"           => new OstInfo("殉道之人", "The Martyr", "Erik Castro", "Guide Ahead"),
                "m_bat_ncrmcr"           => new OstInfo("深池之影", "Shadow of Dublinn", "PMP Music", "What the Firelight Casts"),
                "m_bat_rglk2boss1"       => new OstInfo("Sorrow of We Many", "Gareth Coker", "Mizuki & Caerula Arbor"),
                "m_bat_rglk2boss2"       => new OstInfo("Traveler//Conqueror", "Gareth Coker", "Mizuki & Caerula Arbor"),
                "m_bat_stmkgt_01_loop"   => new OstInfo("Wecgas fore tham Cynge, Searu fore tham Ethle (First half)", "LJCH", "Episode 11: Return to Mist"),
                "m_bat_stmkgt_02"        => new OstInfo("Wecgas fore tham Cynge, Searu fore tham Ethle (Second half)", "LJCH", "Episode 11: Return to Mist"),
                                         
                "m_avg_doubledragons"    => new OstInfo("雙龍", "Double Dragons", "Sterling Maffe", "Episode 08: Roaring Flare"),
                "m_avg_towerfierce"      => new OstInfo("高塔冲突", "Tower Fierce", "Go Shiina", "Episode 08: Roaring Flare"),
                                         
                "m_sys_act12side"        => new OstInfo("Dossoles Holiday", "David Westbom", "Dossoles Holiday"),
                "m_sys_act16side"        => new OstInfo("聖城日常", "Holy City Daily", "Erik Castro", "Guide Ahead"),
                "m_sys_act19side"        => new OstInfo("旅程", "Journey", "Gareth Coker", "Dorothy's Vision"),
                "m_sys_act21side"        => new OstInfo("Il Siracusano", "Adam Gubman", "Il Siracusano"),
                "m_sys_act22side"        => new OstInfo("湮佚詩歌", "Lost Poetry", "PMP Music and Sando Friedrich", "What the Firelight Casts"),
                "m_sys_act25side"        => new OstInfo("Ad astra", "Steven Grove", "Lone Trail"),
                "m_sys_act26side"        => new OstInfo("不存在的乐园", "The False Paradise", "Erik Castro", "Hortus de Escapismo"),
                "m_sys_ccs8"             => new OstInfo("Operation Dawnseeker", "Terry Zhong", "Contingency Contract Dawnseeker (CC#8)"),
                "m_sys_ccs9"             => new OstInfo("Operation Deepness", "Vas Angelov & Bailey Jehl", "Contingency Contract Deepness (CC#9)"),
                "m_sys_ccs10"            => new OstInfo("Operation Ashring", "Erik Castro & X. ARI", "Contingency Contract Ashring (CC#10)"),
                "m_sys_fesready"         => new OstInfo("Ready?", "D.D.D", "Heart of Surging Flame"),
                "m_sys_rglk2theme2_loop" => new OstInfo("Chorus of the Many", "Steven Grove", "Mizuki & Caerula Arbor"),
                "m_sys_tech"             => new OstInfo("Vigilo", eventName:"Vigilo"),

                "stmkgt_mix"             => new OstInfo("Wecgas fore tham Cynge, Searu fore tham Ethle", "LJCH", "Episode 11: Return to Mist"),
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
