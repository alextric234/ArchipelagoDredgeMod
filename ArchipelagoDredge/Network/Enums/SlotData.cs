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
            bool addPassageItems)
        {
            DeathLink = deathLink;
            IncludeIronRigDLC = includeIronRigDlc;
            IncludePaleReachDLC = includePaleReachDlc;
            AddFishingLicenses = addFishingLicenses;
            AddPassageItems = addPassageItems;
        }

        public bool DeathLink { get; }
        public bool IncludeIronRigDLC { get; }
        public bool IncludePaleReachDLC { get; }
        public bool AddFishingLicenses { get; }
        public bool AddPassageItems { get; }
    }
}
