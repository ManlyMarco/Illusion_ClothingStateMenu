using UnityEngine;
using UnityEngine.UI;

namespace ClothingStateMenu
{
    public class ShoeButton : IStateToggleButton
    {
        private static readonly GUIContent _ContentIn = new GUIContent("Shoes: Indoors");
        private static readonly GUIContent _ContentOut = new GUIContent("Shoes: Outdoors");

        private readonly Toggle _toggleIndoors;
        private readonly Toggle _toggleOutdoors;

        public ShoeButton(Toggle toggleIndoors, Toggle toggleOutdoors)
        {
            _toggleIndoors = toggleIndoors;
            _toggleOutdoors = toggleOutdoors;
        }

        public GUIContent Content => _toggleIndoors.isOn ? _ContentIn : _ContentOut;

        public void OnClick()
        {
            if (_toggleIndoors.isOn)
            {
                _toggleIndoors.isOn = false;
                _toggleOutdoors.isOn = true;
            }
            else
            {
                _toggleIndoors.isOn = true;
                _toggleOutdoors.isOn = false;
            }
        }
    }
}