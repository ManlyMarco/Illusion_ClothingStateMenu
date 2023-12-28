using System;
using UnityEngine;

namespace ClothingStateMenu
{
    public readonly struct SpaceButton : IStateToggleButton
    {
        public GUIContent Content => throw new InvalidOperationException();

        public void OnClick()
        {
            throw new InvalidOperationException();
        }
    }
}
