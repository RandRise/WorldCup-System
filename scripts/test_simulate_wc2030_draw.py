"""Unit tests for WC 2030 helpers (Phase 12 Tasks 1–3) — no DB required.

Task 1 draw + Task 2 group-sim + Task 3 standings/best-thirds/bracket.

Run: python test_simulate_wc2030_draw.py
"""

from __future__ import annotations

import itertools
import random
import unittest
from datetime import datetime, timedelta, timezone

from simulate_wc2030 import (
    GROUP_FINISHED_PAD_DAYS,
    GROUP_NAMES,
    GROUP_SPAN_DAYS,
    HOST_NAMES,
    KNOCKOUT_FUTURE_PAD_DAYS,
    KNOCKOUT_MATCH_COUNT,
    QF_FEEDERS,
    R16_FEEDERS,
    R32_SLOT_TEMPLATE,
    SF_FEEDERS,
    THIRD_PLACE_SLOTS,
    TeamStanding,
    assign_third_groups_to_slots,
    build_dummy_stats,
    build_group_stage_schedule,
    build_r32_team_pairings,
    build_team_pool,
    demo_kickoff_anchors,
    draw_groups,
    group_matchday_pairings,
    kickoff_for_matchday_slot,
    knockout_kickoff,
    match_result_points,
    qualify_knockout_teams,
    rank_group_standings,
    rank_standings,
    resolve_country,
    sample_match_score,
    sample_poisson,
    select_best_thirds,
)


def _fake_countries(n: int = 80) -> list[str]:
    names = list(HOST_NAMES)
    i = 0
    while len(names) < n:
        names.append(f"Country{i:03d}")
        i += 1
    return names


def _standing(
    team_id: int,
    group: str,
    points: int,
    gf: int,
    ga: int,
    name: str | None = None,
) -> TeamStanding:
    return TeamStanding(
        team_id=team_id,
        group=group,
        points=points,
        goals_for=gf,
        goals_against=ga,
        name=name or f"Team{team_id}",
    )


def _ranked_groups_uniform() -> dict[str, list[TeamStanding]]:
    """Each group: clear 1st/2nd/3rd/4th by points."""
    ranked: dict[str, list[TeamStanding]] = {}
    team_id = 1
    for group in GROUP_NAMES:
        rows = [
            _standing(team_id, group, 9, 6, 1, f"{group}1"),
            _standing(team_id + 1, group, 6, 4, 2, f"{group}2"),
            _standing(team_id + 2, group, 3, 2, 3, f"{group}3"),
            _standing(team_id + 3, group, 0, 1, 7, f"{group}4"),
        ]
        team_id += 4
        ranked[group] = rows
    return ranked


class DrawHelperTests(unittest.TestCase):
    def test_resolve_country_aliases(self) -> None:
        self.assertEqual(resolve_country("USA"), "United States")
        self.assertEqual(resolve_country("Spain"), "Spain")

    def test_build_team_pool_size_and_excludes_hosts(self) -> None:
        rng = random.Random(2030)
        hosts, others = build_team_pool(_fake_countries(80), HOST_NAMES, rng, other_count=42)
        self.assertEqual(len(hosts), 6)
        self.assertEqual(len(others), 42)
        self.assertTrue(set(hosts).isdisjoint(set(others)))
        self.assertEqual(set(hosts), set(HOST_NAMES))

    def test_build_team_pool_reproducible(self) -> None:
        a_hosts, a_others = build_team_pool(_fake_countries(80), HOST_NAMES, random.Random(7), 42)
        b_hosts, b_others = build_team_pool(_fake_countries(80), HOST_NAMES, random.Random(7), 42)
        self.assertEqual(a_hosts, b_hosts)
        self.assertEqual(a_others, b_others)

    def test_draw_groups_shape_and_host_separation(self) -> None:
        rng = random.Random(2030)
        hosts, others = build_team_pool(_fake_countries(80), HOST_NAMES, rng, 42)
        draw = draw_groups(hosts, others, rng)

        self.assertEqual(list(draw.keys()), list("ABCDEFGHIJKL"))
        self.assertTrue(all(len(teams) == 4 for teams in draw.values()))

        flat = [name for teams in draw.values() for name in teams]
        self.assertEqual(len(flat), 48)
        self.assertEqual(len(set(flat)), 48)

        host_groups = [g for g, teams in draw.items() if any(h in teams for h in hosts)]
        self.assertEqual(len(host_groups), 6)
        self.assertEqual(len(set(host_groups)), 6)

    def test_draw_groups_rejects_wrong_counts(self) -> None:
        with self.assertRaisesRegex(ValueError, "6 hosts"):
            draw_groups(["A"], ["x"] * 42, random.Random(1))
        with self.assertRaisesRegex(ValueError, "42 others"):
            draw_groups(HOST_NAMES, ["x"] * 41, random.Random(1))

    def test_build_team_pool_missing_host_exits(self) -> None:
        with self.assertRaisesRegex(SystemExit, "Host country not found"):
            build_team_pool(["Portugal", "Morocco"], HOST_NAMES, random.Random(1), 42)


