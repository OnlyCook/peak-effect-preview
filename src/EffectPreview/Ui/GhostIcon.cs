using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // independent translucent clone of a real status icon, shown only while the real amount is 0 - forcing the real icon active instead fights the game's own toggling, see RESEARCH.md
    internal class GhostIcon
    {
        private readonly RectTransform _rtf;

        private GhostIcon(RectTransform rtf)
        {
            _rtf = rtf;
        }

        internal bool IsValid => _rtf != null;

        // reparentTo: move the clone to a different (unmasked) parent after creation, preserving world position
        internal static GhostIcon Create(Image template, Transform reparentTo = null)
        {
            GameObject go = Object.Instantiate(template.gameObject, template.transform.parent);
            go.name = template.name + " (EffectPreview Ghost)";

            if (reparentTo != null)
            {
                go.transform.SetParent(reparentTo, worldPositionStays: true);
            }

            foreach (Image image in go.GetComponentsInChildren<Image>(includeInactive: true))
            {
                Color c = Color.Lerp(image.color, Color.white, 0.4f);
                c.a *= 0.65f;
                image.color = c;
            }

            go.transform.localScale = Vector3.one;
            Common.GhostOwnershipTag.Attach(go);
            go.SetActive(false);
            return new GhostIcon(go.GetComponent<RectTransform>());
        }

        internal void Show()
        {
            // walk up and force active any inactive ancestor the game only activates once the real amount is nonzero
            Transform t = _rtf.transform;
            while (t != null && !t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        internal void Hide()
        {
            if (_rtf != null)
            {
                _rtf.gameObject.SetActive(false);
            }
        }
    }
}
