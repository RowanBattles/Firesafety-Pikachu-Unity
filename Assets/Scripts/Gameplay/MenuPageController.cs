using UnityEngine;
using UnityEngine.InputSystem;

namespace FireSafety.Gameplay
{
    public sealed class MenuPageController : MonoBehaviour
    {
        private Texture2D artwork;
        private GUIStyle title, eyebrow, body, choiceTitle, choiceBody;
        private int firstFrame;
        private float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        private readonly string[] names = { "Game", "Tutorial", "Dev" };
        private readonly string[] descriptions = { "A random fire. Your decisions.", "Learn every tool, step by step.", "Choose a fire and experiment." };
        private readonly string[] destinations = { SceneCatalog.Game, SceneCatalog.Tutorial, SceneCatalog.Dev };
        private Rect Choice(int i) => new Rect(48f * Scale, (340f + 92f * i) * Scale, 380f * Scale, 76f * Scale);

        private void Awake()
        {
            firstFrame = Time.frameCount;
            artwork = Resources.Load<Texture2D>("UI/MenuArtwork");
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.035f, 0.075f, 0.11f);
            }
        }

        private void Update()
        {
            if (Time.frameCount == firstFrame || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            Vector2 pointer = Mouse.current.position.ReadValue();
            pointer.y = Screen.height - pointer.y;
            for (int i = 0; i < destinations.Length; i++)
                if (Choice(i).Contains(pointer)) { SceneNavigation.Load(destinations[i]); return; }
        }

        private void OnGUI()
        {
            if (title == null)
            {
                title = Style(56, FontStyle.Bold, Color.white);
                eyebrow = Style(12, FontStyle.Bold, new Color(1f, 0.7f, 0.35f));
                body = Style(17, FontStyle.Normal, new Color(0.7f, 0.81f, 0.83f));
                choiceTitle = Style(23, FontStyle.Bold, Color.white);
                choiceBody = Style(13, FontStyle.Normal, new Color(0.73f, 0.84f, 0.85f));
            }
            title.fontSize = Mathf.Max(15, Mathf.RoundToInt(56 * Scale));
            eyebrow.fontSize = Mathf.Max(8, Mathf.RoundToInt(12 * Scale));
            body.fontSize = Mathf.Max(10, Mathf.RoundToInt(17 * Scale));
            choiceTitle.fontSize = Mathf.Max(12, Mathf.RoundToInt(23 * Scale));
            choiceBody.fontSize = Mathf.Max(8, Mathf.RoundToInt(13 * Scale));
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.035f, 0.075f, 0.11f));
            if (artwork != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), artwork, ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, 480 * Scale, Screen.height), new Color(0.025f, 0.065f, 0.09f, 0.96f));
            GUI.Label(Rect(48, 52, 395, 26), "THINK FAST. STAY SAFE.", eyebrow);
            GUI.Label(Rect(44, 91, 400, 150), "FIRE\nSAFETY", title);
            GUI.Label(Rect(48, 254, 380, 62), "Learn to handle a small fire.\nKnow when it is time to leave.", body);
            Vector2 pointer = Mouse.current == null ? new Vector2(-1, -1) : Mouse.current.position.ReadValue();
            pointer.y = Screen.height - pointer.y;
            for (int i = 0; i < names.Length; i++)
            {
                Rect row = Choice(i);
                bool hover = row.Contains(pointer);
                Color background = i == 0 ? new Color(0.98f, 0.63f, 0.25f)
                    : i == 1 ? new Color(0.08f, 0.29f, 0.34f) : new Color(0.09f, 0.17f, 0.22f);
                if (hover) background = Color.Lerp(background, Color.white, 0.13f);
                Fill(row, background);
                choiceTitle.normal.textColor = i == 0 ? new Color(0.06f, 0.12f, 0.15f) : Color.white;
                choiceBody.normal.textColor = i == 0 ? new Color(0.18f, 0.23f, 0.23f) : new Color(0.73f, 0.84f, 0.85f);
                GUI.Label(new Rect(row.x + 19 * Scale, row.y + 8 * Scale, row.width - 52 * Scale, 31 * Scale), names[i], choiceTitle);
                GUI.Label(new Rect(row.x + 19 * Scale, row.y + 43 * Scale, row.width - 52 * Scale, 25 * Scale), descriptions[i], choiceBody);
                GUI.Label(new Rect(row.xMax - 30 * Scale, row.y + 18 * Scale, 24 * Scale, 36 * Scale), ">", choiceTitle);
            }
            GUI.Label(Rect(48, 674, 390, 25), "SINGLE PLAYER  /  CLICK TO MOVE", eyebrow);
        }

        private Rect Rect(float x, float y, float width, float height) => new Rect(x * Scale, y * Scale, width * Scale, height * Scale);
        private static GUIStyle Style(int size, FontStyle font, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = font, wordWrap = true };
            style.normal.textColor = color;
            return style;
        }
        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
