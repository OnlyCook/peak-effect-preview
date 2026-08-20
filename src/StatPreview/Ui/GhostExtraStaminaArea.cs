using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace StatPreview.Ui
{
    // forces extraBar/extraBarOutline/icon visible+sized from LateUpdate so we always win over StaminaBar.Update(); see RESEARCH.md for the native lerp/masking details this mirrors
    internal class GhostExtraStaminaArea
    {
        private static readonly Vector2 ShownExtraBarSize = new Vector2(45f, 45f);
        private const float OutlinePadding = 12f;
        private const float MinOutlineWidth = 20f;
        private const float ShowDuration = 0.25f;
        private const float HideDuration = 0.2f;
        private const float HiddenThreshold = 0.0001f;

        // hides the doubled-border seam where the ghost clone butts against the real fill
        private const float SeamOverlap = 3f;

        // breathing room from the ghost's outer edge to the outline, differs with/without real fill preceding it
        private const float RightEdgePaddingGhostOnly = 9f;
        private const float RightEdgePaddingWithReal = 1f;

        private const float ShrinkLerpRate = 10f;
        private const float ShrinkMaxLerpStep = 0.1f;
        private const float GrowLerpRate = 40f;
        private const float GrowMaxLerpStep = 0.5f;

        private readonly RectTransform _extraBar;
        private readonly RectTransform _extraBarStamina;
        private readonly RectTransform _extraBarOutline;
        private readonly GhostSegment _fillGhost;
        private readonly GhostIcon _icon;
        private readonly Transform _fillParent;
        private readonly Vector3[] _cornerBuffer = new Vector3[4];

        private Tween _tween;
        private float _tweenTarget;
        private float _displayedDelta;
        private float _lastRealExtraStamina;

        // manual control of extraBarStamina's width for a petrify cap shrink or a post-consume catch-up; released only once converged to realWidth, see RESEARCH.md (cachedExtraStam)
        private bool _manualWidthControl;
        private float _manualDisplayedWidth;

        // outline lerps toward its target like native does, instead of snapping when petrifyPresentOrPreviewed flips
        private float _displayedOutlineWidth;
        private bool _hasDisplayedOutlineWidth;

        internal GhostExtraStaminaArea(RectTransform extraBar, RectTransform extraBarStamina, RectTransform extraBarOutline, Image extraStaminaIcon)
        {
            _extraBar = extraBar;
            _extraBarStamina = extraBarStamina;
            _extraBarOutline = extraBarOutline;
            _fillParent = extraBarStamina.parent;
            _fillGhost = GhostSegment.Create(_fillParent, extraBarStamina);
            _icon = extraStaminaIcon != null ? GhostIcon.Create(extraStaminaIcon) : null;
        }

        internal bool IsValid => _extraBar != null && _extraBarStamina != null && _extraBarOutline != null && _fillGhost.IsValid && (_icon == null || _icon.IsValid);

        // room is capped by petrify (real + previewed), matching AddExtraStamina's own clamp
        internal void Apply(float fullLocalWidth, float realExtraStamina, float delta, int petrifyAmount, bool petrifyActive, float petrifyPreviewDelta, bool petrifyGhostVisible)
        {
            // real growth this frame (item consumed) - snap the ghost down by the same amount instead of tweening through it, and ignore this frame's stale delta so it doesn't immediately re-trigger a grow tween
            float realGrowth = realExtraStamina - _lastRealExtraStamina;
            if (realGrowth > 0.0001f)
            {
                _displayedDelta = Mathf.Max(0f, _displayedDelta - realGrowth);
                _tween?.Kill();
                _tweenTarget = _displayedDelta;
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
                bool growing = targetDelta > _displayedDelta;
                _tween?.Kill();
                _tweenTarget = targetDelta;
                _tween = DOTween.To(() => _displayedDelta, x => _displayedDelta = x, targetDelta, growing ? ShowDuration : HideDuration)
                    .SetEase(growing ? Ease.OutCubic : Ease.InCubic);
            }

            float realWidth = Mathf.Max(0f, realExtraStamina) * fullLocalWidth;
            float overrideTargetWidth = Mathf.Min(realWidth, previewedMaxExtraStamina * fullLocalWidth);
            bool overridingNow = overrideTargetWidth < realWidth - 0.01f;

            // petrifyGhostVisible (not just petrifyPreviewDelta>0) keeps the bar open until petrify's own ghost has visually faded, not just until its target hits 0
            if (targetDelta <= 0f && _displayedDelta < HiddenThreshold && !overridingNow && !_manualWidthControl && !petrifyGhostVisible)
            {
                _fillGhost.Hide();
                _icon?.Hide();
                _hasDisplayedOutlineWidth = false;
                return;
            }

            _extraBar.gameObject.SetActive(true);
            _extraBar.sizeDelta = ShownExtraBarSize;

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
                float lerpStep = Mathf.Min(Time.deltaTime * ShrinkLerpRate, ShrinkMaxLerpStep);
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

            float displayedWidth = _displayedDelta * fullLocalWidth;
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
                ? Mathf.Min(Time.deltaTime * GrowLerpRate, GrowMaxLerpStep)
                : Mathf.Min(Time.deltaTime * ShrinkLerpRate, ShrinkMaxLerpStep);
            _displayedOutlineWidth = Mathf.Lerp(_displayedOutlineWidth, outlineTarget, outlineLerpStep);
            _extraBarOutline.sizeDelta = new Vector2(_displayedOutlineWidth, _extraBarOutline.sizeDelta.y);

            // world corners, not anchoredPosition/pivot math - see RESEARCH.md
            _extraBarStamina.GetWorldCorners(_cornerBuffer);
            float realRightWorldX = _cornerBuffer[2].x;
            float overlap = Mathf.Min(SeamOverlap, displayedWidth);
            float basePadding = realExtraStamina > 0f ? RightEdgePaddingWithReal : RightEdgePaddingGhostOnly;
            float rightPadding = petrifyPresentOrPreviewed ? 0f : Mathf.Min(basePadding, displayedWidth + overlap);
            _fillGhost.Apply(realRightWorldX - overlap, displayedWidth + overlap - rightPadding);
        }

        internal void Hide()
        {
            _tween?.Kill();
            _tween = null;
            _tweenTarget = 0f;
            _displayedDelta = 0f;
            _lastRealExtraStamina = 0f;
            _manualWidthControl = false;
            _hasDisplayedOutlineWidth = false;
            _fillGhost.Hide();
            _icon?.Hide();
        }
    }
}
