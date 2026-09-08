using System;
using UnityEngine;
using VContainer.Unity;
using YG;

namespace Project.Scripts.System.Save
{
    public sealed class WorldSaveService : IInitializable, ITickable, IDisposable
    {
        private const string SaveKey = "merge_td_world";
        private const float CloudSaveDelaySeconds = 2f;
        private const float CloudSaveMaxDelaySeconds = 10f;

        private bool _cloudSavePending;
        private float _cloudSaveAt;
        private float _cloudSaveDeadline;

        public bool TryLoad(out WorldSaveData data)
        {
            if (TryDeserialize(YG2.saves.worldJson, out data))
            {
                SaveLocal(YG2.saves.worldJson);
                return true;
            }

            if (PlayerPrefs.HasKey(SaveKey))
            {
                var localJson = PlayerPrefs.GetString(SaveKey);
                if (TryDeserialize(localJson, out data))
                {
                    YG2.saves.worldJson = localJson;
                    QueueCloudSave();
                    return true;
                }
            }

            data = null;
            return false;
        }

        public void Save(WorldSaveData data)
        {
            var json = JsonUtility.ToJson(data);
            SaveLocal(json);
            YG2.saves.worldJson = json;
            QueueCloudSave();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            YG2.saves.worldJson = string.Empty;
            QueueCloudSave();
        }

        public void Initialize()
        {
            Application.focusChanged += OnFocusChanged;
            Application.quitting += OnApplicationQuitting;
        }

        public void Tick()
        {
            if (_cloudSavePending && Time.unscaledTime >= _cloudSaveAt)
                FlushCloud();
        }

        public void Dispose()
        {
            FlushCloud();
            Application.focusChanged -= OnFocusChanged;
            Application.quitting -= OnApplicationQuitting;
        }

        private void QueueCloudSave()
        {
            var now = Time.unscaledTime;

            if (!_cloudSavePending)
            {
                _cloudSavePending = true;
                _cloudSaveDeadline = now + CloudSaveMaxDelaySeconds;
            }

            _cloudSaveAt = Math.Min(now + CloudSaveDelaySeconds, _cloudSaveDeadline);
        }

        private void FlushCloud()
        {
            if (!_cloudSavePending || !YG2.isSDKEnabled)
                return;

            YG2.SaveProgress();
            _cloudSavePending = false;
        }

        private static bool TryDeserialize(string json, out WorldSaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                data = JsonUtility.FromJson<WorldSaveData>(json);
                return data != null;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        private static void SaveLocal(string json)
        {
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                FlushCloud();
        }

        private void OnApplicationQuitting() => FlushCloud();
    }
}
