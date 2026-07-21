#!/usr/bin/env python3
"""
Idempotent add / close-out of:
  - SF2 England 1-2 Argentina (15 Jul 2026, Atlanta) + goals
  - Final Argentina 0-1 Spain a.e.t. (19 Jul 2026, MetLife) — Ferran Torres 106'

Does NOT wipe existing Match/Bet data.
Goals prefer seeded squad players (Ferran Torres for Final when present).

After applying Final FT, resolve open bets via Admin:
  POST Bet/ResolveBetsForMatch/{matchId} (or SyncResult on the match).

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
PLACEHOLDER_NAME = "Tournament Scorer"
FINAL_SCORER_NAME = "Ferran Torres"
FINAL_GOAL_MINUTE = 106


def utc(iso: str) -> datetime:
    return datetime.fromisoformat(iso.replace("Z", "+00:00")).astimezone(timezone.utc)


def load_team_forwards(cur, team_ids: tuple[int, ...]) -> dict[int, list[int]]:
    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "PlayerPositions" pp ON pp."Id" = p."PositionId"
        WHERE p."TeamId" = ANY(%s)
          AND p."Name" <> %s
          AND p."Number" <> 99
          AND pp."Name" = 'Forward'
        ORDER BY p."TeamId", p."Number"
        """,
        (list(team_ids), PLACEHOLDER_NAME),
    )
    forwards: dict[int, list[int]] = {}
    for team_id, player_id in cur.fetchall():
        forwards.setdefault(team_id, []).append(player_id)

    missing = [team_id for team_id in team_ids if team_id not in forwards]
    if not missing:
        return forwards

    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        WHERE p."TeamId" = ANY(%s)
          AND p."Name" <> %s
          AND p."Number" <> 99
        ORDER BY p."TeamId", p."Number"
        """,
        (missing, PLACEHOLDER_NAME),
    )
    for team_id, player_id in cur.fetchall():
        forwards.setdefault(team_id, []).append(player_id)
    return forwards


def pick_scorer(scorers_by_team: dict[int, list[int]], team_id: int, goal_index: int) -> int:
    scorers = scorers_by_team.get(team_id) or []
    if not scorers:
        raise SystemExit(
            f"No squad scorers for team {team_id}. Run seed_wc2026_players.py first."
        )
    return scorers[goal_index % len(scorers)]


def find_player_id(cur, team_id: int, name: str) -> int | None:
    cur.execute(
        """
        SELECT p."Id"
        FROM "Player" p
        WHERE p."TeamId" = %s AND p."Name" = %s
        ORDER BY p."Id"
        LIMIT 1
        """,
        (team_id, name),
    )
    row = cur.fetchone()
    return int(row[0]) if row else None


def count_goals(cur, match_id: int, team_id: int) -> int:
    cur.execute(
        """
        SELECT COUNT(*)
        FROM "Goal" g
        JOIN "TeamStats" ts ON ts."Id" = g."TeamStatsId"
        WHERE ts."MatchId" = %s AND ts."TeamId" = %s
        """,
        (match_id, team_id),
    )
    return int(cur.fetchone()[0])


def ensure_team_stats(cur, match_id: int, team_id: int) -> int:
    cur.execute(
        """
        SELECT "Id" FROM "TeamStats"
        WHERE "MatchId" = %s AND "TeamId" = %s
        """,
        (match_id, team_id),
    )
    row = cur.fetchone()
    if row:
        return int(row[0])

    cur.execute(
        """
        INSERT INTO "TeamStats"
            ("MatchId", "TeamId", "Possession", "Shots", "ShotsOnTarget", "Points")
        VALUES (%s, %s, 50, 0, 0, 0)
        RETURNING "Id"
        """,
        (match_id, team_id),
    )
    return int(cur.fetchone()[0])


def apply_final_ft(
    cur,
    match_id: int,
    kickoff: datetime,
    argentina_id: int,
    spain_id: int,
    scorers_by_team: dict[int, list[int]],
) -> None:
    """Ensure Final is Argentina 0-1 Spain with Ferran Torres at 106'."""
    home_goals = count_goals(cur, match_id, argentina_id)
    away_goals = count_goals(cur, match_id, spain_id)
    spain_stats_id = ensure_team_stats(cur, match_id, spain_id)
    ensure_team_stats(cur, match_id, argentina_id)

    torres_id = find_player_id(cur, spain_id, FINAL_SCORER_NAME)
    if torres_id is None:
        torres_id = pick_scorer(scorers_by_team, spain_id, 0)
        print(
            f"Final: WARNING — '{FINAL_SCORER_NAME}' not in squad; "
            f"using player Id={torres_id}"
        )

    scored_at = kickoff + timedelta(minutes=FINAL_GOAL_MINUTE)

    if home_goals == 0 and away_goals == 0:
        cur.execute(
            """
            INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
            VALUES (%s, %s, %s, 0)
            """,
            (torres_id, spain_stats_id, scored_at),
        )
        print(
            f"Final: applied FT Argentina 0-1 Spain "
            f"(MatchId={match_id}, {FINAL_SCORER_NAME} {FINAL_GOAL_MINUTE}')"
        )
        print(
            f"Final: resolve bets if needed — POST Bet/ResolveBetsForMatch/{match_id}"
        )
        return

    if home_goals == 0 and away_goals == 1:
        cur.execute(
            """
            SELECT g."Id", g."PlayerId", p."Name"
            FROM "Goal" g
            JOIN "TeamStats" ts ON ts."Id" = g."TeamStatsId"
            JOIN "Player" p ON p."Id" = g."PlayerId"
            WHERE ts."MatchId" = %s AND ts."TeamId" = %s
            ORDER BY g."Id"
            LIMIT 1
            """,
            (match_id, spain_id),
        )
        goal_row = cur.fetchone()
        if not goal_row:
            raise SystemExit(f"Final MatchId={match_id}: expected Spain goal missing")

        goal_id, player_id, player_name = int(goal_row[0]), int(goal_row[1]), goal_row[2]
        if player_id == torres_id:
            cur.execute(
                'UPDATE "Goal" SET "TimeScored" = %s WHERE "Id" = %s',
                (scored_at, goal_id),
            )
            print(
                f"Final: already FT 0-1 (MatchId={match_id}, "
                f"{FINAL_SCORER_NAME} {FINAL_GOAL_MINUTE}')"
            )
            return

        cur.execute(
            """
            UPDATE "Goal"
            SET "PlayerId" = %s, "TimeScored" = %s, "IsOwnGoal" = 0
            WHERE "Id" = %s
            """,
            (torres_id, scored_at, goal_id),
        )
        print(
            f"Final: corrected scorer {player_name} -> {FINAL_SCORER_NAME} "
            f"{FINAL_GOAL_MINUTE}' (MatchId={match_id}, GoalId={goal_id})"
        )
        return

    raise SystemExit(
        f"Final MatchId={match_id} has unexpected scoreline "
        f"{home_goals}-{away_goals}; expected 0-1. Fix manually."
    )


