using System;
using UnityEngine;

namespace FireSafety.Rendering
{
    // Resources includes these materials AND their shaders in standalone players.
    public static class RuntimeMaterials
    {
        public const string HouseResource = "Materials/HouseSurface";
        public const string FlameResource = "Materials/FlameSurface";

        public static Material House(Color color) => Create(HouseResource, color);
        public static Material Flame(Color color) => Create(FlameResource, color);

        private static Material Create(string resource, Color color)
        {
            Material template = Resources.Load<Material>(resource);
            if (template == null || template.shader == null)
                throw new InvalidOperationException("Required game material is missing: Resources/" + resource
                    + ". Pull the project's material assets and rebuild the player.");
            var material = new Material(template);
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
