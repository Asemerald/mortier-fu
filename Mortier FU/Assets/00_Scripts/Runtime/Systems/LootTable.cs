using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public class LootTable<T> : IDisposable
    {
        protected readonly List<LootTableEntry> lootBag;
        protected readonly LootTableConfig config;

        public virtual bool IsEmpty => lootBag.Count <= 0;

        public virtual float TotalWeight
        {
            get
            {
                if (lootBag == null || IsEmpty) return 0f;

                float totalWeight = 0f;
                foreach (var entry in lootBag)
                {
                    totalWeight += entry.Weight;
                }

                return totalWeight;
            }
        }

        public LootTable(LootTableConfig config = null)
        {
            // Create  an empty loot bag
            lootBag = new List<LootTableEntry>();
            this.config = config ?? new LootTableConfig();
        }

        public virtual void PopulateLootBag(List<LootTableEntry> entries)
        {
            lootBag.Clear();

            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        public virtual T Pull()
        {
            if (IsEmpty) return default;

            float randomWeight = Random.Range(0, TotalWeight);
            float currentWeight = 0f;
            foreach (var entry in lootBag)
            {
                currentWeight += entry.Weight;
                if (randomWeight <= currentWeight)
                {
                    return entry.Item;
                }
            }

            return default;
        }

        public virtual T[] BatchPull(int amount)
        {
            T[] results = new T[amount];
            for (int i = 0; i < amount; i++)
            {
                results[i] = Pull();
            }

            return results;
        }

        public virtual void AddEntry(LootTableEntry entry)
        {
            if (!config.AllowDuplicates && lootBag.Contains(entry)) return;
            lootBag.Add(entry);
        }

        public virtual bool RemoveEntry(LootTableEntry entry) => lootBag.Remove(entry);

        public void Dispose()
        {
            lootBag.Clear();
        }

        [Serializable]
        public struct LootTableEntry : IEquatable<LootTableEntry>
        {
            public T Item;
            public float Weight;

            public bool Equals(LootTableEntry other)
            {
                return EqualityComparer<T>.Default.Equals(Item, other.Item) && Weight.Equals(other.Weight);
            }

            public override bool Equals(object obj)
            {
                return obj is LootTableEntry other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Item, Weight);
            }
        }
    }
}