using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // translucent clone of StaminaBar.rainbowStamina, cloning (rather than reimplementing) picks up whatever material/sprite actually drives its animated look for free
    internal class GhostRainbowStamina
    {
        private const float PreviewAlpha = 0.3f;
        private const float FadeRate = 4f;

        private readonly Image _ghost;
        private float _displayedAlpha;

        private GhostRainbowStamina(Image ghost)
        {
            _ghost = ghost;
        }

        internal bool IsValid => _ghost != null;

        // clone's own alpha at instantiation time is whatever the real one happened to be mid-fade (often 0), so it's ignored. Our own fade drives the clone's alpha from here on
        internal static GhostRainbowStamina Create(Image realRainbow)
        {
            GameObject go = Object.Instantiate(realRainbow.gameObject, realRainbow.transform.parent);
            go.name = realRainbow.name + " (EffectPreview Ghost)";

            Image ghost = go.GetComponent<Image>();
            ghost.enabled = true;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
            return new GhostRainbowStamina(ghost);
        }

        // realActive: the real rainbow is already showing (geniune infinite stamina), skip
        internal void Apply(bool show, bool realActive)
        {
            float target = show && !realActive ? PreviewAlpha : 0f;
            _displayedAlpha = Mathf.MoveTowards(_displayedAlpha, target, Time.deltaTime * FadeRate);

            if (_displayedAlpha <= 0.001f)
            {
                Hide();
                return;
            }

            _ghost.gameObject.SetActive(true);
            Color c = _ghost.color;
            c.a = _displayedAlpha;
            _ghost.color = c;
        }

        internal void Hide()
        {
            _displayedAlpha = 0f;
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(false);
            }
        }
    }
}
