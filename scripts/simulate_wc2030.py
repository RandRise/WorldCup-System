#!/usr/bin/env python3
"""
Phase 12 — World Cup 2030 offline simulation.

Task 1: ensure cup, lock 6 hosts + random 42, draw groups A–L.
Task 2: schedule 72 group matches; simulate scores, goals, and TeamStats.
Task 3: best-thirds qualification + R32→Final bracket (no scores; feeder FKs).

Usage:
  python simulate_wc2030.py --seed 2030
  python simulate_wc2030.py --seed 2030 --wipe-matches
  python simulate_wc2030.py --seed 2030 --skip-group-sim
  python simulate_wc2030.py --seed 2030 --skip-knockout
  python simulate_wc2030.py --seed 2030 --dry-run
  python simulate_wc2030.py --seed 2030 --reset-draw

Requires Team.CountryId non-unique (migration AllowMultipleTeamsPerCountry).

Optional:
  WC_DB="host=localhost dbname=TestDatabase user=postgres password=..."
"""

from __future__ import annotations

import argparse
import math
import os
import random
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Any, Iterable

import psycopg2
from psycopg2.extensions import connection as PgConnection
from psycopg2.extensions import cursor as PgCursor

CONN = os.environ.get(
    "WC_DB",
    "host=localhost dbname=TestDatabase user=postgres password=Aa@123456",
)

WC_YEAR = datetime(2030, 6, 13, tzinfo=timezone.utc)
# Fictional calendar labels (docs / cup identity). Match.Date uses demo_kickoff_anchors()
# so SPA Finished/Scheduled status works against UtcNow (group past; knockout future).
GROUP_SPAN_DAYS = 24  # 3 matchdays × 8 days
GROUP_FINISHED_PAD_DAYS = 16  # kickoff+90m must be < now after last group MD
KNOCKOUT_FUTURE_PAD_DAYS = 7


def demo_kickoff_anchors(now: datetime | None = None) -> tuple[datetime, datetime]:
    """
    Anchor Match.Date for SPA semantics (MatchService / StandingsService use UtcNow).

    - Group kickoffs finish entirely in the past → Finished + standings points.
    - Knockout kickoffs start in the future → Scheduled / CanBet for Admin scoring.
    WorldCup.Year remains WC_YEAR (2030 identity); only fixture timestamps shift.
    """
    if now is None:
        now = datetime.now(timezone.utc)
    group_start = (now - timedelta(days=GROUP_SPAN_DAYS + GROUP_FINISHED_PAD_DAYS)).replace(
        hour=15, minute=0, second=0, microsecond=0
    )
    knockout_start = (now + timedelta(days=KNOCKOUT_FUTURE_PAD_DAYS)).replace(
        hour=15, minute=0, second=0, microsecond=0
    )
    return group_start, knockout_start
GROUP_NAMES = list("ABCDEFGHIJKL")
HOST_NAMES = [
    "Spain",
    "Portugal",
    "Morocco",
    "Argentina",
    "Uruguay",
    "Paraguay",
]

# Data.Entities.MatchStage
MATCH_STAGE_GROUP = 0
MATCH_STAGE_ROUND_OF_16 = 1
MATCH_STAGE_QUARTER_FINAL = 2
MATCH_STAGE_SEMI_FINAL = 3
MATCH_STAGE_FINAL = 5
MATCH_STAGE_ROUND_OF_32 = 6

KNOCKOUT_MATCH_COUNT = 31  # 16 R32 + 8 R16 + 4 QF + 2 SF + Final (no ThirdPlace)

PLACEHOLDER_NAME = "Tournament Scorer"
POISSON_LAMBDA = 1.35
MAX_GOALS = 7

# FIFA WC26 Art. 12.6 — winner slots that face a best third, and allowed third groups.
THIRD_PLACE_SLOTS: list[tuple[str, frozenset[str]]] = [
    ("1E", frozenset("ABCDF")),  # M74
    ("1I", frozenset("CDFGH")),  # M77
    ("1A", frozenset("CEFHI")),  # M79
    ("1L", frozenset("EHIJK")),  # M80
    ("1D", frozenset("BEFIJ")),  # M81
    ("1G", frozenset("AEHIJ")),  # M82
    ("1B", frozenset("EFGIJ")),  # M85
    ("1K", frozenset("DEIJL")),  # M87
]

# R32 M73–M88 placeholders: "1X"/"2X" or ("3", winner_slot) for assigned third.
R32_SLOT_TEMPLATE: list[tuple[Any, Any]] = [
    ("2A", "2B"),  # M73
    ("1E", ("3", "1E")),  # M74
    ("1F", "2C"),  # M75
    ("1C", "2F"),  # M76
    ("1I", ("3", "1I")),  # M77
    ("2E", "2I"),  # M78
    ("1A", ("3", "1A")),  # M79
    ("1L", ("3", "1L")),  # M80
    ("1D", ("3", "1D")),  # M81
    ("1G", ("3", "1G")),  # M82
    ("2K", "2L"),  # M83
    ("1H", "2J"),  # M84
    ("1B", ("3", "1B")),  # M85
    ("1J", "2H"),  # M86
    ("1K", ("3", "1K")),  # M87
    ("2D", "2G"),  # M88
]

# Feeder indices into prior round (FIFA Art. 12.7–12.11); skip ThirdPlace.
R16_FEEDERS: list[tuple[int, int]] = [
    (1, 4),  # M89: W74 vs W77
    (0, 2),  # M90: W73 vs W75
    (3, 5),  # M91: W76 vs W78
    (6, 7),  # M92: W79 vs W80
    (10, 11),  # M93: W83 vs W84
    (8, 9),  # M94: W81 vs W82
    (13, 15),  # M95: W86 vs W88
    (12, 14),  # M96: W85 vs W87
]
QF_FEEDERS: list[tuple[int, int]] = [
    (0, 1),  # M97: W89 vs W90
    (4, 5),  # M98: W93 vs W94
    (2, 3),  # M99: W91 vs W92
    (6, 7),  # M100: W95 vs W96
]
SF_FEEDERS: list[tuple[int, int]] = [
    (0, 1),  # M101: W97 vs W98
    (2, 3),  # M102: W99 vs W100
]

# Display / alias -> DB Countries.Name (same map as WC 2026 scripts)
COUNTRY_ALIASES = {
    "Korea Republic": "South Korea",
    "Czechia": "Czech Republic",
    "USA": "United States",
    "USMNT": "United States",
    "Türkiye": "Turkey",
    "Turkiye": "Turkey",
    "Ivory Coast": "Côte d'Ivoire",
    "Cote d'Ivoire": "Côte d'Ivoire",
    "Cabo Verde": "Cape Verde",
    "Congo DR": "DR Congo",
    "Congo": "DR Congo",
    "IR Iran": "Iran",
    "Curacao": "Curaçao",
}


def resolve_country(name: str) -> str:
    cleaned = name.strip()
    return COUNTRY_ALIASES.get(cleaned, cleaned)


