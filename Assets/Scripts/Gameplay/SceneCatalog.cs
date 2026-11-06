namespace FireSafety.Gameplay
{
    public enum ScenarioMode { Dev, Game, Tutorial }

    public static class SceneCatalog
    {
        public const string Menu = "MenuPage", Game = "GameScene", Tutorial = "TutorialScene", Dev = "DevScene";
        public static readonly string[] BuildOrder = { Menu, Game, Tutorial, Dev };
        public static string Path(string scene) => "Assets/Scenes/" + scene + ".unity";
        public static bool IsHouseScene(string scene) => scene == Game || scene == Tutorial || scene == Dev;
    }
}
