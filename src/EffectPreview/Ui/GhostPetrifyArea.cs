using TMPro;
using UnityEngine;

namespace EffectPreview.Ui
{
    // petrify's ghost grows leftward from a fixed right edge inside the extraBar hierarchy; see RESEARCH.md for the native lerp/masking details this mirrors
    internal class GhostPetrifyArea
    {
        private const float SeamOverlap = 6f;
        private const float RightEdgePadding = 6f;
        private const float HiddenThreshold = 0.0001f;

        // matches BarAffliction.UpdateAffliction()'s own lerp rate so the ghost reads as the same animation
        private const float LerpRate = 10f;
        private const float MaxLerpStep = 0.1f;

        // icon hides on its own short timer instead of waiting for the bar's asymptotic lerp to cross HiddenThreshold
        private const float IconHideDelay = 0.2f;

        private readonly RectTransform _petrifyRtf;
        private readonly GhostSegment _ghost;
        private readonly GhostSegment _decreaseGhost;
        private readonly GhostIcon _icon;
        private readonly WasteIndicator _increaseWaste;
        private readonly WasteIndicator _decreaseWaste;
        private readonly BarLabel _increaseCountLabel;
        private readonly BarLabel _decreaseCountLabel;
        private readonly BarLabel _realCountLabel;
        private readonly Color _vanillaForeground;
        private readonly Color _vanillaOutline;
        private readonly Color _ghostForeground;
        private readonly Color _ghostOutline;
        private readonly Vector3[] _cornerBuffer = new Vector3[4];

        private float _displayedDelta;
        private float _timeSinceTargetZero;

        // decrease-ghost overlay width, grows from the real segment's own (fixed) left edge rightward - see Apply
        private float _displayedShrinkWidth;

        internal GhostPetrifyArea(BarAffliction petrifyAffliction, TMP_FontAsset font, Material fontMaterial)
        {
            _petrifyRtf = petrifyAffliction.rtf;
            _ghost = GhostSegment.Create(_petrifyRtf.parent, _petrifyRtf);
            _decreaseGhost = GhostSegment.Create(_petrifyRtf.parent, _petrifyRtf, isRemoval: true);
            _icon = petrifyAffliction.icon != null ? GhostIcon.Create(petrifyAffliction.icon, _petrifyRtf.parent) : null;
            Color fillColor = WasteIndicator.SampleFillColor(_petrifyRtf.gameObject, petrifyAffliction.icon);
            _increaseWaste = WasteIndicator.Create(_petrifyRtf.parent, fillColor);
            _decreaseWaste = WasteIndicator.Create(_petrifyRtf.parent, fillColor);
            _increaseCountLabel = BarLabel.Create(_petrifyRtf.parent, font, fontMaterial);
            _decreaseCountLabel = BarLabel.Create(_petrifyRtf.parent, font, fontMaterial);
            _realCountLabel = BarLabel.Create(_petrifyRtf.parent, font, fontMaterial);
            _vanillaForeground = fillColor;
            _vanillaForeground.a = 1f;
            _vanillaOutline = Common.ColorUtil.Darken(_vanillaForeground);
            _ghostForeground = BarLabel.GhostTint(fillColor);
            _ghostOutline = Common.ColorUtil.Darken(_ghostForeground);
        }

        internal bool IsValid => _petrifyRtf != null && _ghost.IsValid && _decreaseGhost.IsValid && (_icon == null || _icon.IsValid) && _increaseWaste.IsValid && _decreaseWaste.IsValid
            && _increaseCountLabel.IsValid && _decreaseCountLabel.IsValid && _realCountLabel.IsValid;

        // lets GhostExtraStaminaArea wait for this to actually fade before reclaiming the space petrify's border expansion held
        internal float DisplayedDelta => _displayedDelta;

