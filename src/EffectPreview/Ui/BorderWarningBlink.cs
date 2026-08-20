using UnityEngine;
using UnityEngine.UI;

namespace EffectPreview.Ui
{
    // pulses a bar border's own Image color as a warning when the previewed item use would knock the player out or kill them via petrify
    internal class BorderWarningBlink
    {
        private const float PulseSpeed = 6f;
        private const float PulseAmount = 0.6f;
        private const float LerpRate = 12f;
        private static readonly Color WarnColor = new Color(1f, 0.15f, 0.1f, 1f);

        private readonly Image[] _images;
        private readonly Color[] _originalColors;
        private float _sinTime;
        private float _blend;

        internal BorderWarningBlink(params Image[] images)
        {
            _images = images;
            _originalColors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                _originalColors[i] = images[i] != null ? images[i].color : Color.white;
            }
        }

        internal bool IsValid
        {
            get
            {
                for (int i = 0; i < _images.Length; i++)
                {
                    if (_images[i] == null) return false;
                }
                return true;
            }
        }

        internal void Apply(bool active)
        {
            float target = active ? 1f : 0f;
            _blend = Mathf.MoveTowards(_blend, target, Time.deltaTime * LerpRate);
            _sinTime = _blend > 0f ? _sinTime + Time.deltaTime * PulseSpeed : 0f;

            float pulse = (Mathf.Sin(_sinTime) + 1f) * 0.5f * PulseAmount * _blend;
            for (int i = 0; i < _images.Length; i++)
            {
                _images[i].color = Color.Lerp(_originalColors[i], WarnColor, pulse);
            }
        }

        internal void Hide() => Apply(false);
    }
}
