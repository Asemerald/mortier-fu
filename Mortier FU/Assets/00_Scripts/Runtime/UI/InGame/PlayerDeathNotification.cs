using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;
namespace MortierFu
{
    public class PlayerDeathNotification : MonoBehaviour
    {
        [SerializeField] Image _avatarImage;
        [SerializeField] private Sprite[] _avatarSprites;
        [SerializeField] private AnimationClip _clip;

        private DeathFeedUI _deathFeed;
        
        public void Initialize(DeathFeedUI deathFeed, PlayerCharacter character)
        {
            _deathFeed = deathFeed;
            
            if (character == null)
            {
                Logs.LogWarning("Received a null character while initializing Player Death Notification !");
                return;
            }
            
            int playerIndex = character.Owner.PlayerIndex;
            if (playerIndex < 0 || playerIndex >= _avatarSprites.Length)
            {
                Logs.LogWarning("Invalid player index while creating a player death notification in the death feed ui.");
                return;
            }

            _avatarImage.sprite = _avatarSprites[playerIndex];
            
            ReportWhenFinished().Forget();
        }

        private async UniTaskVoid ReportWhenFinished()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_clip.length));
            _deathFeed.OnNotificationFinished(transform);
        } 
    }
}
