using System;

namespace FireSafety.Smoke
{
    public sealed class SmokeExposure
    {
        public double DoseSeconds { get; private set; }
        public double LethalDoseSeconds { get; }
        public float CurrentDensity { get; private set; }
        public bool IsDead { get; private set; }
        public float Fraction => (float)(DoseSeconds / LethalDoseSeconds);
        public bool ShouldCough => !IsDead && CurrentDensity >= 0.05f && DoseSeconds >= 0.75;

        public SmokeExposure(double lethalDoseSeconds = 12.0)
        {
            if (lethalDoseSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(lethalDoseSeconds));
            LethalDoseSeconds = lethalDoseSeconds;
        }

        public void Advance(double seconds, float density)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || float.IsNaN(density))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            CurrentDensity = Math.Max(0f, Math.Min(1f, density));
            if (IsDead) return;
            DoseSeconds = CurrentDensity >= 0.05f
                ? Math.Min(LethalDoseSeconds, DoseSeconds + seconds * CurrentDensity)
                : Math.Max(0.0, DoseSeconds - seconds * 0.5);
            if (DoseSeconds >= LethalDoseSeconds - 1e-9) IsDead = true;
        }
    }
}
