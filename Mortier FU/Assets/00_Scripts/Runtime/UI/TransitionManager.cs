using System;
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
        
        [Header("Blue Transition")]
        [SerializeField] private VideoClip _blueStartClip;
        [SerializeField] private VideoClip _blueLoopClip;
        [SerializeField] private VideoClip _blueExitClip;
        
        [Header("Red Transition")]
        [SerializeField] private VideoClip _redStartClip;
        [SerializeField] private VideoClip _redLoopClip;
        [SerializeField] private VideoClip _redExitClip;
        
        [Header("Yellow Transition")]
        [SerializeField] private VideoClip _yellowStartClip;
        [SerializeField] private VideoClip _yellowLoopClip;
        [SerializeField] private VideoClip _yellowExitClip;
        
        [Header("Green Transition")]
        [SerializeField] private VideoClip _greenStartClip;
        [SerializeField] private VideoClip _greenLoopClip;
        [SerializeField] private VideoClip _greenExitClip;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _isPlaying;
        private bool _shouldExit;
        private TransitionColor _currentColor;
        private CancellationTokenSource _loopCts;

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

        private void OnDestroy()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
        }

        // Démarre la transition (Start → Loop)
        public UniTask StartTransitionAsync(TransitionColor color)
        {
            if (!_semaphore.Wait(0))
                return UniTask.CompletedTask;

            return StartTransitionNoAcquireAsync(color);
        }

        public bool TryStartTransition(TransitionColor color)
        {
            if (!_semaphore.Wait(0))
                return false;

            StartTransitionNoAcquireAsync(color).Forget();
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_TransitionIn);
            return true;
        }

        // Termine la transition (attend la fin du loop actuel → Exit)
        public void EndTransition()
        {
            if (_isPlaying)
            {
                _shouldExit = true;
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_TransitionOut);
            }
        }

        private async UniTask StartTransitionNoAcquireAsync(TransitionColor color)
        {
            _isPlaying = true;
            _shouldExit = false;
            _currentColor = color;
            _loopCts?.Cancel();
            _loopCts = new CancellationTokenSource();

            try
            {
                // 1. Joue le Start
                var startClip = GetStartClip(color);
                if (startClip != null)
                {
                    await PlayClipAsync(startClip);
                }

                // 2. Loop jusqu'à ce qu'on demande l'exit
                var loopClip = GetLoopClip(color);
                if (loopClip != null)
                {
                    await LoopClipAsync(loopClip, _loopCts.Token);
                }

                // 3. Joue l'Exit
                if (_shouldExit)
                {
                    var exitClip = GetExitClip(color);
                    if (exitClip != null)
                    {
                        await PlayClipAsync(exitClip);
                    }
                }
            }
            finally
            {
                _isPlaying = false;
                _shouldExit = false;
                _loopCts?.Dispose();
                _loopCts = null;
                _semaphore.Release();
            }
        }

        private async UniTask PlayClipAsync(VideoClip clip)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            void OnEnd(VideoPlayer vp) => tcs.TrySetResult(true);

            try
            {
                _videoPlayer.clip = clip;
                _videoPlayer.isLooping = false;
                _videoPlayer.loopPointReached += OnEnd;
                _videoPlayer.Play();
                await tcs.Task;
            }
            finally
            {
                _videoPlayer.loopPointReached -= OnEnd;
            }
        }

        private async UniTask LoopClipAsync(VideoClip clip, CancellationToken ct)
        {
            _videoPlayer.clip = clip;
            _videoPlayer.isLooping = false;
            _videoPlayer.Play();

            while (!_shouldExit && !ct.IsCancellationRequested)
            {
                var tcs = new UniTaskCompletionSource<bool>();
                void OnEnd(VideoPlayer vp) => tcs.TrySetResult(true);

                try
                {
                    _videoPlayer.loopPointReached += OnEnd;
                    await tcs.Task;
                }
                finally
                {
                    _videoPlayer.loopPointReached -= OnEnd;
                }

                if (!_shouldExit && !ct.IsCancellationRequested)
                {
                    _videoPlayer.Play();
                }
            }
        }

        private VideoClip GetStartClip(TransitionColor color)
        {
            return color switch
            {
                TransitionColor.Blue => _blueStartClip,
                TransitionColor.Red => _redStartClip,
                TransitionColor.Yellow => _yellowStartClip,
                TransitionColor.Green => _greenStartClip,
                _ => null
            };
        }

        private VideoClip GetLoopClip(TransitionColor color)
        {
            return color switch
            {
                TransitionColor.Blue => _blueLoopClip,
                TransitionColor.Red => _redLoopClip,
                TransitionColor.Yellow => _yellowLoopClip,
                TransitionColor.Green => _greenLoopClip,
                _ => null
            };
        }

        private VideoClip GetExitClip(TransitionColor color)
        {
            return color switch
            {
                TransitionColor.Blue => _blueExitClip,
                TransitionColor.Red => _redExitClip,
                TransitionColor.Yellow => _yellowExitClip,
                TransitionColor.Green => _greenExitClip,
                _ => null
            };
        }
        
        [SerializeField] private float maxLoopTime = 5f;
        
        private float _currentLoopTime = 0f;

        private void Update()
        {
            if (_isPlaying && !_shouldExit)
            {
                _currentLoopTime += Time.deltaTime;
                if (_currentLoopTime >= maxLoopTime)
                {
                    EndTransition();
                    _currentLoopTime = 0f;
                }
            }
            else
            {
                _currentLoopTime = 0f;
            }
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