def build_team_pool(
    all_country_names: Iterable[str],
    hosts: list[str],
    rng: random.Random,
    other_count: int = 42,
) -> tuple[list[str], list[str]]:
    """Return (resolved_hosts, random others) — total 6 + other_count."""
    available = sorted({resolve_country(name) for name in all_country_names})
    resolved_hosts: list[str] = []
    for host in hosts:
        resolved = resolve_country(host)
        if resolved not in available:
            raise SystemExit(f"Host country not found in Countries: {host!r} (resolved {resolved!r})")
        resolved_hosts.append(resolved)

    host_set = set(resolved_hosts)
    if len(host_set) != len(resolved_hosts):
        raise SystemExit(f"Duplicate hosts after resolve: {resolved_hosts}")

    candidates = [name for name in available if name not in host_set]
    if len(candidates) < other_count:
        raise SystemExit(
            f"Need {other_count} non-host countries; only {len(candidates)} available "
            f"(Countries total {len(available)}, hosts {len(host_set)})."
        )

    others = rng.sample(candidates, other_count)
    return resolved_hosts, others


def draw_groups(
    hosts: list[str],
    others: list[str],
    rng: random.Random,
) -> dict[str, list[str]]:
    """
    Place each host in a distinct group when possible, then fill remaining
    slots randomly so every group has exactly 4 teams.
    """
    if len(hosts) != 6:
        raise ValueError(f"Expected 6 hosts, got {len(hosts)}")
    if len(others) != 42:
        raise ValueError(f"Expected 42 others, got {len(others)}")

    result: dict[str, list[str]] = {name: [] for name in GROUP_NAMES}
    host_group_order = GROUP_NAMES[:]
    rng.shuffle(host_group_order)
    for host, group_name in zip(hosts, host_group_order[: len(hosts)]):
        result[group_name].append(host)

    remaining_slots: list[str] = []
    for group_name in GROUP_NAMES:
        need = 4 - len(result[group_name])
        remaining_slots.extend([group_name] * need)

    rng.shuffle(remaining_slots)
    others_shuffled = others[:]
    rng.shuffle(others_shuffled)

    if len(others_shuffled) != len(remaining_slots):
        raise ValueError("Slot count mismatch while drawing groups")

    for country, group_name in zip(others_shuffled, remaining_slots):
        result[group_name].append(country)

    for group_name, teams in result.items():
        if len(teams) != 4:
            raise ValueError(f"Group {group_name} has {len(teams)} teams, expected 4")
        if len(set(teams)) != 4:
            raise ValueError(f"Group {group_name} has duplicate teams: {teams}")

    all_drawn = [team for teams in result.values() for team in teams]
    if len(all_drawn) != 48 or len(set(all_drawn)) != 48:
        raise ValueError("Draw did not produce 48 unique teams")

    return result


def group_matchday_pairings() -> list[list[tuple[int, int]]]:
    """Single round-robin for 4 teams across 3 matchdays (2 games each)."""
    return [
        [(0, 1), (2, 3)],
        [(0, 2), (1, 3)],
        [(0, 3), (1, 2)],
    ]


def sample_poisson(rng: random.Random, lam: float) -> int:
    """Knuth Poisson sample."""
    if lam <= 0:
        return 0
    threshold = math.exp(-lam)
    k = 0
    product = 1.0
    while True:
        k += 1
        product *= rng.random()
        if product <= threshold:
            return k - 1


def sample_match_score(
    rng: random.Random,
    lam: float = POISSON_LAMBDA,
    max_goals: int = MAX_GOALS,
) -> tuple[int, int]:
    home = min(sample_poisson(rng, lam), max_goals)
    away = min(sample_poisson(rng, lam), max_goals)
    return home, away


def match_result_points(home_goals: int, away_goals: int) -> tuple[int, int]:
    if home_goals > away_goals:
        return 3, 0
    if home_goals < away_goals:
        return 0, 3
    return 1, 1


def build_dummy_stats(
    home_goals: int,
    away_goals: int,
    rng: random.Random,
) -> tuple[dict[str, int], dict[str, int]]:
    """Possession / shots / shots on target / points for both sides."""
    home_poss = rng.randint(35, 65)
    away_poss = 100 - home_poss
    home_points, away_points = match_result_points(home_goals, away_goals)

    home_shots = home_goals + rng.randint(3, 10)
    away_shots = away_goals + rng.randint(3, 10)
    home_sot = min(home_shots, home_goals + rng.randint(0, max(1, home_shots - home_goals)))
    away_sot = min(away_shots, away_goals + rng.randint(0, max(1, away_shots - away_goals)))
    home_sot = max(home_sot, home_goals)
    away_sot = max(away_sot, away_goals)

    return (
        {
            "Possession": home_poss,
            "Shots": home_shots,
            "ShotsOnTarget": home_sot,
            "Points": home_points,
        },
        {
            "Possession": away_poss,
            "Shots": away_shots,
            "ShotsOnTarget": away_sot,
            "Points": away_points,
        },
    )


