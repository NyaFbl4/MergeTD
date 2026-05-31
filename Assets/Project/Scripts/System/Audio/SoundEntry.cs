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

        public virtual ESoundId SoundId => _soundId;
        public virtual AudioClip Clip => _clip;
        public virtual float Volume => _volume;
    }
}
