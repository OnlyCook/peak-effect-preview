using UnityEngine;
using UnityEngine.UI;

namespace StatPreview.Ui
{
    // positions from GetWorldCorners instead of anchors/pivot, dodges the layout group and mask that live under extraBarStamina
    internal class GhostSegment
    {
        private readonly RectTransform _rtf;

        private GhostSegment(RectTransform rtf)
        {
            _rtf = rtf;
        }

        internal bool IsValid => _rtf != null;

        internal static GhostSegment Create(Transform ghostRoot, Color tint)
        {
            GameObject go = new GameObject("StatPreview.Ghost", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(ghostRoot, false);

            RectTransform rtf = go.GetComponent<RectTransform>();
            rtf.anchorMin = new Vector2(0.5f, 0.5f);
            rtf.anchorMax = new Vector2(0.5f, 0.5f);
            rtf.pivot = Vector2.zero;

            go.GetComponent<Image>().color = tint;
            go.SetActive(false);
            return new GhostSegment(rtf);
        }

        // fixedLeftEdge: true if the bar grows rightward (left edge stays put)
        internal void Apply(Vector3[] corners, float fullWorldWidth, float delta, bool fixedLeftEdge)
        {
            if (Mathf.Approximately(delta, 0f) || corners == null)
            {
                Hide();
                return;
            }

            float deltaWorldWidth = fullWorldWidth * delta;

            float currentFreeEdgeX = fixedLeftEdge ? corners[2].x : corners[0].x;
            float newFreeEdgeX = fixedLeftEdge ? currentFreeEdgeX + deltaWorldWidth : currentFreeEdgeX - deltaWorldWidth;

            float stripLeftX = Mathf.Min(currentFreeEdgeX, newFreeEdgeX);
            float stripRightX = Mathf.Max(currentFreeEdgeX, newFreeEdgeX);

            Vector3 bottomLeft = new Vector3(stripLeftX, corners[0].y, corners[0].z);
            Vector3 topRight = new Vector3(stripRightX, corners[2].y, corners[2].z);

            Transform parent = _rtf.parent;
            Vector3 localBottomLeft = parent.InverseTransformPoint(bottomLeft);
            Vector3 localTopRight = parent.InverseTransformPoint(topRight);

            _rtf.anchoredPosition = new Vector2(localBottomLeft.x, localBottomLeft.y);
            _rtf.sizeDelta = new Vector2(localTopRight.x - localBottomLeft.x, localTopRight.y - localBottomLeft.y);
            _rtf.gameObject.SetActive(true);
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
