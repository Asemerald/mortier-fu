using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class RaceStartUI : MonoBehaviour
    {
        [SerializeField] private RaceStartDissolveConfig dissolveInConfig;
        [SerializeField] private RaceStartDissolveConfig dissolveOutConfig;
        
        private static readonly int DissolveOneHash = Shader.PropertyToID("_Dissolve_1");
        private static readonly int DissolveSecondHash = Shader.PropertyToID("_Dissolve_2");

        [SerializeField] private Image raceImage;
        [SerializeField] private Material raceMaterial;
        private Material _materialRaceInstance;

        private CancellationTokenSource _ctsRace;

        private struct RaceStartUiData
        {
            public readonly Vector2 dissolveCurrentOne;
            public readonly Vector2 dissolveTargetOne;
            public readonly Vector2 dissolveCurrentSecond;
            public readonly Vector2 dissolveTargetSecond;
            
            public readonly float durationLerp;

            public RaceStartUiData(float dissolveCurrentOneX,
                float dissolveCurrentOneY,
                float dissolveTargetOneX,
                float dissolveTargetOneY,
                float dissolveCurrentSecondX,
                float dissolveCurrentSecondY,
                float dissolveTargetSecondX,
                float dissolveTargetSecondY,
                float durationLerp)
            {
                dissolveCurrentOne = new Vector2(dissolveCurrentOneX, dissolveCurrentOneY);
                dissolveTargetOne =  new Vector2(dissolveTargetOneX, dissolveTargetOneY);
                dissolveCurrentSecond = new Vector2(dissolveCurrentSecondX, dissolveCurrentSecondY);
                dissolveTargetSecond =  new Vector2(dissolveTargetSecondX, dissolveTargetSecondY);
            
                this.durationLerp = durationLerp;
            }
        }
        
        public void TriggerDissolveIn()
        {
            ApplyDissolveConfig(dissolveInConfig);
        }

        public void TriggerDissolveOut()
        {
            ApplyDissolveConfig(dissolveOutConfig);
        }
        
        private void ApplyDissolveConfig(RaceStartDissolveConfig config)
        {
            SetDissolve(
                config.dissolveCurrentOne.x,
                config.dissolveCurrentOne.y,
                config.dissolveTargetOne.x,
                config.dissolveTargetOne.y,
                config.dissolveCurrentSecond.x,
                config.dissolveCurrentSecond.y,
                config.dissolveTargetSecond.x,
                config.dissolveTargetSecond.y,
                config.durationLerp);
        }
        
        public void SetDissolve(
            float dissolveCurrentOneX,
            float dissolveCurrentOneY,
            float dissolveTargetOneX,
            float dissolveTargetOneY,
            float dissolveCurrentSecondX,
            float dissolveCurrentSecondY,
            float dissolveTargetSecondX,
            float dissolveTargetSecondY,
            float durationLerp)
        {
            if (!_materialRaceInstance) SetupMaterialRace();

            RaceStartUiData data = new RaceStartUiData(
                dissolveCurrentOneX,
                dissolveCurrentOneY,
                dissolveTargetOneX,
                dissolveTargetOneY,
                dissolveCurrentSecondX,
                dissolveCurrentSecondY,
                dissolveTargetSecondX,
                dissolveTargetSecondY,
                durationLerp);
            
            _materialRaceInstance.SetVector(DissolveOneHash, data.dissolveCurrentOne);
            _materialRaceInstance.SetVector(DissolveSecondHash, data.dissolveCurrentSecond);

            _ctsRace?.Cancel();
            _ctsRace?.Dispose();
            _ctsRace = new CancellationTokenSource();
            
            SetDissolveAsync(data, _ctsRace).Forget();
        }

        private async UniTask SetDissolveAsync(RaceStartUiData data, CancellationTokenSource cts)
        {
            try
            {
                float elapsedTime = 0f;

                while (elapsedTime < data.durationLerp)
                {
                    elapsedTime += Time.deltaTime;

                    float t = elapsedTime / data.durationLerp;

                    Vector2 dissolveValueOne = Vector2.Lerp(data.dissolveCurrentOne, data.dissolveTargetOne, t);
                    Vector2 dissolveValueSecond = Vector2.Lerp(data.dissolveCurrentSecond, data.dissolveTargetSecond, t);

                    _materialRaceInstance.SetVector(DissolveOneHash, dissolveValueOne);
                    _materialRaceInstance.SetVector(DissolveSecondHash, dissolveValueSecond);

                    await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                }

            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _materialRaceInstance.SetVector(DissolveOneHash, data.dissolveTargetOne);
                _materialRaceInstance.SetVector(DissolveSecondHash, data.dissolveTargetSecond);
            }
        }

        private void SetupMaterialRace()
        {
            _materialRaceInstance = new Material(raceMaterial);
            raceImage.material = _materialRaceInstance;
        }

        private void OnDestroy()
        {
            _materialRaceInstance = null;
            _ctsRace?.Cancel();
            _ctsRace?.Dispose();
        }
    }
}
