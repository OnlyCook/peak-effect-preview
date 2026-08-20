using System.Collections.Generic;
using Peak.Afflictions;
using UnityEngine;

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

            float totalIncrease = 0f;
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, GhostBadge> entry in _statusGhosts)
            {
                preview.StatusIncreases.TryGetValue(entry.Key, out float increase);
                preview.StatusDecreases.TryGetValue(entry.Key, out float decrease);
                _dynamicHealBreakdown.TryGetValue(entry.Key, out float healDecrease);
                decrease += healDecrease;
                float live = character.refs.afflictions.GetCurrentStatus(entry.Key);
                if (preview.ClearsCurableStatusOnUse && CurableStatuses.Contains(entry.Key))
                {
                    decrease = live;
                }
                entry.Value.Apply(fullLocalWidth, live, decrease, increase);

                float shrinkMagnitude = Mathf.Min(decrease, live);
                totalIncrease += Mathf.Max(0f, increase - shrinkMagnitude);
            }

            _staminaArea?.Apply(fullLocalWidth, character.GetMaxStamina(), character.data.currentStamina, totalIncrease);

            // mirrors CharacterAfflictions.shouldPassOut (statusSum > 0.99f), but over every status the item touches, not just the ones with a bar badge
            bool wouldPassOut = character.refs.afflictions.statusSum + ProjectedStatusSumIncrease(character, preview) > 0.99f;
            _passOutBorderBlink?.Apply(wouldPassOut);

            float petrifyDelta = preview.PetrifyDelta + Preview.DynamicPetrifyPreview.Compute(preview, character);
            float petrifyRoom = Mathf.Max(0f, 1f - character.data.petrifyAmount * 0.01f);
            petrifyDelta = Mathf.Max(0f, Mathf.Min(petrifyDelta, petrifyRoom));

            float currentPetrifyFraction = character.data.petrifyAmount * 0.01f;
            float petrifyShrinkDelta = Mathf.Max(0f, preview.PetrifyReductionOnUse);

            // mirrors CharacterData.shouldPetrify (petrifyAmount >= 100) - petrifyDelta is already clamped to petrifyRoom, so it only ever equals petrifyRoom when the item would fill the bar completely
            bool wouldFullyPetrify = petrifyRoom > 0.0005f && petrifyDelta >= petrifyRoom - 0.0005f;
            _petrifyDeathBorderBlink?.Apply(wouldFullyPetrify);

            bool petrifyActive = _bar.petrifyAffliction != null && _bar.petrifyAffliction.gameObject.activeSelf;

            // petrify first so its just-updated DisplayedDelta (not last frame's) gates the bonus-stamina outline
            _petrifyArea?.Apply(fullLocalWidth, petrifyDelta, petrifyShrinkDelta, currentPetrifyFraction, petrifyActive);
            bool petrifyGhostVisible = (_petrifyArea?.DisplayedDelta ?? 0f) > 0.002f;

            _extraStaminaArea?.Apply(fullLocalWidth, character.data.extraStamina, preview.ExtraStaminaDelta, character.data.petrifyAmount, petrifyActive, petrifyDelta, petrifyGhostVisible);

            _rainbowArea?.Apply(preview.GrantsInfiniteStaminaOnUse, character.infiniteStam);
            // CharacterData.isInvincible is internal to the game assembly, so this checks the same thing through the public affliction API instead
            bool realInvincible = character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.Invincibility, out _);
            _shieldArea?.Apply(preview.GrantsInvincibilityOnUse, realInvincible);
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
        }
    }
}
