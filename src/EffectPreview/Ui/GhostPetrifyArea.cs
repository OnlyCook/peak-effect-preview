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
        private readonly Vector3[] _cornerBuffer = new Vector3[4];

        private float _displayedDelta;
        private float _timeSinceTargetZero;

        // decrease-ghost overlay width, grows from the real segment's own (fixed) left edge rightward - see Apply
        private float _displayedShrinkWidth;

        internal GhostPetrifyArea(BarAffliction petrifyAffliction)
        {
            _petrifyRtf = petrifyAffliction.rtf;
            _ghost = GhostSegment.Create(_petrifyRtf.parent, _petrifyRtf);
            _decreaseGhost = GhostSegment.Create(_petrifyRtf.parent, _petrifyRtf);
            _icon = petrifyAffliction.icon != null ? GhostIcon.Create(petrifyAffliction.icon, _petrifyRtf.parent) : null;
        }

        internal bool IsValid => _petrifyRtf != null && _ghost.IsValid && _decreaseGhost.IsValid && (_icon == null || _icon.IsValid);

        // lets GhostExtraStaminaArea wait for this to actually fade before reclaiming the space petrify's border expansion held
        internal float DisplayedDelta => _displayedDelta;

        // realPetrifyActive: real segment/icon already shown natively, so only that one should be visible
        // shrinkDelta/currentPetrifyFraction preview a reduction (e.g. Scout's Honor's Nadir warp): a single lerped value (_displayedShrinkWidth) drives both sides at once, so the ghost's growth and the real segment's shrink are exact complements with no gap or overlap between them - the ghost's own left edge stays fixed at the real segment's original (unshrunk) left edge the whole time, so it reads as "eating into" the segment rather than sliding
        internal void Apply(float fullLocalWidth, float delta, float shrinkDelta, float currentPetrifyFraction, bool realPetrifyActive)
        {
            float lerpStep = Mathf.Min(Time.deltaTime * LerpRate, MaxLerpStep);

            float shrinkMagnitude = Mathf.Clamp(shrinkDelta, 0f, currentPetrifyFraction);
            float shrinkTargetWidth = fullLocalWidth * shrinkMagnitude;
            _displayedShrinkWidth = Mathf.Lerp(_displayedShrinkWidth, shrinkTargetWidth, lerpStep);

            if (shrinkMagnitude > HiddenThreshold || _displayedShrinkWidth > 0.5f)
            {
                float fullRealWidth = fullLocalWidth * currentPetrifyFraction;
                float remainingRealWidth = Mathf.Max(0f, fullRealWidth - _displayedShrinkWidth);

                _petrifyRtf.gameObject.SetActive(true);
                _petrifyRtf.sizeDelta = new Vector2(remainingRealWidth, _petrifyRtf.sizeDelta.y);

                // right edge is a fixed pivot-anchored point, unaffected by the sizeDelta change just above, so this stays the original left edge regardless of how far along the shrink is
                _petrifyRtf.GetWorldCorners(_cornerBuffer);
                float fixedRightEdgeWorldX = _cornerBuffer[2].x;
                float originalLeftWorldX = fixedRightEdgeWorldX - fullRealWidth;

                _decreaseGhost.Apply(originalLeftWorldX + _displayedShrinkWidth, _displayedShrinkWidth);
            }
            else
            {
                _displayedShrinkWidth = 0f;
                _decreaseGhost.Hide();
            }

            float targetDelta = delta > 0f ? delta : 0f;
            _displayedDelta = Mathf.Lerp(_displayedDelta, targetDelta, lerpStep);

            _timeSinceTargetZero = targetDelta > 0f ? 0f : _timeSinceTargetZero + Time.deltaTime;

            if (targetDelta <= 0f && _displayedDelta < HiddenThreshold)
            {
                _displayedDelta = 0f;
                _ghost.Hide();
                _icon?.Hide();
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
        }

        internal void Hide()
        {
            _displayedDelta = 0f;
            _timeSinceTargetZero = IconHideDelay;
            _displayedShrinkWidth = 0f;
            _ghost.Hide();
            _decreaseGhost.Hide();
            _icon?.Hide();
        }
    }
}
