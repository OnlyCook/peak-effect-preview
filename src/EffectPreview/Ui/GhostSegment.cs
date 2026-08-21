using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // clone of a real bar segment, positioned via world-space (see RESEARCH.md for why not anchoredPosition)
    internal class GhostSegment
    {
        private readonly RectTransform _rtf;
        private readonly Image[] _images;
        private readonly float[] _baseAlphas;
        private readonly bool _isRemoval;

        private GhostSegment(RectTransform rtf, Image[] images, float[] baseAlphas, bool isRemoval)
        {
            _rtf = rtf;
            _images = images;
            _baseAlphas = baseAlphas;
            _isRemoval = isRemoval;
        }

        internal bool IsValid => _rtf != null;

        internal RectTransform Rtf => _rtf;

        // clones the real fill's sprite/look instead of a flat rectangle, tinted translucent to read as a preview
        // isRemoval: pulses alpha subtly rather than holding a flat tint, so a "this will be removed" ghost doesn't read identically to a "this will be added" one
        internal static GhostSegment Create(Transform ghostRoot, RectTransform template, bool isRemoval = false)
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

            Image[] images = go.GetComponentsInChildren<Image>(includeInactive: true);
            float[] baseAlphas = new float[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                Color c = Color.Lerp(images[i].color, Color.white, 0.4f);
                c.a *= 0.65f;
                images[i].color = c;
                baseAlphas[i] = c.a;
            }

            go.SetActive(false);
            return new GhostSegment(rtf, images, baseAlphas, isRemoval);
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

            if (_isRemoval)
            {
                RemovalPulse.Apply(_images, _baseAlphas);
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
