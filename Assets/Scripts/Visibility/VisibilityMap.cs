using System;
using System.Collections.Generic;

namespace FireSafety.Visibility
{
    public struct SightObstacle
    {
        public readonly float MinX, MinZ, MaxX, MaxZ;
        public SightObstacle(float minX, float minZ, float maxX, float maxZ)
        { MinX = minX; MinZ = minZ; MaxX = maxX; MaxZ = maxZ; }

        public bool Occludes(float fromX, float fromZ, float toX, float toZ)
        {
            // Most walls are entirely to one side of a ray; avoid divisions for those.
            if (fromX < MinX && toX < MinX || fromX > MaxX && toX > MaxX
                || fromZ < MinZ && toZ < MinZ || fromZ > MaxZ && toZ > MaxZ) return false;
            // The visible wall/door itself should not be hidden by its own front face.
            if (toX >= MinX && toX <= MaxX && toZ >= MinZ && toZ <= MaxZ) return false;
            float near = 0f, far = 1f;
            return Clip(fromX, toX - fromX, MinX, MaxX, ref near, ref far)
                && Clip(fromZ, toZ - fromZ, MinZ, MaxZ, ref near, ref far);
        }

        private static bool Clip(float origin, float direction, float min, float max, ref float near, ref float far)
        {
            if (Math.Abs(direction) < 1e-8f) return origin >= min && origin <= max;
            float a = (min - origin) / direction, b = (max - origin) / direction;
            near = Math.Max(near, Math.Min(a, b));
            far = Math.Min(far, Math.Max(a, b));
            return near <= far;
        }
    }

    // Visibility is recalculated, never permanently remembered through closed doors or corners.
    public sealed class VisibilityMap
    {
        private readonly int width, height;
        private readonly float originX, originZ, step;
        private readonly bool[] visible;

        public VisibilityMap(int width, int height, float originX, float originZ, float step)
        {
            if (width <= 0 || height <= 0 || step <= 0) throw new ArgumentException("Invalid visibility grid.");
            this.width = width; this.height = height; this.originX = originX; this.originZ = originZ; this.step = step;
            visible = new bool[width * height];
        }

        public bool IsVisible(int cell) => cell >= 0 && cell < visible.Length && visible[cell];

        public static float ViewExtent(float orthographicSize, float aspect)
            => Math.Max(32f, orthographicSize * 2f * Math.Max(1f, aspect) + 4f);

        // Glass admits sight only. Navigation, interaction and fire keep their solid wall geometry.
        public static void AddWallWithWindows(SightObstacle wall, IReadOnlyList<SightObstacle> windows,
            List<SightObstacle> result)
        {
            bool horizontal = wall.MaxX - wall.MinX > wall.MaxZ - wall.MinZ;
            var pieces = new List<SightObstacle> { wall };
            for (int window = 0; window < windows.Count; window++)
            {
                SightObstacle gap = windows[window];
                if (horizontal ? gap.MinZ > wall.MinZ || gap.MaxZ < wall.MaxZ
                    : gap.MinX > wall.MinX || gap.MaxX < wall.MaxX) continue;
                for (int i = pieces.Count - 1; i >= 0; i--)
                {
                    SightObstacle piece = pieces[i];
                    float min = horizontal ? piece.MinX : piece.MinZ, max = horizontal ? piece.MaxX : piece.MaxZ;
                    float cutMin = Math.Max(min, horizontal ? gap.MinX : gap.MinZ);
                    float cutMax = Math.Min(max, horizontal ? gap.MaxX : gap.MaxZ);
                    if (cutMax <= cutMin) continue;
                    pieces.RemoveAt(i);
                    if (min < cutMin) pieces.Add(horizontal
                        ? new SightObstacle(min, piece.MinZ, cutMin, piece.MaxZ)
                        : new SightObstacle(piece.MinX, min, piece.MaxX, cutMin));
                    if (cutMax < max) pieces.Add(horizontal
                        ? new SightObstacle(cutMax, piece.MinZ, max, piece.MaxZ)
                        : new SightObstacle(piece.MinX, cutMax, piece.MaxX, max));
                }
            }
            result.AddRange(pieces);
        }

        public static bool CanSee(float fromX, float fromZ, float toX, float toZ, IReadOnlyList<SightObstacle> obstacles)
        {
            for (int i = 0; i < obstacles.Count; i++)
                if (obstacles[i].Occludes(fromX, fromZ, toX, toZ)) return false;
            return true;
        }

        public void Calculate(float fromX, float fromZ, IReadOnlyList<SightObstacle> obstacles)
            => Calculate(fromX, fromZ, obstacles, originX, originZ, step);

        public void Calculate(float fromX, float fromZ, IReadOnlyList<SightObstacle> obstacles,
            float areaOriginX, float areaOriginZ, float cellSize)
        {
            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                    visible[z * width + x] = CanSee(fromX, fromZ,
                        areaOriginX + (x + 0.5f) * cellSize, areaOriginZ + (z + 0.5f) * cellSize, obstacles);
        }
    }
}