class GroupSimHelperTests(unittest.TestCase):
    def test_group_matchday_pairings_cover_all_pairs(self) -> None:
        pairings = group_matchday_pairings()
        self.assertEqual(len(pairings), 3)
        flat = [pair for md in pairings for pair in md]
        self.assertEqual(len(flat), 6)
        normalized = {frozenset(pair) for pair in flat}
        self.assertEqual(len(normalized), 6)
        for i in range(4):
            for j in range(i + 1, 4):
                self.assertIn(frozenset((i, j)), normalized)

    def test_build_group_stage_schedule_shape_and_games_per_team(self) -> None:
        group_teams = {name: [f"{name}{i}" for i in range(4)] for name in GROUP_NAMES}
        start = datetime(2030, 6, 13, 15, 0, tzinfo=timezone.utc)
        fixtures = build_group_stage_schedule(group_teams, start, stadium_count=16, rng=random.Random(2030))

        self.assertEqual(len(fixtures), 72)
        plays: dict[str, int] = {}
        for kickoff, group_name, home, away, stadium_idx in fixtures:
            self.assertEqual(kickoff.tzinfo, timezone.utc)
            self.assertIn(group_name, GROUP_NAMES)
            self.assertGreaterEqual(stadium_idx, 0)
            self.assertLess(stadium_idx, 16)
            plays[home] = plays.get(home, 0) + 1
            plays[away] = plays.get(away, 0) + 1

        self.assertEqual(len(plays), 48)
        self.assertTrue(all(count == 3 for count in plays.values()))

    def test_build_group_stage_schedule_reproducible(self) -> None:
        group_teams = {name: [f"{name}{i}" for i in range(4)] for name in GROUP_NAMES}
        start = datetime(2030, 6, 13, 15, 0, tzinfo=timezone.utc)
        a = build_group_stage_schedule(group_teams, start, 8, random.Random(99))
        b = build_group_stage_schedule(group_teams, start, 8, random.Random(99))
        self.assertEqual(a, b)

    def test_sample_poisson_and_score_bounds(self) -> None:
        rng = random.Random(2030)
        for _ in range(200):
            value = sample_poisson(rng, 1.35)
            self.assertGreaterEqual(value, 0)
            home, away = sample_match_score(rng, lam=1.35, max_goals=7)
            self.assertGreaterEqual(home, 0)
            self.assertGreaterEqual(away, 0)
            self.assertLessEqual(home, 7)
            self.assertLessEqual(away, 7)

    def test_sample_match_score_reproducible(self) -> None:
        self.assertEqual(
            sample_match_score(random.Random(42)),
            sample_match_score(random.Random(42)),
        )

    def test_match_result_points(self) -> None:
        self.assertEqual(match_result_points(2, 1), (3, 0))
        self.assertEqual(match_result_points(0, 1), (0, 3))
        self.assertEqual(match_result_points(1, 1), (1, 1))

    def test_build_dummy_stats_consistency(self) -> None:
        home, away = build_dummy_stats(3, 1, random.Random(1))
        self.assertEqual(home["Possession"] + away["Possession"], 100)
        self.assertEqual(home["Points"], 3)
        self.assertEqual(away["Points"], 0)
        self.assertGreaterEqual(home["ShotsOnTarget"], 3)
        self.assertGreaterEqual(away["ShotsOnTarget"], 1)
        self.assertGreaterEqual(home["Shots"], home["ShotsOnTarget"])
        self.assertGreaterEqual(away["Shots"], away["ShotsOnTarget"])

    def test_kickoff_for_matchday_slot_ordering(self) -> None:
        start = datetime(2030, 6, 13, 15, 0, tzinfo=timezone.utc)
        first = kickoff_for_matchday_slot(start, 1, 0)
        last_md1 = kickoff_for_matchday_slot(start, 1, 23)
        first_md2 = kickoff_for_matchday_slot(start, 2, 0)
        self.assertLess(first, last_md1)
        self.assertLess(last_md1, first_md2)


