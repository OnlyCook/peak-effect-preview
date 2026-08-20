using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // clone of a real bar segment, positioned via world-space (see RESEARCH.md for why not anchoredPosition)
    internal class GhostSegment
    {
        private readonly RectTransform _rtf;

        private GhostSegment(RectTransform rtf)
        {
            _rtf = rtf;
        }

        internal bool IsValid => _rtf != null;

        // clones the real fill's sprite/look instead of a flat rectangle, tinted translucent to read as a preview
        internal static GhostSegment Create(Transform ghostRoot, RectTransform template)
        {
            GameObject go = Object.Instantiate(template.gameObject, ghostRoot);
            go.name = template.name + " (EffectPreview Ghost)";

            RectTransform rtf = go.GetComponent<RectTransform>();

            // strip a cloned BarAffliction's own icon child, or it renders a second copy next to the real one
            BarAffliction driver = go.GetComponent<BarAffliction>();
            if (driver != null)
            {
                if (driver.icon != null)
                {
                    driver.icon.gameObject.SetActive(false);
                }
                Object.Destroy(driver);
            }

            foreach (Image image in go.GetComponentsInChildren<Image>(includeInactive: true))
            {
                Color c = Color.Lerp(image.color, Color.white, 0.4f);
                c.a *= 0.65f;
                image.color = c;
            }

            go.SetActive(false);
            return new GhostSegment(rtf);
        }

        // pivotWorldX places the clone's own (inherited) pivot point at that world X
        internal void Apply(float pivotWorldX, float width)
        {
            if (width <= 0f)
            {
                Hide();
                return;
            }

            Vector3 pos = _rtf.position;
            pos.x = pivotWorldX;
            _rtf.position = pos;
            _rtf.sizeDelta = new Vector2(width, _rtf.sizeDelta.y);
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
