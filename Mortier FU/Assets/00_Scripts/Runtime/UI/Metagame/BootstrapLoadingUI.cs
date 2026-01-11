using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public class BootstrapLoadingUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private GameInitializer _gameInitializer;

        [SerializeField] private Slider _progressBar;
        [SerializeField] private RectTransform _runnerSprite;
        [SerializeField] private float _runnerOffsetY = 75f;

        [Header("Optional")] [SerializeField] private Animator _runnerAnimator;

        private RectTransform _fillArea;
        private Vector2 _targetRunnerPos;

        private void Awake()
        {
            if (_gameInitializer == null)
                Debug.LogWarning("BootstrapPanel: GameInitializer reference is missing");

            if (_progressBar == null || _runnerSprite == null)
                Debug.LogWarning("BootstrapPanel: ProgressBar or RunnerSprite missing");

            _fillArea = _progressBar.fillRect.parent as RectTransform;

            StartProgressLoop().Forget();
        }

        private void Update()
        {
            UpdateRunnerPosition(_progressBar.normalizedValue);
        }

        private void UpdateRunnerPosition(float progress)
        {
            if (_fillArea == null) return;

            Vector3 start = _fillArea.rect.min;
            Vector3 end = _fillArea.rect.max;

            float x = Mathf.Lerp(start.x, end.x, progress);
            float y = _runnerOffsetY;

            _runnerSprite.anchoredPosition = new Vector2(x, y);
        }

        private async UniTaskVoid StartProgressLoop()
        {
            while (_gameInitializer != null)
            {
                float progress = _gameInitializer.GetInitializationProgress();
                _progressBar.value = progress;

                if (_runnerAnimator != null)
                    _runnerAnimator.speed = Mathf.Lerp(0.5f, 2f, progress);

                await UniTask.Yield();
            }

            _progressBar.value = 1f;
        }
    }
}