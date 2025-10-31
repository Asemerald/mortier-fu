using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        public Task OnInitialize()
        {
            modService = ServiceManager.Instance.Get<ModService>();
            return Task.CompletedTask;
        }

        public void Tick() { }
        public bool IsInitialized { get; set; }
        public void Dispose() { }

        public IEnumerator LoadAllModResources()
        {
            Progress = 0f;
            var mods = modService.AllMods.FindAll(m => m.isEnabled);

            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                yield return LoadBundles(mod);
                yield return LoadFmodBanks(mod);
                Progress = (i + 1f) / mods.Count;
            }
        }

        private IEnumerator LoadBundles(ModInfo mod)
        {
            foreach (var bundleName in mod.manifest.assetBundles)
            {
                string bundlePath = Path.Combine(mod.folderPath, bundleName);
                if (!File.Exists(bundlePath))
                    continue;

                var req = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return req;

                if (req.assetBundle != null)
                    loadedBundles.Add(req.assetBundle);
            }
        }

        private IEnumerator LoadFmodBanks(ModInfo mod)
        {
            foreach (var bankFile in mod.manifest.fmodBanks)
            {
                string bankPath = Path.Combine(mod.folderPath, bankFile);
                if (!File.Exists(bankPath))
                    continue;

                byte[] bankBytes = File.ReadAllBytes(bankPath);
                var result = RuntimeManager.StudioSystem.loadBankMemory(bankBytes, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank);
                if (result == FMOD.RESULT.OK)
                    loadedBanks.Add(bank);
            }

            yield return null;
        }
    }
}
