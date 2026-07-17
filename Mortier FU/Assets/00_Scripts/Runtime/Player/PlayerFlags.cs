using System;
using System.Collections.Generic;

namespace MortierFu
{
    [Flags]
    public enum EntityProperties
    {
        // 01
        mud = 1 << 0,

        // 010
        ice = 1 << 1,

    }

    public class PlayerFlags
    {
        private EntityProperties _properties;

        private readonly Dictionary<EntityProperties, int> _sources = new();

        public bool Has(EntityProperties property)
        {
            return (_properties & property) != 0;
        }

        public bool HasAndRemove(EntityProperties property)
        {
            if (Has(property))
            {
                Remove(property);
                return true;
            }
            return false;
        }

        public void Add(EntityProperties property)
        {
            if (_sources.TryGetValue(property, out int count))
            {
                _sources[property] = count + 1;
            }
            else
            {
                _sources[property] = 1;
                _properties |= property;
            }
        }

        public void Remove(EntityProperties property)
        {
            if (!_sources.TryGetValue(property, out int count))
                return;

            count--;

            if (count <= 0)
            {
                _sources.Remove(property);
                _properties &= ~property;
            }
            else
            {
                _sources[property] = count;
            }
        }

        public EntityProperties GetAll()
        {
            return _properties;
        }
    }
}
