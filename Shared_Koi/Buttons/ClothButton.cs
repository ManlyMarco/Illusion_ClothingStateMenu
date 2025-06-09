using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClothingStateMenu
{
    public class ClothButton : IStateToggleButton
    {
        private static readonly Dictionary<ChaFileDefine.ClothesKind, string> _FancyKindNames = new Dictionary<ChaFileDefine.ClothesKind, string>
        {
            {ChaFileDefine.ClothesKind.top, "Top"},
            {ChaFileDefine.ClothesKind.bot, "Bottom"},
            {ChaFileDefine.ClothesKind.bra, "Bra"},
            {ChaFileDefine.ClothesKind.shorts, "Underwear"},
            {ChaFileDefine.ClothesKind.gloves, "Gloves"},
            {ChaFileDefine.ClothesKind.panst, "Pantyhose"},
            {ChaFileDefine.ClothesKind.socks, "Legwear"},
#if KK || KKS
            
            {ChaFileDefine.ClothesKind.shoes_inner, "Shoes"},
            {ChaFileDefine.ClothesKind.shoes_outer, "Shoes"}
#elif EC
            {ChaFileDefine.ClothesKind.shoes, "Shoes"},
#endif
        };

        private static readonly string[] _FancyStateNames =
        {
            "On",
            "Shift",
            "Hang",
            "Off"
        };

        private readonly GUIContent[] _contentStates;
        private readonly GUIContent _contentEmpty;

        public readonly ChaFileDefine.ClothesKind Kind;

        private readonly ChaControl _chaCtrl;

        public GUIContent Content => IsVisible() ? _contentStates[GetState()] : _contentEmpty;

        private bool IsVisible()
        {
            if (_chaCtrl.cusClothesCmp[(int)Kind])
                return true;

            switch (Kind)
            {
                case ChaFileDefine.ClothesKind.top:
                    return _chaCtrl.cusClothesSubCmp[0] || _chaCtrl.cusClothesSubCmp[1] || _chaCtrl.cusClothesSubCmp[2];
                case ChaFileDefine.ClothesKind.bot:
                    return _chaCtrl.notBot;
                case ChaFileDefine.ClothesKind.bra:
                    return _chaCtrl.notBra;
                case ChaFileDefine.ClothesKind.shorts:
                    return _chaCtrl.notShorts;
                default:
                    return false;
            }
        }

        public void OnClick()
        {
            _chaCtrl.SetClothesStateNext((int)Kind);
        }

        public int GetState()
        {
            return _chaCtrl.fileStatus.clothesState[(int)Kind];
        }

        public ClothButton(ChaFileDefine.ClothesKind kind, ChaControl chaCtrl)
        {
            Kind = kind;
            _chaCtrl = chaCtrl;

            var fancyName = _FancyKindNames[kind];
            _contentStates = _FancyStateNames.Select(fancyState => new GUIContent($"{fancyName}: {fancyState}")).ToArray();
            _contentEmpty = new GUIContent(fancyName + ": None");
        }
    }
}
