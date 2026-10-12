"""Builds DREDGE worlds with random options and partial item sets, and records which locations the apworld's logic
says are reachable. LogicParity checks the mod's tracker logic against the result."""
import argparse
import json
import logging
import os
import random
import sys
from argparse import Namespace

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--worlds", type=int, default=200, help="worlds to build; each gives 5 cases")
parser.add_argument("--seed", type=int, default=1)
parser.add_argument("--out", default="fixtures.json")
parser.add_argument("--ap-path", default=os.path.join(os.path.dirname(__file__), "..", "..", "..", "ArchipelagoDredge"),
                    help="Archipelago checkout containing worlds/dredge")
args = parser.parse_args()

out_path = os.path.abspath(args.out)
os.chdir(args.ap_path)
sys.path.insert(0, os.getcwd())
logging.disable(logging.CRITICAL)

from BaseClasses import CollectionState, MultiWorld  # noqa: E402
from worlds.AutoWorld import call_all  # noqa: E402
from worlds.dredge.world import DREDGEWorld  # noqa: E402

LOGIC_OPTIONS = ["include_iron_rig_dlc", "include_pale_reach_dlc", "add_fishing_licenses", "add_passage_items",
                 "logical_nets", "require_boat_performance"]


def build_world(seed: int, options: dict) -> MultiWorld:
    multiworld = MultiWorld(1)
    multiworld.game = {1: "DREDGE"}
    multiworld.player_name = {1: "Player"}
    multiworld.set_seed(seed)
    option_args = Namespace()
    for name, option in DREDGEWorld.options_dataclass.type_hints.items():
        setattr(option_args, name, {1: option.from_any(options.get(name, option.default))})
    multiworld.set_options(option_args)
    multiworld.state = CollectionState(multiworld)
    for step in ("generate_early", "create_regions", "create_items", "set_rules", "connect_entrances",
                 "generate_basic"):
        call_all(multiworld, step)
    return multiworld


def make_case(multiworld: MultiWorld, rng: random.Random, item_count: int) -> dict:
    pool = [item for item in multiworld.itempool if item.player == 1]
    rng.shuffle(pool)
    chosen = pool[:item_count]

    state = CollectionState(multiworld)
    for item in chosen:
        state.collect(item, True)
    counts: dict[str, int] = {}
    for item in list(multiworld.precollected_items[1]) + chosen:
        counts[item.name] = counts.get(item.name, 0) + 1

    # Collect every event that becomes reachable, as a sweep would.
    events = [loc for loc in multiworld.get_locations(1) if loc.address is None and loc.item is not None]
    while new_events := [loc for loc in events if not state.has(loc.item.name, 1) and loc.can_reach(state)]:
        for loc in new_events:
            state.collect(loc.item, True, loc)

    checks = [loc for loc in multiworld.get_locations(1) if loc.address is not None]
    return {
        "items": counts,
        "locations": [loc.name for loc in checks],
        "reachable": sorted(loc.name for loc in checks if loc.can_reach(state)),
    }


rng = random.Random(args.seed)
fixtures = []
for _ in range(args.worlds):
    options = {name: rng.random() < 0.5 for name in LOGIC_OPTIONS}
    multiworld = build_world(rng.randrange(1 << 30), options)
    pool_size = len([item for item in multiworld.itempool if item.player == 1])
    for item_count in (0, pool_size // 8, pool_size // 4, pool_size // 2, pool_size):
        fixtures.append({"options": options, **make_case(multiworld, rng, item_count)})

with open(out_path, "w") as f:
    json.dump(fixtures, f)
print(f"Wrote {len(fixtures)} cases to {out_path}")
