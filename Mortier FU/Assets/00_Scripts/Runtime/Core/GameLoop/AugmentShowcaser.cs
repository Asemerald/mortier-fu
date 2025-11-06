using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class AugmentShowcaser : MonoBehaviour
    {
        // TODO mettre en C# envoyer en constructor les prefabs etc... ce dont j'ai besoin
        // TODO relier avec augment selection system, dispose, etc...
        public static AugmentShowcaser Instance { get; private set; } // TODO dégager ça

        [SerializeField] private Transform _container;
        [SerializeField] private AugmentCardUI _cardPrefab;

        private readonly List<AugmentCardUI> _spawnedCards = new();

        private void Awake() => Instance = this;

        public void Showcase(List<DA_Augment> augments)
        {
            Clear();
            foreach (var augment in augments)
            {
                var card = Instantiate(_cardPrefab, _container);
                card.Setup(augment);
                _spawnedCards.Add(card);
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Clear();
        }

        private void Clear()
        {
            foreach (var c in _spawnedCards)
                Destroy(c.gameObject);
            _spawnedCards.Clear();
        }
    }
}