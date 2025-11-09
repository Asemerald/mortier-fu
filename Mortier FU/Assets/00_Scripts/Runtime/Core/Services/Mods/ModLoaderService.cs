using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using FMODUnity;

namespace MortierFu.Services
{
    public class ModLoaderService : IGameService
    {
        private ModService modService;
        private readonly List<AssetBundle> loadedBundles = new();
        private readonly List<FMOD.Studio.Bank> loadedBanks = new();

        public float Progress { get; private set; }

        public UniTask OnInitialize()
        {
            modService = ServiceManager.Instance.Get<ModService>();

            return UniTask.CompletedTask;
        }

        public void Tick() { }
        public bool IsInitialized { get; set; }
        public void Dispose() { }

        public async UniTask LoadAllModResources()
        {
            Progress = 0f;
            var mods = modService.AllMods.FindAll(m => m.isEnabled);

            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                await LoadBundles(mod);
                await LoadFmodBanks(mod);
                Progress = (i + 1f) / mods.Count;
            }
        }

        private async UniTask LoadBundles(ModInfo mod)
        {
            foreach (var bundleName in mod.manifest.assetBundles)
            {
                string bundlePath = Path.Combine(mod.folderPath, bundleName);
                if (!File.Exists(bundlePath))
                    continue;

                var req = AssetBundle.LoadFromFileAsync(bundlePath);
                await req;

                if (req.assetBundle != null)
                    loadedBundles.Add(req.assetBundle);
            }
        }

        private async UniTask LoadFmodBanks(ModInfo mod)
        {
            foreach (var bankFile in mod.manifest.fmodBanks)
            {
                string bankPath = Path.Combine(mod.folderPath, bankFile);
                if (!File.Exists(bankPath))
                    continue;

                byte[] bankBytes = await File.ReadAllBytesAsync(bankPath); // Changed that to be async. Antoine
                var result = RuntimeManager.StudioSystem.loadBankMemory(bankBytes, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank);
                if (result == FMOD.RESULT.OK)
                    loadedBanks.Add(bank);
            }
        }
    }
}