class BracketHelperTests(unittest.TestCase):
    def test_rank_standings_pts_gd_gf_name(self) -> None:
        rows = [
            _standing(1, "A", 4, 3, 2, "Zeta"),
            _standing(2, "A", 4, 5, 2, "Alpha"),  # better GD
            _standing(3, "A", 4, 5, 2, "Beta"),  # same GD/GF → name
            _standing(4, "A", 6, 1, 0, "Gamma"),
        ]
        ranked = rank_standings(rows)
        self.assertEqual([r.team_id for r in ranked], [4, 2, 3, 1])

    def test_select_best_thirds_takes_top_eight(self) -> None:
        ranked = _ranked_groups_uniform()
        # Boost thirds in A–D so they lose; E–L thirds stay at 3 pts → top 8 are E–L
        for group in "ABCD":
            third = ranked[group][2]
            ranked[group][2] = TeamStanding(
                team_id=third.team_id,
                group=group,
                points=0,
                goals_for=0,
                goals_against=9,
                name=third.name,
            )
        best = select_best_thirds(ranked, count=8)
        self.assertEqual(len(best), 8)
        self.assertEqual({row.group for row in best}, set("EFGHIJKL"))

    def test_qualify_knockout_teams_shape(self) -> None:
        ranked = rank_group_standings(_ranked_groups_uniform())
        winners, runners, thirds, ordered = qualify_knockout_teams(ranked)
        self.assertEqual(len(winners), 12)
        self.assertEqual(len(runners), 12)
        self.assertEqual(len(thirds), 8)
        self.assertEqual(len(ordered), 8)
        self.assertEqual(len(set(winners.values()) | set(runners.values()) | set(thirds.values())), 32)

    def test_assign_third_groups_respects_allowed_pools(self) -> None:
        advancing = set("BDEFIJKL")  # WC 2026 option #67 style
        assignment = assign_third_groups_to_slots(advancing)
        self.assertEqual(set(assignment.values()), advancing)
        allowed = {slot: pool for slot, pool in THIRD_PLACE_SLOTS}
        for slot, group in assignment.items():
            self.assertIn(group, allowed[slot])

    def test_assign_third_groups_all_combinations_of_eight(self) -> None:
        """Annexe C guarantee: every C(12,8) set has a valid assignment."""
        failures = 0
        for combo in itertools.combinations(GROUP_NAMES, 8):
            try:
                assignment = assign_third_groups_to_slots(set(combo))
            except ValueError:
                failures += 1
                continue
            self.assertEqual(set(assignment.values()), set(combo))
        self.assertEqual(failures, 0)

    def test_build_r32_pairings_sixteen_unique_teams(self) -> None:
        ranked = rank_group_standings(_ranked_groups_uniform())
        winners, runners, thirds, _ = qualify_knockout_teams(ranked)
        pairings = build_r32_team_pairings(winners, runners, thirds)
        self.assertEqual(len(pairings), 16)
        self.assertEqual(len(R32_SLOT_TEMPLATE), 16)
        flat = [team for pair in pairings for team in pair]
        self.assertEqual(len(flat), 32)
        self.assertEqual(len(set(flat)), 32)

    def test_feeder_index_shapes(self) -> None:
        self.assertEqual(len(R16_FEEDERS), 8)
        self.assertEqual(len(QF_FEEDERS), 4)
        self.assertEqual(len(SF_FEEDERS), 2)
        for a, b in R16_FEEDERS:
            self.assertNotEqual(a, b)
            self.assertGreaterEqual(a, 0)
            self.assertLess(a, 16)
            self.assertGreaterEqual(b, 0)
            self.assertLess(b, 16)
        self.assertEqual(KNOCKOUT_MATCH_COUNT, 31)

    def test_knockout_kickoff_ordering(self) -> None:
        start = datetime(2030, 7, 7, 15, 0, tzinfo=timezone.utc)
        self.assertLess(knockout_kickoff(start, 0), knockout_kickoff(start, 4))

    def test_demo_kickoff_anchors_group_past_knockout_future(self) -> None:
        now = datetime(2026, 7, 21, 12, 0, tzinfo=timezone.utc)
        group_start, knockout_start = demo_kickoff_anchors(now)
        last_group = kickoff_for_matchday_slot(group_start, 3, 23)
        # StandingsService / MatchService: Finished when kickoff + 90m < now
        self.assertLess(last_group + timedelta(minutes=90), now)
        self.assertGreater(knockout_start, now)
        self.assertEqual(
            group_start,
            (now - timedelta(days=GROUP_SPAN_DAYS + GROUP_FINISHED_PAD_DAYS)).replace(
                hour=15, minute=0, second=0, microsecond=0
            ),
        )
        self.assertEqual(
            knockout_start,
            (now + timedelta(days=KNOCKOUT_FUTURE_PAD_DAYS)).replace(
                hour=15, minute=0, second=0, microsecond=0
            ),
        )


if __name__ == "__main__":
    unittest.main()
