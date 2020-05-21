using UnityEngine;

namespace ClothingStateMenu
{
    public interface IStateToggleButton
    {
        string Text { get; }
        Rect Position { get; }

        void NextState();
    }
}