using System;
using CatapultCats.Core;
using UnityEngine;

namespace CatapultCats.Levels
{
    public sealed class LevelFlowController : MonoBehaviour
    {
        [SerializeField] private LevelSequence sequence;
        [SerializeField] private LevelLoader loader;
        [SerializeField] private AttemptController attempt;

        public LevelSequence Sequence => sequence;
        public AttemptController Attempt => attempt;
        public LevelDefinition CurrentLevel => loader == null ? null : loader.CurrentLevel;
        public int CurrentIndex => sequence == null ? -1 : sequence.IndexOf(CurrentLevel);
        public bool HasNextLevel => sequence != null && sequence.GetNext(CurrentLevel) != null;
        public event Action<LevelDefinition> LevelChanged;
        public event Action<string> LoadFailed;

        public void Begin(LevelDefinition editorOverride = null)
        {
            Load(editorOverride != null ? editorOverride : sequence == null ? null : sequence.GetLevel(0));
        }

        public void Retry()
        {
            Load(CurrentLevel);
        }

        public void ContinueAfterWin()
        {
            if (attempt == null || attempt.State != AttemptState.Won)
            {
                return;
            }

            // Completing the last level returns to the first; no persistent progress is needed.
            Load(HasNextLevel ? sequence.GetNext(CurrentLevel) : sequence == null ? null : sequence.GetLevel(0));
        }

        private void Load(LevelDefinition level)
        {
            if (level == null || loader == null)
            {
                Fail("Choose playable levels in the Level Sequence and apply R4 setup.");
                return;
            }

            try
            {
                loader.Load(level);
                LevelChanged?.Invoke(level);
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        private void Fail(string message)
        {
            Debug.LogError("[CatapultCats R4] " + message);
            LoadFailed?.Invoke(message);
        }
    }
}
