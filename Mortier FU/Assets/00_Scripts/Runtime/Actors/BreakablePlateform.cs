using Cysharp.Threading.Tasks;
using MortierFu;
using UnityEngine;
using System.Collections.Generic;

public class BreakablePlateform : Breakable
{
    [SerializeField] private int _hurtHp;
    [SerializeField] private int _badlyHurtHp;
    [SerializeField] private GameObject _hurtMesh;
    [SerializeField] private GameObject _badlyHurtMesh;

    private GameObject _currentMesh;
    private bool _destructionReported;
    
    public override bool IsDashInteractable => false;
    
    private static readonly HashSet<BreakablePlateform> _activePlatforms = new();

    public static IReadOnlyCollection<BreakablePlateform> ActivePlatforms => _activePlatforms;

    protected override void Awake()
    {
        base.Awake();
        
        if(_hurtMesh)
            _hurtMesh?.SetActive(false);
        if(_badlyHurtMesh)
            _badlyHurtMesh?.SetActive(false);
        
        _currentMesh = _intactMesh;

    }
    
    private void OnEnable()
    {
        _activePlatforms.Add(this);

        EventBus<TriggerBreakablePlatformRegistered>.Raise(new TriggerBreakablePlatformRegistered
        {
            Platform = this
        });
    }

    private void OnDisable()
    {
        _activePlatforms.Remove(this);

        EventBus<TriggerBreakablePlatformUnregistered>.Raise(new TriggerBreakablePlatformUnregistered
        {
            Platform = this
        });
    }
    
    public override void Interact(Vector3 contactPoint)
    {
        if (_destructionReported)
            return;

        _life--;

        if (_life == _hurtHp)
        {
            ChangeCurrentMesh(_hurtMesh);
            return;
        }

        if (_life == _badlyHurtHp)
        {
            ChangeCurrentMesh(_badlyHurtMesh);
            return;
        }

        if (_life > 0)
            return;

        _destructionReported = true;

        AudioService.PlayBreakAudio(AudioService.FMODEvents.SFX_Misc_PlatformFall, contactPoint).Forget();

        if (TryGetComponent<BoxCollider>(out BoxCollider boxCollider))
            boxCollider.enabled = false;

        _currentMesh.SetActive(false);

        Destruct(contactPoint);

        EventBus<TriggerBreakablePlatformDestroyed>.Raise(new TriggerBreakablePlatformDestroyed
        {
            Platform = this
        });
    }

    private void ChangeCurrentMesh(GameObject mesh)
    {
        AudioService.PlayBreakAudio(AudioService.FMODEvents.SFX_Misc_PlatformDamage, _currentMesh.transform.position).Forget();
        
        _currentMesh.SetActive(false);
        _currentMesh = mesh;
        _currentMesh.SetActive(true);
    }
}
