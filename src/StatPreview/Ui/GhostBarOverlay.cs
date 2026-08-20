using System.Collections.Generic;
using UnityEngine;

namespace StatPreview.Ui
{
    internal class GhostBarOverlay : MonoBehaviour
    {
        private StaminaBar _bar;
        private RectTransform _fullBar;
        private bool _built;

        private readonly Dictionary<CharacterAfflictions.STATUSTYPE, GhostBadge> _statusGhosts = new Dictionary<CharacterAfflictions.STATUSTYPE, GhostBadge>();
        private GhostExtraStaminaArea _extraStaminaArea;
        private GhostPetrifyArea _petrifyArea;
        private GhostStaminaArea _staminaArea;

        private void LateUpdate()
        {
            if (!Plugin.Instance.Cfg.EnablePreview.Value || !TryGetBar())
            {
                HideAll();
                return;
            }

            if (_built && IsStale())
            {
                _statusGhosts.Clear();
                _extraStaminaArea = null;
                _petrifyArea = null;
                _staminaArea = null;
                _built = false;
            }

            if (!_built)
            {
                Common.Safe.Run("GhostBarOverlay.Build", Build);
            }

            if (_built)
            {
                Common.Safe.Run("GhostBarOverlay.Refresh", Refresh);
            }
        }

        private bool IsStale()
        {
            foreach (GhostBadge badge in _statusGhosts.Values)
            {
                if (!badge.IsValid)
                {
                    return true;
                }
            }
            if (_extraStaminaArea != null && !_extraStaminaArea.IsValid)
            {
                return true;
            }
            if (_petrifyArea != null && !_petrifyArea.IsValid)
            {
                return true;
            }
            return _staminaArea != null && !_staminaArea.IsValid;
        }

        private bool TryGetBar()
        {
            GUIManager gui = GUIManager.instance;
            if (gui == null || gui.bar == null || gui.bar.fullBar == null || gui.bar.afflictions == null)
            {
                return false;
            }

            _bar = gui.bar;
            _fullBar = _bar.fullBar;
            return true;
        }

        private void Build()
        {
            foreach (BarAffliction affliction in _bar.afflictions)
            {
                if (affliction == null || affliction.isPetrify || _statusGhosts.ContainsKey(affliction.afflictionType))
                {
                    continue;
                }
                _statusGhosts[affliction.afflictionType] = GhostBadge.Create(affliction);
            }

            if (_extraStaminaArea == null && _bar.extraBar != null && _bar.extraBarStamina != null && _bar.extraBarOutline != null && _bar.extraStaminaIcon != null)
            {
                _extraStaminaArea = new GhostExtraStaminaArea(_bar.extraBar, _bar.extraBarStamina, _bar.extraBarOutline, _bar.extraStaminaIcon);
            }

            if (_petrifyArea == null && _bar.petrifyAffliction != null && _bar.petrifyAffliction.rtf != null)
            {
                _petrifyArea = new GhostPetrifyArea(_bar.petrifyAffliction);
            }

            if (_staminaArea == null && _bar.maxStaminaBar != null && _bar.staminaBar != null)
            {
                _staminaArea = new GhostStaminaArea(_bar.maxStaminaBar, _bar.staminaBar);
            }

            _built = true;
        }

        private void Refresh()
        {
            Character character = Character.localCharacter;
            if (character == null)
            {
                HideAll();
                return;
            }

            Preview.ItemPreview preview = Preview.HeldItemPreviewTracker.Instance.Current;
            float fullLocalWidth = _fullBar.sizeDelta.x;

            float totalIncrease = 0f;
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, GhostBadge> entry in _statusGhosts)
            {
                preview.StatusIncreases.TryGetValue(entry.Key, out float increase);
                preview.StatusDecreases.TryGetValue(entry.Key, out float decrease);
                float live = character.refs.afflictions.GetCurrentStatus(entry.Key);
                entry.Value.Apply(fullLocalWidth, live, decrease, increase);

                float shrinkMagnitude = Mathf.Min(decrease, live);
                totalIncrease += Mathf.Max(0f, increase - shrinkMagnitude);
            }

            _staminaArea?.Apply(fullLocalWidth, character.GetMaxStamina(), character.data.currentStamina, totalIncrease);

            float petrifyDelta = preview.PetrifyDelta + Preview.DynamicPetrifyPreview.Compute(preview, character);
            float petrifyRoom = Mathf.Max(0f, 1f - character.data.petrifyAmount * 0.01f);
            petrifyDelta = Mathf.Max(0f, Mathf.Min(petrifyDelta, petrifyRoom));

            bool petrifyActive = _bar.petrifyAffliction != null && _bar.petrifyAffliction.gameObject.activeSelf;

            // petrify first so its just-updated DisplayedDelta (not last frame's) gates the bonus-stamina outline
            _petrifyArea?.Apply(fullLocalWidth, petrifyDelta, petrifyActive);
            bool petrifyGhostVisible = (_petrifyArea?.DisplayedDelta ?? 0f) > 0.002f;

            _extraStaminaArea?.Apply(fullLocalWidth, character.data.extraStamina, preview.ExtraStaminaDelta, character.data.petrifyAmount, petrifyActive, petrifyDelta, petrifyGhostVisible);
        }

        private void HideAll()
        {
            foreach (GhostBadge badge in _statusGhosts.Values)
            {
                badge.Hide();
            }
            _extraStaminaArea?.Hide();
            _petrifyArea?.Hide();
            _staminaArea?.Release();
        }
    }
}
