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

        public readonly ChaFileDefine.ClothesKind Kind;

        private readonly Human _chaCtrl;

        public GUIContent Content => _contentStates[GetState()];

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
        }
    }
}
