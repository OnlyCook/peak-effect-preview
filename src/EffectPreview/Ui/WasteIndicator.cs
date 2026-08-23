using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // thin marker pinned to one edge of a ghost bar, flagging that the item's effect on that stat is only partially applied (some precious stat is wasted)
    // pure decoration: takes no layout space of its own, sits above the ghost bar it marks, and is allowed to clip slightly into it since it belongs right on the edge
    internal class WasteIndicator
    {
        private const float Width = 3f;

        // nudges the marker off dead-center on the edge, so it reads as sitting on the edge rather than somewhere next to it
        private const float LeftEdgeInset = 2f;
        private const float RightEdgeInset = 3f;

        private static readonly Vector3[] MeasureBuffer = new Vector3[4];

        private readonly RectTransform _rtf;
        private readonly Vector3[] _edgeCorners = new Vector3[4];
        private readonly Vector3[] _centerCorners = new Vector3[4];

        private WasteIndicator(RectTransform rtf)
        {
            _rtf = rtf;
        }

        internal bool IsValid => _rtf != null;

        // parent is whatever the target ghost bar itself lives under; ignoreLayout keeps a HorizontalLayoutGroup parent from fighting our manual positioning
        // color: the wasted status/area's own vanilla fill color
        internal static WasteIndicator Create(Transform parent, Color color)
        {
            GameObject go = new GameObject("EffectPreview WasteIndicator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform rtf = (RectTransform)go.transform;
            rtf.SetParent(parent, worldPositionStays: false);
            rtf.anchorMin = (rtf.anchorMax = (rtf.pivot = new Vector2(0.5f, 0.5f)));

            go.GetComponent<LayoutElement>().ignoreLayout = true;

            Image image = go.GetComponent<Image>();
            color.a = 1f;
            image.color = color;
            image.raycastTarget = false;

            go.SetActive(false);
            return new WasteIndicator(rtf);
        }

        // samples a fill color off root (or its children), skipping excludeIcon so a badge's icon Image doesn't get picked over its actual bar-fill color
        internal static Color SampleFillColor(GameObject root, Image excludeIcon)
        {
            Image[] images = root.GetComponentsInChildren<Image>(includeInactive: true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != excludeIcon)
                {
                    return images[i].color;
                }
            }
            return excludeIcon != null ? excludeIcon.color : Color.white;
        }

        // measures a rect's on-screen height, for the one shared "unified" height every marker uses regardless of which bar it marks
        internal static float MeasureHeight(RectTransform rtf)
        {
            rtf.GetWorldCorners(MeasureBuffer);
            return MeasureBuffer[1].y - MeasureBuffer[0].y;
        }

        // edgeRtf: the ghost bar segment whose edge we pin the marker's X to
        // centerRtf: the real (vanilla) bar whose vertical center the marker is aligned to (its own height is ignored - height is unified across every marker)
        // height: the shared, unified marker height (see MeasureHeight)
        // rightEdge: false = left edge (nudged right/inward), true = right edge (nudged left/inward)
        internal void Apply(RectTransform edgeRtf, RectTransform centerRtf, float height, bool rightEdge)
        {
            // activeSelf, not activeInHierarchy - see BarLabel's Apply/ApplyTransition for why, same reasoning applies here
            if (edgeRtf == null || !edgeRtf.gameObject.activeSelf || centerRtf == null)
            {
                Hide();
                return;
            }

            edgeRtf.GetWorldCorners(_edgeCorners);
            centerRtf.GetWorldCorners(_centerCorners);

            float edgeX = rightEdge ? _edgeCorners[2].x : _edgeCorners[0].x;
            edgeX += rightEdge ? -RightEdgeInset : LeftEdgeInset;
            float centerY = (_centerCorners[0].y + _centerCorners[1].y) * 0.5f;

            _rtf.gameObject.SetActive(true);
            _rtf.SetAsLastSibling();
            _rtf.sizeDelta = new Vector2(Width, height);

            Vector3 pos = _rtf.position;
            pos.x = edgeX;
            pos.y = centerY;
            _rtf.position = pos;
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
