using System;

namespace FireSafety.Movement
{
    // The front path is connected to the house through the authored front-door gap only.
    public static class HouseExit
    {
        public const float DoorMinX = -6.4f, DoorMaxX = -3.6f, ThresholdZ = -8f, PathEndZ = -12f;

        public static bool IsWalkableArea(float x, float z, float radius)
        {
            bool indoors = x > -10f + radius && x < 10f - radius
                && z > ThresholdZ + radius && z < 8f - radius;
            bool frontPath = x > DoorMinX + radius && x < DoorMaxX - radius
                && z > PathEndZ + radius && z <= ThresholdZ + radius;
            return indoors || frontPath;
        }

        public static bool IsInsideHouse(float x, float z)
            => x > -10f && x < 10f && z > ThresholdZ && z < 8f;

        public static bool HasEscaped(float x, float z, float radius)
            => x > DoorMinX + radius && x < DoorMaxX - radius
                && z < ThresholdZ - radius - 0.01f && z > PathEndZ + radius;

        public static float TargetX(float clickedX, float radius, float cellSize)
            => Math.Max(DoorMinX + radius + cellSize * 0.5f,
                Math.Min(DoorMaxX - radius - cellSize * 0.5f, clickedX));

        public static float TargetZ(float clickedZ, float radius, float cellSize)
            => Math.Max(PathEndZ + radius + cellSize * 0.5f,
                Math.Min(ThresholdZ - radius - cellSize, clickedZ));
    }
}
