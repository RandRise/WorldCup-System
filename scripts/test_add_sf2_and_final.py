"""Unit tests for Phase 9 Final FT helpers — no DB required.

Covers apply_final_ft paths and Final MATCHES row / stage constant.

Run: python test_add_sf2_and_final.py
"""

from __future__ import annotations

import unittest
from datetime import datetime, timedelta, timezone
from unittest.mock import MagicMock, patch

import add_sf2_and_final as closeout
import import_wc2026_finished_matches as importer


class UtcHelperTests(unittest.TestCase):
    def test_utc_parses_z_suffix(self) -> None:
        kickoff = closeout.utc("2026-07-19T19:00:00Z")
        self.assertEqual(kickoff.tzinfo, timezone.utc)
        self.assertEqual(kickoff, datetime(2026, 7, 19, 19, 0, tzinfo=timezone.utc))


class CloseoutConstantTests(unittest.TestCase):
    def test_final_stage_and_scorer_constants(self) -> None:
        self.assertEqual(closeout.FINAL, 5)
        self.assertEqual(closeout.FINAL_SCORER_NAME, "Ferran Torres")
        self.assertEqual(closeout.FINAL_GOAL_MINUTE, 106)
        self.assertEqual(importer.FINAL, 5)


class PickScorerTests(unittest.TestCase):
    def test_pick_scorer_wraps_by_index(self) -> None:
        scorers = {10: [100, 200]}
        self.assertEqual(closeout.pick_scorer(scorers, 10, 0), 100)
        self.assertEqual(closeout.pick_scorer(scorers, 10, 1), 200)
        self.assertEqual(closeout.pick_scorer(scorers, 10, 2), 100)

    def test_pick_scorer_exits_when_squad_empty(self) -> None:
        with self.assertRaises(SystemExit):
            closeout.pick_scorer({}, 10, 0)


class FinalMatchesRowTests(unittest.TestCase):
    def test_matches_includes_final_argentina_spain_0_1(self) -> None:
        finals = [row for row in importer.MATCHES if row[1] == importer.FINAL]
        self.assertEqual(len(finals), 1)
        kickoff, stage, venue, home, away, home_goals, away_goals = finals[0]
        self.assertEqual(stage, 5)
        self.assertEqual(venue, "New York New Jersey")
        self.assertEqual(home, "Argentina")
        self.assertEqual(away, "Spain")
        self.assertEqual(home_goals, 0)
        self.assertEqual(away_goals, 1)
        self.assertEqual(kickoff, importer.utc("2026-07-19T19:00:00Z"))


class ApplyFinalFtTests(unittest.TestCase):
    def setUp(self) -> None:
        self.cur = MagicMock()
        self.kickoff = closeout.utc("2026-07-19T19:00:00Z")
        self.argentina_id = 1
        self.spain_id = 2
        self.torres_id = 77
        self.spain_stats_id = 50
        self.scorers = {self.spain_id: [self.torres_id, 88]}

    def test_inserts_torres_goal_when_scoreless(self) -> None:
        with (
            patch.object(closeout, "count_goals", side_effect=[0, 0]),
            patch.object(closeout, "ensure_team_stats", return_value=self.spain_stats_id),
            patch.object(closeout, "find_player_id", return_value=self.torres_id),
        ):
            closeout.apply_final_ft(
                self.cur,
                match_id=116,
                kickoff=self.kickoff,
                argentina_id=self.argentina_id,
                spain_id=self.spain_id,
                scorers_by_team=self.scorers,
            )

        expected_scored_at = self.kickoff + timedelta(minutes=106)
        self.cur.execute.assert_called_once()
        sql, params = self.cur.execute.call_args[0]
        self.assertIn('INSERT INTO "Goal"', sql)
        self.assertEqual(
            params,
            (self.torres_id, self.spain_stats_id, expected_scored_at),
        )

    def test_refreshes_time_when_already_torres(self) -> None:
        with (
            patch.object(closeout, "count_goals", side_effect=[0, 1]),
            patch.object(closeout, "ensure_team_stats", return_value=self.spain_stats_id),
            patch.object(closeout, "find_player_id", return_value=self.torres_id),
        ):
            self.cur.fetchone.return_value = (298, self.torres_id, "Ferran Torres")
            closeout.apply_final_ft(
                self.cur,
                match_id=116,
                kickoff=self.kickoff,
                argentina_id=self.argentina_id,
                spain_id=self.spain_id,
                scorers_by_team=self.scorers,
            )

        expected_scored_at = self.kickoff + timedelta(minutes=106)
        sql, params = self.cur.execute.call_args[0]
        self.assertIn('UPDATE "Goal" SET "TimeScored"', sql)
        self.assertEqual(params, (expected_scored_at, 298))

    def test_corrects_wrong_scorer_to_torres(self) -> None:
        with (
            patch.object(closeout, "count_goals", side_effect=[0, 1]),
            patch.object(closeout, "ensure_team_stats", return_value=self.spain_stats_id),
            patch.object(closeout, "find_player_id", return_value=self.torres_id),
        ):
            self.cur.fetchone.return_value = (298, 999, "Morata")
            closeout.apply_final_ft(
                self.cur,
                match_id=116,
                kickoff=self.kickoff,
                argentina_id=self.argentina_id,
                spain_id=self.spain_id,
                scorers_by_team=self.scorers,
            )

        expected_scored_at = self.kickoff + timedelta(minutes=106)
        sql, params = self.cur.execute.call_args[0]
        self.assertIn("UPDATE", sql)
        self.assertIn('"PlayerId"', sql)
        self.assertEqual(params, (self.torres_id, expected_scored_at, 298))

    def test_exits_on_unexpected_scoreline(self) -> None:
        with (
            patch.object(closeout, "count_goals", side_effect=[1, 1]),
            patch.object(closeout, "ensure_team_stats", return_value=self.spain_stats_id),
            patch.object(closeout, "find_player_id", return_value=self.torres_id),
            self.assertRaises(SystemExit) as raised,
        ):
            closeout.apply_final_ft(
                self.cur,
                match_id=116,
                kickoff=self.kickoff,
                argentina_id=self.argentina_id,
                spain_id=self.spain_id,
                scorers_by_team=self.scorers,
            )
        self.assertIn("unexpected scoreline", str(raised.exception))


if __name__ == "__main__":
    unittest.main()
