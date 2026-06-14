using UnityEngine;

namespace Project.Scripts.System.Save
{
    public class ProgressSaveService
    {
        private const string SaveKey = "merge_td_progress_checkpoint_v1";

        public bool TryLoad(out ProgressSaveData data)
        {
            data = null;

            if (!PlayerPrefs.HasKey(SaveKey))
                return false;

            var json = PlayerPrefs.GetString(SaveKey);
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
            PlayerPrefs.Save();
        }
    }
}