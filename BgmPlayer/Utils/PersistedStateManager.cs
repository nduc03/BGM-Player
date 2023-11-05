using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace bgmPlayer
{
    public static class PersistedStateManager
    {
        private static PersistedState? dataCache = null;

        /// <summary>
        /// Used to get the <c>PersistedState</c> or just to cached <c>PersistedState</c> to RAM when initialize app
        /// </summary>
        /// <returns><c>PersistedState</c> object or <c>null</c> when saved state file does not exist</returns>
        public static PersistedState? LoadState()
        {
            if (dataCache != null) return dataCache;

            PersistedState? data;
            if (!File.Exists(AppConstants.SAVED_STATE_LOCATION)) return null;

            string fileContent = File.ReadAllText(AppConstants.SAVED_STATE_LOCATION);
            try
            {
                data = JsonSerializer.Deserialize<PersistedState>(fileContent);
            }
            catch
            {
                Debug.WriteLine("Json error");
                data = null;
            }
            dataCache = data;
            return data;
        }

        public static void SaveState(
            string? IntroPath = null,
            string? LoopPath = null,
            float? Volume = null,
            bool? AutoFill = null,
            int? TitleOption = null
        )
        {
            PersistedState data = dataCache ?? LoadState() ?? new PersistedState();

            if (IntroPath != null) data.IntroPath = IntroPath;
            if (LoopPath != null) data.LoopPath = LoopPath;
            if (Volume != null) data.Volume = Volume;
            if (AutoFill != null) data.AutoFill = AutoFill;
            if (TitleOption != null) data.TitleOption = TitleOption;

            dataCache = data;
            FileHelper.ApplyState(data);
        }

        /// <summary>
        /// Experimental.
        /// Save the app state to disk.
        /// <para>If <c>updateMode</c> is set to <c>UpdateMode.Update</c>, only save non-null property in <c>state</c></para>
        /// </summary>
        public static void SaveState(PersistedState state, UpdateMode updateMode = UpdateMode.Update)
        {
            // TODO: Make this function more convenient to use then delete the other SaveState above
            if (updateMode == UpdateMode.Update)
            {
                PersistedState data = dataCache ?? LoadState() ?? new PersistedState();
                var hasUpdate = false;
                foreach (var stateProp in typeof(PersistedState).GetProperties())
                {
                    hasUpdate = true;
                    var stateVal = stateProp.GetValue(state);
                    if (stateVal != null) stateProp.SetValue(data, stateVal);
                }
                if (hasUpdate)
                {
                    dataCache = data;
                    FileHelper.ApplyState(data);
                }
            }
            else
            {
                dataCache = state;
                FileHelper.ApplyState(state);
            }
        }
    }
}
