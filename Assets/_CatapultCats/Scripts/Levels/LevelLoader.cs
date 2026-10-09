using System;
using System.Collections.Generic;
using System.Linq;
using CatapultCats.Core;
using CatapultCats.Launch;
using CatapultCats.Physics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatapultCats.Levels
{
    public sealed class LevelLoadResult
    {
        public LevelLoadResult(
            IReadOnlyList<GameObject> instances,
            IReadOnlyList<MouseTarget2D> targets,
            IReadOnlyList<Rigidbody2D> monitoredBodies)
        {
            Instances = instances;
            Targets = targets;
            MonitoredBodies = monitoredBodies;
        }

        public IReadOnlyList<GameObject> Instances { get; }
        public IReadOnlyList<MouseTarget2D> Targets { get; }
        public IReadOnlyList<Rigidbody2D> MonitoredBodies { get; }
    }

    public sealed class LevelLoader : MonoBehaviour
    {
        [SerializeField] private PieceCatalog catalog;
        [SerializeField] private Transform runtimeRoot;
        [SerializeField] private SlingshotController2D slingshot;
        [SerializeField] private AttemptController attemptController;
        [SerializeField] private PhysicsResolver2D resolver;

        public PieceCatalog Catalog => catalog;
        public Transform RuntimeRoot => runtimeRoot;
        public LevelDefinition CurrentLevel { get; private set; }
        public event Action<LevelLoadResult> Loaded;

        public LevelLoadResult Load(LevelDefinition level)
        {
            LevelValidationResult validation = LevelValidation.Validate(level, catalog);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot load invalid level:\n" + string.Join("\n", validation.Errors));
            }

            if (runtimeRoot == null || slingshot == null || attemptController == null || resolver == null)
            {
                throw new InvalidOperationException("LevelLoader runtime references are incomplete.");
            }

            ClearRuntimeRoot();
            var instances = new List<GameObject>();
            var targets = new List<MouseTarget2D>();
            var monitoredBodies = new List<Rigidbody2D>();

            foreach (LevelPiecePlacement placement in level.Pieces)
            {
                if (!catalog.TryGetPrefab(placement.PieceType, out GameObject prefab))
                {
                    throw new InvalidOperationException($"No unique prefab mapping exists for {placement.PieceType}.");
                }

                GameObject instance = Instantiate(prefab, placement.Position, Quaternion.Euler(0f, 0f, placement.RotationDegrees), runtimeRoot);
                instance.name = prefab.name;
                instances.Add(instance);

                MouseTarget2D target = instance.GetComponent<MouseTarget2D>();
                if (target != null)
                {
                    targets.Add(target);
                }

                Rigidbody2D body = instance.GetComponent<Rigidbody2D>();
                if (body != null && body.bodyType == RigidbodyType2D.Dynamic && target == null)
                {
                    monitoredBodies.Add(body);
                }
            }

            slingshot.SetAnchorPosition(level.SlingshotPosition);
            var bodiesWithProjectile = new[] { slingshot.ProjectileBody }
                .Concat(monitoredBodies)
                .Where(body => body != null);
            resolver.ConfigureBodies(slingshot.ProjectileBody, bodiesWithProjectile);
            attemptController.ConfigureLevel(targets, level.CatCount);
            CurrentLevel = level;
            var result = new LevelLoadResult(instances, targets, resolver.CriticalBodies);
            Loaded?.Invoke(result);
            return result;
        }

        private void ClearRuntimeRoot()
        {
            for (int index = runtimeRoot.childCount - 1; index >= 0; index--)
            {
                GameObject previousInstance = runtimeRoot.GetChild(index).gameObject;
                previousInstance.SetActive(false);
                Object.Destroy(previousInstance);
            }
        }
    }
}
