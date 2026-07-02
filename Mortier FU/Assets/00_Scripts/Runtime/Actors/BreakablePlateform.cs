using Cysharp.Threading.Tasks;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;

public class BreakablePlateform : Breakable
{
    [SerializeField] private int _hurtPercent;
    [SerializeField] private int _badlyHurtPercent;
    [SerializeField] private GameObject _hurtMesh;
    [SerializeField] private GameObject _badlyHurtMesh;

    private GameObject _currentMesh;

    protected override void Awake()
    {
        base.Awake();
        if(_hurtMesh)
            _hurtMesh?.SetActive(false);
        if(_badlyHurtMesh)
            _badlyHurtMesh?.SetActive(false);
        
        _currentMesh = _intactMesh;
    }
    
    public override void Interact(Vector3 contactPoint)
    {
        _life--;
        if (_life == _hurtPercent)
        {
           ChangeCurrentMesh(_hurtMesh);
           return;
        }
        if (_life == _badlyHurtPercent)
        {
            ChangeCurrentMesh(_badlyHurtMesh);
            return;
        }
         
        if (_life > 0) return;
        AudioService.PlayBreakAudio(AudioService.FMODEvents.SFX_Misc_Break, contactPoint).Forget();
        
        _currentMesh.SetActive(false);
        Destruct(contactPoint);
    }

    private void ChangeCurrentMesh(GameObject mesh)
    {
        _currentMesh.SetActive(false);
        _currentMesh = mesh;
        _currentMesh.SetActive(true);
    }
}
