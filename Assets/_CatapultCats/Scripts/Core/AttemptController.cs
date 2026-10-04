using System;
using System.Linq;
using CatapultCats.Launch;
using CatapultCats.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatapultCats.Core
{
    public sealed class AttemptController : MonoBehaviour
    {
        [SerializeField] private SlingshotController2D slingshot;
        [SerializeField] private PhysicsResolver2D resolver;
        [SerializeField] private MouseTarget2D[] targets;
        [SerializeField, Min(0)] private int maximumShots = 3;

        private float timeSinceLaunch;

        public AttemptState State { get; private set; } = AttemptState.Aiming;
        public ShotCounter Shots { get; private set; }
        public SlingshotController2D Slingshot => slingshot;
        public PhysicsResolver2D Resolver => resolver;
        public MouseTarget2D[] Targets => targets;
        public int MaximumShots => maximumShots;
        public event Action<AttemptState> StateChanged;

        private void Awake()
        {
            Shots = new ShotCounter(maximumShots);
        }

        private void OnEnable()
        {
            if (slingshot != null)
            {
                slingshot.Launched += HandleLaunched;
            }

            if (resolver != null)
            {
                resolver.Resolved += HandleResolved;
            }
        }

        private void OnDisable()
        {
            if (slingshot != null)
            {
                slingshot.Launched -= HandleLaunched;
            }

            if (resolver != null)
            {
                resolver.Resolved -= HandleResolved;
            }
        }

        private void Update()
        {
            if (State != AttemptState.CatInFlight || resolver == null)
            {
                return;
            }

            timeSinceLaunch += Time.deltaTime;
            if (timeSinceLaunch >= resolver.MinimumObservationTime)
            {
                ChangeState(AttemptState.ResolvingPhysics);
            }
        }

        public void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleLaunched(Vector2 launchVelocity)
        {
            if (State != AttemptState.Aiming || Shots == null || !Shots.Consume())
            {
                return;
            }

            timeSinceLaunch = 0f;
            ChangeState(AttemptState.CatInFlight);
            resolver?.BeginResolution();
        }

        private void HandleResolved()
        {
            if (AllTargetsDefeated())
            {
                ChangeState(AttemptState.Won);
                return;
            }

            if (Shots != null && Shots.HasShotsRemaining)
            {
                slingshot.ResetForAiming();
                ChangeState(AttemptState.Aiming);
                return;
            }

            ChangeState(AttemptState.Failed);
        }

        private bool AllTargetsDefeated()
        {
            return targets != null && targets.Length > 0 && targets.All(target => target != null && target.IsDefeated);
        }

        private void ChangeState(AttemptState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(State);

            if (State == AttemptState.Won)
            {
                Debug.Log("[CatapultCats] Attempt WON");
            }
            else if (State == AttemptState.Failed)
            {
                Debug.Log("[CatapultCats] Attempt FAILED");
            }
        }
    }
}
