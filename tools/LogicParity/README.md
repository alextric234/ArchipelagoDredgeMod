# Logic parity check

The in-game tracker (`ArchipelagoDredge/Game/Tracker`) is a C# port of the apworld's `rules.py`, `fitting.py` and
region graph. This tool checks the two agree. Run it after changing logic on either side.

Both steps expect the apworld checked out next to this repo (`../ArchipelagoDredge`), like the mod build does.

1. Generate fixtures from the apworld. This needs Python with Archipelago's requirements installed:

   ```
   py -3.13 tools/LogicParity/generate_fixtures.py --worlds 200 --out fixtures.json
   ```

   Each world gets random logic options and five partial item sets; the fixture records which locations the apworld
   says are reachable. `--ap-path` points at a different Archipelago checkout.

2. Run the mod's logic against them:

   ```
   dotnet run --project tools/LogicParity -- fixtures.json
   ```

   It prints any case where the two disagree and exits non-zero if any do. It also checks that every location's
   "Logic needs" list in the journal tab is all met exactly when the location is in logic. Set `-p:ApWorldDataPath=...` on the
   `dotnet` command if the apworld data isn't next to this repo.
