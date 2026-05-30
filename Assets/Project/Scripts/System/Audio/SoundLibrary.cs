using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.System.Audio
{
    [CreateAssetMenu(menuName = "Project/Audio/Sound Library", fileName = "Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip _backgroundMusic;
        [SerializeField] private List<SoundEntry> _sounds;

        public AudioClip BackgroundMusic => _backgroundMusic;
        public IReadOnlyList<SoundEntry> Sounds => _sounds;
    }
}