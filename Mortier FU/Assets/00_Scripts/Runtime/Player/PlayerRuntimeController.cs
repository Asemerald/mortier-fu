using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class PlayerRuntimeController
    {
        private readonly PlayerManager _owner;
        private readonly GameObject _playerInGamePrefab;

        private GameObject _characterGO;
        private PlayerCharacter _character;

        public GameObject CharacterGO => _characterGO;

        public PlayerCharacter Character
        {
            get
            {
                if (_character == null && _characterGO != null)
                {
                    _character = _characterGO.GetComponent<PlayerCharacter>();
                }

                return _character;
            }
        }

        public bool IsInGame { get; private set; }

        public PlayerRuntimeController(
            PlayerManager owner,
            GameObject playerInGamePrefab
        )
        {
            _owner = owner;
            _playerInGamePrefab = playerInGamePrefab;
        }

        public bool Spawn(
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            out bool createdCharacter
        )
        {
            createdCharacter = false;

            if (_owner == null)
            {
                Logs.LogError("[PlayerRuntimeController] Owner is missing.");
                return false;
            }

            if (_characterGO == null)
            {
                if (_playerInGamePrefab == null)
                {
                    Logs.LogError($"[PlayerRuntimeController] No in-game prefab assigned for Player {_owner.PlayerIndex}.");
                    return false;
                }

                _characterGO = Object.Instantiate(
                    _playerInGamePrefab,
                    spawnPosition,
                    spawnRotation
                );

                _character = _characterGO.GetComponent<PlayerCharacter>();

                if (_character == null)
                {
                    Logs.LogError(
                        $"[PlayerRuntimeController] In-game prefab for Player {_owner.PlayerIndex} does not contain a PlayerCharacter component."
                    );

                    Object.Destroy(_characterGO);
                    _characterGO = null;
                    return false;
                }

                _character.Initialize(_owner);
                createdCharacter = true;
            }

            _characterGO.transform.SetPositionAndRotation(
                spawnPosition,
                spawnRotation
            );

            _characterGO.SetActive(true);

            IsInGame = true;

            return true;
        }

        public void Despawn()
        {
            if (_characterGO != null)
            {
                _characterGO.SetActive(false);
            }

            IsInGame = false;
        }

        public void DestroyRuntime()
        {
            if (_characterGO != null)
            {
                Object.Destroy(_characterGO);
            }

            _characterGO = null;
            _character = null;
            IsInGame = false;
        }
    }
}