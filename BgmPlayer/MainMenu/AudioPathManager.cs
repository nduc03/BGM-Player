using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace bgmPlayer
{
    public static partial class AudioPathManager
    {
        // TODO: need refactor
        private static readonly OpenFileDialog IntroPath = new();
        private static readonly OpenFileDialog LoopPath = new();
        private static TextBlock? introField;
        private static TextBlock? loopField;

        // private static Note ExtRegex().IsMatch(string) = string.Endswith(a list of allowed extensions);
        [GeneratedRegex(AppConstants.VALID_PATH_REGEX)]
        private static partial Regex ExtRegex();

        public delegate void PathState();
        public static event PathState? AllEmpty;

        public static bool AutoFill = false;

        public static string Intro
        {
            get
            {
                var ip = IntroPath.FileName;
#if DEBUG
                if (ip != string.Empty && !File.Exists(ip)) throw new FileNotFoundException("Cannot find the intro file!");
                if (ip != string.Empty && !ExtRegex().IsMatch(ip)) throw new Exception("Wrong file format");
#endif
                return ip;
            }
            set
            {
                if (value != string.Empty && !ExtRegex().IsMatch(value)) return;
                if (File.Exists(value) || value == string.Empty) IntroPath.FileName = value; else return;
                UpdateGUI();
                if (AutoFill) TryAutoSetLoop();
                if (value == string.Empty) CheckAllEmpty();
                PersistedStateManager.SaveState(IntroPath: Intro);
            }
        }
        public static string Loop
        {
            get
            {
                var lp = LoopPath.FileName;
#if DEBUG
                if (lp != string.Empty && !File.Exists(lp)) throw new FileNotFoundException("Cannot find the loop file!");
                if (lp != string.Empty && !ExtRegex().IsMatch(lp)) throw new Exception("Wrong file format");
#endif
                return lp;
            }
            set
            {
                if (value != string.Empty && !ExtRegex().IsMatch(value)) return;
                LoopPath.FileName = (File.Exists(value) || value == string.Empty) ? value : Loop;
                UpdateGUI();
                if (AutoFill) TryAutoSetIntro();
                if (value != string.Empty) CheckAllEmpty();
                PersistedStateManager.SaveState(LoopPath: Loop);
            }
        }
        public static void Init(PersistedState state)
        {
            IntroPath.Filter = AppConstants.FILTER;
            LoopPath.Filter = AppConstants.FILTER;
            AutoFill = state.AutoFill ?? false;

            if (File.Exists(state.IntroPath))
            {
                IntroPath.FileName = state.IntroPath;
            }
            if (File.Exists(state.LoopPath))
            {
                LoopPath.FileName = state.LoopPath;
            }

            CheckAllEmpty();
        }

        public static void InitTextBlock(TextBlock IntroField, TextBlock LoopField)
        {
            introField = IntroField;
            loopField = LoopField;
            IntroPath.FileOk += OnFileOkEvent;
            LoopPath.FileOk += OnFileOkEvent;
            UpdateGUI();
        }

        public static string? OpenIntroPathDialog()
        {
            if (IntroPath.ShowDialog() == true)
            {
                Intro = IntroPath.FileName;
                return Intro;
            }
            else return null;
        }

        public static string? OpenLoopPathDialog()
        {
            if (LoopPath.ShowDialog() == true)
            {
                Loop = LoopPath.FileName;
                return Loop;
            }
            else return null;
        }

        private static void TryAutoSetIntro()
        {
            if (AutoFill == false) return;

            string? expectedIntroPath = GetOtherPattern(Loop, AppConstants.LOOP_END, AppConstants.INTRO_END);

            if (expectedIntroPath == null) return;

            if (Intro == expectedIntroPath) return;

            if (File.Exists(expectedIntroPath))
            {
                Intro = expectedIntroPath;
            }
        }

        private static void TryAutoSetLoop()
        {
            if (AutoFill == false) return;

            string? expectedLoopPath = GetOtherPattern(Intro, AppConstants.INTRO_END, AppConstants.LOOP_END);

            if (expectedLoopPath == null) return;

            if (Loop == expectedLoopPath) return;

            if (File.Exists(expectedLoopPath))
            {
                Loop = expectedLoopPath;
            }
        }

        private static string? GetOtherPattern(string path, string currentEndPattern, string expectedEndPattern)
        {
            if (!Path.GetFileNameWithoutExtension(path).EndsWith(currentEndPattern)) return null;
            return path[..path.LastIndexOf(currentEndPattern[0])] + expectedEndPattern + Path.GetExtension(path);
        }

        private static void CheckAllEmpty()
        {
            if (Intro == string.Empty && Loop == string.Empty) AllEmpty?.Invoke();
        }

        private static void UpdateGUI()
        {
            if (introField != null) introField.Text = Intro;
            if (loopField != null) loopField.Text = Loop;
        }

        private static void OnFileOkEvent(object? o, System.ComponentModel.CancelEventArgs e)
        {
            UpdateGUI();
        }
    }
}
