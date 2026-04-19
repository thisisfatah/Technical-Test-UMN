using UnityEngine;

namespace Aquascape
{
    public sealed class AquascapeHud : MonoBehaviour
    {
        private AquariumWorld world;
        private AquariumPaths paths;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;

        public void Initialize(AquariumWorld aquariumWorld, AquariumPaths aquariumPaths)
        {
            world = aquariumWorld;
            paths = aquariumPaths;
        }

        private void OnGUI()
        {
            if (world == null)
            {
                return;
            }

            EnsureStyles();

            GUILayout.BeginArea(new Rect(16f, 16f, 440f, 160f), GUIContent.none, boxStyle);
            GUILayout.Label("Aquascape Runtime", labelStyle);
            GUILayout.Label($"Fish: {world.FishCount}  |  Trash: {world.TrashCount}  |  Food: {world.FoodCount}", labelStyle);
            GUILayout.Label("Left click empty water: drop food", labelStyle);
            GUILayout.Label("Left click fish: scare fish", labelStyle);
            GUILayout.Label("Left click trash: clean trash", labelStyle);
            GUILayout.Label($"RuntimeImports: {paths.RuntimeImportsPath}", labelStyle);
            GUILayout.Label($"Config: {paths.ConfigPath}", labelStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (boxStyle != null && labelStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = false,
                wordWrap = true
            };
        }
    }
}
