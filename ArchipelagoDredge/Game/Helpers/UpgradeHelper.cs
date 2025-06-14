using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winch.Core;

namespace ArchipelagoDredge.Game.Helpers
{
    public static class UpgradeHelper
    {
        private static readonly Dictionary<HullTier, EquipmentTierMapping> HullToEquipmentMapping = new()
        {
            [HullTier.Tier1] = new EquipmentTierMapping { EngineTier = 0, FishingRodTier = 0, NetTier = 0, LightTier = 0, StorageTier = 0 },
            [HullTier.Tier2] = new EquipmentTierMapping { EngineTier = 1, FishingRodTier = 1, NetTier = 1, LightTier = 1, StorageTier = 0 },
            [HullTier.Tier3] = new EquipmentTierMapping { EngineTier = 2, FishingRodTier = 2, NetTier = 1, LightTier = 1, StorageTier = 2 },
            [HullTier.Tier4] = new EquipmentTierMapping { EngineTier = 3, FishingRodTier = 3, NetTier = 3, LightTier = 3, StorageTier = 3 },
            [HullTier.Tier5] = new EquipmentTierMapping { EngineTier = 4, FishingRodTier = 4, NetTier = 3, LightTier = 4, StorageTier = 4 },
        };

        private static readonly PlayerUpgradeState _upgradeState = new();

        public static void UpgradeItem(string itemName)
        {
            switch (itemName)
            {
                case "Progressive Hull":
                    UpgradeHull((HullTier)Math.Min((int)_upgradeState.HullTier + 1, (int)HullTier.Tier5));
                    break;
                case "Progressive Engine":
                    _upgradeState.EngineTier = Math.Min(_upgradeState.EngineTier + 1, HullToEquipmentMapping[HullTier.Tier5].EngineTier);
                    ApplyUpgradesToGame();
                    break;
                case "Progressive Fishing":
                    _upgradeState.FishingRodTier = Math.Min(_upgradeState.FishingRodTier + 1, HullToEquipmentMapping[HullTier.Tier5].FishingRodTier);
                    ApplyUpgradesToGame();
                    break;
                case "Progressive Net":
                    _upgradeState.NetTier = Math.Min(_upgradeState.NetTier + 1, HullToEquipmentMapping[HullTier.Tier5].NetTier);
                    ApplyUpgradesToGame();
                    break;
                case "Progressive Light":
                    _upgradeState.LightTier = Math.Min(_upgradeState.LightTier + 1, HullToEquipmentMapping[HullTier.Tier5].LightTier);
                    ApplyUpgradesToGame();
                    break;
                case "Progressive Storage":
                    _upgradeState.StorageTier = Math.Min(_upgradeState.StorageTier + 1, HullToEquipmentMapping[HullTier.Tier5].StorageTier);
                    ApplyUpgradesToGame();
                    break;
            }
        }

        private static void UpgradeHull(HullTier newTier)
        {
            if (newTier <= _upgradeState.HullTier)
                return;

            _upgradeState.HullTier = newTier;

            if (newTier == HullTier.Tier5)
            {
                MaxOutAllSystems();
                return;
            }

            ApplyEffectiveUpgrades();
        }
        private static void ApplyEffectiveUpgrades()
        {
            var mapping = HullToEquipmentMapping[_upgradeState.HullTier];

            _upgradeState.EngineTier = Math.Max(_upgradeState.EngineTier, mapping.EngineTier);
            _upgradeState.FishingRodTier = Math.Max(_upgradeState.FishingRodTier, mapping.FishingRodTier);
            _upgradeState.NetTier = Math.Max(_upgradeState.NetTier, mapping.NetTier);
            _upgradeState.LightTier = Math.Max(_upgradeState.LightTier, mapping.LightTier);
            _upgradeState.StorageTier = Math.Max(_upgradeState.StorageTier, mapping.StorageTier);

            ApplyUpgradesToGame();
        }

        private static void ApplyUpgradesToGame()
        {
            ApplyUpgradeIfEligible($"tier-{_upgradeState.EngineTier}-engines-1");
            ApplyUpgradeIfEligible($"tier-{_upgradeState.FishingRodTier}-fishing-1");
            ApplyUpgradeIfEligible($"tier-{_upgradeState.NetTier}-net-1");
            ApplyUpgradeIfEligible($"tier-{_upgradeState.LightTier}-lights-1");
            ApplyUpgradeIfEligible($"tier-{_upgradeState.StorageTier}-storage-1");
            ApplyUpgradeIfEligible($"tier-{(int)_upgradeState.HullTier}-hull");
        }

        private static void ApplyUpgradeIfEligible(string upgradeId)
        {
            var upgradeData = GameManager.Instance.UpgradeManager.GetUpgradeDataById(upgradeId);
            if (upgradeData != null)
            {
                GameManager.Instance.UpgradeManager.AddUpgrade(upgradeData, true);
            }
        }

        private static void MaxOutAllSystems()
        {
            var max = HullToEquipmentMapping[HullTier.Tier5];
            _upgradeState.EngineTier = max.EngineTier;
            _upgradeState.FishingRodTier = max.FishingRodTier;
            _upgradeState.NetTier = max.NetTier;
            _upgradeState.LightTier = max.LightTier;
            _upgradeState.StorageTier = max.StorageTier;
            ApplyUpgradesToGame();
        }
    }
    public enum HullTier
    {
        Tier1 = 1,
        Tier2,
        Tier3,
        Tier4,
        Tier5
    }

    public class EquipmentTierMapping
    {
        public int EngineTier { get; set; }
        public int FishingRodTier { get; set; }
        public int NetTier { get; set; }
        public int LightTier { get; set; }
        public int StorageTier { get; set; }
    }

    public class PlayerUpgradeState
    {
        public HullTier HullTier { get; set; } = HullTier.Tier1;
        public int EngineTier { get; set; } = 0;
        public int FishingRodTier { get; set; } = 0;
        public int NetTier { get; set; } = 0;
        public int LightTier { get; set; } = 0;
        public int StorageTier { get; set; } = 0;
        public bool IsFullyUpgraded => HullTier == HullTier.Tier5;
    }
}
