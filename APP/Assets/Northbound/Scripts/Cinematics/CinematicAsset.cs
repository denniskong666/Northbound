using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;

namespace Northbound.Cinematics
{
    [System.Serializable]
    public sealed class CinematicSubtitleCue
    {
        public float startSeconds;
        [TextArea] public string text;
    }

    [CreateAssetMenu(menuName = "Northbound/Cinematic", fileName = "Cinematic")]
    public sealed class CinematicAsset : ScriptableObject
    {
        public string id;
        public VideoClip clip;
        public string completionFact;
        [TextArea] public string subtitle;
        public CinematicSubtitleCue[] subtitleCues = System.Array.Empty<CinematicSubtitleCue>();
        public AudioMixerSnapshot cinematicAudioSnapshot;
        public AudioMixerSnapshot gameplayAudioSnapshot;
    }
}
