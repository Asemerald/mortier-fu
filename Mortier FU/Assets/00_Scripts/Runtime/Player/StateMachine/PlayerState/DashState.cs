using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class DashState : BaseState
    {
        private Collider[] _overlapBuffer = new Collider[100];

        private HashSet<GameObject> _processedRoots;
        private HashSet<PlayerCharacter> _hitCharacters;

        private int _availableCharges;
        private CountdownTimer _dashCooldownTimer;
        private CountdownTimer _dashTriggerTimer;

        private LayerMask _whatIsStrikable;
        private TrailRenderer _trailInstance;

        public DashState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _processedRoots = new HashSet<GameObject>();
            _hitCharacters = new HashSet<PlayerCharacter>();

            _dashCooldownTimer = new CountdownTimer(character.Stats.GetDashCooldown());
            _dashCooldownTimer.OnTimerStop += OnCooldownTimerStop;
            _dashTriggerTimer = new CountdownTimer(character.Stats.DashDuration.Value);

            character.Stats.DashCooldown.OnDirtyUpdated += UpdateDashCooldown;
            character.Stats.DashDuration.OnDirtyUpdated += UpdateDashDuration;
            character.Stats.DashCharges.OnDirtyUpdated += UpdateDashCharges;

            _whatIsStrikable = LayerMask.GetMask("DynamicActors");
        }

        public void InitializeTrail(GameObject trailPrefab)
        {
            if (!trailPrefab)
            {
                Logs.LogError("Null trail prefab in dash state !");
                return;
            }

            var vfxGO = Object.Instantiate(trailPrefab, character.transform);
            _trailInstance = vfxGO.GetComponent<TrailRenderer>();
            _trailInstance.material = character.Aspect.GetDashTrailMaterial();
            _trailInstance.emitting = false;
        }

        public bool IsFinished => _dashTriggerTimer.IsFinished;
        public int AvailableCharges => _availableCharges;

        public float DashCooldownProgress => _dashCooldownTimer.Progress;

        public override void OnEnter()
        {
            _availableCharges -= 1;
            if (!_dashCooldownTimer.IsRunning)
            {
                _dashCooldownTimer.Start();
            }

            _dashTriggerTimer.Start();

            animator.CrossFade(DashHash, k_crossFadeDuration, 0);

            EventBus<TriggerDash>.Raise(new TriggerDash()
            {
                Character = character,
            });

            TEMP_FXHandler.Instance.InstantiateDashFX(character.GetStrikePoint(),
                character.Stats.GetStrikeRadius() * 0.5f);
            if (debug)
                Logs.Log("Entering Dash State");

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Strike_Dash, character.transform.position);
            character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);

            Vector3 dashDir = character.Controller.GetDashDirection();
            character.Controller.rigidbody.AddForce(dashDir * 7.2f, ForceMode.Impulse);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            _processedRoots.Clear();
            _hitCharacters.Clear();

            if (_trailInstance)
                _trailInstance.emitting = true;
        }

        public override void Update()
        {
            character.Controller.HandleMovementUpdate(0.2f);
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();

            ExecuteStrike();
        }

        public override void OnExit()
        {
            _dashTriggerTimer.Stop();

            if (_trailInstance)
                _trailInstance.emitting = false;

            if (debug)
                Logs.Log("Exiting Dash State");
        }

        public void Reset()
        {
            _dashTriggerTimer.Stop();

            _dashCooldownTimer.OnTimerStop -= OnCooldownTimerStop;
            _dashCooldownTimer.Stop();
            _dashCooldownTimer.OnTimerStop += OnCooldownTimerStop;

            _availableCharges = Mathf.RoundToInt(character.Stats.DashCharges.Value);
        }

        // While dashing, the strike is executed every frame to bump other players or interact with objects.
        private void ExecuteStrike()
        {
            var strikePosition = character.GetStrikePoint().position;
            float radius = character.Stats.GetStrikeRadius();

            var count = Physics.OverlapSphereNonAlloc(strikePosition, radius,
                _overlapBuffer, _whatIsStrikable);

            for (var i = 0; i < count; i++)
            {
                var hit = _overlapBuffer[i];
                if (hit == null) continue;

                var root = hit.attachedRigidbody;
                if (root == null) continue;
                if (root == character.Controller.rigidbody) continue; // Skip self

                if (!_processedRoots.Add(root.gameObject)) continue;

                character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);

                Debug.Log("Processing " + root.gameObject.name);

                if (root.TryGetComponent(out IInteractable interactable) && interactable.IsDashInteractable)
                {
                    Vector3 contactPoint = Physics.ClosestPoint(strikePosition, hit,
                        hit.transform.position, hit.transform.rotation);

                    interactable.Interact(contactPoint);
                    continue;
                }

                var other = root.GetComponentInParent<PlayerCharacter>();
                if (other == null) continue;
                if (other == character) continue;

                _hitCharacters.Add(other);

                int dashDamage = Mathf.RoundToInt(character.Stats.StrikeDamage.Value);
                if (dashDamage > 0 && other.Health.IsAlive)
                {
                    other.Health.TakeDamage(dashDamage, character);
                }

                var knockbackDir = (other.transform.position - character.transform.position).normalized;
                var knockbackForce = knockbackDir * character.Stats.GetDashPushForce();
                float knockbackDuration = character.Stats.StrikeKnockbackDuration.Value;
                float stunDuration = character.Stats.GetKnockbackStunDuration();
                other.ReceiveKnockback(knockbackDuration, knockbackForce, stunDuration, character);
            }

            if (_hitCharacters.Count > 0)
            {
                EventBus<TriggerStrike>.Raise(new TriggerStrike()
                {
                    Character = character,
                    HitCharacters = _hitCharacters.ToArray(),
                });
            }

            // if (blockedBombshells.Count > 0)
            // {
            //     EventBus<TriggerStrikeHitBombshell>.Raise(new TriggerStrikeHitBombshell()
            //     {
            //         Character = character,
            //         HitBombshells = blockedBombshells.ToArray(),
            //     });
            // }
        }

        private void UpdateDashCooldown()
        {
            float dashCooldown = character.Stats.GetDashCooldown();
            _dashCooldownTimer.DynamicUpdate(dashCooldown);
        }

        private void UpdateDashDuration()
        {
            float dashDuration = character.Stats.DashDuration.Value;
            _dashTriggerTimer.DynamicUpdate(dashDuration);
        }

        private void UpdateDashCharges()
        {
            int maxCharges = Mathf.RoundToInt(character.Stats.DashCharges.Value);
            if (_availableCharges > maxCharges)
            {
                _availableCharges = maxCharges;
            }

            if (!_dashCooldownTimer.IsRunning)
            {
                _dashCooldownTimer.Start();
            }
        }

        private void OnCooldownTimerStop()
        {
            _availableCharges += 1;

            // If more charges to refill restart
            int maxCharges = Mathf.RoundToInt(character.Stats.DashCharges.Value);
            if (_availableCharges < maxCharges)
            {
                _dashCooldownTimer.Reset();
                _dashCooldownTimer.Start();
            }
        }

        public override void Dispose()
        {
            if (_trailInstance)
                Object.Destroy(_trailInstance.gameObject);

            character.Stats.DashCooldown.OnDirtyUpdated -= UpdateDashCooldown;
            character.Stats.DashDuration.OnDirtyUpdated -= UpdateDashDuration;
            character.Stats.DashCharges.OnDirtyUpdated -= UpdateDashCharges;

            _dashCooldownTimer.OnTimerStop -= OnCooldownTimerStop;

            _dashCooldownTimer.Dispose();
            _dashTriggerTimer.Dispose();
        }
    }
}