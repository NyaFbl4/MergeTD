using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.System.Audio
{
    public class AudioManager : IInitializable, IAudioManager
    {
        private readonly SoundLibrary _soundLibrary;

        private readonly Dictionary<ESoundId, SoundEntry> _soundMap = new();

        private GameObject _root;
        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private bool _musicEnabled = true;
        private bool _sfxEnabled = true;

        public AudioManager(SoundLibrary soundLibrary)
        {
            _soundLibrary = soundLibrary;
        }

        public void Initialize()
        {
            BuildMap();
            CreateAudioSources();
            PlayMusic();
        }

        private void BuildMap()
        {
            _soundMap.Clear();

            if (_soundLibrary == null || _soundLibrary.Sounds == null)
                return;

            for (var i = 0; i < _soundLibrary.Sounds.Count; i++)
            {
                var entry = _soundLibrary.Sounds[i];
                if (entry == null)
                    continue;

                _soundMap[entry.SoundId] = entry;
            }
        }

        private void CreateAudioSources()
        {
            _root = new GameObject("[AudioManager]");
            Object.DontDestroyOnLoad(_root);

            _musicSource = _root.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            _sfxSource = _root.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        public void PlaySfx(ESoundId soundId)
        {
            if (!_sfxEnabled)
                return;

            if (!_soundMap.TryGetValue(soundId, out var entry))
                return;

            if (entry.Clip == null)
                return;

            _sfxSource.PlayOneShot(entry.Clip, entry.Volume);
        }

        public void PlayMusic()
        {
            if (!_musicEnabled)
                return;

            if (_soundLibrary == null || _soundLibrary.BackgroundMusic == null)
                return;

            if (_musicSource.clip == _soundLibrary.BackgroundMusic && _musicSource.isPlaying)
                return;

            _musicSource.clip = _soundLibrary.BackgroundMusic;
            _musicSource.volume = 1f;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null)
                _musicSource.Stop();
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;

            if (_musicEnabled)
                PlayMusic();
            else
                StopMusic();
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
        }
    }
}