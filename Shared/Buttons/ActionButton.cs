using System;
using UnityEngine;

namespace ClothingStateMenu
{
    public readonly struct ActionButton : IStateToggleButton
    {
        private readonly Action _action;

        public GUIContent Content { get; }

        public void OnClick()
        {
            _action();
        }

        public ActionButton(string action, Action changeAction)
        {
            Content = new GUIContent(action);
            _action = changeAction;
        }
    }

}