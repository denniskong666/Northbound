using Northbound.UI;

namespace Northbound.Cinematics
{
    public interface ICinematicPresentation
    {
        void Show(CinematicAsset asset, SettingsModel settings);
        void SetPlaybackTime(CinematicAsset asset, float elapsedSeconds, SettingsModel settings);
        void Hide();
        void RestoreGameplayAudio(CinematicAsset asset);
        void RestoreCamera();
    }
}
