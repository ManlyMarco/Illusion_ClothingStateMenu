using System;
using UnityEngine;

namespace ClothingStateMenu
{
    public struct BodyButton : IStateToggleButton
    {
        private bool _visible;

        public string Text => $"Body - {(_visible ? "Visible" : "Hidden")}";
        public Rect Position { get; }
        private readonly ChaControl _chaCtrl;

        public BodyButton(ChaControl chaCtrl, Rect position)
        {
            _visible = true;
            _chaCtrl = chaCtrl;
            Position = position;
        }

        public void NextState()
        {
            _visible = !_visible;
            SetVisibleState(_chaCtrl, _visible);
        }

        /// <summary>
        /// Based on https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_InvisibleBody/KK_InvisibleBody.cs#L93
        /// </summary>
        private static void SetVisibleState(ChaControl chaControl, bool visible)
        {
            if (chaControl == null)
                throw new ArgumentNullException(nameof(chaControl));

            Transform cf_j_root = chaControl.gameObject.transform.Find("BodyTop/p_cf_body_bone/cf_j_root");
            if (cf_j_root != null)
                IterateVisible(cf_j_root.gameObject, visible);

            //female
            Transform cf_o_rootf = chaControl.gameObject.transform.Find("BodyTop/p_cf_body_00/cf_o_root/");
            if (cf_o_rootf != null)
                IterateVisible(cf_o_rootf.gameObject, visible);

            //male
            Transform cf_o_rootm = chaControl.gameObject.transform.Find("BodyTop/p_cm_body_00/cf_o_root/");
            if (cf_o_rootm != null)
                IterateVisible(cf_o_rootm.gameObject, visible);
        }

        /// <summary>
        /// Sets the visible state of the game object and all it's children.
        /// </summary>
        private static void IterateVisible(GameObject go, bool visible)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                //do not change visibility of attached items such as studio items and character accessories
                if (!go.name.StartsWith("a_n_"))
                {
                    //change visibility of everything else
                    IterateVisible(go.transform.GetChild(i).gameObject, visible);
                }
            }

            if (go.GetComponent<Renderer>())
                go.GetComponent<Renderer>().enabled = visible;
        }
    }
}