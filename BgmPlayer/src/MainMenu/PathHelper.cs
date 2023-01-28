﻿using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;

namespace bgmPlayer
{
    public static class PathHelper
    {
        private static bool autoUpdateGUI = false;

        private static readonly OpenFileDialog IntroPath = new();
        private static readonly OpenFileDialog LoopPath = new();
        private static TextBlock? introField;
        private static TextBlock? loopField;


        public delegate void PathState();
        public static event PathState? AllEmpty;

        public static bool AutoFill = false;
        public static string Intro
        {
            get
            {
                var ip = IntroPath.FileName;
                if (ip != string.Empty && !File.Exists(ip)) throw new FileNotFoundException("Cannot find the intro file!");
                return IntroPath.FileName;
            }
            set
            {
                if (value == Intro) return;
                IntroPath.FileName = (File.Exists(value) || value == string.Empty) ? value : Intro;
                if (autoUpdateGUI) UpdateGUI();
                if (AutoFill) TryAutoSetLoop();
                if (value == string.Empty) CheckAllEmpty();
                PreferencesHelper.SavePreferences(IntroPath: Intro);
            }
        }
        public static string Loop
        {
            get
            {
                var lp = LoopPath.FileName;
                if (lp != string.Empty && !File.Exists(lp)) throw new FileNotFoundException("Cannot find the loop file!");
                return LoopPath.FileName;
            }
            set 
            {
                if (value == Loop) return;
                LoopPath.FileName = (File.Exists(value) || value == string.Empty) ? value : Loop;
                if (autoUpdateGUI) UpdateGUI();
                if (AutoFill) TryAutoSetIntro();
                if (value != string.Empty) CheckAllEmpty();
                PreferencesHelper.SavePreferences(LoopPath: Loop);
            }
        }
        public static void Init(Preferences? preferences = null, bool autoFill = false)
        {
            IntroPath.Filter = "Wave sound|*.wav|MP3 (not recommended)|*.mp3";
            LoopPath.Filter = "Wave sound|*.wav|MP3 (not recommended)|*.mp3";
            AutoFill = autoFill;

            if (preferences != null)
            {
                if (File.Exists(preferences.IntroPath))
                {
                    IntroPath.FileName = preferences.IntroPath;
                }
                if (File.Exists(preferences.LoopPath))
                {
                    LoopPath.FileName = preferences.LoopPath;
                }
            }
            CheckAllEmpty();
        }

        public static void TurnOnAutoUpdateGUI(TextBlock IntroField, TextBlock LoopField)
        {
            autoUpdateGUI= true;
            introField = IntroField;
            loopField = LoopField;
            IntroPath.FileOk += OnFileOkEvent;
            LoopPath.FileOk += OnFileOkEvent;
            UpdateGUI();
        }

        public static void TurnOffAutoUpdateGUI() 
        {
            autoUpdateGUI = false;
            introField = null;
            loopField = null;
            IntroPath.FileOk -= OnFileOkEvent;
            LoopPath.FileOk -= OnFileOkEvent;
        }

        public static void UpdateGUI(TextBlock IntroField, TextBlock LoopField)
        {
            IntroField.Text = Intro;
            LoopField.Text = Loop;
        }

        public static string? OpenIntroPathDialog()
        {
            if (IntroPath.ShowDialog() == true)
            {
                Intro = Intro;
                return Intro;
            }
            else return null;
        }

        public static string? OpenLoopPathDialog()
        {
            if (LoopPath.ShowDialog() == true)
            {
                Loop = Loop;
                return Loop;
            }
            else return null;
        }

        public static void TryAutoSetIntro()
        {
            if (AutoFill == false) return;
            string loopPath = Loop;

            if (!Path.GetFileNameWithoutExtension(loopPath).EndsWith("_loop")) return;

            string expectedIntroPath = loopPath[..loopPath.LastIndexOf('_')] + AppConstants.INTRO_END + Path.GetExtension(loopPath);

            if (Intro == expectedIntroPath) return;

            if (File.Exists(expectedIntroPath))
            {
                Intro = expectedIntroPath;
            }
        }

        public static void TryAutoSetLoop()
        {
            if (AutoFill == false) return;
            string introPath = Intro;

            if (!Path.GetFileNameWithoutExtension(introPath).EndsWith("_intro")) return;

            string expectedLoopPath = introPath[..introPath.LastIndexOf('_')] + AppConstants.LOOP_END + Path.GetExtension(introPath);

            if (Loop == expectedLoopPath) return;

            if (File.Exists(expectedLoopPath))
            {
                Loop = expectedLoopPath;
            }
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
