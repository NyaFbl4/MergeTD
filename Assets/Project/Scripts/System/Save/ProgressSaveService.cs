using UnityEngine;
using Project.Scripts.Gameplay.Run;

namespace Project.Scripts.System.Save
{
    public class ProgressSaveService
    {
        private const string LegacySaveKey = "merge_td_progress_checkpoint_v1";
        private const string PlayerRatedKey = "player_rated_game";

        private readonly IRunSelectionService _runSelection;

        private string SaveKey => $"{LegacySaveKey}_{_runSelection.SelectedRun.Id}";

        public ProgressSaveService(IRunSelectionService runSelection)
        {
            _runSelection = runSelection;
        }

        public bool TryLoad(out ProgressSaveData data)
        {
            data = null;

            var saveKey = ResolveExistingSaveKey();
            if (saveKey == null)
                return false;

            var json = PlayerPrefs.GetString(saveKey);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                data = JsonUtility.FromJson<ProgressSaveData>(json);
                return data != null;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        public void Save(ProgressSaveData data)
        {
            if (data == null)
                return;

            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            DeleteLegacySaveForFirstRun();
            PlayerPrefs.DeleteKey(PlayerRatedKey);
            PlayerPrefs.Save();
        }

        public void ClearCheckpoint()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            DeleteLegacySaveForFirstRun();
            PlayerPrefs.Save();
        }

        private string ResolveExistingSaveKey()
        {
            if (PlayerPrefs.HasKey(SaveKey))
                return SaveKey;

            return _runSelection.SelectedIndex == 0 && PlayerPrefs.HasKey(LegacySaveKey)
                ? LegacySaveKey
                : null;
        }

        private void DeleteLegacySaveForFirstRun()
        {
            if (_runSelection.SelectedIndex == 0)
                PlayerPrefs.DeleteKey(LegacySaveKey);
        }
    }
}
