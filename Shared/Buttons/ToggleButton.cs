using UnityEngine;
using UnityEngine.UI;

namespace ClothingStateMenu
{
    public class ToggleButton : IStateToggleButton
    {
        private readonly GUIContent _contentOn;
        private readonly GUIContent _contentOff;

        private readonly Toggle _toggle;

        public GUIContent Content => _toggle.isOn ? _contentOn : _contentOff;

        public void OnClick()
        {
            _toggle.isOn = !_toggle.isOn;
        }

        public ToggleButton(Toggle toggle, GUIContent contentOn, GUIContent contentOff)
        {
            _toggle = toggle;
            _contentOn = contentOn;
            _contentOff = contentOff;
        }
    }
}
