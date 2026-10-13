namespace ArchipelagoDredge.Network.Enums
{
    /// <summary>
    ///     The seed's options, as sent in slot data on login. All false until the first connect.
    /// </summary>
    public class SlotData
    {
        public SlotData()
        {
        }

        public SlotData(bool deathLink, bool includeIronRigDlc, bool includePaleReachDlc, bool addFishingLicenses,
            bool addPassageItems, int progressiveAberrationChanceItems, bool guaranteedAberrationsItem, bool logicalNets,
            bool requireBoatPerformance)
        {
            DeathLink = deathLink;
            IncludeIronRigDLC = includeIronRigDlc;
            IncludePaleReachDLC = includePaleReachDlc;
            AddFishingLicenses = addFishingLicenses;
            AddPassageItems = addPassageItems;
            ProgressiveAberrationChanceItems = progressiveAberrationChanceItems;
            GuaranteedAberrationsItem = guaranteedAberrationsItem;
            LogicalNets = logicalNets;
            RequireBoatPerformance = requireBoatPerformance;
        }

        public bool DeathLink { get; }
        public bool IncludeIronRigDLC { get; }
        public bool IncludePaleReachDLC { get; }
        public bool AddFishingLicenses { get; }
        public bool AddPassageItems { get; }
        public int ProgressiveAberrationChanceItems { get; }
        public bool GuaranteedAberrationsItem { get; }
        public bool LogicalNets { get; }
        public bool RequireBoatPerformance { get; }
    }
}
