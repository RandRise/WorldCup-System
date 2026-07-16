#!/usr/bin/env python3
"""
Idempotent add of:
  - SF2 England 1-2 Argentina (15 Jul 2026, Atlanta) + goals
  - Final Argentina vs Spain (19 Jul 2026, MetLife) — future kickoff for betting

Does NOT wipe existing Match/Bet data.

Usage:
  python add_sf2_and_final.py
"""

from __future__ import annotations

import os
from datetime import datetime, timedelta, timezone

import psycopg2

CONN = os.environ.get(
    "WC_DB",
    "host=localhost dbname=TestDatabase user=postgres password=Aa@123456 connect_timeout=10",
)

SF = 3
FINAL = 5


def utc(iso: str) -> datetime:
    return datetime.fromisoformat(iso.replace("Z", "+00:00")).astimezone(timezone.utc)


def main() -> None:
    conn = psycopg2.connect(CONN)
    conn.autocommit = False
    cur = conn.cursor()

    cur.execute('SELECT "Id", "Name" FROM "Stadium"')
    stadium_by_name = {name: sid for sid, name in cur.fetchall()}

    cur.execute(
        """
        SELECT t."Id", c."Name"
        FROM "Team" t
        JOIN "Countries" c ON c."Id" = t."CountryId"
        """
    )
    team_by_country = {name: tid for tid, name in cur.fetchall()}

    england = team_by_country["England"]
    argentina = team_by_country["Argentina"]
    spain = team_by_country["Spain"]
    atlanta = stadium_by_name["Mercedes-Benz Stadium"]
    metlife = stadium_by_name["MetLife Stadium"]

    # Ensure placeholder scorers exist
    cur.execute(
        """
        INSERT INTO "Player" ("Name", "Number", "TeamId", "PositionId")
        SELECT 'Tournament Scorer', 99, t."Id", 4
        FROM "Team" t
        WHERE t."Id" IN (%s, %s, %s)
          AND NOT EXISTS (
            SELECT 1 FROM "Player" p
            WHERE p."TeamId" = t."Id" AND p."Name" = 'Tournament Scorer'
          )
        """,
        (england, argentina, spain),
    )
    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        WHERE p."Name" = 'Tournament Scorer'
          AND p."TeamId" IN (%s, %s, %s)
        """,
        (england, argentina, spain),
    )
    scorer_by_team = {tid: pid for tid, pid in cur.fetchall()}

    def ensure_match(
        kickoff: datetime,
        stage: int,
        stadium_id: int,
        team_one_id: int,
        team_two_id: int,
        home_goals: int | None,
        away_goals: int | None,
        label: str,
    ) -> None:
        cur.execute(
            """
            SELECT m."Id"
            FROM "Match" m
            WHERE m."Stage" = %s
              AND m."TeamOneId" = %s
              AND m."TeamTwoId" = %s
            """,
            (stage, team_one_id, team_two_id),
        )
        row = cur.fetchone()
        if row:
            print(f"{label}: already exists (MatchId={row[0]})")
            return

        cur.execute(
            """
            INSERT INTO "Match"
                ("Date", "StadiumId", "TeamOneId", "TeamTwoId", "Stage",
                 "FeederOneTakesLoser", "FeederTwoTakesLoser")
            VALUES (%s, %s, %s, %s, %s, FALSE, FALSE)
            RETURNING "Id"
            """,
            (kickoff, stadium_id, team_one_id, team_two_id, stage),
        )
        match_id = cur.fetchone()[0]

        cur.execute(
            """
            INSERT INTO "TeamStats"
                ("MatchId", "TeamId", "Possession", "Shots", "ShotsOnTarget", "Points")
            VALUES (%s, %s, 50, 0, 0, 0)
            RETURNING "Id"
            """,
            (match_id, team_one_id),
        )
        stats_one = cur.fetchone()[0]

        cur.execute(
            """
            INSERT INTO "TeamStats"
                ("MatchId", "TeamId", "Possession", "Shots", "ShotsOnTarget", "Points")
            VALUES (%s, %s, 50, 0, 0, 0)
            RETURNING "Id"
            """,
            (match_id, team_two_id),
        )
        stats_two = cur.fetchone()[0]

        if home_goals is not None and away_goals is not None:
            # Approximate minutes from real SF: Gordon 55, Fernandez 85, Lautaro 92
            goal_times_home = [55] if home_goals == 1 else [
                10 + i * 12 for i in range(home_goals)
            ]
            goal_times_away = [85, 92] if away_goals == 2 and home_goals == 1 else [
                15 + i * 12 for i in range(away_goals)
            ]
            for minute in goal_times_home[:home_goals]:
                scored_at = kickoff + timedelta(minutes=minute)
                cur.execute(
                    """
                    INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                    VALUES (%s, %s, %s, 0)
                    """,
                    (scorer_by_team[team_one_id], stats_one, scored_at),
                )
            for minute in goal_times_away[:away_goals]:
                scored_at = kickoff + timedelta(minutes=minute)
                cur.execute(
                    """
                    INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                    VALUES (%s, %s, %s, 0)
                    """,
                    (scorer_by_team[team_two_id], stats_two, scored_at),
                )

        print(f"{label}: inserted MatchId={match_id}")

    ensure_match(
        utc("2026-07-15T19:00:00Z"),
        SF,
        atlanta,
        england,
        argentina,
        1,
        2,
        "SF2 England 1-2 Argentina",
    )
    ensure_match(
        utc("2026-07-19T19:00:00Z"),
        FINAL,
        metlife,
        argentina,
        spain,
        None,
        None,
        "Final Argentina vs Spain",
    )

    conn.commit()
    cur.close()
    conn.close()
    print("Done.")


if __name__ == "__main__":
    main()
