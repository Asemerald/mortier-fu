using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

namespace MortierFu
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private VideoClip _blueTransitionClip;
        [SerializeField] private VideoClip _redTransitionClip;
        [SerializeField] private VideoClip _yellowTransitionClip;
        [SerializeField] private VideoClip _greenTransitionClip;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _isPlaying;

        public bool IsTransitionPlaying => _isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // Awaitable 
        public UniTask PlayTransitionAsync(TransitionColor color)
        {
            // Try to acquire immediately; if busy, ignore and return completed task.
            if (!_semaphore.Wait(0))
                return UniTask.CompletedTask;

            // We own the semaphore now; return the task so caller can await the play.
            return PlayTransitionNoAcquireAsync(color);
        }

        // Fire-and-forget 
        public bool TryPlayTransition(TransitionColor color)
        {
            if (!_semaphore.Wait(0))
                return false;

            // Start without awaiting the caller
            PlayTransitionNoAcquireAsync(color).Forget();
            return true;
        }

        // Assumes semaphore already acquired.
        private async UniTask PlayTransitionNoAcquireAsync(TransitionColor color)
        {
            _isPlaying = true;
            var tcs = new UniTaskCompletionSource<bool>();

            void OnEnd(VideoPlayer vp) => tcs.TrySetResult(true);

            try
            {
                var clip = GetClip(color);
                if (clip == null)
                {
                    tcs.TrySetResult(true);
                }
                else
                {
                    _videoPlayer.clip = clip;
                    _videoPlayer.loopPointReached += OnEnd;
                    _videoPlayer.Play();
                }

                await tcs.Task;
            }
            finally
            {
                _videoPlayer.loopPointReached -= OnEnd;
                _isPlaying = false;
                _semaphore.Release();
            }
        }

        private VideoClip GetClip(TransitionColor color)
        {
            return color switch
            {
                TransitionColor.Blue => _blueTransitionClip,
                TransitionColor.Red => _redTransitionClip,
                TransitionColor.Yellow => _yellowTransitionClip,
                TransitionColor.Green => _greenTransitionClip,
                _ => null
            };
        }
    }

    public enum TransitionColor
    {
        Blue,
        Red,
        Yellow,
        Green
    }
}
