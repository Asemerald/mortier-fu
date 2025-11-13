using System;
using UnityEngine;

namespace MortierFu {
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver {
        [SerializeField] string assemblyQualifiedName = string.Empty;
        
        public Type Type { get; private set; }
        
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            assemblyQualifiedName = Type?.AssemblyQualifiedName ?? assemblyQualifiedName;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return;
            }
                
            if (!TryGetType(assemblyQualifiedName, out var type)) {
                Debug.LogError($"Type {assemblyQualifiedName} not found");
                return;
            }
            Type = type;
        }

        static bool TryGetType(string typeString, out Type type) {
            type = Type.GetType(typeString);
            return type != null || !string.IsNullOrEmpty(typeString);
        }
        
        // Implicit conversion from SerializableType to Type
        public static implicit operator Type(SerializableType sType) => sType.Type;

        // Implicit conversion from Type to SerializableType
        public static implicit operator SerializableType(Type type) => new() { Type = type };
    }
}