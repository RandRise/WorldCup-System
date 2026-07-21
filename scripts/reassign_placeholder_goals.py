#!/usr/bin/env python3
"""
Reassign Goal rows credited to "Tournament Scorer" onto seeded squad forwards.

Does not change scores — only PlayerId on existing goals (round-robin per team).
Run after seed_wc2026_players.py.

Usage:
  python reassign_placeholder_goals.py
  python reassign_placeholder_goals.py --dry-run
"""

from __future__ import annotations

import argparse
import os
import sys

import psycopg2

CONN = os.environ.get(
    "WC_DB",
    "host=localhost dbname=TestDatabase user=postgres password=Aa@123456",
)

PLACEHOLDER_NAME = "Tournament Scorer"


def load_forwards_by_team(cur) -> dict[int, list[int]]:
    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "PlayerPositions" pp ON pp."Id" = p."PositionId"
        WHERE p."Name" <> %s
          AND p."Number" <> 99
          AND pp."Name" = 'Forward'
        ORDER BY p."TeamId", p."Number"
        """,
        (PLACEHOLDER_NAME,),
    )
    forwards: dict[int, list[int]] = {}
    for team_id, player_id in cur.fetchall():
        forwards.setdefault(team_id, []).append(player_id)

    if forwards:
        return forwards

    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        WHERE p."Name" <> %s AND p."Number" <> 99
        ORDER BY p."TeamId", p."Number"
        """,
        (PLACEHOLDER_NAME,),
    )
    for team_id, player_id in cur.fetchall():
        forwards.setdefault(team_id, []).append(player_id)
    return forwards


def main() -> None:
    parser = argparse.ArgumentParser(description="Reassign Tournament Scorer goals to squad players.")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    conn = psycopg2.connect(CONN)
    cur = conn.cursor()

    scorers_by_team = load_forwards_by_team(cur)
    if not scorers_by_team:
        raise SystemExit("No squad players found. Run seed_wc2026_players.py first.")

    cur.execute(
        """
        SELECT g."Id", ts."TeamId"
        FROM "Goal" g
        JOIN "TeamStats" ts ON ts."Id" = g."TeamStatsId"
        JOIN "Player" p ON p."Id" = g."PlayerId"
        WHERE p."Name" = %s
        ORDER BY ts."TeamId", g."TimeScored", g."Id"
        """,
        (PLACEHOLDER_NAME,),
    )
    rows = cur.fetchall()
    if not rows:
        print("No Tournament Scorer goals to reassign.")
        cur.close()
        conn.close()
        return

    counters: dict[int, int] = {}
    updated = 0
    skipped = 0

    for goal_id, team_id in rows:
        scorers = scorers_by_team.get(team_id)
        if not scorers:
            skipped += 1
            continue
        index = counters.get(team_id, 0)
        new_player_id = scorers[index % len(scorers)]
        counters[team_id] = index + 1

        if args.dry_run:
            updated += 1
            continue

        cur.execute(
            'UPDATE "Goal" SET "PlayerId" = %s WHERE "Id" = %s',
            (new_player_id, goal_id),
        )
        updated += 1

    if args.dry_run:
        conn.rollback()
        print(f"Dry run OK — would reassign {updated} goal(s), skip {skipped}.")
    else:
        conn.commit()
        print(f"Reassigned {updated} goal(s) from {PLACEHOLDER_NAME}; skipped {skipped}.")

    cur.close()
    conn.close()


if __name__ == "__main__":
    try:
        main()
    except psycopg2.Error as exc:
        print(f"Database error: {exc}", file=sys.stderr)
        raise SystemExit(1) from exc
