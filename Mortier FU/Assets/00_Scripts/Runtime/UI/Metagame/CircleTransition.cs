using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using PrimeTween;
using MortierFu.Shared;

namespace MortierFu
{
    public class CircleTransition : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private Material _material;

        public static CircleTransition Instance { get; private set; }

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

        public async UniTask OpenAsync(float duration)
        {
            _image.gameObject.SetActive(true);
            
            _material.SetFloat("_Progress", 0);
            
            await Tween.MaterialProperty(_material, Shader.PropertyToID("_Progress"), 1f, duration, Ease.InOutQuad);
            
        }

        public async UniTask CloseAsync(float duration)
        {
            _image.gameObject.SetActive(true);
            
            _material.SetFloat("_Progress", 1);
            
            await Tween.MaterialProperty(_material, Shader.PropertyToID("_Progress"), 0f, duration, Ease.InOutQuad);
        }
    }
}