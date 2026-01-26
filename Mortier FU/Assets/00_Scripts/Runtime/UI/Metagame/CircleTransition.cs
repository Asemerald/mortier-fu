using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MortierFu.Shared;

namespace MortierFu
{
    public class CircleOpen : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _duration = 1f;

        private Material _material;
        
        public static CircleOpen Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Logs.LogError("[CircleOpen] Multiple instances detected! Destroying duplicate.", this);
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            
            _material = _image.material;
            _material.SetFloat("_Progress", 0);
            
        }

        private void Start()
        {
            OpenAsync().Forget();
        }

        private async UniTask OpenAsync()
        {
            _image.gameObject.SetActive(true);

            _material.DOFloat(1f, "_Progress", _duration)
                .SetEase(Ease.InOutQuad);

            await UniTask.Delay(TimeSpan.FromSeconds(_duration));
        
            _image.gameObject.SetActive(false);

        }
    }
}