using UnityEngine;

namespace ClothingStateMenu
{
    public interface IStateToggleButton
    {
        GUIContent Content { get; }
        void OnClick();
    }
}