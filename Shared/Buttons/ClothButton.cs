using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClothingStateMenu
{
    public readonly struct ClothButton : IStateToggleButton
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

        public readonly ChaFileDefine.ClothesKind Kind;

        private readonly ChaControl _chaCtrl;

        public GUIContent Content => _contentStates[GetState()];

        public void OnClick()
        {
            _chaCtrl.SetClothesStateNext((int)Kind);
        }

        public int GetState()
        {
            return _chaCtrl.fileStatus.clothesState[(int)Kind];
        }

        public ClothButton(ChaFileDefine.ClothesKind kind, ChaControl chaCtrl) : this()
        {
            Kind = kind;
            _chaCtrl = chaCtrl;

            var fancyName = _FancyKindNames[kind];
            _contentStates = _FancyStateNames.Select(fancyState => new GUIContent($"{fancyName}: {fancyState}")).ToArray();
        }
    }
}
