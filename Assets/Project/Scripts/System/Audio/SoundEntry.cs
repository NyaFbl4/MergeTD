using System;
using UnityEngine;

namespace Project.Scripts.System.Audio
{
    [Serializable]
    public class SoundEntry
    {
        [SerializeField] private ESoundId _soundId;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _volume = 1f;

        public ESoundId SoundId => _soundId;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
    }
}