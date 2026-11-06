using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FireSafety.Gameplay
{
    [DefaultExecutionOrder(-400)]
    public sealed class SceneNavigation : MonoBehaviour
    {
        private static int transitionFrame = -1;
        private GUIStyle menuStyle;
        public static bool TransitionThisFrame => transitionFrame == Time.frameCount;
        private Rect MenuButton => new Rect(Screen.width - 108f, 14f, 94f, 34f);

        public static void Load(string scene, bool restartTutorial = true)
        {
            if (TransitionThisFrame) return;
            if (scene == SceneCatalog.Tutorial && restartTutorial) TutorialWalkthrough.ResetEntry();
            transitionFrame = Time.frameCount;
            SceneManager.LoadScene(scene);
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            Vector2 pointer = Mouse.current.position.ReadValue();
            if (MenuButton.Contains(new Vector2(pointer.x, Screen.height - pointer.y))) Load(SceneCatalog.Menu);
        }

        private void OnGUI()
        {
            int depth = GUI.depth; GUI.depth = -1000;
            if (menuStyle == null) menuStyle = new GUIStyle(GUI.skin.button)
                { alignment = TextAnchor.MiddleCenter, fontSize = 14, padding = new RectOffset(0, 0, 0, 0) };
            GUI.Box(MenuButton, "Menu", menuStyle); GUI.depth = depth;
        }
    }
}
