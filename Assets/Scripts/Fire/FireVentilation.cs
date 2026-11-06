using System;
using System.Collections.Generic;

namespace FireSafety.Fire
{
    public enum HouseRoom { Outside, LivingRoom, Kitchen, Bedroom }
    public enum HouseOpening { KitchenDoor, BedroomDoor, FrontDoor, LivingRoomWindow, LivingRoomGardenWindow, BedroomWindow, KitchenWindow }

    public struct VentilationOpening
    {
        public readonly HouseRoom FirstRoom, SecondRoom;
        public readonly bool IsDoor, IsOpen;
        public VentilationOpening(HouseRoom first, HouseRoom second, bool isDoor, bool isOpen)
        { FirstRoom = first; SecondRoom = second; IsDoor = isDoor; IsOpen = isOpen; }
    }

    // Gameplay tuning: closing openings slows a fire; it does not put it out.
    public static class FireVentilation
    {
        public const double ClosedRate = 0.5, OpenDoorBonus = 0.35, OpenWindowBonus = 0.4, MaximumRate = 3.0;

        public static VentilationOpening InitialState(HouseOpening opening)
        {
            switch (opening)
            {
                case HouseOpening.KitchenDoor: return new VentilationOpening(HouseRoom.LivingRoom, HouseRoom.Kitchen, true, true);
                case HouseOpening.BedroomDoor: return new VentilationOpening(HouseRoom.LivingRoom, HouseRoom.Bedroom, true, false);
                case HouseOpening.FrontDoor: return new VentilationOpening(HouseRoom.LivingRoom, HouseRoom.Outside, true, false);
                case HouseOpening.LivingRoomWindow: return new VentilationOpening(HouseRoom.LivingRoom, HouseRoom.Outside, false, true);
                case HouseOpening.LivingRoomGardenWindow: return new VentilationOpening(HouseRoom.LivingRoom, HouseRoom.Outside, false, false);
                case HouseOpening.BedroomWindow: return new VentilationOpening(HouseRoom.Bedroom, HouseRoom.Outside, false, false);
                case HouseOpening.KitchenWindow: return new VentilationOpening(HouseRoom.Kitchen, HouseRoom.Outside, false, false);
                default: throw new ArgumentOutOfRangeException(nameof(opening));
            }
        }

        public static HouseRoom RoomAt(float x, float z)
        {
            if (x <= -10 || x >= 10 || z <= -8 || z >= 8) return HouseRoom.Outside;
            return x < 0 ? HouseRoom.LivingRoom : z < 0 ? HouseRoom.Kitchen : HouseRoom.Bedroom;
        }

        public static double GrowthRate(HouseRoom fireRoom, IReadOnlyList<VentilationOpening> openings)
        {
            int connected = RoomBit(fireRoom), previous;
            do
            {
                previous = connected;
                for (int i = 0; i < openings.Count; i++)
                {
                    VentilationOpening opening = openings[i];
                    if (!opening.IsDoor || !opening.IsOpen || opening.FirstRoom == HouseRoom.Outside
                        || opening.SecondRoom == HouseRoom.Outside) continue;
                    int rooms = RoomBit(opening.FirstRoom) | RoomBit(opening.SecondRoom);
                    if ((connected & rooms) != 0) connected |= rooms;
                }
            } while (connected != previous);

            double rate = ClosedRate;
            for (int i = 0; i < openings.Count; i++)
            {
                VentilationOpening opening = openings[i];
                if (opening.IsOpen && (connected & (RoomBit(opening.FirstRoom) | RoomBit(opening.SecondRoom))) != 0)
                    rate += opening.IsDoor ? OpenDoorBonus : OpenWindowBonus;
            }
            return Math.Min(MaximumRate, rate);
        }

        private static int RoomBit(HouseRoom room) => room == HouseRoom.Outside ? 0 : 1 << (int)room;
    }
}
