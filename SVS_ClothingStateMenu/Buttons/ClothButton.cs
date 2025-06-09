using System.Collections.Generic;
using System.Linq;
using Character;
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
            {ChaFileDefine.ClothesKind.shoes, "Shoes"},
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

        private readonly Human _chaCtrl;

        public GUIContent Content => IsVisible() ? _contentStates[GetState()] : _contentEmpty;

        private bool IsVisible()
        {
            if (_chaCtrl.cloth.Clothess[(int)Kind]?.cusClothesCmp)
                return true;

            switch (Kind)
            {
                case ChaFileDefine.ClothesKind.top:
                    return _chaCtrl.cloth.ClothesSubs[0]?.cusClothesCmp || _chaCtrl.cloth.ClothesSubs[1]?.cusClothesCmp || _chaCtrl.cloth.ClothesSubs[2]?.cusClothesCmp;
                case ChaFileDefine.ClothesKind.bot:
                    return _chaCtrl.cloth.notBot;
                case ChaFileDefine.ClothesKind.bra:
                    return _chaCtrl.cloth.notBra;
                case ChaFileDefine.ClothesKind.shorts:
                    return _chaCtrl.cloth.notShorts;
                default:
                    return false;
            }
        }

        public void OnClick()
        {
            _chaCtrl.cloth.SetClothesStateNext(Kind);
            _chaCtrl.cloth.UpdateClothesStateAll();
        }

        public int GetState()
        {
            return _chaCtrl.fileStatus.clothesState[(int)Kind];
        }

        public ClothButton(ChaFileDefine.ClothesKind kind, Human chaCtrl)
        {
            Kind = kind;
            _chaCtrl = chaCtrl;

            var fancyName = _FancyKindNames[kind];
            _contentStates = _FancyStateNames.Select(fancyState => new GUIContent($"{fancyName}: {fancyState}")).ToArray();
            _contentEmpty = new GUIContent(fancyName + ": None");
        }
    }
}
