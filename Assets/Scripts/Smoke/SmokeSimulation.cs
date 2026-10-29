using System;

namespace FireSafety.Smoke
{
    // Fixed-speed smoke front. Closed passage cells block propagation, including diagonal leaks.
    public sealed class SmokeSimulation
    {
        private readonly int width, height;
        private readonly double stepSeconds;
        private float[] density, next;
        private double accumulator;
        public int Revision { get; private set; }

        public SmokeSimulation(int width, int height, double cellSize, double speedMetersPerSecond = 2.0)
        {
            if (width <= 0 || height <= 0 || cellSize <= 0 || speedMetersPerSecond <= 0)
                throw new ArgumentException("Invalid smoke grid.");
            this.width = width; this.height = height;
            stepSeconds = cellSize / speedMetersPerSecond;
            density = new float[width * height];
            next = new float[density.Length];
        }

        public float DensityAt(int cell) => cell < 0 || cell >= density.Length ? 0f : density[cell];

        public void Advance(double seconds, bool[] passage, Predicate<int> emitsSmoke)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (passage == null || passage.Length != density.Length || emitsSmoke == null)
                throw new ArgumentException("Invalid smoke passage or source.");
            accumulator += seconds;
            while (accumulator + 1e-9 >= stepSeconds)
            {
                accumulator -= stepSeconds;
                bool changed = false;
                for (int cell = 0; cell < density.Length; cell++)
                {
                    float value = density[cell];
                    if (passage[cell])
                    {
                        if (emitsSmoke(cell)) value = 1f;
                        else
                        {
                            int x = cell % width, z = cell / width;
                            float neighbour = 0f;
                            if (x > 0 && passage[cell - 1]) neighbour = Math.Max(neighbour, density[cell - 1]);
                            if (x + 1 < width && passage[cell + 1]) neighbour = Math.Max(neighbour, density[cell + 1]);
                            if (z > 0 && passage[cell - width]) neighbour = Math.Max(neighbour, density[cell - width]);
                            if (z + 1 < height && passage[cell + width]) neighbour = Math.Max(neighbour, density[cell + width]);
                            value = Math.Max(value, Math.Max(0f, neighbour - 0.015f));
                        }
                    }
                    next[cell] = value;
                    if (value != density[cell]) changed = true;
                }
                var swap = density; density = next; next = swap;
                if (changed) Revision++;
            }
        }
    }
}
