using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelMoments.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/PixelMoments.unity";
        static ProjectSetup() { EditorApplication.delayCall += EnsureScene; }
        [MenuItem("Pixel Moments/Prepare scene")]
        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath)) {
                // Never replace an unsaved user scene.
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "GatheringLight";
            PlayerSettings.productName = "像素拾光 Pixel Moments";
            PlayerSettings.defaultScreenWidth = 900;
            PlayerSettings.defaultScreenHeight = 1600;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }
        [MenuItem("Pixel Moments/Build Android APK")]
        public static void BuildAndroid()
        {
            EnsureScene();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.gatheringlight.pixelmoments");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");
            var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Android/PixelMoments.apk", BuildTarget.Android, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Android build failed: " + result.summary.result);
        }
        [MenuItem("Pixel Moments/Build Windows Demo")]
        public static void BuildWindows()
        {
            EnsureScene();
            Directory.CreateDirectory("Builds/Windows");
            var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Windows/PixelMoments.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + result.summary.result);
        }
    }
}
