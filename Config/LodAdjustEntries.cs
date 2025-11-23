using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTweaks.Config
{
    [Serializable]
    public struct LodAdjustEntry
    {
        public string type;
        public string name;
        public int deltaMinLod;
    }

    [Serializable]
    public struct LodAdjustConfig
    {
        public static readonly List<LodAdjustEntry> Entries = new()
        {
            // Reason: no LOD meshes and high vertex count
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder04", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder05", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder06", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder07", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Boulder08", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Barn01", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Barn02", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Barn03", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Prop_TrashContainerEmpty04_Empty", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Prop_TrashContainerEmpty04_Full", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "DogParkTube01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "DogParkTube02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "FoodCart02", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "FoodCart03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PlaygroundPlayset01", deltaMinLod = 1 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PlaygroundPlayset02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "RuinsCastle01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "RuinsStoneCairns01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "RuinsStoneCairns02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "RuinsStoneCairns03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "TelecomTower01", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "SatelliteUplink01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "WindTurbine03", deltaMinLod = 8 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "WaterTower02", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "WaterTower03", deltaMinLod = 4 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "SewageOutlet01", deltaMinLod = 4 },

            // Reason: no LOD meshes and high vertex count
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "LightpoleIndustrial04", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandSmallCorner01", deltaMinLod = 4 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandSmallCorner02", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandSmall01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandSmall02", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandSmall02", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandLargeCorner01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandLargeCorner02", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandLarge01", deltaMinLod = 4 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandLarge02", deltaMinLod = 4 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandLarge03", deltaMinLod = 4 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "SportParkStandLarge04", deltaMinLod = 4 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ContainerCrane01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ContainerCrane02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileSmallLow01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileSmallMedium01", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileSmallHigh01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileMediumLow01", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileMediumMedium01", deltaMinLod = 7 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileMediumHigh01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileLargeLow01", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileLargeMedium01", deltaMinLod = 7 },
            new LodAdjustEntry { type = "BuildingExtensionPrefab", name = "ShippingContainerPileLargeHigh01", deltaMinLod = 3 },

            // Reason: minLod is set too low
            new LodAdjustEntry { type = "PathwayPrefab", name = "Covered Pedestrian Bridge", deltaMinLod = 5 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "BarrelPile01", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "BarrelPile02", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "BarrelPile03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "RubblePylonLarge01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "RubblePylonLarge02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "RubblePylonLarge03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "RubblePylonLarge04", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "StageSmall01", deltaMinLod = 4 },

            // Reason: minLod is set too low
            new LodAdjustEntry { type = "BuildingPrefab", name = "FirewatchTower01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "EarlyDisasterWarningSystem01", deltaMinLod = 2 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "PoliceStation01", deltaMinLod = 5 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "PoliceStation03", deltaMinLod = 2 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "TramStation01", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "TramStation02", deltaMinLod = 6 },
            new LodAdjustEntry { type = "BuildingPrefab", name = "TramStationTerminus01", deltaMinLod = 6 },

            // Reason: minLod is set too low
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola01", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola02", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola04", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola05", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola06", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "Pergola07", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular02", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular03", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular04", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular05", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular06", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModular07", deltaMinLod = 6 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway01", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway03", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway04", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway05", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway06", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularDoorway07", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen04", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen05", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen06", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularOpen07", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall01", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall02", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall03", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall04", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall05", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall06", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaModularWall07", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaSmall05", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaSmall07", deltaMinLod = 3 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaSmallDouble05", deltaMinLod = 2 },
            new LodAdjustEntry { type = "StaticObjectPrefab", name = "PergolaSmallDouble07", deltaMinLod = 3 },
        };
    }
}
