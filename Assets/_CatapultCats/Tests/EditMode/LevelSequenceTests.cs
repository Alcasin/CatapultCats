using CatapultCats.Levels;
using NUnit.Framework;
using UnityEngine;

namespace CatapultCats.Tests.EditMode
{
    public sealed class LevelSequenceTests
    {
        [Test]
        public void OrderedLevelsHaveExplicitFirstNextAndEnd()
        {
            var sequence = ScriptableObject.CreateInstance<LevelSequence>();
            var first = ScriptableObject.CreateInstance<LevelDefinition>();
            var second = ScriptableObject.CreateInstance<LevelDefinition>();
            var outside = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                sequence.Configure(new[] { first, second });
                Assert.That(sequence.GetLevel(0), Is.SameAs(first));
                Assert.That(sequence.GetNext(first), Is.SameAs(second));
                Assert.That(sequence.GetNext(second), Is.Null);
                Assert.That(sequence.GetNext(outside), Is.Null);
                Assert.That(sequence.GetLevel(-1), Is.Null);
                Assert.That(sequence.GetLevel(2), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(sequence);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(outside);
            }
        }
    }
}
