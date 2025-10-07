using UnityEngine;

namespace MortierFu
{
    public class Mortar : MonoBehaviour
    {
        [Header("Statistics")]
        public CharacterStat AttackSpeed = new CharacterStat(2.0f);
        public CharacterStat ShotRange = new CharacterStat(20.0f);
        
        [Header("References")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;

        [Header("Debugging")] 
        [SerializeField] private bool _enableDebug = true;
        
        private MortarShootStrategy _shootStrategy;
        private GameObject _aimWidget;
        
        private void Start()
        {
            _shootStrategy = new MortarShootStrategyPositionLimited();

            _aimWidget = Instantiate(_aimWidgetPrefab, transform);
            _aimWidget.SetActive(false);
        }

        public void BeginAiming()
        {
            
        }

        public void UpdateAiming(Vector2 mousePosition)
        {
            
        }
        
        public void EndAiming()
        {
            
        }
        
        public void Shoot()
        {
            
        }

        private void EnableAimWidget(bool enable = true)
        {
            if (_aimWidget != null) return;
            
            _aimWidget.SetActive(enable);
        }
    }
}