def kickoff_for_matchday_slot(start: datetime, matchday: int, slot_in_md: int) -> datetime:
    """
    Stagger 24 fixtures per matchday over 8 calendar days (3 kickoffs/day).
    matchday is 1..3; slot_in_md is 0..23. Matchdays do not overlap.
    """
    hours = (15, 18, 21)
    day_base = (matchday - 1) * 8
    day = day_base + (slot_in_md // 3)
    hour = hours[slot_in_md % 3]
    kickoff = start + timedelta(days=day)
    return kickoff.replace(hour=hour, minute=0, second=0, microsecond=0)


def build_group_stage_schedule(
    group_teams: dict[str, list[Any]],
    kickoff_start: datetime,
    stadium_count: int,
    rng: random.Random,
) -> list[tuple[datetime, str, Any, Any, int]]:
    """
    Build 72 group fixtures.

    Returns list of (kickoff, group_name, home, away, stadium_index).
    `home` / `away` are the objects from group_teams (names or team ids).
    """
    if stadium_count < 1:
        raise ValueError("Need at least one stadium")
    for group_name in GROUP_NAMES:
        teams = group_teams.get(group_name)
        if teams is None or len(teams) != 4:
            raise ValueError(f"Group {group_name} must have exactly 4 teams")

    fixtures: list[tuple[datetime, str, Any, Any, int]] = []
    pairings_by_md = group_matchday_pairings()
    stadium_cursor = 0

    for matchday, pairings in enumerate(pairings_by_md, start=1):
        slot = 0
        for group_name in GROUP_NAMES:
            teams = group_teams[group_name]
            for home_i, away_i in pairings:
                home, away = teams[home_i], teams[away_i]
                if rng.random() < 0.5:
                    home, away = away, home
                kickoff = kickoff_for_matchday_slot(kickoff_start, matchday, slot)
                fixtures.append((kickoff, group_name, home, away, stadium_cursor % stadium_count))
                stadium_cursor += 1
                slot += 1

    if len(fixtures) != 72:
        raise ValueError(f"Expected 72 fixtures, got {len(fixtures)}")
    return fixtures


@dataclass(frozen=True)
class TeamStanding:
    team_id: int
    group: str
    points: int
    goals_for: int
    goals_against: int
    name: str

    @property
    def goal_difference(self) -> int:
        return self.goals_for - self.goals_against

    def sort_key(self) -> tuple[int, int, int, str]:
        # Pts → GD → GF → name (ascending name is last resort)
        return (-self.points, -self.goal_difference, -self.goals_for, self.name.lower())


def rank_standings(rows: Iterable[TeamStanding]) -> list[TeamStanding]:
    return sorted(rows, key=lambda row: row.sort_key())


def rank_group_standings(
    standings_by_group: dict[str, list[TeamStanding]],
) -> dict[str, list[TeamStanding]]:
    """Sort each group's four teams; raise if a group is incomplete."""
    ranked: dict[str, list[TeamStanding]] = {}
    for group_name in GROUP_NAMES:
        rows = standings_by_group.get(group_name, [])
        if len(rows) != 4:
            raise ValueError(f"Group {group_name} needs 4 standings, got {len(rows)}")
        ranked[group_name] = rank_standings(rows)
    return ranked


def select_best_thirds(
    ranked_by_group: dict[str, list[TeamStanding]],
    count: int = 8,
) -> list[TeamStanding]:
    """Third-placed teams ranked Pts → GD → GF → name; take top `count`."""
    thirds = [ranked_by_group[name][2] for name in GROUP_NAMES]
    return rank_standings(thirds)[:count]


def qualify_knockout_teams(
    ranked_by_group: dict[str, list[TeamStanding]],
) -> tuple[dict[str, int], dict[str, int], dict[str, int], list[TeamStanding]]:
    """
    Returns (winners, runners_up, advancing_thirds_by_group, ordered_best_thirds).

    winners/runners_up: group letter → team_id
    advancing_thirds_by_group: group letter → team_id for the 8 best thirds
    """
    winners: dict[str, int] = {}
    runners: dict[str, int] = {}
    for group_name in GROUP_NAMES:
        ordered = ranked_by_group[group_name]
        winners[group_name] = ordered[0].team_id
        runners[group_name] = ordered[1].team_id

    best_thirds = select_best_thirds(ranked_by_group, count=8)
    thirds_by_group = {row.group: row.team_id for row in best_thirds}
    return winners, runners, thirds_by_group, best_thirds


def assign_third_groups_to_slots(advancing_groups: set[str]) -> dict[str, str]:
    """
    Map each third-facing winner slot (e.g. '1A') → advancing group letter.

    Deterministic backtracking over FIFA Art. 12.6 allowed pools (Annexe C
    guarantee: every set of 8 groups has at least one valid assignment).
    """
    if len(advancing_groups) != 8:
        raise ValueError(f"Expected 8 advancing third groups, got {sorted(advancing_groups)}")

    slots = THIRD_PLACE_SLOTS
    assignment: dict[str, str] = {}

    def search(index: int, remaining: set[str]) -> bool:
        if index >= len(slots):
            return True
        slot_name, allowed = slots[index]
        candidates = sorted(group for group in remaining if group in allowed)
        for group in candidates:
            assignment[slot_name] = group
            remaining.remove(group)
            if search(index + 1, remaining):
                return True
            remaining.add(group)
            del assignment[slot_name]
        return False

    if not search(0, set(advancing_groups)):
        raise ValueError(
            f"No valid third-place slot assignment for groups {sorted(advancing_groups)}"
        )
    return assignment


def _resolve_r32_side(
    side: Any,
    winners: dict[str, int],
    runners: dict[str, int],
    thirds_by_group: dict[str, int],
    third_slot_groups: dict[str, str],
) -> int:
    if isinstance(side, tuple) and side[0] == "3":
        winner_slot = str(side[1])
        group = third_slot_groups[winner_slot]
        return thirds_by_group[group]

    label = str(side)
    if len(label) != 2 or label[0] not in ("1", "2") or label[1] not in GROUP_NAMES:
        raise ValueError(f"Invalid R32 place label: {side!r}")
    group = label[1]
    if label[0] == "1":
        return winners[group]
    return runners[group]


def build_r32_team_pairings(
    winners: dict[str, int],
    runners: dict[str, int],
    thirds_by_group: dict[str, int],
) -> list[tuple[int, int]]:
    """16 R32 (team_one, team_two) pairs from FIFA skeleton + third assignment."""
    third_slot_groups = assign_third_groups_to_slots(set(thirds_by_group.keys()))
    pairings: list[tuple[int, int]] = []
    for side_a, side_b in R32_SLOT_TEMPLATE:
        team_a = _resolve_r32_side(side_a, winners, runners, thirds_by_group, third_slot_groups)
        team_b = _resolve_r32_side(side_b, winners, runners, thirds_by_group, third_slot_groups)
        if team_a == team_b:
            raise ValueError(f"R32 pairing collapsed to same team id={team_a}")
        pairings.append((team_a, team_b))
    if len(pairings) != 16:
        raise ValueError(f"Expected 16 R32 pairings, got {len(pairings)}")
    return pairings


def knockout_kickoff(start: datetime, index: int) -> datetime:
    """Stagger knockout fixtures: 4 per day at 15/18/21/00(+1d boundary via hour)."""
    hours = (15, 18, 21, 12)
    day = index // 4
    hour = hours[index % 4]
    kickoff = start + timedelta(days=day)
    return kickoff.replace(hour=hour, minute=0, second=0, microsecond=0)


def ensure_world_cup(cur: PgCursor, dry_run: bool) -> int:
    cur.execute(
        """
        SELECT "Id" FROM "WorldCups"
        WHERE EXTRACT(YEAR FROM "Year" AT TIME ZONE 'UTC') = 2030
        ORDER BY "Id"
        LIMIT 1
        """
    )
    row = cur.fetchone()
    if row:
        return int(row[0])

    if dry_run:
        print(f"[dry-run] Would INSERT WorldCups Year={WC_YEAR.isoformat()}")
        return -1

    cur.execute(
        """
        INSERT INTO "WorldCups" ("Year")
        VALUES (%s)
        RETURNING "Id"
        """,
        (WC_YEAR,),
    )
    return int(cur.fetchone()[0])


def ensure_groups(cur: PgCursor, world_cup_id: int, dry_run: bool) -> dict[str, int]:
    if world_cup_id < 0:
        return {name: -1 for name in GROUP_NAMES}

    cur.execute(
        """
        SELECT "Id", "Name" FROM "Group"
        WHERE "WorldCupId" = %s
        """,
        (world_cup_id,),
    )
    existing = {name: int(gid) for gid, name in cur.fetchall()}

    for name in GROUP_NAMES:
        if name in existing:
            continue
        if dry_run:
            print(f"[dry-run] Would INSERT Group {name} for WorldCupId={world_cup_id}")
            existing[name] = -1
            continue
        cur.execute(
            """
            INSERT INTO "Group" ("Name", "WorldCupId")
            VALUES (%s, %s)
            RETURNING "Id"
            """,
            (name, world_cup_id),
        )
        existing[name] = int(cur.fetchone()[0])

    missing = [name for name in GROUP_NAMES if name not in existing]
    if missing:
        raise SystemExit(f"Missing groups for WorldCup {world_cup_id}: {missing}")

    return {name: existing[name] for name in GROUP_NAMES}


def load_countries(cur: PgCursor) -> dict[str, int]:
    cur.execute('SELECT "Id", "Name" FROM "Countries"')
    return {name: int(cid) for cid, name in cur.fetchall()}


def find_team_in_world_cup(cur: PgCursor, country_id: int, world_cup_id: int) -> int | None:
    cur.execute(
        """
        SELECT t."Id"
        FROM "Team" t
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE t."CountryId" = %s AND g."WorldCupId" = %s
        LIMIT 1
        """,
        (country_id, world_cup_id),
    )
    row = cur.fetchone()
    return int(row[0]) if row else None


def team_has_dependencies(cur: PgCursor, team_id: int) -> bool:
    cur.execute(
        """
        SELECT
            EXISTS (SELECT 1 FROM "Player" p WHERE p."TeamId" = %s)
            OR EXISTS (SELECT 1 FROM "Coach" c WHERE c."TeamId" = %s)
            OR EXISTS (SELECT 1 FROM "Match" m WHERE m."TeamOneId" = %s OR m."TeamTwoId" = %s)
            OR EXISTS (SELECT 1 FROM "Bet" b WHERE b."TeamId" = %s)
            OR EXISTS (SELECT 1 FROM "TeamStats" ts WHERE ts."TeamId" = %s)
        """,
        (team_id, team_id, team_id, team_id, team_id, team_id),
    )
    return bool(cur.fetchone()[0])


def reset_draw_teams(cur: PgCursor, world_cup_id: int, dry_run: bool) -> int:
    """Remove 2030-only teams that have no match/player dependencies (redraw prep)."""
    cur.execute(
        """
        SELECT t."Id"
        FROM "Team" t
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
        """,
        (world_cup_id,),
    )
    team_ids = [int(row[0]) for row in cur.fetchall()]
    removed = 0
    for team_id in team_ids:
        if team_has_dependencies(cur, team_id):
            raise SystemExit(
                f"Cannot --reset-draw: Team Id={team_id} still has players/coaches/matches/bets/stats. "
                "Run with --wipe-matches first (and remove players if needed), then --reset-draw."
            )
        if dry_run:
            print(f"[dry-run] Would DELETE Team Id={team_id} (2030 redraw)")
            removed += 1
            continue
        cur.execute('DELETE FROM "Team" WHERE "Id" = %s', (team_id,))
        removed += 1
    return removed


def assign_draw(
    cur: PgCursor,
    world_cup_id: int,
    group_ids: dict[str, int],
    draw: dict[str, list[str]],
    country_ids: dict[str, int],
    dry_run: bool,
) -> tuple[int, int]:
    """Upsert Team rows for this World Cup. Returns (created, updated)."""
    created = 0
    updated = 0

    for group_name, team_names in draw.items():
        group_id = group_ids[group_name]
        for team_name in team_names:
            if team_name not in country_ids:
                raise SystemExit(f"Country not in DB: {team_name!r}")
            country_id = country_ids[team_name]

            if world_cup_id < 0 or group_id < 0:
                print(f"[dry-run] Would place {team_name} in Group {group_name}")
                created += 1
                continue

            existing_id = find_team_in_world_cup(cur, country_id, world_cup_id)
            if existing_id is not None:
                if dry_run:
                    print(f"[dry-run] Would UPDATE Team Id={existing_id} -> Group {group_name}")
                else:
                    cur.execute(
                        'UPDATE "Team" SET "GroupId" = %s WHERE "Id" = %s',
                        (group_id, existing_id),
                    )
                updated += 1
            else:
                if dry_run:
                    print(f"[dry-run] Would INSERT Team {team_name} -> Group {group_name}")
                else:
                    cur.execute(
                        """
                        INSERT INTO "Team" ("CountryId", "GroupId")
                        VALUES (%s, %s)
                        """,
                        (country_id, group_id),
                    )
                created += 1

    return created, updated


def print_draw(draw: dict[str, list[str]], hosts: list[str]) -> None:
    host_set = set(hosts)
    print("=== WC 2030 group draw ===")
    for group_name in GROUP_NAMES:
        teams = draw[group_name]
        labeled = [f"{name}*" if name in host_set else name for name in teams]
        print(f"  Group {group_name}: {', '.join(labeled)}")
    print("  (* = host)")


def world_cup_team_ids_sql() -> str:
    return """
        SELECT t."Id"
        FROM "Team" t
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
    """


def load_world_cup_match_ids(cur: PgCursor, world_cup_id: int) -> list[int]:
    """
    Matches involving this cup's teams, plus any descendant feeder matches
    (R16→Final TBD rows with null teams).
    """
    cur.execute(
        f"""
        SELECT DISTINCT m."Id" FROM "Match" m
        WHERE m."TeamOneId" IN ({world_cup_team_ids_sql()})
           OR m."TeamTwoId" IN ({world_cup_team_ids_sql()})
        """,
        (world_cup_id, world_cup_id),
    )
    match_ids = {int(row[0]) for row in cur.fetchall()}
    while True:
        if not match_ids:
            break
        cur.execute(
            """
            SELECT "Id" FROM "Match"
            WHERE "FeederMatchOneId" = ANY(%s)
               OR "FeederMatchTwoId" = ANY(%s)
            """,
            (list(match_ids), list(match_ids)),
        )
        added = {int(row[0]) for row in cur.fetchall()} - match_ids
        if not added:
            break
        match_ids |= added
    return sorted(match_ids)


def count_world_cup_matches(cur: PgCursor, world_cup_id: int) -> int:
    return len(load_world_cup_match_ids(cur, world_cup_id))


def count_world_cup_matches_by_stage(
    cur: PgCursor,
    world_cup_id: int,
    stage: int,
) -> int:
    match_ids = load_world_cup_match_ids(cur, world_cup_id)
    if not match_ids:
        return 0
    cur.execute(
        """
        SELECT COUNT(*) FROM "Match"
        WHERE "Id" = ANY(%s) AND "Stage" = %s
        """,
        (match_ids, stage),
    )
    return int(cur.fetchone()[0])


def count_knockout_matches(cur: PgCursor, world_cup_id: int) -> int:
    match_ids = load_world_cup_match_ids(cur, world_cup_id)
    if not match_ids:
        return 0
    cur.execute(
        """
        SELECT COUNT(*) FROM "Match"
        WHERE "Id" = ANY(%s) AND "Stage" <> %s
        """,
        (match_ids, MATCH_STAGE_GROUP),
    )
    return int(cur.fetchone()[0])


def wipe_world_cup_matches(cur: PgCursor, world_cup_id: int, dry_run: bool) -> int:
    """Delete matches (and goals/stats/bets) for this World Cup only (incl. feeder TBD)."""
    match_ids = load_world_cup_match_ids(cur, world_cup_id)
    if not match_ids:
        return 0

    if dry_run:
        print(f"[dry-run] Would wipe {len(match_ids)} match(es) for WorldCupId={world_cup_id}")
        return len(match_ids)

    cur.execute(
        """
        UPDATE "Match"
        SET "FeederMatchOneId" = NULL
        WHERE "FeederMatchOneId" = ANY(%s)
        """,
        (match_ids,),
    )
    cur.execute(
        """
        UPDATE "Match"
        SET "FeederMatchTwoId" = NULL
        WHERE "FeederMatchTwoId" = ANY(%s)
        """,
        (match_ids,),
    )
    cur.execute(
        """
        DELETE FROM "BetResult"
        WHERE "BetId" IN (SELECT "Id" FROM "Bet" WHERE "MatchId" = ANY(%s))
        """,
        (match_ids,),
    )
    cur.execute('DELETE FROM "Bet" WHERE "MatchId" = ANY(%s)', (match_ids,))
    cur.execute(
        """
        DELETE FROM "Goal"
        WHERE "TeamStatsId" IN (SELECT "Id" FROM "TeamStats" WHERE "MatchId" = ANY(%s))
        """,
        (match_ids,),
    )
    cur.execute(
        """
        DELETE FROM "Card"
        WHERE "TeamStatsId" IN (SELECT "Id" FROM "TeamStats" WHERE "MatchId" = ANY(%s))
        """,
        (match_ids,),
    )
    cur.execute('DELETE FROM "TeamStats" WHERE "MatchId" = ANY(%s)', (match_ids,))
    cur.execute('DELETE FROM "Match" WHERE "Id" = ANY(%s)', (match_ids,))
    return len(match_ids)


def load_stadium_ids(cur: PgCursor) -> list[int]:
    cur.execute('SELECT "Id" FROM "Stadium" ORDER BY "Id"')
    ids = [int(row[0]) for row in cur.fetchall()]
    if not ids:
        raise SystemExit("No stadiums in DB — seed stadiums before simulating matches.")
    return ids


def load_teams_by_group(cur: PgCursor, world_cup_id: int) -> dict[str, list[int]]:
    cur.execute(
        """
        SELECT g."Name", t."Id"
        FROM "Group" g
        JOIN "Team" t ON t."GroupId" = g."Id"
        WHERE g."WorldCupId" = %s AND g."Name" = ANY(%s)
        ORDER BY g."Name", t."Id"
        """,
        (world_cup_id, GROUP_NAMES),
    )
    result: dict[str, list[int]] = {name: [] for name in GROUP_NAMES}
    for group_name, team_id in cur.fetchall():
        result[str(group_name)].append(int(team_id))

    bad = {name: teams for name, teams in result.items() if len(teams) != 4}
    if bad:
        raise SystemExit(f"Expected 4 teams per group for WC {world_cup_id}; bad={bad}")
    return result


def resolve_forward_position_id(cur: PgCursor) -> int:
    cur.execute(
        """
        SELECT "Id" FROM "PlayerPositions"
        WHERE "Name" = 'Forward'
        ORDER BY "Id"
        LIMIT 1
        """
    )
    row = cur.fetchone()
    if not row:
        raise SystemExit(
            "Missing PlayerPositions row 'Forward'. Start the API once to seed positions."
        )
    return int(row[0])


def ensure_placeholder_players(cur: PgCursor, world_cup_id: int, dry_run: bool) -> int:
    """Insert Tournament Scorer for 2030 teams that have no real squad players."""
    if dry_run:
        print("[dry-run] Would ensure Tournament Scorer placeholders for empty 2030 squads")
        return 0

    forward_position_id = resolve_forward_position_id(cur)
    cur.execute(
        """
        INSERT INTO "Player" ("Name", "Number", "TeamId", "PositionId")
        SELECT %s, 99, t."Id", %s
        FROM "Team" t
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
          AND NOT EXISTS (
              SELECT 1 FROM "Player" p
              WHERE p."TeamId" = t."Id" AND p."Name" = %s
          )
          AND NOT EXISTS (
              SELECT 1 FROM "Player" p
              WHERE p."TeamId" = t."Id" AND p."Name" <> %s AND p."Number" <> 99
          )
        """,
        (PLACEHOLDER_NAME, forward_position_id, world_cup_id, PLACEHOLDER_NAME, PLACEHOLDER_NAME),
    )
    return int(cur.rowcount)


def load_scorers_by_team(cur: PgCursor, world_cup_id: int) -> dict[int, list[int]]:
    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "Team" t ON t."Id" = p."TeamId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        LEFT JOIN "PlayerPositions" pp ON pp."Id" = p."PositionId"
        WHERE g."WorldCupId" = %s
          AND p."Name" <> %s
          AND p."Number" <> 99
          AND pp."Name" = 'Forward'
        ORDER BY p."TeamId", p."Number"
        """,
        (world_cup_id, PLACEHOLDER_NAME),
    )
    forwards_by_team: dict[int, list[int]] = {}
    for team_id, player_id in cur.fetchall():
        forwards_by_team.setdefault(int(team_id), []).append(int(player_id))

    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "Team" t ON t."Id" = p."TeamId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
          AND p."Name" <> %s
          AND p."Number" <> 99
        ORDER BY p."TeamId", p."Number"
        """,
        (world_cup_id, PLACEHOLDER_NAME),
    )
    any_by_team: dict[int, list[int]] = {}
    for team_id, player_id in cur.fetchall():
        any_by_team.setdefault(int(team_id), []).append(int(player_id))

    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "Team" t ON t."Id" = p."TeamId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s AND p."Name" = %s
        """,
        (world_cup_id, PLACEHOLDER_NAME),
    )
    placeholder_by_team = {int(team_id): int(player_id) for team_id, player_id in cur.fetchall()}

    team_ids = set(forwards_by_team) | set(any_by_team) | set(placeholder_by_team)
    scorers: dict[int, list[int]] = {}
    for team_id in team_ids:
        if team_id in forwards_by_team:
            scorers[team_id] = forwards_by_team[team_id]
        elif team_id in any_by_team:
            scorers[team_id] = any_by_team[team_id]
        else:
            scorers[team_id] = [placeholder_by_team[team_id]]
    return scorers


def pick_scorer(scorers_by_team: dict[int, list[int]], team_id: int, goal_index: int) -> int:
    scorers = scorers_by_team.get(team_id) or []
    if not scorers:
        raise SystemExit(
            f"No scorers for team {team_id}. Placeholder '{PLACEHOLDER_NAME}' should have been created."
        )
    return scorers[goal_index % len(scorers)]


def simulate_group_stage(
    cur: PgCursor,
    world_cup_id: int,
    rng: random.Random,
    dry_run: bool,
    group_kickoff_start: datetime | None = None,
) -> tuple[int, int]:
    """Schedule and simulate 72 group matches. Returns (matches, goals)."""
    if group_kickoff_start is None:
        group_kickoff_start, _ = demo_kickoff_anchors()
    stadium_ids = load_stadium_ids(cur)
    teams_by_group = load_teams_by_group(cur, world_cup_id)
    fixtures = build_group_stage_schedule(
        teams_by_group,
        group_kickoff_start,
        len(stadium_ids),
        rng,
    )

    if dry_run:
        sample = fixtures[:3]
        print(f"[dry-run] Would insert {len(fixtures)} group matches (sample kickoffs):")
        for kickoff, group_name, home, away, stadium_idx in sample:
            home_g, away_g = sample_match_score(rng)
            print(
                f"  Group {group_name}: Team {home} vs Team {away} "
                f"@ stadium[{stadium_idx}] {kickoff.isoformat()} → {home_g}-{away_g}"
            )
        return len(fixtures), 0

    placeholders = ensure_placeholder_players(cur, world_cup_id, dry_run=False)
    print(f"Placeholder scorers ensured (inserted rowcount={placeholders})")
    scorers_by_team = load_scorers_by_team(cur, world_cup_id)

    matches_inserted = 0
    goals_inserted = 0

    for kickoff, _group_name, home_id, away_id, stadium_idx in fixtures:
        stadium_id = stadium_ids[stadium_idx]
        home_goals, away_goals = sample_match_score(rng)
        home_stats, away_stats = build_dummy_stats(home_goals, away_goals, rng)

        cur.execute(
            """
            INSERT INTO "Match"
                ("Date", "StadiumId", "TeamOneId", "TeamTwoId", "Stage",
                 "FeederOneTakesLoser", "FeederTwoTakesLoser")
            VALUES (%s, %s, %s, %s, %s, FALSE, FALSE)
            RETURNING "Id"
            """,
            (kickoff, stadium_id, home_id, away_id, MATCH_STAGE_GROUP),
        )
        match_id = int(cur.fetchone()[0])

        cur.execute(
            """
            INSERT INTO "TeamStats"
                ("MatchId", "TeamId", "Possession", "Shots", "ShotsOnTarget", "Points")
            VALUES (%s, %s, %s, %s, %s, %s)
            RETURNING "Id"
            """,
            (
                match_id,
                home_id,
                home_stats["Possession"],
                home_stats["Shots"],
                home_stats["ShotsOnTarget"],
                home_stats["Points"],
            ),
        )
        stats_one = int(cur.fetchone()[0])

        cur.execute(
            """
            INSERT INTO "TeamStats"
                ("MatchId", "TeamId", "Possession", "Shots", "ShotsOnTarget", "Points")
            VALUES (%s, %s, %s, %s, %s, %s)
            RETURNING "Id"
            """,
            (
                match_id,
                away_id,
                away_stats["Possession"],
                away_stats["Shots"],
                away_stats["ShotsOnTarget"],
                away_stats["Points"],
            ),
        )
        stats_two = int(cur.fetchone()[0])

        for minute_offset in range(home_goals):
            scored_at = kickoff + timedelta(minutes=10 + minute_offset * 12)
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (pick_scorer(scorers_by_team, int(home_id), minute_offset), stats_one, scored_at),
            )
            goals_inserted += 1

        for minute_offset in range(away_goals):
            scored_at = kickoff + timedelta(minutes=15 + minute_offset * 12)
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (pick_scorer(scorers_by_team, int(away_id), minute_offset), stats_two, scored_at),
            )
            goals_inserted += 1

        matches_inserted += 1

    return matches_inserted, goals_inserted


def load_group_standings(cur: PgCursor, world_cup_id: int) -> dict[str, list[TeamStanding]]:
    """Compute Pts/GF/GA per team from finished group matches (goals + TeamStats.Points)."""
    cur.execute(
        """
        SELECT t."Id", g."Name", c."Name"
        FROM "Team" t
        JOIN "Group" g ON g."Id" = t."GroupId"
        JOIN "Countries" c ON c."Id" = t."CountryId"
        WHERE g."WorldCupId" = %s AND g."Name" = ANY(%s)
        """,
        (world_cup_id, GROUP_NAMES),
    )
    teams: dict[int, tuple[str, str]] = {
        int(team_id): (str(group_name), str(country_name))
        for team_id, group_name, country_name in cur.fetchall()
    }
    if len(teams) != 48:
        raise SystemExit(f"Expected 48 teams for standings, found {len(teams)}")

    stats: dict[int, dict[str, int]] = {
        team_id: {"points": 0, "gf": 0, "ga": 0} for team_id in teams
    }

    cur.execute(
        f"""
        SELECT m."Id", m."TeamOneId", m."TeamTwoId"
        FROM "Match" m
        WHERE m."Stage" = %s
          AND m."TeamOneId" IN ({world_cup_team_ids_sql()})
          AND m."TeamTwoId" IN ({world_cup_team_ids_sql()})
        """,
        (MATCH_STAGE_GROUP, world_cup_id, world_cup_id),
    )
    group_matches = [(int(r[0]), int(r[1]), int(r[2])) for r in cur.fetchall()]
    if len(group_matches) != 72:
        raise SystemExit(
            f"Need 72 finished group matches before bracket; found {len(group_matches)}"
        )

    for match_id, home_id, away_id in group_matches:
        cur.execute(
            """
            SELECT ts."TeamId", ts."Points",
                   (SELECT COUNT(*) FROM "Goal" g
                    WHERE g."TeamStatsId" = ts."Id" AND g."IsOwnGoal" = 0),
                   (SELECT COUNT(*) FROM "Goal" g
                    WHERE g."TeamStatsId" = ts."Id" AND g."IsOwnGoal" <> 0)
            FROM "TeamStats" ts
            WHERE ts."MatchId" = %s
            """,
            (match_id,),
        )
        rows = cur.fetchall()
        if len(rows) != 2:
            raise SystemExit(f"Match {match_id} missing TeamStats rows")

        side_data: dict[int, tuple[int, int, int]] = {}
        for team_id, points, non_og, own_goals in rows:
            side_data[int(team_id)] = (int(points), int(non_og), int(own_goals))

        if home_id not in side_data or away_id not in side_data:
            raise SystemExit(f"Match {match_id} TeamStats teams do not match fixture")

        home_pts, home_non_og, home_og = side_data[home_id]
        away_pts, away_non_og, away_og = side_data[away_id]
        # Own goals credit the opponent (GoalService convention).
        home_gf = home_non_og + away_og
        away_gf = away_non_og + home_og

        stats[home_id]["points"] += home_pts
        stats[away_id]["points"] += away_pts
        stats[home_id]["gf"] += home_gf
        stats[away_id]["gf"] += away_gf
        stats[home_id]["ga"] += away_gf
        stats[away_id]["ga"] += home_gf

    by_group: dict[str, list[TeamStanding]] = {name: [] for name in GROUP_NAMES}
    for team_id, (group_name, country_name) in teams.items():
        row = stats[team_id]
        by_group[group_name].append(
            TeamStanding(
                team_id=team_id,
                group=group_name,
                points=row["points"],
                goals_for=row["gf"],
                goals_against=row["ga"],
                name=country_name,
            )
        )
    return by_group


def _insert_knockout_match(
    cur: PgCursor,
    stage: int,
    kickoff: datetime,
    stadium_id: int,
    team_one_id: int | None,
    team_two_id: int | None,
    feeder_one_id: int | None,
    feeder_two_id: int | None,
) -> int:
    cur.execute(
        """
        INSERT INTO "Match"
            ("Date", "StadiumId", "TeamOneId", "TeamTwoId", "Stage",
             "FeederMatchOneId", "FeederMatchTwoId",
             "FeederOneTakesLoser", "FeederTwoTakesLoser")
        VALUES (%s, %s, %s, %s, %s, %s, %s, FALSE, FALSE)
        RETURNING "Id"
        """,
        (
            kickoff,
            stadium_id,
            team_one_id,
            team_two_id,
            stage,
            feeder_one_id,
            feeder_two_id,
        ),
    )
    return int(cur.fetchone()[0])


def build_knockout_bracket(
    cur: PgCursor,
    world_cup_id: int,
    dry_run: bool,
    knockout_kickoff_start: datetime | None = None,
) -> int:
    """
    Qualify 32 teams and insert R32→Final (31 matches, no ThirdPlace).
    R32 has teams filled; later rounds are TBD with feeder FKs.
    """
    if knockout_kickoff_start is None:
        _, knockout_kickoff_start = demo_kickoff_anchors()
    stadium_ids = load_stadium_ids(cur)
    standings = load_group_standings(cur, world_cup_id)
    ranked = rank_group_standings(standings)
    winners, runners, thirds_by_group, best_thirds = qualify_knockout_teams(ranked)

    print(
        "Best thirds: "
        + ", ".join(f"3{row.group}={row.name} ({row.points}pts)" for row in best_thirds)
    )

    r32_pairings = build_r32_team_pairings(winners, runners, thirds_by_group)
    if dry_run:
        print(f"[dry-run] Would insert {KNOCKOUT_MATCH_COUNT} knockout matches "
              f"(16 R32 seeded + feeders through Final)")
        for index, (a, b) in enumerate(r32_pairings[:4]):
            print(f"  R32[{index}]: Team {a} vs Team {b}")
        return KNOCKOUT_MATCH_COUNT

    kickoff_index = 0
    r32_ids: list[int] = []
    for team_one, team_two in r32_pairings:
        match_id = _insert_knockout_match(
            cur,
            MATCH_STAGE_ROUND_OF_32,
            knockout_kickoff(knockout_kickoff_start, kickoff_index),
            stadium_ids[kickoff_index % len(stadium_ids)],
            team_one,
            team_two,
            None,
            None,
        )
        r32_ids.append(match_id)
        kickoff_index += 1

    r16_ids: list[int] = []
    for feeder_a, feeder_b in R16_FEEDERS:
        match_id = _insert_knockout_match(
            cur,
            MATCH_STAGE_ROUND_OF_16,
            knockout_kickoff(knockout_kickoff_start, kickoff_index),
            stadium_ids[kickoff_index % len(stadium_ids)],
            None,
            None,
            r32_ids[feeder_a],
            r32_ids[feeder_b],
        )
        r16_ids.append(match_id)
        kickoff_index += 1

    qf_ids: list[int] = []
    for feeder_a, feeder_b in QF_FEEDERS:
        match_id = _insert_knockout_match(
            cur,
            MATCH_STAGE_QUARTER_FINAL,
            knockout_kickoff(knockout_kickoff_start, kickoff_index),
            stadium_ids[kickoff_index % len(stadium_ids)],
            None,
            None,
            r16_ids[feeder_a],
            r16_ids[feeder_b],
        )
        qf_ids.append(match_id)
        kickoff_index += 1

    sf_ids: list[int] = []
    for feeder_a, feeder_b in SF_FEEDERS:
        match_id = _insert_knockout_match(
            cur,
            MATCH_STAGE_SEMI_FINAL,
            knockout_kickoff(knockout_kickoff_start, kickoff_index),
            stadium_ids[kickoff_index % len(stadium_ids)],
            None,
            None,
            qf_ids[feeder_a],
            qf_ids[feeder_b],
        )
        sf_ids.append(match_id)
        kickoff_index += 1

    _insert_knockout_match(
        cur,
        MATCH_STAGE_FINAL,
        knockout_kickoff(knockout_kickoff_start, kickoff_index),
        stadium_ids[kickoff_index % len(stadium_ids)],
        None,
        None,
        sf_ids[0],
        sf_ids[1],
    )

    created = len(r32_ids) + len(r16_ids) + len(qf_ids) + len(sf_ids) + 1
    if created != KNOCKOUT_MATCH_COUNT:
        raise SystemExit(f"Expected {KNOCKOUT_MATCH_COUNT} knockout matches, created {created}")
    return created


def run(
    seed: int,
    dry_run: bool,
    reset_draw: bool,
    wipe_matches: bool,
    skip_group_sim: bool,
    skip_knockout: bool,
) -> None:
    rng = random.Random(seed)
    conn: PgConnection = psycopg2.connect(CONN)
    conn.autocommit = False
    cur = conn.cursor()

    try:
        # Fail fast if unique CountryId still blocks multi-cup inserts.
        cur.execute(
            """
            SELECT i.indisunique
            FROM pg_index i
            JOIN pg_class c ON c.oid = i.indexrelid
            WHERE c.relname = 'IX_Team_CountryId'
            """
        )
        index_row = cur.fetchone()
        if index_row and bool(index_row[0]):
            raise SystemExit(
                "IX_Team_CountryId is still UNIQUE. Apply migration "
                "AllowMultipleTeamsPerCountry (or restart API so Program.cs repair runs) "
                "before drawing WC 2030 alongside WC 2026."
            )

        country_ids = load_countries(cur)
        hosts, others = build_team_pool(country_ids.keys(), HOST_NAMES, rng, other_count=42)
        draw = draw_groups(hosts, others, rng)
        print_draw(draw, hosts)
        print(f"Seed={seed} hosts={len(hosts)} others={len(others)}")

        world_cup_id = ensure_world_cup(cur, dry_run)
        print(f"WorldCup 2030 Id={world_cup_id if world_cup_id > 0 else '(dry-run pending)'}")

        group_kickoff_start, knockout_kickoff_start = demo_kickoff_anchors()
        print(
            f"Demo kickoffs: group from {group_kickoff_start.isoformat()} "
            f"(Finished vs UtcNow); knockout from {knockout_kickoff_start.isoformat()} "
            f"(Scheduled / CanBet). Cup Year={WC_YEAR.date().isoformat()}."
        )

        if wipe_matches and world_cup_id > 0:
            wiped = wipe_world_cup_matches(cur, world_cup_id, dry_run)
            print(f"Wiped {wiped} prior 2030 match(es)")

        bracket_only = False
        if not dry_run and world_cup_id > 0 and not wipe_matches:
            existing = count_world_cup_matches(cur, world_cup_id)
            existing_group = count_world_cup_matches_by_stage(
                cur, world_cup_id, MATCH_STAGE_GROUP
            )
            existing_ko = count_knockout_matches(cur, world_cup_id)
            if existing > 0:
                if not skip_group_sim:
                    raise SystemExit(
                        f"WorldCup {world_cup_id} already has {existing} match(es). "
                        "Re-run with --wipe-matches before redrawing teams or resimulating "
                        "(avoids reassigning teams while fixtures still exist)."
                    )
                # Bracket-only path: keep existing 72 group matches, add knockout.
                if skip_knockout:
                    raise SystemExit(
                        f"WorldCup {world_cup_id} already has {existing} match(es) and "
                        "--skip-group-sim --skip-knockout leaves nothing to do. "
                        "Use --wipe-matches to resimulate."
                    )
                if existing_ko > 0:
                    raise SystemExit(
                        f"WorldCup {world_cup_id} already has {existing_ko} knockout match(es). "
                        "Re-run with --wipe-matches before rebuilding the bracket."
                    )
                if existing_group != 72:
                    raise SystemExit(
                        f"Bracket-only mode needs exactly 72 group matches; found {existing_group}."
                    )
                bracket_only = True
                print(
                    f"Keeping {existing_group} existing group matches; "
                    "building knockout bracket only (no redraw)."
                )

        if bracket_only:
            # Existing fixtures lock team/group membership — do not re-draw.
            pass
        else:
            if reset_draw and world_cup_id > 0:
                removed = reset_draw_teams(cur, world_cup_id, dry_run)
                print(f"Reset draw removed {removed} prior 2030 Team row(s)")

            group_ids = ensure_groups(cur, world_cup_id, dry_run)
            created, updated = assign_draw(
                cur, world_cup_id, group_ids, draw, country_ids, dry_run
            )
            print(f"Teams created={created} updated={updated}")

        if not skip_group_sim and world_cup_id > 0:
            matches, goals = simulate_group_stage(
                cur, world_cup_id, rng, dry_run, group_kickoff_start
            )
            print(f"Group stage matches={matches} goals={goals}")
        elif skip_group_sim:
            print("Skipped group simulation (--skip-group-sim).")

        if not skip_knockout and world_cup_id > 0:
            if dry_run and count_world_cup_matches_by_stage(
                cur, world_cup_id, MATCH_STAGE_GROUP
            ) != 72:
                print(
                    "[dry-run] Would build R32→Final bracket after group sim "
                    f"({KNOCKOUT_MATCH_COUNT} matches; skipped detail — no 72 group rows in DB)."
                )
            else:
                ko_count = build_knockout_bracket(
                    cur, world_cup_id, dry_run, knockout_kickoff_start
                )
                print(f"Knockout matches={ko_count}")
        elif skip_knockout:
            print("Skipped knockout bracket (--skip-knockout).")

        if dry_run:
            conn.rollback()
            print("Dry-run complete — no changes committed.")
        else:
            conn.commit()
            # Verify 12×4
            cur.execute(
                """
                SELECT g."Name", COUNT(t."Id")
                FROM "Group" g
                LEFT JOIN "Team" t ON t."GroupId" = g."Id"
                WHERE g."WorldCupId" = %s AND g."Name" = ANY(%s)
                GROUP BY g."Name"
                ORDER BY g."Name"
                """,
                (world_cup_id, GROUP_NAMES),
            )
            counts = cur.fetchall()
            bad = [(name, n) for name, n in counts if int(n) != 4]
            if bad or len(counts) != 12:
                raise SystemExit(f"Verification failed — group sizes: {counts}")
            print("Verified: 12 groups x 4 teams for World Cup 2030.")

            if not skip_group_sim or not skip_knockout:
                group_stage = count_world_cup_matches_by_stage(
                    cur, world_cup_id, MATCH_STAGE_GROUP
                )
                if group_stage != 72:
                    raise SystemExit(
                        f"Verification failed — expected 72 group-stage matches, got {group_stage}"
                    )
                print("Verified: 72 group-stage matches for World Cup 2030.")

            if not skip_knockout:
                ko = count_knockout_matches(cur, world_cup_id)
                if ko != KNOCKOUT_MATCH_COUNT:
                    raise SystemExit(
                        f"Verification failed — expected {KNOCKOUT_MATCH_COUNT} "
                        f"knockout matches, got {ko}"
                    )
                r32 = count_world_cup_matches_by_stage(
                    cur, world_cup_id, MATCH_STAGE_ROUND_OF_32
                )
                if r32 != 16:
                    raise SystemExit(f"Verification failed — expected 16 R32 matches, got {r32}")
                total = count_world_cup_matches(cur, world_cup_id)
                expected_total = 72 + KNOCKOUT_MATCH_COUNT
                if total != expected_total:
                    raise SystemExit(
                        f"Verification failed — expected {expected_total} total matches, got {total}"
                    )
                print(
                    f"Verified: {KNOCKOUT_MATCH_COUNT} knockout matches "
                    f"(16 R32 + feeders through Final); total {total}."
                )
    except Exception:
        conn.rollback()
        raise
    finally:
        cur.close()
        conn.close()


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="WC 2030 draw + group sim + knockout bracket (Phase 12 Tasks 1–3)"
    )
    parser.add_argument("--seed", type=int, default=2030, help="RNG seed for draw + scores (default 2030)")
    parser.add_argument("--dry-run", action="store_true", help="Print plan; roll back DB writes")
    parser.add_argument(
        "--reset-draw",
        action="store_true",
        help="Delete existing dependency-free 2030 Team rows before re-assigning",
    )
    parser.add_argument(
        "--wipe-matches",
        action="store_true",
        help="Delete existing 2030 matches/goals/stats/bets before group simulation",
    )
    parser.add_argument(
        "--skip-group-sim",
        action="store_true",
        help="Skip scheduling/simulating group matches (draw only, or bracket-only with existing 72)",
    )
    parser.add_argument(
        "--skip-knockout",
        action="store_true",
        help="Skip best-thirds qualification and R32→Final bracket",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> None:
    args = parse_args(argv)
    run(
        seed=args.seed,
        dry_run=args.dry_run,
        reset_draw=args.reset_draw,
        wipe_matches=args.wipe_matches,
        skip_group_sim=args.skip_group_sim,
        skip_knockout=args.skip_knockout,
    )


if __name__ == "__main__":
    main()
