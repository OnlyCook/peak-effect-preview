using Peak;
using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // ghost clone of BackpackWheel.fuelGaugeArrow, shown while hovering the jetpack fuel slice with an item held
    // previews where the needle would land if the item were converted to fuel right now
    internal class GhostJetpackFuelGauge : MonoBehaviour
    {
        private BackpackWheel _wheel;
        private Transform _sourceArrow;
        private RectTransform _ghostRtf;

        private void LateUpdate()
        {
            if (!Plugin.Instance.Cfg.EnablePreview.Value || !TryGetWheel())
            {
                Hide();
                return;
            }

            Common.Safe.Run("GhostJetpackFuelGauge.Refresh", Refresh);
        }

        private bool TryGetWheel()
        {
            GUIManager gui = GUIManager.instance;
            if (gui == null || gui.backpackWheel == null)
            {
                return false;
            }
            _wheel = gui.backpackWheel;
            return true;
        }

        private void Refresh()
        {
            if (!_wheel.gameObject.activeSelf
                || _wheel.backpackType != BackpackSlot.BackpackType.Jetpack
                || _wheel.fuelGaugeArrow == null
                || !_wheel.chosenSlice.IsSome
                || !_wheel.chosenSlice.Value.isJetpackFuelSlice)
            {
                Hide();
                return;
            }

            Character character = Character.localCharacter;
            Item currentItem = character != null ? character.data.currentItem : null;
            if (currentItem == null)
            {
                Hide();
                return;
            }

            if (_ghostRtf == null || _sourceArrow != _wheel.fuelGaugeArrow)
            {
                Build(_wheel.fuelGaugeArrow);
            }

            if (_ghostRtf == null)
            {
                return;
            }

            float currentPercent = 0f;
            ItemInstanceData data = _wheel.backpack.GetItemInstanceData();

            if (data != null && data.TryGetDataEntry<FloatItemData>(DataEntryKey.UseRemainingPercentage, out var value))
            {
                currentPercent = value.Value;
            }

            int addedFuel = Preview.JetpackFuelPreviewCalculator.ComputeAddedFuel(currentItem);
            float newPercent = Mathf.Clamp01(currentPercent + addedFuel / 100f);

            _ghostRtf.gameObject.SetActive(true);
            _ghostRtf.position = _sourceArrow.position;
            _ghostRtf.localRotation = Quaternion.Euler(0f, 0f, 50f - newPercent * 100f);
        }

        private void Build(Transform arrow)
        {
            _sourceArrow = arrow;
            GameObject go = Instantiate(arrow.gameObject, arrow.parent);
            go.name = arrow.name + " (EffectPreview Ghost)";

            foreach (Image image in go.GetComponentsInChildren<Image>(includeInactive: true))
            {
                Color c = Color.Lerp(image.color, Color.white, 0.4f);
                c.a *= 0.65f;
                image.color = c;
            }

            go.transform.localScale = Vector3.one;
            _ghostRtf = go.GetComponent<RectTransform>();
            go.SetActive(false);
        }

        private void Hide()
        {
            if (_ghostRtf != null)
            {
                _ghostRtf.gameObject.SetActive(false);
            }
        }
    }
}
