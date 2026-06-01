namespace Project.Scripts.System.Audio
{
    public interface IAudioManager
    {
        bool IsMusicEnabled { get; }
        bool IsSoundEnabled { get; }
        
        void PlaySound(ESoundId soundId);
        void PlayMusic();
        void StopMusic();
        void SetMusicEnabled(bool enabled);
        void SetSoundEnabled(bool enabled);
    }
}