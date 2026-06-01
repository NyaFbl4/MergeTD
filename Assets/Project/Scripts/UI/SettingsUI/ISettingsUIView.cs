using System;
using Project.Scripts.Systems.UI;

namespace Project.Scripts.UI.SettingsUI
{
    public interface ISettingsUIView : ILayoutView
    {
        event Action CloseButtonClicked;
        event Action SoundButtonClicked;
        event Action MusicButtonClicked;
        event Action RuButtonClicked;
        event Action EnButtonClicked;
        
        void SetTitle(string title);
        void SetMusicLabel(string label);
        void SetSoundLabel(string label);
        void SetLanguageLabel(string label);
        void SetMusicEnabled(bool enabled);
        void SetSoundEnabled(bool enabled);
    }
}