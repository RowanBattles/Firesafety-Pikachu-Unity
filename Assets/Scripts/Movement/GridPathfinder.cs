using System.Collections.Generic;

namespace FireSafety.Movement
{
    // Uniform-cost, four-neighbour search: no diagonal corner cutting through walls.
    public sealed class GridPathfinder
    {
        public int Width { get; }
        public int Height { get; }
        private readonly bool[] walkable;

        public GridPathfinder(int width, int height, bool[] walkable)
        {
            if (width <= 0 || height <= 0 || walkable == null || walkable.Length != width * height)
                throw new System.ArgumentException("Grid dimensions must match walkable cells.");
            Width = width;
            Height = height;
            this.walkable = (bool[])walkable.Clone();
        }

        public bool IsWalkable(int cell) => cell >= 0 && cell < walkable.Length && walkable[cell];

        public bool TryFindPath(int start, int goal, List<int> path)
        {
            path.Clear();
            if (!IsWalkable(start) || !IsWalkable(goal)) return false;
            var previous = new int[walkable.Length];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            var frontier = new Queue<int>();
            frontier.Enqueue(start);
            previous[start] = start;
            while (frontier.Count > 0 && previous[goal] < 0)
            {
                int current = frontier.Dequeue();
                int x = current % Width;
                int z = current / Width;
                if (x > 0) Visit(current - 1, current, previous, frontier);
                if (x + 1 < Width) Visit(current + 1, current, previous, frontier);
                if (z > 0) Visit(current - Width, current, previous, frontier);
                if (z + 1 < Height) Visit(current + Width, current, previous, frontier);
            }
            if (previous[goal] < 0) return false;
            for (int cell = goal; cell != start; cell = previous[cell]) path.Add(cell);
            path.Add(start);
            path.Reverse();
            return true;
        }

        private void Visit(int next, int current, int[] previous, Queue<int> frontier)
        {
            if (!walkable[next] || previous[next] >= 0) return;
            previous[next] = current;
            frontier.Enqueue(next);
        }
    }
}
