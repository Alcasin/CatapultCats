using NUnit.Framework;
using UnityEditor;

namespace CatapultCats.Tests.EditMode
{
    public sealed class R0FoundationTests
    {
        private const string GameplayScenePath = "Assets/_CatapultCats/Scenes/Gameplay.unity";

        [Test]
        public void GameplaySceneAssetExists()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath), Is.Not.Null);
        }

        [Test]
        public void BuildSettingsContainOnlyEnabledGameplayScene()
        {
            var scenes = EditorBuildSettings.scenes;

            Assert.That(scenes, Has.Length.EqualTo(1));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[0].path, Is.EqualTo(GameplayScenePath));
        }

        [Test]
        public void StandaloneDefaultsUseReferenceDimensions()
        {
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(1920));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(1080));
        }

        [Test]
        public void OrientationAllowsLandscapeOnly()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
        }
    }
}
