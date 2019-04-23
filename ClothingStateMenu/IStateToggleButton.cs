using UnityEngine;

namespace KK_ClothingStateMenu
{
    public interface IStateToggleButton
    {
        string Text { get; }
        Rect Position { get; }

        void NextState();
    }
}