using System;
using System.Collections;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MortierFu
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }
        
        private static readonly int MaskAmount = Shader.PropertyToID("_MaskAmount");
        [SerializeField] private Material material;

        private float maskAmount = 0f;
        private float targetValue = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Logs.LogWarning("[TransitionManager]: Multiple instances detected. Destroying duplicate.", this);
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            material.SetFloat(MaskAmount, 0f);
        }

        private void Update() {
            float maskAmountChange = targetValue > maskAmount ? +.1f : -.1f;
            maskAmount += maskAmountChange * Time.deltaTime * 6f; //TODO: expose speed
            maskAmount = Mathf.Clamp01(maskAmount);

            material.SetFloat(MaskAmount, maskAmount);
        }
        
        public void FadeIn()
        {
            targetValue = 1f;
        }
        
        public void EndTransition()
        {
            targetValue = 0f;
        }
        
        public bool IsTransitionning()
        {
            return !Mathf.Approximately(maskAmount, targetValue);
        }
        
        
    }
}