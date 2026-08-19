using System.Collections.Generic;
using UnityEngine;

namespace StatPreview.Ui
{
    internal class GhostBarOverlay : MonoBehaviour
    {
        private static readonly Color ExtraStaminaGhostTint = new Color(1f, 0.9f, 0.4f, 0.5f);

        private StaminaBar _bar;
        private RectTransform _fullBar;
        private bool _built;

        private readonly Dictionary<CharacterAfflictions.STATUSTYPE, GhostBadge> _statusGhosts = new Dictionary<CharacterAfflictions.STATUSTYPE, GhostBadge>();
        private GhostSegment _extraStaminaGhost;

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
                _extraStaminaGhost = null;
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
            return _extraStaminaGhost != null && !_extraStaminaGhost.IsValid;
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

            if (_extraStaminaGhost == null && _bar.extraBarStamina != null)
            {
                _extraStaminaGhost = GhostSegment.Create(_bar.transform, ExtraStaminaGhostTint);
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

            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, GhostBadge> entry in _statusGhosts)
            {
                preview.StatusDeltas.TryGetValue(entry.Key, out float delta);
                float live = character.refs.afflictions.GetCurrentStatus(entry.Key);
                entry.Value.Apply(fullLocalWidth, live, delta);
            }

            if (_extraStaminaGhost != null)
            {
                if (Mathf.Approximately(preview.ExtraStaminaDelta, 0f))
                {
                    _extraStaminaGhost.Hide();
                }
                else
                {
                    Vector3[] fullBarCorners = new Vector3[4];
                    _fullBar.GetWorldCorners(fullBarCorners);
                    float fullWorldWidth = fullBarCorners[2].x - fullBarCorners[0].x;

                    Vector3[] corners = new Vector3[4];
                    _bar.extraBarStamina.GetWorldCorners(corners);
                    _extraStaminaGhost.Apply(corners, fullWorldWidth, preview.ExtraStaminaDelta, fixedLeftEdge: true);
                }
            }
        }

        private void HideAll()
        {
            foreach (GhostBadge badge in _statusGhosts.Values)
            {
                badge.Hide();
            }
            _extraStaminaGhost?.Hide();
        }
    }
}
