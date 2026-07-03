using UnityEngine;

namespace Helpers.Runtime.Shading
{
    public static class ShaderHelper
    {
        private static readonly MaterialPropertyBlock _propertyBlock = new();

        public static void SetColorProperty(this Renderer renderer, string propertyName, Color color)
        {
            int id = Shader.PropertyToID(propertyName);
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(id, color);
            renderer.SetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
        }
        
        public static void SetColorProperty(this Renderer renderer, int propertyId, Color color)
        {
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(propertyId, color);
            renderer.SetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
        }

        public static void SetFloatProperty(this Renderer renderer, string propertyName, float value)
        {
            int id = Shader.PropertyToID(propertyName);
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(id, value);
            renderer.SetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
        }

        public static void SetFloatProperty(this Renderer renderer, int propertyId, float value)
        {
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(propertyId, value);
            renderer.SetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
        }
    }
}