        // realPetrifyActive: real segment/icon already shown natively, so only that one should be visible
        // rawDelta: the increase before GhostBarOverlay clamped it to petrifyRoom, used only to detect waste - delta itself stays the clamped value driving the ghost's own width
        // shrinkDelta/currentPetrifyFraction preview a reduction (e.g. Scout's Honor's Nadir warp): a single lerped value (_displayedShrinkWidth) drives both sides at once, so the ghost's growth and the real segment's shrink are exact complements with no gap or overlap between them - the ghost's own left edge stays fixed at the real segment's original (unshrunk) left edge the whole time, so it reads as "eating into" the segment rather than sliding
        // wasteHeight: the shared, unified waste-marker height every area uses (see WasteIndicator.MeasureHeight)
        internal void Apply(float fullLocalWidth, float delta, float rawDelta, float shrinkDelta, float currentPetrifyFraction, bool realPetrifyActive, float wasteHeight)
        {
            float lerpStep = Mathf.Min(Time.deltaTime * LerpRate, MaxLerpStep);

            float shrinkMagnitude = Mathf.Clamp(shrinkDelta, 0f, currentPetrifyFraction);
            float shrinkTargetWidth = fullLocalWidth * shrinkMagnitude;
            _displayedShrinkWidth = Mathf.Lerp(_displayedShrinkWidth, shrinkTargetWidth, lerpStep);

            // what the real (possibly mid-shrink) segment is actually showing right now, for the vanilla count label - not the pre-shrink currentPetrifyFraction
            float remainingPetrifyFraction = currentPetrifyFraction;

            if (shrinkMagnitude > HiddenThreshold || _displayedShrinkWidth > 0.5f)
            {
                float fullRealWidth = fullLocalWidth * currentPetrifyFraction;
                float remainingRealWidth = Mathf.Max(0f, fullRealWidth - _displayedShrinkWidth);
                remainingPetrifyFraction = fullLocalWidth > 0f ? remainingRealWidth / fullLocalWidth : currentPetrifyFraction;

                _petrifyRtf.gameObject.SetActive(true);
                _petrifyRtf.sizeDelta = new Vector2(remainingRealWidth, _petrifyRtf.sizeDelta.y);

                // right edge is a fixed pivot-anchored point, unaffected by the sizeDelta change just above, so this stays the original left edge regardless of how far along the shrink is
                _petrifyRtf.GetWorldCorners(_cornerBuffer);
                float fixedRightEdgeWorldX = _cornerBuffer[2].x;
                float originalLeftWorldX = fixedRightEdgeWorldX - fullRealWidth;

                _decreaseGhost.Apply(originalLeftWorldX + _displayedShrinkWidth, _displayedShrinkWidth);

                bool showDecreaseWaste = Plugin.Instance.Cfg.EnableWasteIndicator.Value && shrinkMagnitude > 0.0005f && shrinkDelta - shrinkMagnitude > 0.0005f;
                if (showDecreaseWaste)
                {
                    _decreaseWaste.Apply(_decreaseGhost.Rtf, _petrifyRtf, wasteHeight, rightEdge: false);
                }
                else
                {
                    _decreaseWaste.Hide();
                }

                if (Plugin.Instance.Cfg.ShowGhostBarCounts.Value && _displayedShrinkWidth > 0.5f)
                {
                    _decreaseCountLabel.Apply(_decreaseGhost.Rtf, BarLabel.FormatCount(shrinkMagnitude), _ghostForeground, _ghostOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
                }
                else
                {
                    _decreaseCountLabel.Hide();
                }
            }
            else
            {
                _displayedShrinkWidth = 0f;
                _decreaseGhost.Hide();
                _decreaseWaste.Hide();
                _decreaseCountLabel.Hide();
            }

            float targetDelta = delta > 0f ? delta : 0f;
            _displayedDelta = Mathf.Lerp(_displayedDelta, targetDelta, lerpStep);

            _timeSinceTargetZero = targetDelta > 0f ? 0f : _timeSinceTargetZero + Time.deltaTime;

            if (targetDelta <= 0f && _displayedDelta < HiddenThreshold)
            {
                _displayedDelta = 0f;
                _ghost.Hide();
                _icon?.Hide();
                _increaseWaste.Hide();
                _increaseCountLabel.Hide();
                ApplyRealCountLabel(remainingPetrifyFraction);
                return;
            }

            // petrifyRtf must be active for the icon (nested under it) to render; native keeps it inactive at 0 petrify
            _petrifyRtf.gameObject.SetActive(true);

            bool iconShouldShow = !realPetrifyActive && (targetDelta > 0f || _timeSinceTargetZero < IconHideDelay);
            if (iconShouldShow)
            {
                _icon?.Show();
            }
            else
            {
                _icon?.Hide();
            }

            float displayedWidth = _displayedDelta * fullLocalWidth;

            _petrifyRtf.GetWorldCorners(_cornerBuffer);
            float realLeftWorldX = _cornerBuffer[0].x;
            float overlap = Mathf.Min(SeamOverlap, displayedWidth);
            float rightPadding = Mathf.Min(RightEdgePadding, displayedWidth + overlap);
            float pivotWorldX = realLeftWorldX + overlap - rightPadding;
            float width = displayedWidth + overlap - rightPadding;
            _ghost.Apply(pivotWorldX, width);

            bool showIncreaseWaste = Plugin.Instance.Cfg.EnableWasteIndicator.Value && targetDelta > 0.0005f && Mathf.Max(0f, rawDelta) - targetDelta > 0.0005f;
            if (showIncreaseWaste)
            {
                _increaseWaste.Apply(_ghost.Rtf, _petrifyRtf, wasteHeight, rightEdge: false);
            }
            else
            {
                _increaseWaste.Hide();
            }

            if (Plugin.Instance.Cfg.ShowGhostBarCounts.Value && _displayedDelta > 0.0005f)
            {
                _increaseCountLabel.Apply(_ghost.Rtf, BarLabel.FormatCount(_displayedDelta), _ghostForeground, _ghostOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
            }
            else
            {
                _increaseCountLabel.Hide();
            }

            ApplyRealCountLabel(currentPetrifyFraction);
        }

        private void ApplyRealCountLabel(float currentPetrifyFraction)
        {
            if (Plugin.Instance.Cfg.ShowVanillaBarCounts.Value && currentPetrifyFraction > 0.0005f)
            {
                _realCountLabel.Apply(_petrifyRtf, BarLabel.FormatCount(currentPetrifyFraction), _vanillaForeground, _vanillaOutline, Plugin.Instance.Cfg.BarCountFontScale.Value);
            }
            else
            {
                _realCountLabel.Hide();
            }
        }

        internal void Hide()
        {
            _displayedDelta = 0f;
            _timeSinceTargetZero = IconHideDelay;
            _displayedShrinkWidth = 0f;
            _ghost.Hide();
            _decreaseGhost.Hide();
            _icon?.Hide();
            _increaseWaste.Hide();
            _decreaseWaste.Hide();
            _increaseCountLabel.Hide();
            _decreaseCountLabel.Hide();
            _realCountLabel.Hide();
        }
    }
}
