using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace bgmPlayer
{
    /// <summary>
    /// Get BGM name of intro and loop file.
    /// Only work correctly with correct pattern.
    /// Pattern: file_name_intro, file_name_loop
    /// </summary>
    /// <param name="path1">Full absolute path to intro or loop file</param>
    /// <param name="path2">Full absolute path to intro or loop file</param>
    /// <returns>If correct pattern return BGM name. Return null when function cannot find the pattern</returns>
    public static class Utils
    {
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
