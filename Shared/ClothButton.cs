using System.Collections.Generic;
using UnityEngine;

namespace ClothingStateMenu
{
    public readonly struct ClothButton : IStateToggleButton
    {
        private static readonly Dictionary<ChaFileDefine.ClothesKind, string> _fancyKindNames = new Dictionary<ChaFileDefine.ClothesKind, string>
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

        private static readonly Dictionary<int, string> _fancyStateNames = new Dictionary<int, string>
        {
            {0, "On"},
            {1, "Shift"},
            {2, "Hang"},
            {3, "Off"}
        };

        public string Text => $"{_fancyName} - {_fancyStateNames[GetState()]}";

        public Rect Position { get; }
        public readonly ChaFileDefine.ClothesKind Kind;

        private readonly ChaControl _chaCtrl;
        private readonly string _fancyName;

        public void NextState()
        {
            _chaCtrl.SetClothesStateNext((int)Kind);
        }

        public int GetState()
        {
            return _chaCtrl.fileStatus.clothesState[(int)Kind];
        }

        public ClothButton(Rect position, ChaFileDefine.ClothesKind kind, ChaControl chaCtrl) : this()
        {
            Kind = kind;
            _fancyName = _fancyKindNames[kind];
            Position = position;
            _chaCtrl = chaCtrl;
        }
    }
}
