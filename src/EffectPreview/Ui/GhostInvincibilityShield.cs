using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // translucent clone of StaminaBar.shield, a plain GameObject (icon + whatever border glow it carries), toggled on/off natively with no color logic of its own to reimplement
    internal class GhostInvincibilityShield
    {
        private readonly GameObject _ghost;

        private GhostInvincibilityShield(GameObject ghost)
        {
            _ghost = ghost;
        }

        internal bool IsValid => _ghost != null;

        internal static GhostInvincibilityShield Create(GameObject realShield)
        {
            GameObject go = Object.Instantiate(realShield, realShield.transform.parent);
            go.name = realShield.name + " (EffectPreview Ghost)";

            foreach (Image image in go.GetComponentsInChildren<Image>(includeInactive: true))
            {
                Color c = Color.Lerp(image.color, Color.white, 0.4f);
                c.a *= 0.65f;
                image.color = c;
            }

            go.transform.SetAsLastSibling();
            Common.GhostOwnershipTag.Attach(go);
            go.SetActive(false);
            return new GhostInvincibilityShield(go);
        }

        // realActive: the real shield is already showing (genuine invincibility), so just skip here
        internal void Apply(bool show, bool realActive)
        {
            _ghost.SetActive(show && !realActive);
        }

        internal void Hide()
        {
            if (_ghost != null)
            {
                _ghost.SetActive(false);
            }
        }
    }
}
