using System;
using UnityEngine;

namespace ClothingStateMenu
{
    public class SpaceButton : IStateToggleButton
    {
        public GUIContent Content => throw new InvalidOperationException();

        public void OnClick()
        {
            throw new InvalidOperationException();
        }
    }
}
