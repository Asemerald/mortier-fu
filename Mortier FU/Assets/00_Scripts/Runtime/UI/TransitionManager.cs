using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MortierFu
{
    public class TransitionManager : MonoBehaviour
    {
        private static readonly int MaskAmount = Shader.PropertyToID("_MaskAmount");
        [SerializeField] private Material material;

        private float maskAmount = 0f;
        private float targetValue = 1f;

        private void Start()
        {
            material.SetFloat(MaskAmount, 0f);
        }

        private void Update() {
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                targetValue = -.1f;
            }
            if (Keyboard.current.yKey.wasPressedThisFrame) {
                targetValue = 1f;
            }

            float maskAmountChange = targetValue > maskAmount ? +.1f : -.1f;
            maskAmount += maskAmountChange * Time.deltaTime * 6f; //TODO: expose speed
            maskAmount = Mathf.Clamp01(maskAmount);

            material.SetFloat(MaskAmount, maskAmount);
        }
    }
}