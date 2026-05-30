namespace Project.Scripts.System.Audio
{
    public interface IAudioManager
    {
        void PlaySfx(ESoundId soundId);
        void PlayMusic();
        void StopMusic();
        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);
    }
}