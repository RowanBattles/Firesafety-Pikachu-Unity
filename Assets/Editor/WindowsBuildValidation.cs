using System.Linq;
using FireSafety.Gameplay;
using FireSafety.Movement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FireSafety.Editor
{
    // Check the active profile as well as the global scene list: profiles can override it.
    public sealed class WindowsBuildValidation : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("Fire Safety/Validate Windows Build")]
        public static void ValidateFromMenu()
        {
            ValidateSetup();
            Debug.Log("Windows build setup is valid: MenuPage starts first, all three modes and runtime assets are included.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (IsWindows(report.summary.platform)) ValidateSetup();
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || !IsWindows(report.summary.platform)) return;
            if (scene.name == SceneCatalog.Menu)
            {
                if (!scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MenuPageController>(true))
                    .Any(menu => menu.enabled && menu.gameObject.activeInHierarchy))
                    throw new BuildFailedException("MenuPage needs an active Menu Page Controller.");
                return;
            }
            if (!SceneCatalog.IsHouseScene(scene.name)) return;
            HousePrototype startup = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<HousePrototype>(true))
                .FirstOrDefault(house => house != null && house.enabled && house.gameObject.activeInHierarchy);
            if (startup == null)
                throw new BuildFailedException(scene.name + " needs an active, enabled House Prototype component.");
            if (startup.PrimitiveDefaultMaterial == null)
                throw new BuildFailedException(scene.name + " needs its Primitive Default Material reference. Pull the updated scene before rebuilding.");
            ScenarioMode expected = scene.name == SceneCatalog.Game ? ScenarioMode.Game
                : scene.name == SceneCatalog.Tutorial ? ScenarioMode.Tutorial : ScenarioMode.Dev;
            if (startup.Mode != expected)
                throw new BuildFailedException(scene.name + " has the wrong Scenario Mode on House Prototype. Expected " + expected + ".");
        }

        private static bool IsWindows(BuildTarget target)
            => target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows;

        private static void ValidateSetup()
        {
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            EditorBuildSettingsScene[] scenes = profile != null && profile.overrideGlobalScenes
                ? profile.scenes : EditorBuildSettings.scenes;
            EditorBuildSettingsScene first = scenes.FirstOrDefault(scene => scene.enabled);
            if (first == null || first.path != SceneCatalog.Path(SceneCatalog.Menu))
                throw new BuildFailedException("Open Build Profiles > Scene List. Enable " + SceneCatalog.Path(SceneCatalog.Menu)
                    + " and put it first. Check the Windows profile's scene override if it has one.");
            foreach (string name in SceneCatalog.BuildOrder)
                if (!scenes.Any(scene => scene.enabled && scene.path == SceneCatalog.Path(name)))
                    throw new BuildFailedException("The active scene list is missing " + name
                        + ". Enable all four scenes in Build Profiles > Scene List, including any profile override.");
            RequireMaterial("HouseSurface", "Universal Render Pipeline/Lit");
            RequireMaterial("FlameSurface", "Universal Render Pipeline/Unlit");
            RequireShader("VisibilityOverlay");
            RequireShader("SmokeParticles");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/UI/MenuArtwork.png") == null)
                throw new BuildFailedException("The menu illustration is missing. Pull Assets/Resources/UI/MenuArtwork.png and let Unity import it.");
        }

        private static void RequireMaterial(string name, string shaderName)
        {
            string path = "Assets/Resources/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name != shaderName)
                throw new BuildFailedException("Missing or invalid runtime material: " + path
                    + ". Pull the material assets and let Unity finish importing before building.");
        }

        private static void RequireShader(string name)
        {
            string path = "Assets/Resources/Effects/" + name + ".shader";
            if (AssetDatabase.LoadAssetAtPath<Shader>(path) == null)
                throw new BuildFailedException("Required runtime shader is missing: " + path);
        }
    }
}
