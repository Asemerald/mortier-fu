using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class AugmentCardUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private Image _background;

        public void Setup(DA_Augment augment)
        {
            _icon.sprite = augment.Icon;
            _name.text = augment.Name;
            _description.text = augment.Description;
            _background.color = augment.BgColor;
        }
    }
}