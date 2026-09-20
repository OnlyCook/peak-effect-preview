using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // splits a real BarAffliction badge into a shrunk real part plus decrease/increase ghost siblings, shown independently rather than netted together
    internal class GhostBadge
    {
        private const float LerpStep100Fps = 0.1f;
        private static readonly AccessTools.FieldRef<ThornOnMe, float> ThornPopOutTime = AccessTools.FieldRefAccess<ThornOnMe, float>("popOutTime");

        private readonly RectTransform _realRtf;
        private readonly GameObject _realIcon;
        private readonly Strip _decreaseGhost;
        private readonly Strip _increaseGhost;
        private readonly WasteIndicator _decreaseWaste;
        private readonly WasteIndicator _increaseWaste;
        private readonly BarLabel _decreaseCountLabel;
        private readonly BarLabel _increaseCountLabel;
        private readonly BarLabel _realCountLabel;
        private readonly BarLabel _countdownLabel;
        private readonly GhostStatusCapIcon _capIcon;
        private readonly Color _vanillaForeground;
        private readonly Color _vanillaOutline;
        private readonly Color _ghostForeground;
        private readonly Color _ghostOutline;
        private readonly CharacterAfflictions.STATUSTYPE _statusType;
        private bool _realShrinking;
        private float _realDisplayedWidth;

        private GhostBadge(RectTransform realRtf, GameObject realIcon, Strip decreaseGhost, Strip increaseGhost, WasteIndicator decreaseWaste, WasteIndicator increaseWaste,
            BarLabel decreaseCountLabel, BarLabel increaseCountLabel, BarLabel realCountLabel, BarLabel countdownLabel, GhostStatusCapIcon capIcon, CharacterAfflictions.STATUSTYPE statusType)
        {
            _statusType = statusType;
            _realRtf = realRtf;
            _realIcon = realIcon;
            _decreaseGhost = decreaseGhost;
            _increaseGhost = increaseGhost;
            _decreaseWaste = decreaseWaste;
            _increaseWaste = increaseWaste;
            _decreaseCountLabel = decreaseCountLabel;
            _increaseCountLabel = increaseCountLabel;
            _realCountLabel = realCountLabel;
            _countdownLabel = countdownLabel;
            _capIcon = capIcon;

            if (statusType == CharacterAfflictions.STATUSTYPE.Curse)
            {
                BarLabel.PaletteCountColors(BarLabel.CurseText, BarLabel.CurseOutline, false, out _vanillaForeground, out _vanillaOutline);
                BarLabel.PaletteCountColors(BarLabel.CurseText, BarLabel.CurseOutline, true, out _ghostForeground, out _ghostOutline);
            }
            else
            {
                BarLabel.CountColors(decreaseGhost.FillColor, false, out _vanillaForeground, out _vanillaOutline);
                BarLabel.CountColors(decreaseGhost.FillColor, true, out _ghostForeground, out _ghostOutline);
            }
        }

        internal bool IsValid => _realRtf != null && _decreaseGhost.IsValid && _increaseGhost.IsValid && _decreaseWaste.IsValid && _increaseWaste.IsValid
            && _decreaseCountLabel.IsValid && _increaseCountLabel.IsValid && _realCountLabel.IsValid && _countdownLabel.IsValid && (_capIcon == null || _capIcon.IsValid);

        internal static GhostBadge Create(BarAffliction realAffliction, TMP_FontAsset font, Material fontMaterial, Transform countdownParent)
        {
            RectTransform realBadge = realAffliction.rtf;
            Strip decreaseGhost = Strip.CloneFrom(realBadge, realBadge, isRemoval: true);
            Strip increaseGhost = Strip.CloneFrom(realBadge, decreaseGhost.Rtf);
            WasteIndicator decreaseWaste = WasteIndicator.Create(realBadge.parent, decreaseGhost.FillColor);
            WasteIndicator increaseWaste = WasteIndicator.Create(realBadge.parent, increaseGhost.FillColor);
            BarLabel decreaseCountLabel = BarLabel.Create(realBadge.parent, font, fontMaterial);
            BarLabel increaseCountLabel = BarLabel.Create(realBadge.parent, font, fontMaterial);
            BarLabel realCountLabel = BarLabel.Create(realBadge.parent, font, fontMaterial);
            BarLabel countdownLabel = BarLabel.Create(countdownParent, font, fontMaterial, shadow: true);
            GhostStatusCapIcon capIcon = GhostStatusCapIcon.Create(realAffliction.icon);

            GameObject realIcon = realAffliction.icon != null ? realAffliction.icon.gameObject : null;
            return new GhostBadge(realBadge, realIcon, decreaseGhost, increaseGhost, decreaseWaste, increaseWaste, decreaseCountLabel, increaseCountLabel, realCountLabel, countdownLabel, capIcon, realAffliction.afflictionType);
        }

        // decreaseActive: this frame's normal ghost-bar decrease (ApplyWidths) is already controlling the real icon's
        // visibility for this status, so the two systems don't fight over the same icon
        internal void ApplyRemovalCap(float liveValue, float cap, bool decreaseActive)
        {
            if (_capIcon == null)
            {
                return;
            }
            if (decreaseActive || cap <= 0f || liveValue <= 0f)
            {
                _capIcon.Hide();
                return;
            }
            _capIcon.Apply(fullyRemovable: liveValue <= cap);
        }

        // the row this badge lives in (shared by every affliction badge, and by maxStaminaBar at sibling 0) - callers use this to do exactly
        // one LayoutRebuilder pass per frame for the whole row, instead of each badge separately rebuilding against whatever partially-updated
        // state its neighbors (including maxStaminaBar, resized by GhostStaminaArea) happen to be in at that point in the loop
        internal RectTransform RowParent => _realRtf.parent as RectTransform;

        // updates real/ghost bar widths only - no layout rebuild and no waste/label positioning here, since those read world corners and need
        // the WHOLE row (every badge, plus maxStaminaBar) to have already settled this frame's widths first; see ApplyOverlays
        internal void ApplyWidths(float fullLocalWidth, float liveValue, float decreaseAmount, float increaseAmount)
        {
            float lerpStep = Common.AnimUtil.LerpStep(LerpStep100Fps);
            float shrinkMagnitude = Mathf.Min(decreaseAmount, liveValue);

            if (shrinkMagnitude > 0f)
            {
                float remaining = Mathf.Max(0f, liveValue - shrinkMagnitude);
                float targetWidth = fullLocalWidth * remaining;

                if (!_realShrinking)
                {
                    _realDisplayedWidth = _realRtf.sizeDelta.x;
                }
                _realShrinking = true;

                _realDisplayedWidth = Mathf.Lerp(_realDisplayedWidth, targetWidth, lerpStep);
                _realRtf.sizeDelta = new Vector2(_realDisplayedWidth, _realRtf.sizeDelta.y);

                if (_realIcon != null)
                {
                    _realIcon.SetActive(remaining > 0.001f);
                }
            }
            else if (_realShrinking)
            {
                _realShrinking = false;
                if (_realIcon != null)
                {
                    _realIcon.SetActive(true);
                }
            }

            // only the pure removal and the pure increase beyond it need their own separate room
            float overlap = Mathf.Min(shrinkMagnitude, increaseAmount);
            _decreaseGhost.Apply(fullLocalWidth * (shrinkMagnitude - overlap), lerpStep);
            _increaseGhost.Apply(fullLocalWidth * increaseAmount, lerpStep);
        }

        // sum of every row element's settled target width, not its current mid-lerp width, see RESEARCH.md
        internal float GetTargetRowWidth(float fullLocalWidth, float liveValue, float decreaseAmount, float increaseAmount)
        {
            float shrinkMagnitude = Mathf.Min(decreaseAmount, liveValue);
            float realWidth;
            if (shrinkMagnitude > 0f)
            {
                float remaining = Mathf.Max(0f, liveValue - shrinkMagnitude);
                realWidth = fullLocalWidth * remaining;
            }
            else
            {
                // not touched by us this frame - whatever's currently there is vanilla's own (already-settled) width
                realWidth = _realRtf.gameObject.activeSelf ? _realRtf.sizeDelta.x : 0f;
            }

            float overlap = Mathf.Min(shrinkMagnitude, increaseAmount);
            float decreaseTargetWidth = fullLocalWidth * (shrinkMagnitude - overlap);
            float increaseTargetWidth = fullLocalWidth * increaseAmount;
            return realWidth + decreaseTargetWidth + increaseTargetWidth;
        }

        // waste markers and count labels read world corners, so this must run only after the row-wide layout rebuild (see GhostBarOverlay)
        // has settled this frame's positions for every badge and maxStaminaBar alike
        // statusCap: CharacterAfflictions.GetStatusCap(type) - most statuses cap at 200%, Injury caps at 100%
        // wasteHeight: the shared, unified waste-marker height every area uses (see WasteIndicator.MeasureHeight)
        internal void ApplyOverlays(float liveValue, float decreaseAmount, float increaseAmount, float statusCap, float wasteHeight)
        {
            float shrinkMagnitude = Mathf.Min(decreaseAmount, liveValue);

            // partial waste only: something has to actually land for there to be a "the rest didn't fit" cue - fully-wasted (nothing applied) shows nothing
            bool wasteEnabled = Plugin.Instance.Cfg.EnableWasteIndicator.Value;
            bool showDecreaseWaste = wasteEnabled && shrinkMagnitude > 0.0005f && decreaseAmount - shrinkMagnitude > 0.0005f;
            if (showDecreaseWaste)
            {
                _decreaseWaste.Apply(_decreaseGhost.Rtf, _realRtf, wasteHeight, rightEdge: false);
            }
            else
            {
                _decreaseWaste.Hide();
            }

            float postDecreaseLive = Mathf.Max(0f, liveValue - shrinkMagnitude);
            float increaseRoom = Mathf.Max(0f, statusCap - postDecreaseLive);
            float appliedIncrease = Mathf.Min(increaseAmount, increaseRoom);
            bool showIncreaseWaste = wasteEnabled && appliedIncrease > 0.0005f && increaseAmount - appliedIncrease > 0.0005f;
            if (showIncreaseWaste)
            {
                _increaseWaste.Apply(_increaseGhost.Rtf, _realRtf, wasteHeight, rightEdge: false);
            }
            else
            {
                _increaseWaste.Hide();
            }

            float fontScale = Plugin.Instance.Cfg.BarCountFontScale.Value;
            if (Plugin.Instance.Cfg.ShowGhostBarCounts.Value)
            {
                // matches ApplyWidths' own overlap subtraction, so the label reads the same number as the strip's actual rendered width
                // (e.g. Energy Drink: -50 Drowsy clamped to 32 live, then +25 back - the decrease strip (and its label) only ever covers the net 7 that doesn't come back)
                float overlap = Mathf.Min(shrinkMagnitude, increaseAmount);
                float decreaseLabelAmount = shrinkMagnitude - overlap;
                _decreaseCountLabel.Apply(_decreaseGhost.Rtf, decreaseLabelAmount > 0.0005f ? BarLabel.FormatCount(decreaseLabelAmount) : null, _ghostForeground, _ghostOutline, fontScale);
                _increaseCountLabel.Apply(_increaseGhost.Rtf, increaseAmount > 0.0005f ? BarLabel.FormatCount(increaseAmount) : null, _ghostForeground, _ghostOutline, fontScale);
            }
            else
            {
                _decreaseCountLabel.Hide();
                _increaseCountLabel.Hide();
            }

            bool showCount = Plugin.Instance.Cfg.ShowVanillaBarCounts.Value;
            bool showCountdown = Plugin.Instance.Cfg.ShowAfflictionCountdowns.Value;
            bool live = postDecreaseLive > 0.0005f;
            if (showCount && live)
            {
                _realCountLabel.Apply(_realRtf, BarLabel.FormatCount(postDecreaseLive), _vanillaForeground, _vanillaOutline, fontScale);
            }
            else
            {
                _realCountLabel.Hide();
            }

            string countdown = showCountdown && live ? FormatCountdown(postDecreaseLive) : null;
            if (countdown != null)
            {
                _countdownLabel.ApplyCountdown(_realRtf, countdown, _vanillaForeground, _vanillaOutline);
            }
            else
            {
                _countdownLabel.Hide();
            }
        }

        private string FormatCountdown(float value)
        {
            CharacterAfflictions afflictions = Character.localCharacter?.refs.afflictions;
            if (afflictions == null)
            {
                return null;
            }

            if (_statusType == CharacterAfflictions.STATUSTYPE.Thorns)
            {
                float latest = 0f;
                for (int i = 0; i < afflictions.physicalThorns.Count; i++)
                {
                    ThornOnMe thorn = afflictions.physicalThorns[i];
                    if (thorn != null && thorn.stuckIn && thorn.isThorn && thorn.popsOutAutomatically)
                    {
                        latest = Mathf.Max(latest, ThornPopOutTime(thorn));
                    }
                }
                float remaining = latest - Time.time;
                return remaining > 0f ? "(" + Mathf.CeilToInt(remaining) + "s)" : null;
            }

            float perSecond;
            float cooldown;
            switch (_statusType)
            {
                case CharacterAfflictions.STATUSTYPE.Poison:
                    perSecond = afflictions.poisonReductionPerSecond;
                    cooldown = afflictions.poisonReductionCooldown;
                    break;
                case CharacterAfflictions.STATUSTYPE.Drowsy:
                    perSecond = afflictions.drowsyReductionPerSecond;
                    cooldown = afflictions.drowsyReductionCooldown;
                    break;
                case CharacterAfflictions.STATUSTYPE.Hot:
                    perSecond = afflictions.hotReductionPerSecond;
                    cooldown = afflictions.hotReductionCooldown;
                    break;
                case CharacterAfflictions.STATUSTYPE.Spores:
                    perSecond = afflictions.sporesReductionPerSecond;
                    cooldown = afflictions.sporesReductionCooldown;
                    break;
                default:
                    return null;
            }
            if (perSecond <= 0f)
            {
                return null;
            }

            float wait = Mathf.Max(0f, cooldown - (Time.time - afflictions.LastAddedStatus(_statusType)));
            int decaySeconds = Mathf.CeilToInt(value / perSecond - 0.0005f);
            return wait > 0f ? "(" + decaySeconds + "s+" + Mathf.CeilToInt(wait) + ")" : "(" + decaySeconds + "s)";
        }

        internal void Hide()
        {
            _realShrinking = false;
            _decreaseGhost.Hide();
            _increaseGhost.Hide();
            _decreaseWaste.Hide();
            _increaseWaste.Hide();
            _decreaseCountLabel.Hide();
            _increaseCountLabel.Hide();
            _realCountLabel.Hide();
            _countdownLabel.Hide();
            _capIcon?.Hide();
            if (_realIcon != null)
            {
                _realIcon.SetActive(true);
            }
        }

        // one cloned, tinted badge sibling whose width is driven independently
        // isRemoval pulses alpha subtly (via the shared, globally-synced RemovalPulse clock) so a decrease reads differently from an increase
        private readonly struct Strip
        {
            internal readonly RectTransform Rtf;
            internal readonly Color FillColor;
            private readonly Image[] _images;
            private readonly float[] _baseAlphas;
            private readonly bool _isRemoval;
            private readonly RectTransform _iconRtf;
            private readonly RectTransform _strikethroughRtf;
            private readonly CanvasGroup _group;

            private Strip(RectTransform rtf, Color fillColor, Image[] images, float[] baseAlphas, bool isRemoval, RectTransform iconRtf, RectTransform strikethroughRtf, CanvasGroup group)
            {
                Rtf = rtf;
                FillColor = fillColor;
                _images = images;
                _baseAlphas = baseAlphas;
                _isRemoval = isRemoval;
                _iconRtf = iconRtf;
                _strikethroughRtf = strikethroughRtf;
                _group = group;
            }

            internal bool IsValid => Rtf != null;

            // diagonal line thickness as a fraction of the icon's shorter side
            private const float StrikethroughThicknessRatio = 0.16f;

            // deactivating a strip drops its whole remaining width out of the badge row's layout in one frame, so this has to stay
            // far below a pixel or the row visibly lurches, see RESEARCH.md's ghost strip hide jitter note
            private const float HideBelowWidth = 0.05f;

            // dismissal-only fade window, so the strip is gone from view long before its rect finishes shrinking, see RESEARCH.md
            private const float FadeStartWidth = 20f;
            private const float FadeEndWidth = 4f;

            internal static Strip CloneFrom(RectTransform sourceBadge, Transform insertAfter, bool isRemoval = false)
            {
                GameObject go = Object.Instantiate(sourceBadge.gameObject, sourceBadge.parent);
                go.name = sourceBadge.name + " (EffectPreview Ghost)";

                BarAffliction driver = go.GetComponent<BarAffliction>();
                Image icon = driver != null ? driver.icon : null;
                if (driver != null)
                {
                    Object.Destroy(driver);
                }

                // icon.transform.localScale can be mid-flight through BarAffliction.OnEnable()'s DOScale(1f, OutElastic) pop-in
                // at the exact instant we clone it (that tween overshoots past 1 before settling)
                // reset or the ghost freezes at whatever scale it happened to catch
                if (icon != null)
                {
                    icon.transform.localScale = Vector3.one;
                }

                // sampled before the tint loop below touches these colors, so this is still the vanilla, undimmed fill color
                Color fillColor = WasteIndicator.SampleFillColor(go, icon);
                Color.RGBToHSV(fillColor, out float _, out float fillSaturation, out float _);
                if (icon != null && fillSaturation < 0.15f)
                {
                    fillColor = icon.color;
                }

                Image[] images = go.GetComponentsInChildren<Image>(includeInactive: true);

                float[] baseAlphas = new float[images.Length];
                for (int i = 0; i < images.Length; i++)
                {
                    Color c = Color.Lerp(images[i].color, Color.white, 0.4f);
                    c.a = 0.65f;
                    images[i].color = c;
                    baseAlphas[i] = c.a;
                }

                // icon.color at this point is already the translucent tint just applied above - reuse it so the strikethrough
                // reads as "this icon, crossed out" rather than a generic red X (the badge's own fill color is often a neutral
                // gray/white shared across every status, the icon glyph itself is what actually carries the per-status color)
                RectTransform strikethroughRtf = isRemoval && icon != null ? CreateStrikethrough(icon.rectTransform, icon.color) : null;

                // one group alpha covers every child at once, strikethrough included (that one isn't in the images array above, it gets created after it)
                CanvasGroup group = go.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = go.AddComponent<CanvasGroup>();
                }
                group.blocksRaycasts = false;
                group.interactable = false;

                go.transform.SetSiblingIndex(insertAfter.GetSiblingIndex() + 1);
                Common.GhostOwnershipTag.Attach(go);
                go.SetActive(false);
                return new Strip(go.GetComponent<RectTransform>(), fillColor, images, baseAlphas, isRemoval, icon != null ? icon.rectTransform : null, strikethroughRtf, group);
            }

            internal void Apply(float targetWidth, float lerpStep)
            {
                bool wasActive = Rtf.gameObject.activeSelf;
                float current = wasActive ? Rtf.sizeDelta.x : 0f;

                if (targetWidth <= 0f && current < HideBelowWidth)
                {
                    Hide();
                    return;
                }

                float newWidth = Mathf.Lerp(current, Mathf.Max(0f, targetWidth), lerpStep);
                Rtf.sizeDelta = new Vector2(newWidth, Rtf.sizeDelta.y);
                if (!wasActive)
                {
                    Rtf.gameObject.SetActive(true);
                }

                // only ever fades on the way out - a legitimately narrow strip (a small previewed amount) stays fully opaque however thin it is
                float opacity = targetWidth > 0f ? 1f : Mathf.InverseLerp(FadeEndWidth, FadeStartWidth, newWidth);
                if (_group != null && _group.alpha != opacity)
                {
                    _group.alpha = opacity;
                }

                if (opacity <= 0f)
                {
                    return;
                }

                if (_isRemoval)
                {
                    RemovalPulse.Apply(_images, _baseAlphas);
                }

                // the icon's own rect can keep changing (pop-in scale, layout rebuilds) after we first sized this, so resync every frame
                UpdateStrikethrough();
            }

            internal void Hide()
            {
                if (Rtf != null)
                {
                    Rtf.gameObject.SetActive(false);
                }
            }

            // the icon's RectTransform is stretched horizontally to the badge's own (shrinking/growing) width with a fixed height - vanilla
            // relies on Image.preserveAspect to letterbox the actual glyph down to a min(width, height) square within that rect, so that's
            // the square we cross out too, rather than the raw (often much wider or narrower) rect
            private void UpdateStrikethrough()
            {
                if (_strikethroughRtf == null || _iconRtf == null)
                {
                    return;
                }

                float side = Mathf.Min(_iconRtf.rect.width, _iconRtf.rect.height);
                if (side <= 0f)
                {
                    return;
                }

                _strikethroughRtf.sizeDelta = new Vector2(side * 1.41421356f, side * StrikethroughThicknessRatio);
            }

            // parented under the icon itself so it always shares the icon's position/scale; size is (re)synced every Apply via UpdateStrikethrough
            private static RectTransform CreateStrikethrough(RectTransform iconRtf, Color tintColor)
            {
                GameObject go = new GameObject("GhostStrikethrough", typeof(RectTransform), typeof(Image));
                RectTransform rtf = (RectTransform)go.transform;
                rtf.SetParent(iconRtf, worldPositionStays: false);
                rtf.anchorMin = (rtf.anchorMax = (rtf.pivot = new Vector2(0.5f, 0.5f)));
                rtf.anchoredPosition = Vector2.zero;
                rtf.localRotation = Quaternion.Euler(0f, 0f, 45f);

                Image image = go.GetComponent<Image>();
                image.color = tintColor;
                image.raycastTarget = false;
                return rtf;
            }
        }
    }
}
