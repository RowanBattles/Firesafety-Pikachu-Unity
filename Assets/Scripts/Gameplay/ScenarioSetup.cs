using System;
using FireSafety.Fire;

namespace FireSafety.Gameplay
{
    public struct FireChoice
    {
        public readonly FireType Type;
        public readonly FireLocation Location;
        public FireChoice(FireType type, FireLocation location) { Type = type; Location = location; }
    }

    // Randomize scenario state, while the house geometry and player spawn stay shared.
    public sealed class ScenarioSetup
    {
        private readonly VentilationOpening[] openings;
        public FireChoice Fire { get; }
        public ScenarioMode Mode { get; }

        public ScenarioSetup(ScenarioMode mode, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            Mode = mode;
            FireType type = mode == ScenarioMode.Game ? (FireType)random.Next(4) : FireType.A;
            FireLocation location = type == FireType.B ? FireLocation.PaintBottle
                : type == FireType.C ? FireLocation.Hob : type == FireType.F ? FireLocation.DeepFryer
                : mode == ScenarioMode.Game && random.Next(2) == 1 ? FireLocation.Bed : FireLocation.Table;
            Fire = new FireChoice(type, location);
            openings = new VentilationOpening[Enum.GetValues(typeof(HouseOpening)).Length];
            foreach (HouseOpening id in Enum.GetValues(typeof(HouseOpening)))
            {
                VentilationOpening initial = FireVentilation.InitialState(id);
                bool open = mode == ScenarioMode.Game ? id != HouseOpening.FrontDoor && random.Next(2) == 1
                    : mode == ScenarioMode.Dev && initial.IsOpen;
                openings[(int)id] = new VentilationOpening(initial.FirstRoom, initial.SecondRoom, initial.IsDoor, open);
            }
        }

        public VentilationOpening Opening(HouseOpening id) => openings[(int)id];
    }
}
