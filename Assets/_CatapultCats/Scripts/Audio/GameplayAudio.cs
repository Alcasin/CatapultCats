using System;
using System.Collections.Generic;
using System.Linq;
using CatapultCats.Core;
using CatapultCats.Levels;
using CatapultCats.Physics;
using UnityEngine;

namespace CatapultCats.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameplayAudio : MonoBehaviour
    {
        public const int VoiceCount = 6;
        private enum Cue { Stretch, Launch, Impact, Wood, Glass, Mouse, Win, Fail }
        [SerializeField] private LevelFlowController flow;
        [SerializeField] private LevelLoader loader;
        [SerializeField] private AudioSource[] voices;
        [SerializeField] private AudioClip slingshotStretch;
        [SerializeField] private AudioClip catLaunch;
        [SerializeField] private AudioClip impactThump;
        [SerializeField] private AudioClip woodBreak;
        [SerializeField] private AudioClip glassBreak;
        [SerializeField] private AudioClip mouseDefeat;
        [SerializeField] private AudioClip levelWin;
        [SerializeField] private AudioClip levelFail;
        [SerializeField, Range(0f, 1f)] private float stretchVolume = 0.40f;
        [SerializeField, Range(0f, 1f)] private float launchVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float impactVolume = 0.38f;
        [SerializeField, Range(0f, 1f)] private float woodVolume = 0.48f;
        [SerializeField, Range(0f, 1f)] private float glassVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float mouseVolume = 0.48f;
        [SerializeField, Range(0f, 1f)] private float winVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float failVolume = 0.48f;

        private readonly Dictionary<BreakablePiece2D, bool> breakables = new Dictionary<BreakablePiece2D, bool>();
        private readonly List<MouseTarget2D> mice = new List<MouseTarget2D>();
        private readonly List<ImpactAudioObserver2D> observers = new List<ImpactAudioObserver2D>();
        private readonly Dictionary<int, int> effectFrames = new Dictionary<int, int>();
        private readonly Dictionary<ulong, float> pairTimes = new Dictionary<ulong, float>();
        private readonly List<PendingImpact> pending = new List<PendingImpact>();
        private readonly int[] priorities = new int[VoiceCount];
        private readonly double[] voiceStarts = new double[VoiceCount];
        private System.Random pitchRandom = new System.Random(5105);
        private float nextImpactTime;
        private bool resultPlayed;

        private struct PendingImpact
        {
            public ulong Pair;
            public int FirstOwner, SecondOwner, Frame;
            public float Strength;
        }

        private void Awake()
        {
            if (flow == null || flow.Attempt == null || flow.Attempt.Slingshot == null || loader == null ||
                voices == null || voices.Length != VoiceCount || voices.Any(voice => voice == null) ||
                Enum.GetValues(typeof(Cue)).Cast<Cue>().Any(cue => Clip(cue) == null))
            {
                Debug.LogError("[CatapultCats R5] Audio wiring incomplete; run Apply R5 Audio. Gameplay is unaffected.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (flow == null || flow.Attempt == null || flow.Attempt.Slingshot == null || loader == null) return;
            flow.Attempt.Slingshot.DragStarted += HandleDrag;
            flow.Attempt.Slingshot.Launched += HandleLaunch;
            flow.Attempt.StateChanged += HandleState;
            flow.LoadFailed += HandleLoadFailed;
            loader.Loaded += HandleLoaded;
            Observe(flow.Attempt.Slingshot.ProjectileBody);
            // Catch up only on a late enable. Normal startup uses Loader.Loaded, before any physics step.
            if (loader.CurrentLevel != null)
                BindPieces(loader.RuntimeRoot.Cast<Transform>().Where(child => child.gameObject.activeSelf)
                    .Select(child => child.gameObject).ToArray());
        }

        private void OnDisable()
        {
            if (flow != null && flow.Attempt != null && flow.Attempt.Slingshot != null)
            {
                flow.Attempt.Slingshot.DragStarted -= HandleDrag;
                flow.Attempt.Slingshot.Launched -= HandleLaunch;
                flow.Attempt.StateChanged -= HandleState;
            }
            if (flow != null) flow.LoadFailed -= HandleLoadFailed;
            if (loader != null) loader.Loaded -= HandleLoaded;
            ClearPieceBindings();
            foreach (ImpactAudioObserver2D observer in observers) if (observer != null) observer.Bind(null);
            observers.Clear();
            ResetPlayback();
        }

        private void HandleLoaded(LevelLoadResult result) => BindPieces(result.Instances);
        private void BindPieces(IReadOnlyList<GameObject> instances)
        {
            ClearPieceBindings();
            ResetPlayback();
            // Remove destroyed previous-level observers; the projectile observer remains reusable.
            observers.RemoveAll(observer => observer == null || !observer.gameObject.activeInHierarchy);
            if (loader.CurrentLevel == null || instances.Count != loader.CurrentLevel.Pieces.Count) return;
            for (int index = 0; index < instances.Count; index++)
            {
                GameObject instance = instances[index];
                BreakablePiece2D piece = instance.GetComponent<BreakablePiece2D>();
                if (piece != null)
                {
                    LevelPieceType type = loader.CurrentLevel.Pieces[index].PieceType;
                    breakables.Add(piece, type == LevelPieceType.GlassBeam || type == LevelPieceType.GlassBlock);
                    piece.Broken += HandleBroken;
                }
                MouseTarget2D mouse = instance.GetComponent<MouseTarget2D>();
                if (mouse != null) { mice.Add(mouse); mouse.Defeated += HandleDefeated; }
                // Runtime-only observer components: no prefab edits or changes to bodies/colliders.
                foreach (Rigidbody2D body in instance.GetComponentsInChildren<Rigidbody2D>(true)) Observe(body);
            }
        }

        private void ClearPieceBindings()
        {
            foreach (BreakablePiece2D piece in breakables.Keys) if (piece != null) piece.Broken -= HandleBroken;
            foreach (MouseTarget2D mouse in mice) if (mouse != null) mouse.Defeated -= HandleDefeated;
            breakables.Clear();
            mice.Clear();
        }

        private void Observe(Rigidbody2D body)
        {
            if (body == null) return;
            ImpactAudioObserver2D observer = body.GetComponent<ImpactAudioObserver2D>();
            if (observer == null) observer = body.gameObject.AddComponent<ImpactAudioObserver2D>();
            observer.Bind(this);
            if (!observers.Contains(observer)) observers.Add(observer);
        }

        private void ResetPlayback()
        {
            if (voices != null) foreach (AudioSource voice in voices) if (voice != null) voice.Stop();
            pending.Clear();
            pairTimes.Clear();
            effectFrames.Clear();
            pitchRandom = new System.Random(5105);
            nextImpactTime = 0f;
            resultPlayed = false;
        }

        private void HandleLoadFailed(string message) => ResetPlayback();
        private void HandleDrag() => Play(Cue.Stretch, 0);
        private void HandleLaunch(Vector2 velocity) => Play(Cue.Launch, 2);
        private void HandleBroken(BreakablePiece2D piece)
        {
            effectFrames[piece.gameObject.GetInstanceID()] = Time.frameCount;
            if (breakables.TryGetValue(piece, out bool glass)) Play(glass ? Cue.Glass : Cue.Wood, 1, true);
        }
        private void HandleDefeated(MouseTarget2D mouse)
        {
            effectFrames[mouse.gameObject.GetInstanceID()] = Time.frameCount;
            Play(Cue.Mouse, 2);
        }
        private void HandleState(AttemptState state)
        {
            if (resultPlayed || (state != AttemptState.Won && state != AttemptState.Failed)) return;
            resultPlayed = true;
            foreach (AudioSource voice in voices) voice.Stop();
            pending.Clear();
            Play(state == AttemptState.Won ? Cue.Win : Cue.Fail, 3);
        }

        internal void QueueImpact(Collision2D collision)
        {
            if (!isActiveAndEnabled || resultPlayed || pending.Count >= 64 || collision == null ||
                collision.collider == null || collision.otherCollider == null) return;
            float impulse = ImpactMath2D.GetImpactImpulse(collision);
            // Audio-only gate, deliberately separate from all damage/crush thresholds.
            if (impulse < 1.25f || collision.relativeVelocity.sqrMagnitude < 1f) return;
            uint first = unchecked((uint)collision.collider.GetInstanceID());
            uint second = unchecked((uint)collision.otherCollider.GetInstanceID());
            ulong pair = ((ulong)Math.Min(first, second) << 32) | Math.Max(first, second);
            pending.Add(new PendingImpact { Pair = pair, Frame = Time.frameCount, Strength = impulse,
                FirstOwner = OwnerId(collision.collider), SecondOwner = OwnerId(collision.otherCollider) });
        }

        private static int OwnerId(Collider2D collider)
        {
            BreakablePiece2D piece = collider.GetComponentInParent<BreakablePiece2D>();
            if (piece != null) return piece.gameObject.GetInstanceID();
            MouseTarget2D mouse = collider.GetComponentInParent<MouseTarget2D>();
            return mouse != null ? mouse.gameObject.GetInstanceID() : collider.gameObject.GetInstanceID();
        }

        private void LateUpdate()
        {
            // Drain only queued collision callbacks, not polling gameplay state.
            // Waiting until all collision callbacks finish avoids thump + break on the same contact.
            PendingImpact? strongest = null;
            foreach (PendingImpact impact in pending)
            {
                bool separateEffect = (effectFrames.TryGetValue(impact.FirstOwner, out int firstFrame) && firstFrame == impact.Frame) ||
                    (effectFrames.TryGetValue(impact.SecondOwner, out int secondFrame) && secondFrame == impact.Frame);
                if (separateEffect || (pairTimes.TryGetValue(impact.Pair, out float last) && Time.unscaledTime - last < .30f)) continue;
                if (!strongest.HasValue || impact.Strength > strongest.Value.Strength) strongest = impact;
            }
            if (strongest.HasValue && Time.unscaledTime >= nextImpactTime)
            {
                pairTimes[strongest.Value.Pair] = Time.unscaledTime;
                nextImpactTime = Time.unscaledTime + .16f;
                Play(Cue.Impact, 0, true);
            }
            pending.Clear();
        }

        private void Play(Cue cue, int priority, bool varyPitch = false)
        {
            AudioClip clip = Clip(cue);
            if (clip == null || voices == null || voices.Length != VoiceCount) return;
            int slot = Array.FindIndex(voices, voice => voice != null && !voice.isPlaying);
            if (slot < 0)
            {
                for (int i = 0; i < voices.Length; i++)
                    if (priorities[i] <= priority && (slot < 0 || priorities[i] < priorities[slot] ||
                        (priorities[i] == priorities[slot] && voiceStarts[i] < voiceStarts[slot]))) slot = i;
            }
            if (slot < 0) return; // Never evict a more important cue for a resting/debris impact.
            AudioSource source = voices[slot];
            source.Stop();
            source.clip = clip;
            source.loop = false;
            source.pitch = varyPitch ? .94f + (float)pitchRandom.NextDouble() * .12f : 1f;
            // Initial balance leaves master headroom for six simultaneous generated voices.
            source.volume = Volume(cue) * .35f;
            source.Play();
            priorities[slot] = priority;
            voiceStarts[slot] = AudioSettings.dspTime;
        }

        private AudioClip Clip(Cue cue)
        {
            switch (cue)
            {
                case Cue.Stretch: return slingshotStretch;
                case Cue.Launch: return catLaunch;
                case Cue.Impact: return impactThump;
                case Cue.Wood: return woodBreak;
                case Cue.Glass: return glassBreak;
                case Cue.Mouse: return mouseDefeat;
                case Cue.Win: return levelWin;
                default: return levelFail;
            }
        }
        private float Volume(Cue cue)
        {
            switch (cue)
            {
                case Cue.Stretch: return stretchVolume;
                case Cue.Launch: return launchVolume;
                case Cue.Impact: return impactVolume;
                case Cue.Wood: return woodVolume;
                case Cue.Glass: return glassVolume;
                case Cue.Mouse: return mouseVolume;
                case Cue.Win: return winVolume;
                default: return failVolume;
            }
        }
    }
}
