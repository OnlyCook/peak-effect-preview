using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // forces extraBar/extraBarOutline/icon visible+sized from LateUpdate so we always win over StaminaBar.Update(); see RESEARCH.md for the native lerp/masking details this mirrors
    internal class GhostExtraStaminaArea
    {
        private static readonly Vector2 ShownExtraBarSize = new Vector2(45f, 45f);
        private const float OutlinePadding = 12f;
        private const float MinOutlineWidth = 20f;
        private const float ShowDuration = 0.34f;
        private const float HideDuration = 0.27f;
        private const float HiddenThreshold = 0.0001f;

        // hides the doubled-border seam where the ghost clone butts against the real fill
        private const float SeamOverlap = 3f;

        // breathing room from the ghost's outer edge to the outline, differs with/without real fill preceding it
        private const float RightEdgePaddingGhostOnly = 9f;
        private const float RightEdgePaddingWithReal = 1f;

        private const float ShrinkLerpStep100Fps = 0.14f;
        private const float GrowLerpStep100Fps = 0.6f;

        // the tween never quite lands on its target mid-flight (eased curve, or retargeted before completing)
        // so the label snaps to the exact target based on how close the animation is now (mirrors GhostPetrifyArea's DisplayedDeltaSnapEpsilon)
        private const float DisplayedDeltaSnapEpsilon = 0.003f;

        private readonly RectTransform _extraBar;
        private readonly RectTransform _extraBarStamina;
        private readonly RectTransform _extraBarOutline;
        private readonly Image _extraStaminaIcon;
        private readonly GhostSegment _fillGhost;
        private readonly GhostIcon _icon;
        private readonly WasteIndicator _waste;
        private readonly BarLabel _ghostCountLabel;
        private readonly BarLabel _realCountLabel;
        private readonly Color _vanillaForeground;
        private readonly Color _vanillaOutline;
        private readonly Color _ghostForeground;
        private readonly Color _ghostOutline;
        private readonly Transform _fillParent;
        private readonly Vector3[] _cornerBuffer = new Vector3[4];

        private Tween _tween;
        private float _tweenTarget;

        // drives the actual bar/ghost animation, tweens continuously and never snaps
        private float _animatedDelta;

        // drives the count label only, snaps early per DisplayedDeltaSnapEpsilon
        private float _displayedDelta;
        private float _lastRealExtraStamina;

        // manual control of extraBarStamina's width for a petrify cap shrink or a post-consume catch-up; released only once converged to realWidth, see RESEARCH.md (cachedExtraStam)
        private bool _manualWidthControl;
        private float _manualDisplayedWidth;

        // outline lerps toward its target like native does, instead of snapping when petrifyPresentOrPreviewed flips
        private float _displayedOutlineWidth;
        private bool _hasDisplayedOutlineWidth;

        internal GhostExtraStaminaArea(RectTransform extraBar, RectTransform extraBarStamina, RectTransform extraBarOutline, Image extraStaminaIcon, TMP_FontAsset font, Material fontMaterial)
        {
            _extraBar = extraBar;
            _extraBarStamina = extraBarStamina;
            _extraBarOutline = extraBarOutline;
            _extraStaminaIcon = extraStaminaIcon;
            _fillParent = extraBarStamina.parent;
            _fillGhost = GhostSegment.Create(_fillParent, extraBarStamina);
            _icon = extraStaminaIcon != null ? GhostIcon.Create(extraStaminaIcon) : null;

            Color fillColor = WasteIndicator.SampleFillColor(extraBarStamina.gameObject, null);
            _waste = WasteIndicator.Create(_fillParent, fillColor);
            _ghostCountLabel = BarLabel.Create(_fillParent, font, fontMaterial);
            _realCountLabel = BarLabel.Create(_fillParent, font, fontMaterial);
            _vanillaForeground = fillColor;
            _vanillaForeground.a = 1f;
            _vanillaOutline = Common.ColorUtil.Darken(_vanillaForeground);
            _ghostForeground = BarLabel.GhostTint(fillColor);
            _ghostOutline = Common.ColorUtil.Darken(_ghostForeground);
        }

        internal bool IsValid => _extraBar != null && _extraBarStamina != null && _extraBarOutline != null && _fillGhost.IsValid && (_icon == null || _icon.IsValid) && _waste.IsValid
            && _ghostCountLabel.IsValid && _realCountLabel.IsValid;

        // room is capped by petrify (real + previewed), matching AddExtraStamina's own clamp
        internal void Apply(float fullLocalWidth, float realExtraStamina, float delta, int petrifyAmount, bool petrifyActive, float petrifyPreviewDelta, bool petrifyGhostVisible)
        {
            float requestedDelta = delta;

            // real growth this frame (item consumed) - snap the ghost down by the same amount instead of tweening through it, and ignore this frame's stale delta so it doesn't immediately re-trigger a grow tween
            float realGrowth = realExtraStamina - _lastRealExtraStamina;
            if (realGrowth > 0.0001f)
            {
                _animatedDelta = Mathf.Max(0f, _animatedDelta - realGrowth);
                _tween?.Kill();
                _tweenTarget = _animatedDelta;
                delta = 0f;
                _manualWidthControl = true;
                _manualDisplayedWidth = _extraBarStamina.sizeDelta.x;
            }
            _lastRealExtraStamina = realExtraStamina;

            float maxExtraStamina = Mathf.Max(0f, 1f - petrifyAmount * 0.01f);
            float previewedMaxExtraStamina = Mathf.Max(0f, maxExtraStamina - Mathf.Max(0f, petrifyPreviewDelta));
            float room = Mathf.Max(0f, previewedMaxExtraStamina - Mathf.Max(0f, realExtraStamina));
            float targetDelta = delta > 0f ? Mathf.Min(delta, room) : 0f;

            if (!Mathf.Approximately(targetDelta, _tweenTarget))
            {
                bool growing = targetDelta > _animatedDelta;
                _tween?.Kill();
                _tweenTarget = targetDelta;
                _tween = DOTween.To(() => _animatedDelta, x => _animatedDelta = x, targetDelta, growing ? ShowDuration : HideDuration)
                    .SetEase(growing ? Ease.OutCubic : Ease.InCubic);
            }

            _displayedDelta = Mathf.Abs(_animatedDelta - _tweenTarget) < DisplayedDeltaSnapEpsilon ? _tweenTarget : _animatedDelta;

            float realWidth = Mathf.Max(0f, realExtraStamina) * fullLocalWidth;
            float overrideTargetWidth = Mathf.Min(realWidth, previewedMaxExtraStamina * fullLocalWidth);
            bool overridingNow = overrideTargetWidth < realWidth - 0.01f;

            // the width the real fill is settling towards, not its current mid-lerp value - keeps the label's arrow-vs-compact decision stable
            float settledLocalWidth = overridingNow ? overrideTargetWidth : realWidth;

            // petrifyGhostVisible (not just petrifyPreviewDelta>0) keeps the bar open until petrify's own ghost has visually faded, not just until its target hits 0
            if (targetDelta <= 0f && _animatedDelta < HiddenThreshold && !overridingNow && !_manualWidthControl && !petrifyGhostVisible)
            {
                _fillGhost.Hide();
                _icon?.Hide();
                _waste.Hide();
                _ghostCountLabel.Hide();
                ApplyRealCountLabel(realExtraStamina, realExtraStamina, settledLocalWidth);

                // only ours to reset when nothing real is driving the outline either - native grows it on its own for real bonus stamina/petrify
                if (realExtraStamina <= 0f && !petrifyActive)
                {
                    ResetOutlineToResting();
                }
                else
                {
                    _hasDisplayedOutlineWidth = false;
                }
                return;
            }

            _extraBar.gameObject.SetActive(true);
            _extraBar.sizeDelta = ShownExtraBarSize;

            // set ourselves instead of waiting a frame for native's own catch-up, avoiding a stale one-frame icon flash
            bool realStaminaVisible = realWidth > 6.1f;
            _extraBarStamina.gameObject.SetActive(realStaminaVisible);
            if (_extraStaminaIcon != null)
            {
                _extraStaminaIcon.gameObject.SetActive(realStaminaVisible);
            }

            // real icon shows itself natively once real amount is above threshold; ghost stands in only while there's nothing real yet
            if (realExtraStamina > 0f)
            {
                _icon?.Hide();
            }
            else if (targetDelta > 0f)
            {
                _icon?.Show();
            }
            else
            {
                _icon?.Hide();
            }

            if (overridingNow || _manualWidthControl)
            {
                float lerpStep = Common.AnimUtil.LerpStep(ShrinkLerpStep100Fps);
                if (!_manualWidthControl)
                {
                    _manualDisplayedWidth = _extraBarStamina.sizeDelta.x;
                }
                _manualWidthControl = true;

                float manualTarget = overridingNow ? overrideTargetWidth : realWidth;
                _manualDisplayedWidth = Mathf.Lerp(_manualDisplayedWidth, manualTarget, lerpStep);
                _extraBarStamina.sizeDelta = new Vector2(_manualDisplayedWidth, _extraBarStamina.sizeDelta.y);

                if (!overridingNow && Mathf.Abs(_manualDisplayedWidth - realWidth) < 0.5f)
                {
                    _manualWidthControl = false;
                }
            }

            float displayedWidth = _animatedDelta * fullLocalWidth;
            // petrify sits pinned to the bar's right edge, so any petrify (real or previewed) needs the outline at fullLocalWidth to wrap around it
            bool petrifyPresentOrPreviewed = petrifyActive || petrifyGhostVisible;
            float outlineWidth = petrifyPresentOrPreviewed ? fullLocalWidth : realWidth + displayedWidth;
            float outlineTarget = Mathf.Max(MinOutlineWidth, outlineWidth + OutlinePadding);
            if (!_hasDisplayedOutlineWidth)
            {
                _displayedOutlineWidth = _extraBarOutline.sizeDelta.x;
                _hasDisplayedOutlineWidth = true;
            }
            // grows fast so the border always stays ahead of whatever's expanding inside it (fill or petrify's ghost, from either edge); shrinks at the normal smooth pace since a briefly-oversized border is never a clipping risk
            bool outlineGrowing = outlineTarget > _displayedOutlineWidth;
            float outlineLerpStep = outlineGrowing
                ? Common.AnimUtil.LerpStep(GrowLerpStep100Fps)
                : Common.AnimUtil.LerpStep(ShrinkLerpStep100Fps);
            _displayedOutlineWidth = Mathf.Lerp(_displayedOutlineWidth, outlineTarget, outlineLerpStep);
            _extraBarOutline.sizeDelta = new Vector2(_displayedOutlineWidth, _extraBarOutline.sizeDelta.y);

            // world corners, not anchoredPosition/pivot math - see RESEARCH.md
            _extraBarStamina.GetWorldCorners(_cornerBuffer);
            float realRightWorldX = _cornerBuffer[2].x;
            float overlap = Mathf.Min(SeamOverlap, displayedWidth);
            float basePadding = realExtraStamina > 0f ? RightEdgePaddingWithReal : RightEdgePaddingGhostOnly;
            float rightPadding = petrifyPresentOrPreviewed ? 0f : Mathf.Min(basePadding, displayedWidth + overlap);
            _fillGhost.Apply(realRightWorldX - overlap, displayedWidth + overlap - rightPadding);

            // partial waste only: room clamped it below what was actually requested, and at least some of it landed
            bool showWaste = Plugin.Instance.Cfg.EnableWasteIndicator.Value && targetDelta > 0.0005f && Mathf.Max(0f, requestedDelta) - targetDelta > 0.0005f;
            if (showWaste)
            {
                _waste.Apply(_fillGhost.Rtf, _extraBarStamina, WasteIndicator.MeasureHeight(_extraBarStamina), rightEdge: true);
            }
            else
            {
                _waste.Hide();
            }

            if (Plugin.Instance.Cfg.ShowGhostBarCounts.Value && _displayedDelta > 0.0005f)
            {
                _ghostCountLabel.Apply(_fillGhost.Rtf, BarLabel.FormatCount(_displayedDelta), _ghostForeground, _ghostOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
            }
            else
            {
                _ghostCountLabel.Hide();
            }

            // covers both directions in one clamp: an item's own add (targetDelta) growing it, and a previewed petrify gain shrinking previewedMaxExtraStamina
            // out from under the real amount - ApplyTransition itself only surfaces the latter (a decrease), since the former already has its own ghost bar
            float afterExtraStamina = Mathf.Min(realExtraStamina + targetDelta, previewedMaxExtraStamina);
            ApplyRealCountLabel(realExtraStamina, afterExtraStamina, settledLocalWidth);
        }

        // snaps the outline back to resting instead of leaving it stale-expanded for the next reveal to reseed from
        private void ResetOutlineToResting()
        {
            _displayedOutlineWidth = MinOutlineWidth;
            _hasDisplayedOutlineWidth = true;
            _extraBarOutline.sizeDelta = new Vector2(MinOutlineWidth, _extraBarOutline.sizeDelta.y);
        }

        private void ApplyRealCountLabel(float realExtraStamina, float afterExtraStamina, float settledLocalWidth)
        {
            if (Plugin.Instance.Cfg.ShowVanillaBarCounts.Value && realExtraStamina > 0.0005f)
            {
                // world-space via lossyScale.x, so this doesn't need a live (still-animating) GetWorldCorners read
                float predictedFitWidth = settledLocalWidth * _extraBarStamina.lossyScale.x;
                _realCountLabel.ApplyTransition(_extraBarStamina, realExtraStamina, afterExtraStamina, _vanillaForeground, _vanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value, predictedFitWidth);
            }
            else
            {
                _realCountLabel.Hide();
            }
        }

        internal void Hide()
        {
            _tween?.Kill();
            _tween = null;
            _tweenTarget = 0f;
            _animatedDelta = 0f;
            _displayedDelta = 0f;
            _lastRealExtraStamina = 0f;
            _manualWidthControl = false;

            // same don't-fight-native reasoning as the early-hide branch above
            Character character = Character.localCharacter;
            bool realActive = character != null && (character.data.extraStamina > 0f || character.data.petrifyAmount > 0);
            if (realActive)
            {
                _hasDisplayedOutlineWidth = false;
            }
            else
            {
                ResetOutlineToResting();
            }

            _fillGhost.Hide();
            _icon?.Hide();
            _waste.Hide();
            _ghostCountLabel.Hide();
            _realCountLabel.Hide();
        }
    }
}
