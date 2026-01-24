using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;
namespace MortierFu
{
    public class PlayerDeathNotification : MonoBehaviour {
        [SerializeField] private CanvasGroup[] _killerExclusive;
        [SerializeField] private Image _killerImg;
        [SerializeField] private Image _victimImg;
        [SerializeField] private Image _signImg;
        [SerializeField] private Sprite _bombshellKillSign;
        [SerializeField] private Sprite _fellKillSign;
        [SerializeField] private Sprite _carCrashKillSign;
        [SerializeField] private Sprite[] _avatarSprites;
        [SerializeField] private AnimationClip _enterClip;
        [SerializeField] private AnimationClip _exitClip;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _duration = 3f;

        private DeathFeedUI _deathFeed;
        
        public void Initialize(DeathFeedUI deathFeed, PlayerCharacter victimCharacter, DeathContext deathContext)
        {
            _deathFeed = deathFeed;
            
            if (victimCharacter == null)
            {
                Logs.LogWarning("Received a null character while initializing Player Death Notification !");
                return;
            }
            
            int victimId = victimCharacter.Owner.PlayerIndex;
            if (victimId < 0 || victimId >= _avatarSprites.Length)
            {
                Logs.LogWarning("Invalid player index while creating a player death notification in the death feed ui.");
                return;
            }
            
            _victimImg.sprite = _avatarSprites[victimId];
            _signImg.sprite = deathContext.DeathCause switch {
                E_DeathCause.Fall => _fellKillSign,
                E_DeathCause.VehicleCrash => _carCrashKillSign,
                _ => _bombshellKillSign
            };
            
            if (deathContext.Killer != null) {
                int killerId = deathContext.Killer.Owner.PlayerIndex;
                _killerImg.sprite = _avatarSprites[killerId];
                transform.localPosition = Vector3.zero;
            }
            else
            {
                transform.localPosition = Vector3.right * -150;
            }

            foreach (var canvasGroup in _killerExclusive) {
                canvasGroup.alpha = deathContext.Killer == null ? 0f : 1f;
            }
            
            ReportWhenFinished().Forget();
        }

        private async UniTaskVoid ReportWhenFinished()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_enterClip.length + _duration));
            
            _animator.Play(Animator.StringToHash("A_KillFeed_Exit"));

            await UniTask.Delay(TimeSpan.FromSeconds(_exitClip.length));
            
            _deathFeed.OnNotificationFinished(transform);
        } 
    }
}
