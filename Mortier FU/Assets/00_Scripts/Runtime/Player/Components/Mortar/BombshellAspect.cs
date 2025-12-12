using UnityEngine;
namespace MortierFu
{
    public class BombshellAspect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem _smokeParticles;
        [Space]
        [SerializeField] private MeshRenderer _cone03;
        [SerializeField] private MeshRenderer _cone02;
        [SerializeField] private MeshRenderer _dotsCone;
        [SerializeField] private TrailRenderer _trailThin01;
        [SerializeField] private TrailRenderer _trailThin02;
        [SerializeField] private TrailRenderer _trailFat01;
        [SerializeField] private ParticleSystem _light;
        private Transform _smokeParent;
        private Vector3 _smokeInitialLocalPos;

        private Bombshell _bombshell;
        
        public void Initialize(Bombshell bombshell)
        {
            _bombshell = bombshell;
            
            _smokeParent = _smokeParticles.transform.parent;
            _smokeInitialLocalPos = _smokeParticles.transform.localPosition;
        }

        public void OnGet()
        {
            Colorize();
            
            _smokeParticles.transform.SetParent(_smokeParent);
            _smokeParticles.transform.localPosition = _smokeInitialLocalPos;
            _smokeParticles.transform.localRotation = Quaternion.identity;
            _smokeParticles.Play();

            _trailThin01.Clear();
            _trailThin01.emitting = true;
            _trailThin02.Clear();
            _trailThin02.emitting = true;
            _trailFat01.Clear();
            _trailFat01.emitting = true;
        }

        public void OnRelease()
        {
            _smokeParticles.transform.SetParent(null);
            _smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _trailThin01.emitting = false;
            _trailThin02.emitting = false;
            _trailFat01.emitting = false;
        }
        
        private void Colorize()
        {
            var aspect = _bombshell.Owner.Aspect;
            var mats = aspect.AspectMaterials;
            
            _cone03.sharedMaterial = mats.BurnBaseVoronoiMat;
            _cone02.sharedMaterial = mats.OrangeSpikesMat;
            _dotsCone.sharedMaterial = mats.DotsAlphaSpikesMat;
            _trailThin01.sharedMaterial = mats.TrailThinMat;
            _trailThin02.sharedMaterial = mats.TrailThinMat;
            _trailFat01.sharedMaterial = mats.TrailFatMat;
            
            var main = _light.main;
            main.startColor = mats.LightColor;
        }
    }
}