def main() -> None:
    conn = psycopg2.connect(CONN)
    conn.autocommit = False
    cur = conn.cursor()

    cur.execute(
        """
        SELECT "Id" FROM "WorldCups"
        WHERE EXTRACT(YEAR FROM "Year" AT TIME ZONE 'UTC') = 2026
        ORDER BY "Id"
        LIMIT 1
        """
    )
    wc_row = cur.fetchone()
    if not wc_row:
        raise SystemExit("No WorldCups row for year 2026")
    world_cup_id = int(wc_row[0])

    cur.execute('SELECT "Id", "Name" FROM "Stadium"')
    stadium_by_name = {name: sid for sid, name in cur.fetchall()}

    cur.execute(
        """
        SELECT t."Id", c."Name"
        FROM "Team" t
        JOIN "Countries" c ON c."Id" = t."CountryId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
        """,
        (world_cup_id,),
    )
    team_by_country = {name: tid for tid, name in cur.fetchall()}

    england = team_by_country["England"]
    argentina = team_by_country["Argentina"]
    spain = team_by_country["Spain"]
    atlanta = stadium_by_name["Mercedes-Benz Stadium"]
    metlife = stadium_by_name["MetLife Stadium"]

    scorers_by_team = load_team_forwards(cur, (england, argentina, spain))

    def ensure_match(
        kickoff: datetime,
        stage: int,
        stadium_id: int,
        team_one_id: int,
        team_two_id: int,
        home_goals: int | None,
        away_goals: int | None,
        label: str,
        *,
        away_scorer_name: str | None = None,
        away_goal_minutes: list[int] | None = None,
    ) -> int:
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
            match_id = int(row[0])
            print(f"{label}: already exists (MatchId={match_id})")
            return match_id

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
        match_id = int(cur.fetchone()[0])

        stats_one = ensure_team_stats(cur, match_id, team_one_id)
        stats_two = ensure_team_stats(cur, match_id, team_two_id)

        if home_goals is not None and away_goals is not None:
            # Approximate minutes from real SF: Gordon 55'; Fernandez 85'; Lautaro 92
            goal_times_home = [55] if home_goals == 1 else [
                10 + i * 12 for i in range(home_goals)
            ]
            if away_goal_minutes is not None:
                goal_times_away = away_goal_minutes
            elif away_goals == 2 and home_goals == 1:
                goal_times_away = [85, 92]
            else:
                goal_times_away = [15 + i * 12 for i in range(away_goals)]

            for index, minute in enumerate(goal_times_home[:home_goals]):
                scored_at = kickoff + timedelta(minutes=minute)
                cur.execute(
                    """
                    INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                    VALUES (%s, %s, %s, 0)
                    """,
                    (pick_scorer(scorers_by_team, team_one_id, index), stats_one, scored_at),
                )
            for index, minute in enumerate(goal_times_away[:away_goals]):
                scored_at = kickoff + timedelta(minutes=minute)
                scorer_id = pick_scorer(scorers_by_team, team_two_id, index)
                if index == 0 and away_scorer_name:
                    named = find_player_id(cur, team_two_id, away_scorer_name)
                    if named is not None:
                        scorer_id = named
                cur.execute(
                    """
                    INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                    VALUES (%s, %s, %s, 0)
                    """,
                    (scorer_id, stats_two, scored_at),
                )

        print(f"{label}: inserted MatchId={match_id}")
        return match_id

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

    final_kickoff = utc("2026-07-19T19:00:00Z")
    final_match_id = ensure_match(
        final_kickoff,
        FINAL,
        metlife,
        argentina,
        spain,
        0,
        1,
        "Final Argentina 0-1 Spain",
        away_scorer_name=FINAL_SCORER_NAME,
        away_goal_minutes=[FINAL_GOAL_MINUTE],
    )
    apply_final_ft(
        cur,
        final_match_id,
        final_kickoff,
        argentina,
        spain,
        scorers_by_team,
    )

    conn.commit()
    cur.close()
    conn.close()
    print("Done.")


if __name__ == "__main__":
    main()
