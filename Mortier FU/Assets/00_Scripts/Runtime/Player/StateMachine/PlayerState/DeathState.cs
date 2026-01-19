using MortierFu.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class DeathState : BaseState
    {
        public DeathState(PlayerCharacter character, Animator animator) : base(character, animator)
        { }

        public override void OnEnter()
        {
            if(debug) Logs.Log("Entering Death State");
            
            character.Controller.ResetVelocity();
            character.Owner.DespawnInGame();
            
            // Spawn tomb
            var levelSystem = SystemManager.Instance.Get<LevelSystem>();
            if (levelSystem.GetCurrentLevelScene(out Scene scene))
            {
                var prefab = character.Aspect.AspectMaterials.TombPrefab;
                if (prefab)
                {
                    var tomb = Object.Instantiate(prefab, character.transform.position, Quaternion.identity);
                    SceneManager.MoveGameObjectToScene(tomb, scene);
                }
            }
        }
        
        public override void OnExit()
        {
            if (debug) Logs.Log("Exiting Death State");
        }
    }
}