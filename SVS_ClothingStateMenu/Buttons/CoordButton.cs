using System;
using UnityEngine;

namespace ClothingStateMenu
{
    public class CoordButton : IStateToggleButton
    {
        public Rect Position { get; }
        private readonly int _coordId;
        private readonly Action<int> _action;

        public GUIContent Content { get; }

        public void OnClick()
        {
            _action(_coordId);
        }

        public CoordButton(int coordId, Action<int> changeAction, Rect position)
        {
            Position = position;
            Content = new GUIContent((coordId + 1).ToString());
            _coordId = coordId;
            _action = changeAction;
        }
    }
}