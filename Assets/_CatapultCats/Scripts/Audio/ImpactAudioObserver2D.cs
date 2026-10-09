using UnityEngine;

namespace CatapultCats.Audio
{
    // Runtime-only collision observer; never mutates physics or performs damage checks.
    [DisallowMultipleComponent]
    public sealed class ImpactAudioObserver2D : MonoBehaviour
    {
        private GameplayAudio manager;
        internal void Bind(GameplayAudio value) => manager = value;
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (manager != null) manager.QueueImpact(collision);
        }
    }
}
