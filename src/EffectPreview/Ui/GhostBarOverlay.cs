using System.Collections.Generic;
using Peak.Afflictions;
using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    internal class GhostBarOverlay : MonoBehaviour
    {
        private StaminaBar _bar;
        private RectTransform _fullBar;
        private bool _built;

        // mirrors CharacterAfflictions.StatusIsCurable(type, isCurseCurable: false, isPetrifyCurable: false) - what GoToVoidRoutine's ClearAllStatus() actually zeroes out
        private static readonly HashSet<CharacterAfflictions.STATUSTYPE> CurableStatuses = new HashSet<CharacterAfflictions.STATUSTYPE>
        {
            CharacterAfflictions.STATUSTYPE.Injury,
            CharacterAfflictions.STATUSTYPE.Hunger,
            CharacterAfflictions.STATUSTYPE.Cold,
            CharacterAfflictions.STATUSTYPE.Poison,
            CharacterAfflictions.STATUSTYPE.Drowsy,
            CharacterAfflictions.STATUSTYPE.Hot,
            CharacterAfflictions.STATUSTYPE.Spores,
            CharacterAfflictions.STATUSTYPE.Web,
            CharacterAfflictions.STATUSTYPE.FlyTrap
        };

        private readonly Dictionary<CharacterAfflictions.STATUSTYPE, GhostBadge> _statusGhosts = new Dictionary<CharacterAfflictions.STATUSTYPE, GhostBadge>();
        private readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> _dynamicHealBreakdown = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
        private GhostExtraStaminaArea _extraStaminaArea;
        private GhostPetrifyArea _petrifyArea;
        private GhostStaminaArea _staminaArea;
        private GhostRainbowStamina _rainbowArea;
        private GhostInvincibilityShield _shieldArea;
        private BorderWarningBlink _passOutBorderBlink;
        private BorderWarningBlink _petrifyDeathBorderBlink;
        private BarLabel _staminaCountLabel;
        private Color _staminaVanillaForeground;
        private Color _staminaVanillaOutline;

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
                _rainbowArea = null;
                _shieldArea = null;
                _passOutBorderBlink = null;
                _petrifyDeathBorderBlink = null;
                _staminaCountLabel = null;
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
            if (_rainbowArea != null && !_rainbowArea.IsValid)
            {
                return true;
            }
            if (_shieldArea != null && !_shieldArea.IsValid)
            {
                return true;
            }
            if (_passOutBorderBlink != null && !_passOutBorderBlink.IsValid)
            {
                return true;
            }
            if (_petrifyDeathBorderBlink != null && !_petrifyDeathBorderBlink.IsValid)
            {
                return true;
            }
            if (_staminaCountLabel != null && !_staminaCountLabel.IsValid)
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
            // the game's own TMP font/material (moraleBoostText), reused so the bar-count labels read as native UI rather than a mod font
            TMPro.TMP_FontAsset font = _bar.moraleBoostText != null ? _bar.moraleBoostText.font : null;
            UnityEngine.Material fontMaterial = _bar.moraleBoostText != null ? _bar.moraleBoostText.fontSharedMaterial : null;

            foreach (BarAffliction affliction in _bar.afflictions)
            {
                if (affliction == null || affliction.isPetrify || _statusGhosts.ContainsKey(affliction.afflictionType))
                {
                    continue;
                }
                _statusGhosts[affliction.afflictionType] = GhostBadge.Create(affliction, font, fontMaterial);
            }

            if (_extraStaminaArea == null && _bar.extraBar != null && _bar.extraBarStamina != null && _bar.extraBarOutline != null && _bar.extraStaminaIcon != null)
            {
                _extraStaminaArea = new GhostExtraStaminaArea(_bar.extraBar, _bar.extraBarStamina, _bar.extraBarOutline, _bar.extraStaminaIcon, font, fontMaterial);
            }

            if (_petrifyArea == null && _bar.petrifyAffliction != null && _bar.petrifyAffliction.rtf != null)
            {
                _petrifyArea = new GhostPetrifyArea(_bar.petrifyAffliction, font, fontMaterial);
            }

            if (_staminaCountLabel == null && _bar.staminaBar != null)
            {
                _staminaCountLabel = BarLabel.Create(_bar.staminaBar.parent, font, fontMaterial);
                _staminaVanillaForeground = WasteIndicator.SampleFillColor(_bar.staminaBar.gameObject, null);
                _staminaVanillaForeground.a = 1f;
                _staminaVanillaOutline = Common.ColorUtil.Darken(_staminaVanillaForeground);
            }

            if (_staminaArea == null && _bar.maxStaminaBar != null && _bar.staminaBar != null)
            {
                _staminaArea = new GhostStaminaArea(_bar.maxStaminaBar, _bar.staminaBar);
            }

            if (_rainbowArea == null && _bar.rainbowStamina != null)
            {
                _rainbowArea = GhostRainbowStamina.Create(_bar.rainbowStamina);
            }

            if (_shieldArea == null && _bar.shield != null)
            {
                _shieldArea = GhostInvincibilityShield.Create(_bar.shield);
            }

            // staminaBarOutline has no Image of its own, the visible border sprites are its "OutlineImage"/"OutlineCap" children (confirmed via runtime dump)
            if (_passOutBorderBlink == null && _bar.staminaBarOutline != null)
            {
                Transform outlineImage = _bar.staminaBarOutline.Find("OutlineImage");
                Transform outlineCap = _bar.staminaBarOutline.Find("OutlineCap");
                UnityEngine.UI.Image img1 = outlineImage != null ? outlineImage.GetComponent<UnityEngine.UI.Image>() : null;
                UnityEngine.UI.Image img2 = outlineCap != null ? outlineCap.GetComponent<UnityEngine.UI.Image>() : null;
                if (img1 != null && img2 != null)
                {
                    _passOutBorderBlink = new BorderWarningBlink(img1, img2);
                }
                else if (img1 != null)
                {
                    _passOutBorderBlink = new BorderWarningBlink(img1);
                }
            }

            // extraBarOutline (the bonus-stamina/petrify border) carries its own Image directly
            if (_petrifyDeathBorderBlink == null && _bar.extraBarOutline != null)
            {
                UnityEngine.UI.Image img = _bar.extraBarOutline.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    _petrifyDeathBorderBlink = new BorderWarningBlink(img);
                }
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

            _dynamicHealBreakdown.Clear();
            if (preview.HealingGemAction != null)
            {
                Preview.DynamicPetrifyPreview.ComputeHealBreakdown(preview, character, _dynamicHealBreakdown);
            }

            // every waste marker uses this same height regardless of which bar it sits on - the bonus-stamina bar's is the tallest of the bunch
            float unifiedWasteHeight = _bar.extraBarStamina != null ? WasteIndicator.MeasureHeight(_bar.extraBarStamina) : 0f;

            // pass 1: widths only, for every badge; deliberately no per-badge layout rebuild here, see the single rebuild below
            float totalIncrease = 0f;
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, GhostBadge> entry in _statusGhosts)
            {
                GetStatusPreview(character, preview, entry.Key, out float live, out float decrease, out float increase, out _);
                entry.Value.ApplyWidths(fullLocalWidth, live, decrease, increase);

                float shrinkMagnitude = Mathf.Min(decrease, live);
                totalIncrease += Mathf.Max(0f, increase - shrinkMagnitude);
            }

            _staminaArea?.Apply(fullLocalWidth, character.GetMaxStamina(), character.data.currentStamina, totalIncrease);

            // one conslidated rebuild for the whole row (every badge, plus maxStaminaBar at sibling 0) now that every width for this frame is final
            // each badge's own waste markers/labels below read world corners, so they need this settled first
            RectTransform rowParent = null;
            foreach (GhostBadge badge in _statusGhosts.Values)
            {
                rowParent = badge.RowParent;
                break;
            }
            if (rowParent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowParent);
            }

            // pass 2: waste markers + count labels, now that the row-wide rebuild above reflects this frame's final widths for every badge
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, GhostBadge> entry in _statusGhosts)
            {
                GetStatusPreview(character, preview, entry.Key, out float live, out float decrease, out float increase, out float statusCap);
                entry.Value.ApplyOverlays(live, decrease, increase, statusCap, unifiedWasteHeight);
            }

            if (_staminaCountLabel != null)
            {
                if (Plugin.Instance.Cfg.ShowVanillaBarCounts.Value && character.data.currentStamina > 0.0005f)
                {
                    // mirrors GhostStaminaArea's own shrink math - the "after" value this bar would clamp down to once totalIncrease eats into max stamina
                    float projectedMaxStamina = Mathf.Max(0f, character.GetMaxStamina() - totalIncrease);
                    float projectedCurrentStamina = Mathf.Min(character.data.currentStamina, projectedMaxStamina);
                    _staminaCountLabel.ApplyTransition(_bar.staminaBar, character.data.currentStamina, projectedCurrentStamina, _staminaVanillaForeground, _staminaVanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
                }
                else
                {
                    _staminaCountLabel.Hide();
                }
            }

            // mirrors CharacterAfflictions.shouldPassOut (statusSum > 0.99f), but over every status the item touches, not just the ones with a bar badge
            float projectedStatusSum = character.refs.afflictions.statusSum + ProjectedStatusSumIncrease(character, preview);
            bool wouldPassOut = projectedStatusSum > 0.99f;
            _passOutBorderBlink?.Apply(wouldPassOut);

            // mirrors StaminaBar.Update's staminaBarOutline widening (14 + max(1, statusSum) * fullBar width) using the projected sum, so the
            // 100%-mark line actually moves past its resting spot before the overflow cue below has anything to sit past. Only ever widen -
            // StaminaBar.Update already sets/shrinks this every frame off the real (non-projected) statusSum, so never narrow past that.
            if (_bar.staminaBarOutline != null)
            {
                float projectedOutlineWidth = 14f + Mathf.Max(1f, projectedStatusSum) * fullLocalWidth;
                if (projectedOutlineWidth > _bar.staminaBarOutline.sizeDelta.x)
                {
                    _bar.staminaBarOutline.sizeDelta = new Vector2(projectedOutlineWidth, _bar.staminaBarOutline.sizeDelta.y);
                }
            }

            // mirrors StaminaBar.Update's staminaBarOutlineOverflowBar activation (statusSum > 1.005f), the vanilla "bar spills past 100%" cue.
            // Only ever force it on here - StaminaBar.Update already turns it back off every frame off the real (non-projected) statusSum.
            if (projectedStatusSum > 1.005f && _bar.staminaBarOutlineOverflowBar != null)
            {
                _bar.staminaBarOutlineOverflowBar.gameObject.SetActive(true);
            }

            float rawPetrifyDelta = Mathf.Max(0f, preview.PetrifyDelta + Preview.DynamicPetrifyPreview.Compute(preview, character));
            float petrifyRoom = Mathf.Max(0f, 1f - character.data.petrifyAmount * 0.01f);
            float petrifyDelta = Mathf.Min(rawPetrifyDelta, petrifyRoom);

            float currentPetrifyFraction = character.data.petrifyAmount * 0.01f;
            float petrifyShrinkDelta = Mathf.Max(0f, preview.PetrifyReductionOnUse);

            // mirrors CharacterData.shouldPetrify (petrifyAmount >= 100) - petrifyDelta is already clamped to petrifyRoom, so it only ever equals petrifyRoom when the item would fill the bar completely
            bool wouldFullyPetrify = petrifyRoom > 0.0005f && petrifyDelta >= petrifyRoom - 0.0005f;
            _petrifyDeathBorderBlink?.Apply(wouldFullyPetrify);

            bool petrifyActive = _bar.petrifyAffliction != null && _bar.petrifyAffliction.gameObject.activeSelf;

            // petrify first so its just-updated DisplayedDelta (not last frame's) gates the bonus-stamina outline
            _petrifyArea?.Apply(fullLocalWidth, petrifyDelta, rawPetrifyDelta, petrifyShrinkDelta, currentPetrifyFraction, petrifyActive, unifiedWasteHeight);
            bool petrifyGhostVisible = (_petrifyArea?.DisplayedDelta ?? 0f) > 0.002f;

            _extraStaminaArea?.Apply(fullLocalWidth, character.data.extraStamina, preview.ExtraStaminaDelta, character.data.petrifyAmount, petrifyActive, petrifyDelta, petrifyGhostVisible);

            _rainbowArea?.Apply(preview.GrantsInfiniteStaminaOnUse, character.infiniteStam);
            // CharacterData.isInvincible is internal to the game assembly, so this checks the same thing through the public affliction API instead
            bool realInvincible = character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.Invincibility, out _);
            _shieldArea?.Apply(preview.GrantsInvincibilityOnUse, realInvincible);
        }

        // shared live/decrease/increase/cap computation for one status, reused across both GhostBadge passes so they stay in sync
        private void GetStatusPreview(Character character, Preview.ItemPreview preview, CharacterAfflictions.STATUSTYPE type, out float live, out float decrease, out float increase, out float statusCap)
        {
            preview.StatusIncreases.TryGetValue(type, out increase);
            preview.StatusDecreases.TryGetValue(type, out decrease);
            _dynamicHealBreakdown.TryGetValue(type, out float healDecrease);
            decrease += healDecrease;
            live = character.refs.afflictions.GetCurrentStatus(type);
            if (preview.ClearsCurableStatusOnUse && CurableStatuses.Contains(type))
            {
                decrease = live;
            }
            statusCap = character.refs.afflictions.GetStatusCap(type);
        }

        // sums how much the item's status increases would raise statusSum by, over every status it touches (not just ones with a bar badge), net of anything it decreases on the same status
        private float ProjectedStatusSumIncrease(Character character, Preview.ItemPreview preview)
        {
            float total = 0f;
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, float> entry in preview.StatusIncreases)
            {
                CharacterAfflictions.STATUSTYPE type = entry.Key;
                float increase = entry.Value;
                preview.StatusDecreases.TryGetValue(type, out float decrease);
                _dynamicHealBreakdown.TryGetValue(type, out float healDecrease);
                decrease += healDecrease;
                float live = character.refs.afflictions.GetCurrentStatus(type);
                if (preview.ClearsCurableStatusOnUse && CurableStatuses.Contains(type))
                {
                    decrease = live;
                }
                float shrinkMagnitude = Mathf.Min(decrease, live);
                total += Mathf.Max(0f, increase - shrinkMagnitude);
            }
            return total;
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
            _rainbowArea?.Hide();
            _shieldArea?.Hide();
            _passOutBorderBlink?.Hide();
            _petrifyDeathBorderBlink?.Hide();
            _staminaCountLabel?.Hide();
        }
    }
}
