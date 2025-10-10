using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace MortierFu {
    [Serializable]
    public class CharacterStat {
        public float BaseValue;

        public virtual float Value {
            get {
                if (isDirty || !Mathf.Approximately(BaseValue, lastBaseValue)) {
                    _value = CalculateFinalValue();
                    lastBaseValue = BaseValue;
                    isDirty = false;
                }
                
                return _value;
            }
        }
        
        protected float _value;
        protected bool isDirty = true;
        protected float lastBaseValue = float.MinValue;
        
        protected readonly List<StatModifier> statModifiers;
        public readonly ReadOnlyCollection<StatModifier> StatModifiers;

        readonly Comparison<StatModifier> _comparison;
        readonly Predicate<StatModifier> _predicate;
        object _sourceToRemove; 

        public CharacterStat() {
            statModifiers = new List<StatModifier>();
            StatModifiers = statModifiers.AsReadOnly();
            _comparison = CompareModifierOrder;
            _predicate = mod => mod.Source == _sourceToRemove;
        }
        
        public CharacterStat(float baseValue) : this() {
            BaseValue = baseValue;
        }

        public virtual void AddModifier(StatModifier mod) {
            isDirty = true;
            statModifiers.Add(mod);
            statModifiers.Sort(CompareModifierOrder);
        }
        
        public virtual bool RemoveModifier(StatModifier mod) {
            if (statModifiers.Remove(mod)) {
                isDirty = true;
                return true;                
            }

            return false;
        }

        public virtual bool RemoveAllModifiersFromSource(object source) {
            _sourceToRemove = source;
            int numRemovals = statModifiers.RemoveAll(_predicate);
            _sourceToRemove = null;
            
            if (numRemovals > 0) {
                isDirty = true;
                return true;
            }

            return false;
        }

        protected virtual float CalculateFinalValue() {
            float finalValue = BaseValue;
            float sumPercentAdd = 0f;
            
            statModifiers.Sort(_comparison);
            
            for (int i = 0; i < statModifiers.Count; i++) {
                var mod = statModifiers[i];

                switch (mod.Type) {
                    case StatModType.Flat:
                    {
                        finalValue += statModifiers[i].Value;
                        break;
                    }
                    case StatModType.PercentAdd:
                    {
                        sumPercentAdd += mod.Value;
                        
                        if(i + 1 >= statModifiers.Count || statModifiers[i + 1].Type != StatModType.PercentAdd) {
                            finalValue *= 1 + sumPercentAdd;
                            sumPercentAdd = 0;
                        }
                        break;
                    }
                    case StatModType.PercentMult:
                    {
                        finalValue *= 1 + mod.Value;
                        break;
                    }
                }
            }

            // n.0001f
            return (float)Math.Round(finalValue, 4);
        }
        
        protected virtual int CompareModifierOrder(StatModifier a, StatModifier b) {
            if (a.Order < b.Order)
                return -1;
            else if (a.Order > b.Order)
                return 1;
            return 0;
        }
    }
}