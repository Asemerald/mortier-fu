using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using MortierFu.Shared;

namespace MortierFu.Analytics
{
    public partial class AnalyticsSystem
    {
        private EventBinding<TriggerAugmentsShown> _triggerAugmentsShownBinding;
        private Dictionary<int, AnalyticsAugmentEntry> _augmentStats;
        private AsyncOperationHandle<SO_AugmentLibrary> _augmentLibraryHandle;

        private async UniTask InitializeAugmentTracking()
        {
            _augmentLibraryHandle = SystemManager.Config.AugmentLibrary.LoadAssetAsync();
            SO_AugmentLibrary library = await _augmentLibraryHandle.Task;

            _augmentStats = new Dictionary<int, AnalyticsAugmentEntry>();

            foreach (var augment in library.Augments)
            {
                if (augment == null) continue;

                _augmentStats[augment.ID] = new AnalyticsAugmentEntry
                {
                    augmentId = augment.ID,
                    augmentName = augment.Name,
                    timesShown = 0,
                    timesPicked = 0
                };
            }
        }

        private void RegisterAugmentEvents()
        {
            _triggerAugmentsShownBinding = new EventBinding<TriggerAugmentsShown>(OnTriggerAugmentsShown);
            EventBus<TriggerAugmentsShown>.Register(_triggerAugmentsShownBinding);
        }

        private void DeregisterAugmentEvents()
        {
            EventBus<TriggerAugmentsShown>.Deregister(_triggerAugmentsShownBinding);

            if (_augmentLibraryHandle.IsValid())
                Addressables.Release(_augmentLibraryHandle);
        }

        private void OnTriggerAugmentsShown(TriggerAugmentsShown trigger)
        {
            if (trigger.Augments == null) return;

            foreach (var augment in trigger.Augments)
            {
                if (augment == null) continue;

                if (_augmentStats.TryGetValue(augment.ID, out var entry))
                    entry.timesShown++;
            }
        }

        public void OnAugmentPicked(SO_Augment augment)
        {
            if (augment == null) return;

            if (_augmentStats.TryGetValue(augment.ID, out var entry))
                entry.timesPicked++;
        }
    }
}