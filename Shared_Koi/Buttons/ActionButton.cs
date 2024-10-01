using System;
using UnityEngine;

namespace ClothingStateMenu
{
    public class ActionButton : IStateToggleButton
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