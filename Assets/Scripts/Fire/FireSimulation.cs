using System;
using System.Collections.Generic;

namespace FireSafety.Fire
{
    public enum FireType { A, B, C, F }
    public enum FireLocation { Table, Bed, PaintBottle, Hob, DeepFryer }

    // Engine-independent fire growth. Walls and closed doors are supplied as a passage mask.
    public sealed class FireSimulation
    {
        private const double StepSeconds = 0.05;
        private const double InitialHoldSeconds = 20.0;
        private readonly int width, height;
        private readonly double cellSize;
        private readonly bool[] burning;
        private readonly double[] heat;
        private readonly List<int> ignitions = new List<int>();
        private double accumulator, steppedTime;
        public FireType Type { get; }
        public FireLocation Location { get; }
        public int Seed { get; }
        public double ElapsedSeconds { get; private set; }
        public int BurningCellCount { get; private set; } = 1;
        public bool IsExtinguished => BurningCellCount == 0;
        public double DoublingSeconds { get; }
        public double InitialSizeCm { get; }
        // The exponential drive is bounded numerically; the actual footprint stays inside the house.
        public double GrowthSizeCm => IsExtinguished ? 0 : GrowthAt(ElapsedSeconds);
        public double SourceWidthMeters => Math.Min(GrowthSizeCm / 100.0, cellSize * 2.0);

        public FireSimulation(int width, int height, double cellSize, int seed, FireType type,
            FireLocation location, double initialSizeCm = 25.0, double doublingSeconds = 15.0)
        {
            if (width <= 0 || height <= 0 || cellSize <= 0 || seed < 0 || seed >= width * height
                || initialSizeCm <= 0 || doublingSeconds <= 0) throw new ArgumentException("Invalid fire configuration.");
            bool valid = type == FireType.A && (location == FireLocation.Table || location == FireLocation.Bed)
                || type == FireType.B && location == FireLocation.PaintBottle
                || type == FireType.C && location == FireLocation.Hob
                || type == FireType.F && location == FireLocation.DeepFryer;
            if (!valid) throw new ArgumentException("Fire class and ignition location do not match.");
            this.width = width; this.height = height; this.cellSize = cellSize;
            Seed = seed; Type = type; Location = location;
            InitialSizeCm = initialSizeCm; DoublingSeconds = doublingSeconds;
            burning = new bool[width * height];
            heat = new double[burning.Length];
            burning[seed] = true;
        }

        public bool IsBurning(int cell) => cell >= 0 && cell < burning.Length && burning[cell];

        // Width of the occupied fire footprint in centimetres, capped naturally by room boundaries.
        public double SizeCm
        {
            get
            {
                if (IsExtinguished) return 0;
                double radius = SourceWidthMeters * 0.5;
                double minX = (Seed % width) * cellSize - radius, maxX = minX + radius * 2;
                double minZ = (Seed / width) * cellSize - radius, maxZ = minZ + radius * 2;
                for (int cell = 0; cell < burning.Length; cell++)
                {
                    if (!burning[cell] || cell == Seed) continue;
                    double x = (cell % width) * cellSize, z = (cell / width) * cellSize;
                    minX = Math.Min(minX, x - cellSize * 0.5); maxX = Math.Max(maxX, x + cellSize * 0.5);
                    minZ = Math.Min(minZ, z - cellSize * 0.5); maxZ = Math.Max(maxZ, z + cellSize * 0.5);
                }
                return Math.Max(maxX - minX, maxZ - minZ) * 100;
            }
        }

        public void Advance(double seconds, bool[] passage)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (passage == null || passage.Length != burning.Length) throw new ArgumentException("Wrong passage mask.");
            if (IsExtinguished) return;
            ElapsedSeconds += seconds;
            accumulator += seconds;
            while (accumulator + 1e-9 >= StepSeconds)
            {
                double travel = steppedTime >= InitialHoldSeconds + DoublingSeconds * 20.0 ? cellSize
                    : (GrowthAt(steppedTime + StepSeconds) - GrowthAt(steppedTime)) / 200.0;
                steppedTime += StepSeconds;
                accumulator -= StepSeconds;
                ignitions.Clear();
                for (int cell = 0; cell < burning.Length; cell++)
                {
                    if (burning[cell] || !passage[cell]) continue;
                    int x = cell % width, z = cell / width;
                    bool adjacent = x > 0 && burning[cell - 1] && passage[cell - 1]
                        || x + 1 < width && burning[cell + 1] && passage[cell + 1]
                        || z > 0 && burning[cell - width] && passage[cell - width]
                        || z + 1 < height && burning[cell + width] && passage[cell + width];
                    if (!adjacent) continue;
                    heat[cell] += travel;
                    if (heat[cell] >= cellSize) ignitions.Add(cell);
                }
                // Ignite together so a large frame cannot jump an entire room or a closed doorway.
                foreach (int cell in ignitions) { burning[cell] = true; BurningCellCount++; }
            }
        }

        public bool TryExtinguish(double maximumSizeCm = 50)
        {
            if (IsExtinguished || SizeCm >= maximumSizeCm) return false;
            Array.Clear(burning, 0, burning.Length);
            Array.Clear(heat, 0, heat.Length);
            ignitions.Clear();
            BurningCellCount = 0;
            accumulator = 0;
            return true;
        }

        // Size and spread share the same delayed growth clock, without a jump when the hold ends.
        private double GrowthAt(double time) => InitialSizeCm * Math.Pow(2.0,
            Math.Min(Math.Max(0.0, time - InitialHoldSeconds) / DoublingSeconds, 20.0));
    }
}
