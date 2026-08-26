using System.Collections.Generic;
using UnityEngine;

namespace EffectPreview.Common
{
    // marks a GameObject as ours
    // if a FriendsInfo fork later instantiates the whole
    // vanilla bar hierarchy Unity deep-clones this component right along with it
    //
    // the clone gets a fresh InstanceID that was never registered below, so its OnEnable self-destructs it instead
    // of leaving a frozen ghost preview baked onto someone else's bar
    internal class GhostOwnershipTag : MonoBehaviour
    {
        private static readonly HashSet<int> LegitIds = new HashSet<int>();

        internal static void Attach(GameObject go)
        {
            LegitIds.Add(go.GetInstanceID());
            go.AddComponent<GhostOwnershipTag>();
        }

        private void OnEnable()
        {
            if (!LegitIds.Contains(gameObject.GetInstanceID()))
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}
