using System;
using Character;
using UnityEngine;

namespace ClothingStateMenu
{
    public class BodyButton : IStateToggleButton
    {
        private static readonly GUIContent _ContentVis = new GUIContent("Body: Visible");
        private static readonly GUIContent _ContentHid = new GUIContent("Body: Hidden");

        private bool _visible;
        private readonly Human _chaCtrl;

        public BodyButton(Human chaCtrl)
        {
            _visible = true;
            _chaCtrl = chaCtrl;
        }

        public GUIContent Content => _visible ? _ContentVis : _ContentHid;

        public void OnClick()
        {
            _visible = !_visible;
            SetVisibleState(_chaCtrl, _visible);
        }

        /// <summary>
        /// Based on https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_InvisibleBody/KK_InvisibleBody.cs#L93
        /// </summary>
        private static void SetVisibleState(Human chaControl, bool visible)
        {
            if (chaControl == null)
                throw new ArgumentNullException(nameof(chaControl));

            var boneRoot = chaControl.body.objBone.transform.Find("cf_j_root");
            if (boneRoot != null)
                IterateVisible(boneRoot.gameObject, visible);

            var bodyRoot = chaControl.body.objBody.transform;
            if (bodyRoot != null)
                IterateVisible(bodyRoot.gameObject, visible);

            var nailRoot = chaControl.body.hand.nailObject.obj?.transform;
            if (nailRoot != null)
                IterateVisible(nailRoot.gameObject, visible);
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