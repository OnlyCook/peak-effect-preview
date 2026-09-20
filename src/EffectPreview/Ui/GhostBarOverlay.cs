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
        private readonly HashSet<CharacterAfflictions.STATUSTYPE> _netDeltaVisited = new HashSet<CharacterAfflictions.STATUSTYPE>();
        private GhostExtraStaminaArea _extraStaminaArea;
        private GhostPetrifyArea _petrifyArea;
        private GhostStaminaArea _staminaArea;
        private GhostRainbowStamina _rainbowArea;
        private GhostInvincibilityShield _shieldArea;
        private BorderWarningBlink _passOutBorderBlink;
        private BorderWarningBlink _petrifyDeathBorderBlink;
        private BarLabel _staminaCountLabel;
        private InvincibilityBorderVisual _invincibilityBorderVisual;
        private InfiniteStaminaDurationVisual _infiniteStaminaDurationVisual;
        private BarLabel _invincibilityCountLabel;
        private SpeedBoostDurationLine _speedBoostLine;
        private RectTransform _shieldIconRect;
        private Color _staminaVanillaForeground;
        private Color _staminaVanillaOutline;
        private TMPro.TMP_FontAsset _font;
        private UnityEngine.Material _fontMaterial;
        private readonly Preview.InfiniteStaminaGraceTracker _infiniteStaminaGraceTracker = new Preview.InfiniteStaminaGraceTracker();
        private readonly Preview.InfiniteStaminaUnifiedTimer _infiniteStaminaUnifiedTimer = new Preview.InfiniteStaminaUnifiedTimer();
        private readonly Preview.InfiniteStaminaGraceTracker _speedBoostGraceTracker = new Preview.InfiniteStaminaGraceTracker();

        private void LateUpdate()
        {
            // built/refreshed even while previews are disabled
            if (!TryGetBar())
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
                _invincibilityBorderVisual = null;
                _infiniteStaminaDurationVisual = null;
                _invincibilityCountLabel = null;
                _speedBoostLine = null;
                _shieldIconRect = null;
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
            if (_invincibilityBorderVisual != null && !_invincibilityBorderVisual.IsValid)
            {
                return true;
            }
            if (_infiniteStaminaDurationVisual != null && !_infiniteStaminaDurationVisual.IsValid)
            {
                return true;
            }
            if (_invincibilityCountLabel != null && !_invincibilityCountLabel.IsValid)
            {
                return true;
            }
            if (_speedBoostLine != null && !_speedBoostLine.IsValid)
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
            _font = font;
            _fontMaterial = fontMaterial;

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

            if (_staminaCountLabel == null && _bar.staminaBar != null && font != null)
            {
                _staminaCountLabel = BarLabel.Create(_bar.staminaBar.parent, font, fontMaterial);
                BarLabel.CountColors(WasteIndicator.SampleFillColor(_bar.staminaBar.gameObject, null), false, out _staminaVanillaForeground, out _staminaVanillaOutline);
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
            Transform outlineImageTransform = _bar.staminaBarOutline != null ? _bar.staminaBarOutline.Find("OutlineImage") : null;
            Transform outlineCapTransform = _bar.staminaBarOutline != null ? _bar.staminaBarOutline.Find("OutlineCap") : null;
            RectTransform outlineImageRtf = outlineImageTransform as RectTransform;
            RectTransform outlineCapRtf = outlineCapTransform as RectTransform;

            if (_passOutBorderBlink == null && _bar.staminaBarOutline != null)
            {
                UnityEngine.UI.Image img1 = outlineImageRtf != null ? outlineImageRtf.GetComponent<UnityEngine.UI.Image>() : null;
                UnityEngine.UI.Image img2 = outlineCapRtf != null ? outlineCapRtf.GetComponent<UnityEngine.UI.Image>() : null;
                if (img1 != null && img2 != null)
                {
                    _passOutBorderBlink = new BorderWarningBlink(img1, img2);
                }
                else if (img1 != null)
                {
                    _passOutBorderBlink = new BorderWarningBlink(img1);
                }
            }

            RectTransform shieldRtf = _bar.shield != null ? _bar.shield.GetComponent<RectTransform>() : null;

            if (_invincibilityBorderVisual == null && shieldRtf != null)
            {
                _invincibilityBorderVisual = new InvincibilityBorderVisual(shieldRtf);
            }

            if (_infiniteStaminaDurationVisual == null && _bar.rainbowStamina != null)
            {
                _infiniteStaminaDurationVisual = new InfiniteStaminaDurationVisual(_bar.rainbowStamina.rectTransform);
            }

            if (_speedBoostLine == null && _bar.staminaBarOutline != null && _bar.extraBarOutline != null && _bar.extraBarOutline.parent != null)
            {
                _speedBoostLine = SpeedBoostDurationLine.Create(_bar.staminaBarOutline, _bar.extraBarOutline, FindUnmaskedAncestorParent(_bar.extraBar != null ? _bar.extraBar.transform : _bar.extraBarOutline), font, fontMaterial);
            }

            // _invincibilityCountLabel itself isn't created here (see note on its lazy creation in Refresh())
            _shieldIconRect = shieldRtf;

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
            // observedCharacter, not localCharacter
            // while spectating, GUIManager.bar itself renders specCharacter's data 
            // (native code reads through the same property), so any ghost overlay math driven off localCharacter
            // fights the real bar instead of tracking whoever it's actually showing
            Character character = Character.observedCharacter;
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

            // pass 1: widths only, for every badge; deliberately no per-badge layout rebuild here, see the single rebuild below.
            // also sums each badge's target row width, used below to cap the stamina shrink - see RESEARCH.md
            float totalRowWidth = 0f;
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, GhostBadge> entry in _statusGhosts)
            {
                GetStatusPreview(character, preview, entry.Key, out float live, out float decrease, out float increase, out _);
                entry.Value.ApplyWidths(fullLocalWidth, live, decrease, increase);
                totalRowWidth += entry.Value.GetTargetRowWidth(fullLocalWidth, live, decrease, increase);
            }

            // true net change in statusSum this item would cause, see ComputeNetStatusSumDelta
            float netStatusSumDelta = ComputeNetStatusSumDelta(character, preview);
            float maxAllowedWidthPx = Mathf.Max(0f, fullLocalWidth - totalRowWidth);

            // the shrink-ghost visual only ever shrinks, never grows back, so it only wants the positive half of the delta
            _staminaArea?.Apply(fullLocalWidth, character.GetMaxStamina(), character.data.currentStamina, Mathf.Max(0f, netStatusSumDelta), maxAllowedWidthPx);

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

                float removalCap = 0f;
                bool capPreviewEnabled = Plugin.Instance.Cfg.EnableTimedUsagePreview.Value;
                if (capPreviewEnabled)
                {
                    preview.StatusRemovalCaps.TryGetValue(entry.Key, out removalCap);
                }
                bool decreaseActive = Mathf.Min(decrease, live) > 0f;
                entry.Value.ApplyRemovalCap(live, capPreviewEnabled ? removalCap : 0f, decreaseActive);
            }

            bool showSpecialCounts = Plugin.Instance.Cfg.ShowSpecialStatusCounts.Value;
            bool showSpecialDurationVisual = Plugin.Instance.Cfg.ShowSpecialStatusDurationVisual.Value;

            // shared by the label below and the rainbow-shrink visual further down
            if (!character.infiniteStam)
            {
                _infiniteStaminaGraceTracker.Reset();
                _infiniteStaminaUnifiedTimer.Reset();
            }

            bool infStamHasData;
            float infStamRemainingSeconds = 0f;
            float infStamRemainingFraction = 0f;
            string infStamGraceSuffix = null;

            // read even while Radiate is active
            // it may have been separately extended
            bool directActive = Preview.SpecialStatusDuration.TryGetDirectInfiniteStaminaAffliction(character, out Affliction_InfiniteStamina directInfStam);
            float directRemaining = directActive ? Mathf.Max(0f, directInfStam.totalTime - directInfStam.timeElapsed) : 0f;

            bool radiateActive = Preview.SpecialStatusDuration.TryGetRadiateInfiniteStamAffliction(character, out Affliction_RadiateInfiniteStam radiateAffliction);
            float unifiedRemaining = _infiniteStaminaUnifiedTimer.Tick(radiateActive, radiateActive ? radiateAffliction.totalTime : 0f, radiateActive ? radiateAffliction.timeElapsed : 0f, directActive, directRemaining);

            if (unifiedRemaining >= 0f)
            {
                infStamHasData = true;
                infStamRemainingSeconds = unifiedRemaining;
                infStamRemainingFraction = _infiniteStaminaUnifiedTimer.TotalDuration > 0f ? Mathf.Clamp01(unifiedRemaining / _infiniteStaminaUnifiedTimer.TotalDuration) : 0f;
                _infiniteStaminaGraceTracker.Reset();
            }
            else if (directActive)
            {
                infStamHasData = true;
                infStamRemainingSeconds = directRemaining;
                infStamRemainingFraction = directInfStam.totalTime > 0f ? Mathf.Clamp01(directRemaining / directInfStam.totalTime) : 0f;

                if (_infiniteStaminaGraceTracker.TryGetRemainingGrace(directInfStam, directInfStam.climbDelay, out float graceRemaining))
                {
                    infStamGraceSuffix = "+" + Mathf.CeilToInt(graceRemaining);
                }
            }
            else
            {
                infStamHasData = false;
                _infiniteStaminaGraceTracker.Reset();
            }


            if (_staminaCountLabel != null)
            {
                if (character.infiniteStam && infStamHasData && showSpecialCounts)
                {
                    string secondsText = "(" + Mathf.CeilToInt(infStamRemainingSeconds) + "s" + infStamGraceSuffix + ")";
                    string content = Plugin.Instance.Cfg.ShowVanillaBarCounts.Value ? ("∞ " + secondsText) : secondsText;
                    _staminaCountLabel.Apply(_bar.maxStaminaBar, content, _staminaVanillaForeground, _staminaVanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
                }
                else if (character.infiniteStam && !infStamHasData && showSpecialCounts)
                {
                    // freeze, same as the overlay below
                }
                else if (!Plugin.Instance.Cfg.ShowVanillaBarCounts.Value)
                {
                    _staminaCountLabel.Hide();
                }
                else if (character.infiniteStam)
                {
                    // CharacterData.currentStamina's setter silently blocks any decrease while infiniteStam is active, so
                    // natural regen just keeps adding to it forever with nothing to clamp it back down - staminaBar's own
                    // width is driven straight off that runaway value (see StaminaBar.Update), so anchoring the label to it
                    // drags the label along as it grows arbitrarily far past the visible bar. maxStaminaBar isn't affected
                    // by that quirk (it's driven by GetMaxStamina(), a normal derived value), so anchor to that instead
                    _staminaCountLabel.Apply(_bar.maxStaminaBar, "∞", _staminaVanillaForeground, _staminaVanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
                }
                else if (character.data.currentStamina > 0.0005f)
                {
                    // clamped for display only, not written back - infiniteStam leaves currentStamina able to sit far above
                    // GetMaxStamina() for a frame right after it wears off, see RESEARCH.md
                    float maxStamina = character.GetMaxStamina();
                    float displayedCurrentStamina = Mathf.Min(character.data.currentStamina, maxStamina);

                    // the real, signed delta (not just its positive half like GhostStaminaArea's visual) - see RESEARCH.md
                    float projectedMaxStamina = Mathf.Max(0f, maxStamina - netStatusSumDelta);
                    float projectedCurrentStamina = Mathf.Min(displayedCurrentStamina, projectedMaxStamina);

                    // staminaBar's own rect is still lerping toward the stale (pre-clamp) currentStamina for a few frames right after infiniteStam ends
                    // since the clamp only lands on the next FixedUpdate regen tick, not synchronously;
                    // anchor to maxStaminaBar instead while thats happening, it isn't affected by the same lag
                    // and matches the clamped display value exactly in that state (weird but works)
                    RectTransform positionTarget = character.data.currentStamina > maxStamina ? _bar.maxStaminaBar : _bar.staminaBar;
                    _staminaCountLabel.ApplyTransition(positionTarget, displayedCurrentStamina, projectedCurrentStamina, _staminaVanillaForeground, _staminaVanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
                }
                else
                {
                    _staminaCountLabel.Hide();
                }
            }

            // mirrors CharacterAfflictions.shouldPassOut (statusSum > 0.99f), using the same true net delta as the stamina label above
            float projectedStatusSum = character.refs.afflictions.statusSum + netStatusSumDelta;
            bool wouldPassOut = projectedStatusSum > 0.99f;
            _passOutBorderBlink?.Apply(wouldPassOut);

            // mirrors StaminaBar.Update's staminaBarOutline widening (14 + max(1, statusSum) * fullBar width) using the projected sum, so the
            // 100%-mark line actually moves past its resting spot before the overflow cue below has anything to sit past. Only ever widen -
            // StaminaBar.Update already sets/shrinks this every frame off the real (non-projected) statusSum, so never narrow past that.

            // also widened to fit totalRowWidth, (tried to) explain in RESEARCH.md's badge row overflow note
            if (_bar.staminaBarOutline != null)
            {
                float projectedOutlineWidth = Mathf.Max(14f + Mathf.Max(1f, projectedStatusSum) * fullLocalWidth, 14f + Mathf.Max(fullLocalWidth, totalRowWidth));
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

            // the bonus-stamina cap is fed Petrify's own animated DisplayedDelta (not the raw instant target) so the room it opens/closes each frame
            // exactly matches what petrify's ghost is actually showing right then
            float animatedPetrifyPreviewDelta = _petrifyArea?.DisplayedDelta ?? petrifyDelta;
            _extraStaminaArea?.Apply(fullLocalWidth, character.data.extraStamina, preview.ExtraStaminaDelta, character.data.petrifyAmount, petrifyActive, animatedPetrifyPreviewDelta, petrifyGhostVisible);

            _rainbowArea?.Apply(preview.GrantsInfiniteStaminaOnUse, character.infiniteStam);
            // CharacterData.isInvincible is internal to the game assembly, so this checks the same thing through the public affliction API instead
            bool realInvincible = character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.Invincibility, out _);
            _shieldArea?.Apply(preview.GrantsInvincibilityOnUse, realInvincible);

            // shield's own active flag, not realInvincible, avoids a one-frame race with native's own StaminaBar.Update()
            bool shieldActive = _bar.shield != null && _bar.shield.gameObject.activeSelf;

            bool hasInvincibilityDuration = Preview.SpecialStatusDuration.TryGetInvincibilityRemaining(character, out float invincibilitySeconds, out _) && showSpecialCounts;
            if (shieldActive && hasInvincibilityDuration && _shieldIconRect != null)
            {
                // lazy: shield sits inactive when not invincible, and a fresh component under an inactive hierarchy defers Awake()
                if (_invincibilityCountLabel == null && _font != null)
                {
                    // parented above shield's masking ancestor or the label gets silently clipped
                    _invincibilityCountLabel = BarLabel.Create(FindUnmaskedAncestorParent(_shieldIconRect), _font, _fontMaterial);
                }
                _invincibilityCountLabel?.Apply(_shieldIconRect, Mathf.CeilToInt(invincibilitySeconds).ToString(), _staminaVanillaForeground, _staminaVanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value, extraVerticalOffset: 3f);
            }
            else if (!shieldActive)
            {
                _invincibilityCountLabel?.Hide();
            }
            // else: freeze

            bool hasInvincibilityFraction = Preview.SpecialStatusDuration.TryGetInvincibilityRemaining(character, out _, out float invincibilityFraction) && showSpecialDurationVisual;
            if (shieldActive && hasInvincibilityFraction)
            {
                _invincibilityBorderVisual?.Apply(invincibilityFraction);
            }
            else if (!shieldActive)
            {
                _invincibilityBorderVisual?.Hide();
            }
            // else: freeze

            bool hasInfiniteStaminaFraction = infStamHasData && showSpecialDurationVisual;
            if (!character.infiniteStam)
            {
                _infiniteStaminaDurationVisual?.Hide();
            }
            else if (hasInfiniteStaminaFraction)
            {
                _infiniteStaminaDurationVisual?.Apply(infStamRemainingFraction);
            }
            // else: freeze

            RefreshSpeedBoost(character, showSpecialCounts, showSpecialDurationVisual);
        }

        private void RefreshSpeedBoost(Character character, bool showCounts, bool showVisual)
        {
            if (_speedBoostLine == null)
            {
                return;
            }

            if ((!showCounts && !showVisual) || !Preview.SpecialStatusDuration.TryGetSpeedBoostAffliction(character, out Affliction_FasterBoi boost) || boost.totalTime <= 0f)
            {
                _speedBoostGraceTracker.Reset();
                _speedBoostLine.Hide();
                return;
            }

            float remaining = Mathf.Max(0f, boost.totalTime - boost.timeElapsed);
            string text = Mathf.CeilToInt(remaining) + "s";
            if (_speedBoostGraceTracker.TryGetRemainingGrace(boost, boost.climbDelay, out float graceRemaining))
            {
                text += "+" + Mathf.CeilToInt(graceRemaining);
            }
            _speedBoostLine.Apply(remaining / boost.totalTime, text, showVisual, showCounts, Plugin.Instance.Cfg.BarCountFontScale.Value);
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

        // signed net statusSum change over every touched status, not floored at zero per type - a pure decrease (Hunger,
        // Weight) genuinely offsets an increase (Drowsy) elsewhere instead of being discarded, see RESEARCH.md
        private float ComputeNetStatusSumDelta(Character character, Preview.ItemPreview preview)
        {
            float total = 0f;
            HashSet<CharacterAfflictions.STATUSTYPE> visited = _netDeltaVisited;
            visited.Clear();
            foreach (CharacterAfflictions.STATUSTYPE type in preview.StatusIncreases.Keys)
            {
                if (visited.Add(type))
                {
                    total += NetStatusDelta(character, preview, type);
                }
            }
            foreach (CharacterAfflictions.STATUSTYPE type in preview.StatusDecreases.Keys)
            {
                if (visited.Add(type))
                {
                    total += NetStatusDelta(character, preview, type);
                }
            }
            return total;
        }

        private float NetStatusDelta(Character character, Preview.ItemPreview preview, CharacterAfflictions.STATUSTYPE type)
        {
            GetStatusPreview(character, preview, type, out float live, out float decrease, out float increase, out _);
            float shrinkMagnitude = Mathf.Min(decrease, live);
            return increase - shrinkMagnitude;
        }

        // returns the parent of the topmost Mask/RectMask2D ancestor in the chain, or start's own parent if none
        private static Transform FindUnmaskedAncestorParent(Transform start)
        {
            Transform result = start.parent;
            Transform current = start;
            while (current != null)
            {
                if (current.GetComponent<Mask>() != null || current.GetComponent<RectMask2D>() != null)
                {
                    result = current.parent;
                }
                current = current.parent;
            }
            return result;
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
            _invincibilityBorderVisual?.Hide();
            _infiniteStaminaDurationVisual?.Hide();
            _invincibilityCountLabel?.Hide();
            _speedBoostLine?.Hide();
            _speedBoostGraceTracker.Reset();
            _infiniteStaminaGraceTracker.Reset();
            _infiniteStaminaUnifiedTimer.Reset();
        }
    }
}
