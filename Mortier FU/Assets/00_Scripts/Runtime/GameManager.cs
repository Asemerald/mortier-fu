using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class GameManager : MonoBehaviour
    {
        public Transform[] spawnPoints;

        void Start()
        {
            var players = Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

            for (int i = 0; i < players.Length; i++)
            {
                players[i].SpawnInGame(spawnPoints[i % spawnPoints.Length].position);
            }
        }
    }
}
