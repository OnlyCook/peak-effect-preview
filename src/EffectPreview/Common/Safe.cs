using System;
using System.Collections.Generic;
using UnityEngine;

namespace EffectPreview.Common
{
    internal static class Safe
    {
        private const float LogIntervalSeconds = 5f;

        private class State
        {
            internal int ConsecutiveFailures;
            internal float LastLogTime;
            internal bool HasLogged;
        }

        private static readonly Dictionary<string, State> States = new Dictionary<string, State>();

        internal static bool Run(string context, Action body)
        {
            if (!States.TryGetValue(context, out State state))
            {
                state = new State();
                States[context] = state;
            }

            try
            {
                body();
                state.ConsecutiveFailures = 0;
                return true;
            }
            catch (Exception e)
            {
                state.ConsecutiveFailures++;
                float now;
                try { now = Time.unscaledTime; }
                catch { now = state.LastLogTime; }

                bool shouldLog = !state.HasLogged || now - state.LastLogTime >= LogIntervalSeconds;
                if (shouldLog)
                {
                    state.HasLogged = true;
                    state.LastLogTime = now;
                    string suffix = state.ConsecutiveFailures > 1
                        ? $" (failure #{state.ConsecutiveFailures}, further identical errors suppressed for {LogIntervalSeconds:0}s)"
                        : string.Empty;
                    try { Plugin.Instance?.Log?.LogError($"EffectPreview: {context} failed{suffix}: {e}"); }
                    catch { }
                }
                return false;
            }
        }
    }
}
