using System;
using UnityEngine.Video;

namespace Northbound.Cinematics
{
    public interface IVideoPlayback
    {
        event Action Prepared;
        event Action Finished;
        event Action<string> Failed;
        void Prepare(VideoClip clip);
        void Play();
        void Stop();
    }

    public sealed class VideoPlayerPlayback : IVideoPlayback
    {
        private readonly VideoPlayer player;

        public event Action Prepared;
        public event Action Finished;
        public event Action<string> Failed;

        public VideoPlayerPlayback(VideoPlayer value)
        {
            player = value ?? throw new ArgumentNullException(nameof(value));
            player.prepareCompleted += _ => Prepared?.Invoke();
            player.loopPointReached += _ => Finished?.Invoke();
            player.errorReceived += (_, message) => Failed?.Invoke(message);
        }

        public void Prepare(VideoClip clip)
        {
            player.Stop();
            player.clip = clip;
            player.Prepare();
        }

        public void Play() => player.Play();

        public void Stop() => player.Stop();
    }
}
