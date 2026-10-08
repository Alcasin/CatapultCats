using System.Collections;
using CatapultCats.Physics;
using UnityEngine;

namespace CatapultCats.Presentation
{
    public sealed class BreakBurst2D : MonoBehaviour
    {
        [SerializeField] private Sprite spark;
        [SerializeField] private Color tint = Color.white;
        private BreakablePiece2D piece;

        private void Awake() => piece = GetComponent<BreakablePiece2D>();
        private void OnEnable() { if (piece != null) piece.Broken += HandleBroken; }
        private void OnDisable() { if (piece != null) piece.Broken -= HandleBroken; }
        private void HandleBroken(BreakablePiece2D broken) => StartCoroutine(Burst());

        private IEnumerator Burst()
        {
            if (spark == null) yield break;
            var particles = new SpriteRenderer[7];
            var origins = new Vector3[7];
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = new GameObject("Break Spark");
                particle.transform.SetParent(transform, false);
                particle.transform.localScale = Vector3.one * (i % 2 == 0 ? 0.12f : 0.08f);
                particles[i] = particle.AddComponent<SpriteRenderer>();
                particles[i].sprite = spark;
                particles[i].color = tint;
                particles[i].sortingOrder = 9;
                origins[i] = particle.transform.position;
            }
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < particles.Length; i++)
                {
                    float angle = i * Mathf.PI * 2f / particles.Length;
                    Vector3 travel = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * elapsed * 1.8f;
                    particles[i].transform.position = origins[i] + travel + Vector3.down * elapsed * elapsed;
                    particles[i].color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(1f - elapsed / 0.4f));
                }
                yield return null;
            }
            foreach (SpriteRenderer particle in particles) Destroy(particle.gameObject);
        }
    }
}
