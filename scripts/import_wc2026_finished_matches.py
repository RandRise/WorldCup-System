#!/usr/bin/env python3
"""
Import finished FIFA World Cup 2026 matches (group stage + knockouts through Final).

Sources: FIFA match centre / schedule page + Yahoo Sports / ESPN results
(through Spain 1-0 Argentina Final a.e.t., Ferran Torres 106').

WARNING: This script wipes Match/TeamStats/Goal/Bet rows for WC 2026 then reloads.
For Final-only upsert without wipe, use add_sf2_and_final.py instead.

Usage:
  python import_wc2026_finished_matches.py
"""

from __future__ import annotations

import os
from datetime import datetime, timedelta, timezone

import psycopg2

CONN = os.environ.get(
    "WC_DB",
    "host=localhost dbname=TestDatabase user=postgres password=Aa@123456",
)

# Stage enum (Data.Entities.MatchStage)
GROUP = 0
R16 = 1
QF = 2
SF = 3
FINAL = 5
R32 = 6

# Stadium name aliases -> DB "Stadium"."Name"
STADIUMS = {
    "Mexico City": "Estadio Azteca",
    "Guadalajara": "Estadio Akron",
    "Toronto": "BMO Field",
    "Los Angeles": "SoFi Stadium",
    "Boston": "Gillette Stadium",
    "New York New Jersey": "MetLife Stadium",
    "San Francisco Bay Area": "Levi's Stadium",
    "Philadelphia": "Lincoln Financial Field",
    "Houston": "NRG Stadium",
    "Dallas": "AT&T Stadium",
    "Monterrey": "Estadio BBVA",
    "Miami": "Hard Rock Stadium",
    "Atlanta": "Mercedes-Benz Stadium",
    "Seattle": "Lumen Field",
    "Kansas City": "Arrowhead Stadium",
    "Vancouver": "BC Place",
}

# Country display name in match list -> DB Countries.Name
COUNTRY_ALIASES = {
    "Korea Republic": "South Korea",
    "Czechia": "Czech Republic",
    "USA": "United States",
    "Türkiye": "Turkey",
    "Turkiye": "Turkey",
    "Ivory Coast": "Côte d'Ivoire",
    "Cote d'Ivoire": "Côte d'Ivoire",
    "Cabo Verde": "Cape Verde",
    "Congo DR": "DR Congo",
    "IR Iran": "Iran",
    "USMNT": "United States",
    "Curacao": "Curaçao",
}


def utc(iso: str) -> datetime:
    return datetime.fromisoformat(iso.replace("Z", "+00:00")).astimezone(timezone.utc)


# Each row: (kickoff_utc, stage, venue_alias, home, away, home_goals, away_goals)
# Group-stage home/away & venues: FIFA schedule. Scores: FIFA where listed, else Yahoo.
# Knockouts: Yahoo + FIFA venues; SFs + Final finished (Spain champions).
MATCHES: list[tuple[datetime, int, str, str, str, int, int]] = [
    # ---- Group stage MD1 ----
    (utc("2026-06-11T19:00:00Z"), GROUP, "Mexico City", "Mexico", "South Africa", 2, 0),
    (utc("2026-06-11T22:00:00Z"), GROUP, "Guadalajara", "Korea Republic", "Czechia", 2, 1),
    (utc("2026-06-12T19:00:00Z"), GROUP, "Toronto", "Canada", "Bosnia and Herzegovina", 1, 1),
    (utc("2026-06-12T22:00:00Z"), GROUP, "Los Angeles", "USA", "Paraguay", 4, 1),
    (utc("2026-06-13T16:00:00Z"), GROUP, "Boston", "Haiti", "Scotland", 0, 1),
    (utc("2026-06-13T19:00:00Z"), GROUP, "Vancouver", "Australia", "Türkiye", 2, 0),
    (utc("2026-06-13T22:00:00Z"), GROUP, "New York New Jersey", "Brazil", "Morocco", 1, 1),
    (utc("2026-06-14T01:00:00Z"), GROUP, "San Francisco Bay Area", "Qatar", "Switzerland", 1, 1),
    (utc("2026-06-14T16:00:00Z"), GROUP, "Philadelphia", "Ivory Coast", "Ecuador", 1, 0),
    (utc("2026-06-14T19:00:00Z"), GROUP, "Houston", "Germany", "Curaçao", 7, 1),
    (utc("2026-06-14T22:00:00Z"), GROUP, "Dallas", "Netherlands", "Japan", 2, 2),
    (utc("2026-06-15T01:00:00Z"), GROUP, "Monterrey", "Sweden", "Tunisia", 5, 1),
    (utc("2026-06-15T16:00:00Z"), GROUP, "Miami", "Saudi Arabia", "Uruguay", 1, 1),
    (utc("2026-06-15T19:00:00Z"), GROUP, "Atlanta", "Spain", "Cape Verde", 0, 0),
    (utc("2026-06-15T22:00:00Z"), GROUP, "Los Angeles", "Iran", "New Zealand", 2, 2),
    (utc("2026-06-16T01:00:00Z"), GROUP, "Seattle", "Belgium", "Egypt", 1, 1),
    (utc("2026-06-16T16:00:00Z"), GROUP, "New York New Jersey", "France", "Senegal", 3, 1),
    (utc("2026-06-16T19:00:00Z"), GROUP, "Boston", "Iraq", "Norway", 1, 4),
    (utc("2026-06-16T22:00:00Z"), GROUP, "Kansas City", "Argentina", "Algeria", 3, 0),
    (utc("2026-06-17T01:00:00Z"), GROUP, "San Francisco Bay Area", "Austria", "Jordan", 3, 1),
    (utc("2026-06-17T16:00:00Z"), GROUP, "Toronto", "Ghana", "Panama", 1, 0),
    (utc("2026-06-17T19:00:00Z"), GROUP, "Dallas", "England", "Croatia", 4, 2),
    (utc("2026-06-17T22:00:00Z"), GROUP, "Houston", "Portugal", "DR Congo", 1, 1),
    (utc("2026-06-18T01:00:00Z"), GROUP, "Mexico City", "Uzbekistan", "Colombia", 1, 3),
    # ---- Group stage MD2 ----
    (utc("2026-06-18T16:00:00Z"), GROUP, "Atlanta", "Czechia", "South Africa", 1, 1),
    (utc("2026-06-18T19:00:00Z"), GROUP, "Los Angeles", "Switzerland", "Bosnia and Herzegovina", 4, 1),
    (utc("2026-06-18T22:00:00Z"), GROUP, "Vancouver", "Canada", "Qatar", 6, 0),
    (utc("2026-06-19T01:00:00Z"), GROUP, "Guadalajara", "Mexico", "Korea Republic", 1, 0),
    (utc("2026-06-19T16:00:00Z"), GROUP, "Philadelphia", "Brazil", "Haiti", 3, 0),
    (utc("2026-06-19T19:00:00Z"), GROUP, "Boston", "Scotland", "Morocco", 0, 1),
    (utc("2026-06-19T22:00:00Z"), GROUP, "San Francisco Bay Area", "Türkiye", "Paraguay", 0, 1),
    (utc("2026-06-20T01:00:00Z"), GROUP, "Seattle", "USA", "Australia", 2, 0),
    (utc("2026-06-20T16:00:00Z"), GROUP, "Toronto", "Germany", "Ivory Coast", 2, 1),
    (utc("2026-06-20T19:00:00Z"), GROUP, "Kansas City", "Ecuador", "Curaçao", 0, 0),
    (utc("2026-06-20T22:00:00Z"), GROUP, "Houston", "Netherlands", "Sweden", 5, 1),
    (utc("2026-06-21T01:00:00Z"), GROUP, "Monterrey", "Tunisia", "Japan", 0, 4),
    (utc("2026-06-21T16:00:00Z"), GROUP, "Miami", "Uruguay", "Cape Verde", 2, 2),
    (utc("2026-06-21T19:00:00Z"), GROUP, "Atlanta", "Spain", "Saudi Arabia", 4, 0),
    (utc("2026-06-21T22:00:00Z"), GROUP, "Los Angeles", "Belgium", "Iran", 0, 0),
    (utc("2026-06-22T01:00:00Z"), GROUP, "Vancouver", "New Zealand", "Egypt", 1, 3),
    (utc("2026-06-22T16:00:00Z"), GROUP, "New York New Jersey", "Norway", "Senegal", 3, 2),
    (utc("2026-06-22T19:00:00Z"), GROUP, "Philadelphia", "France", "Iraq", 3, 0),
    (utc("2026-06-22T22:00:00Z"), GROUP, "Dallas", "Argentina", "Austria", 2, 0),
    (utc("2026-06-23T01:00:00Z"), GROUP, "San Francisco Bay Area", "Jordan", "Algeria", 1, 2),
    (utc("2026-06-23T16:00:00Z"), GROUP, "Boston", "England", "Ghana", 0, 0),
    (utc("2026-06-23T19:00:00Z"), GROUP, "Toronto", "Panama", "Croatia", 0, 1),
    (utc("2026-06-23T22:00:00Z"), GROUP, "Houston", "Portugal", "Uzbekistan", 5, 0),
    (utc("2026-06-24T01:00:00Z"), GROUP, "Guadalajara", "Colombia", "DR Congo", 1, 0),
    # ---- Group stage MD3 (final group games) ----
    (utc("2026-06-24T16:00:00Z"), GROUP, "Miami", "Scotland", "Brazil", 0, 3),
    (utc("2026-06-24T19:00:00Z"), GROUP, "Atlanta", "Morocco", "Haiti", 4, 2),
    (utc("2026-06-24T22:00:00Z"), GROUP, "Vancouver", "Switzerland", "Canada", 2, 1),  # FIFA score
    (utc("2026-06-25T01:00:00Z"), GROUP, "Seattle", "Bosnia and Herzegovina", "Qatar", 3, 1),
    (utc("2026-06-25T03:00:00Z"), GROUP, "Mexico City", "Czechia", "Mexico", 0, 3),
    (utc("2026-06-25T05:00:00Z"), GROUP, "Monterrey", "South Africa", "Korea Republic", 1, 0),
    (utc("2026-06-25T16:00:00Z"), GROUP, "Philadelphia", "Curaçao", "Ivory Coast", 0, 2),
    (utc("2026-06-25T19:00:00Z"), GROUP, "New York New Jersey", "Ecuador", "Germany", 2, 1),
    (utc("2026-06-25T22:00:00Z"), GROUP, "Dallas", "Japan", "Sweden", 1, 1),
    (utc("2026-06-26T01:00:00Z"), GROUP, "Kansas City", "Tunisia", "Netherlands", 1, 3),
    (utc("2026-06-26T03:00:00Z"), GROUP, "Los Angeles", "Türkiye", "USA", 3, 2),
    (utc("2026-06-26T05:00:00Z"), GROUP, "San Francisco Bay Area", "Paraguay", "Australia", 0, 0),
    (utc("2026-06-26T16:00:00Z"), GROUP, "Boston", "Norway", "France", 1, 4),
    (utc("2026-06-26T19:00:00Z"), GROUP, "Toronto", "Senegal", "Iraq", 5, 0),
    (utc("2026-06-26T22:00:00Z"), GROUP, "Seattle", "Egypt", "Iran", 1, 1),
    (utc("2026-06-27T01:00:00Z"), GROUP, "Vancouver", "New Zealand", "Belgium", 1, 5),
    (utc("2026-06-27T03:00:00Z"), GROUP, "Houston", "Cape Verde", "Saudi Arabia", 0, 0),
    (utc("2026-06-27T05:00:00Z"), GROUP, "Guadalajara", "Uruguay", "Spain", 0, 1),
    (utc("2026-06-27T16:00:00Z"), GROUP, "New York New Jersey", "Panama", "England", 0, 2),
    (utc("2026-06-27T19:00:00Z"), GROUP, "Philadelphia", "Croatia", "Ghana", 2, 1),
    (utc("2026-06-27T22:00:00Z"), GROUP, "Kansas City", "Algeria", "Austria", 3, 3),
    (utc("2026-06-28T01:00:00Z"), GROUP, "Dallas", "Jordan", "Argentina", 1, 3),
    (utc("2026-06-28T03:00:00Z"), GROUP, "Miami", "Colombia", "Portugal", 0, 0),
    (utc("2026-06-28T05:00:00Z"), GROUP, "Atlanta", "DR Congo", "Uzbekistan", 3, 1),
    # ---- Round of 32 ----
    (utc("2026-06-28T19:00:00Z"), R32, "Los Angeles", "South Africa", "Canada", 0, 1),
    (utc("2026-06-29T16:00:00Z"), R32, "Boston", "Germany", "Paraguay", 1, 1),  # PAR on pens
    (utc("2026-06-29T19:00:00Z"), R32, "Monterrey", "Netherlands", "Morocco", 1, 1),  # MAR on pens
    (utc("2026-06-29T22:00:00Z"), R32, "Houston", "Brazil", "Japan", 2, 1),
    (utc("2026-06-30T16:00:00Z"), R32, "New York New Jersey", "France", "Sweden", 3, 0),
    (utc("2026-06-30T19:00:00Z"), R32, "Dallas", "Norway", "Ivory Coast", 2, 1),
    (utc("2026-06-30T22:00:00Z"), R32, "Mexico City", "Mexico", "Ecuador", 2, 0),
    (utc("2026-07-01T16:00:00Z"), R32, "Atlanta", "England", "DR Congo", 2, 1),
    (utc("2026-07-01T19:00:00Z"), R32, "Seattle", "Belgium", "Senegal", 3, 2),  # a.e.t.
    (utc("2026-07-01T22:00:00Z"), R32, "San Francisco Bay Area", "USA", "Bosnia and Herzegovina", 2, 0),
    (utc("2026-07-02T16:00:00Z"), R32, "Toronto", "Portugal", "Croatia", 2, 1),
    (utc("2026-07-02T19:00:00Z"), R32, "Los Angeles", "Spain", "Austria", 3, 0),
    (utc("2026-07-02T22:00:00Z"), R32, "Vancouver", "Switzerland", "Algeria", 2, 0),
    (utc("2026-07-03T16:00:00Z"), R32, "Dallas", "Australia", "Egypt", 1, 1),  # EGY on pens
    (utc("2026-07-03T19:00:00Z"), R32, "Miami", "Argentina", "Cape Verde", 3, 2),  # a.e.t.
    (utc("2026-07-03T22:00:00Z"), R32, "Kansas City", "Colombia", "Ghana", 1, 0),
    # ---- Round of 16 ----
    (utc("2026-07-04T19:00:00Z"), R16, "Houston", "Morocco", "Canada", 3, 0),
    (utc("2026-07-04T22:00:00Z"), R16, "Philadelphia", "France", "Paraguay", 1, 0),
    (utc("2026-07-05T19:00:00Z"), R16, "New York New Jersey", "Norway", "Brazil", 2, 1),
    (utc("2026-07-05T22:00:00Z"), R16, "Mexico City", "England", "Mexico", 3, 2),
    (utc("2026-07-06T19:00:00Z"), R16, "Dallas", "Spain", "Portugal", 1, 0),
    (utc("2026-07-06T22:00:00Z"), R16, "Seattle", "Belgium", "USA", 4, 1),
    (utc("2026-07-07T19:00:00Z"), R16, "Atlanta", "Argentina", "Egypt", 3, 2),
    (utc("2026-07-07T22:00:00Z"), R16, "Vancouver", "Switzerland", "Colombia", 0, 0),  # SUI on pens
    # ---- Quarter-finals ----
    (utc("2026-07-09T19:00:00Z"), QF, "Boston", "France", "Morocco", 2, 0),
    (utc("2026-07-10T19:00:00Z"), QF, "Los Angeles", "Spain", "Belgium", 2, 1),
    (utc("2026-07-11T19:00:00Z"), QF, "Miami", "England", "Norway", 2, 1),
    (utc("2026-07-11T22:00:00Z"), QF, "Kansas City", "Argentina", "Switzerland", 3, 1),  # a.e.t.
    # ---- Semi-finals (finished) ----
    (utc("2026-07-14T19:00:00Z"), SF, "Dallas", "France", "Spain", 0, 2),
    # England 1-2 Argentina (Atlanta): Gordon 55'; Fernandez 85'; Lautaro 90+2
    (utc("2026-07-15T19:00:00Z"), SF, "Atlanta", "England", "Argentina", 1, 2),
    # ---- Final (finished a.e.t.) ----
    # Argentina 0-1 Spain (MetLife): Ferran Torres 106' (special-cased in insert loop).
    (utc("2026-07-19T19:00:00Z"), FINAL, "New York New Jersey", "Argentina", "Spain", 0, 1),
]


def resolve_country(name: str) -> str:
    return COUNTRY_ALIASES.get(name, name)


PLACEHOLDER_NAME = "Tournament Scorer"


def resolve_world_cup_id(cur, year: int = 2026) -> int:
    cur.execute(
        """
        SELECT "Id" FROM "WorldCups"
        WHERE EXTRACT(YEAR FROM "Year" AT TIME ZONE 'UTC') = %s
        ORDER BY "Id"
        LIMIT 1
        """,
        (year,),
    )
    row = cur.fetchone()
    if not row:
        raise SystemExit(f"No WorldCups row for year {year}")
    return int(row[0])


def resolve_forward_position_id(cur) -> int:
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


def world_cup_team_ids_sql() -> str:
    return """
        SELECT t."Id"
        FROM "Team" t
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
    """


def wipe_world_cup_matches(cur, world_cup_id: int) -> int:
    """Delete matches/goals/stats/bets for this World Cup only (multi-cup safe)."""
    cur.execute(
        f"""
        SELECT DISTINCT m."Id" FROM "Match" m
        WHERE m."TeamOneId" IN ({world_cup_team_ids_sql()})
           OR m."TeamTwoId" IN ({world_cup_team_ids_sql()})
        """,
        (world_cup_id, world_cup_id),
    )
    match_ids = [int(row[0]) for row in cur.fetchall()]
    if not match_ids:
        return 0

    cur.execute(
        'UPDATE "Match" SET "FeederMatchOneId" = NULL WHERE "FeederMatchOneId" = ANY(%s)',
        (match_ids,),
    )
    cur.execute(
        'UPDATE "Match" SET "FeederMatchTwoId" = NULL WHERE "FeederMatchTwoId" = ANY(%s)',
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


def load_scorers_by_team(cur, world_cup_id: int) -> dict[int, list[int]]:
    """Prefer seeded Forwards; else any non-placeholder player; else Tournament Scorer."""
    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "Team" t ON t."Id" = p."TeamId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        JOIN "PlayerPositions" pp ON pp."Id" = p."PositionId"
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
        forwards_by_team.setdefault(team_id, []).append(player_id)

    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        JOIN "Team" t ON t."Id" = p."TeamId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
          AND p."Name" <> %s AND p."Number" <> 99
        ORDER BY p."TeamId", p."Number"
        """,
        (world_cup_id, PLACEHOLDER_NAME),
    )
    any_by_team: dict[int, list[int]] = {}
    for team_id, player_id in cur.fetchall():
        any_by_team.setdefault(team_id, []).append(player_id)

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
    placeholder_by_team = {team_id: player_id for team_id, player_id in cur.fetchall()}

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
            f"No scorers for team {team_id}. Run seed_wc2026_players.py "
            f"or ensure placeholder '{PLACEHOLDER_NAME}' exists."
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


def main() -> None:
    conn = psycopg2.connect(CONN)
    conn.autocommit = False
    cur = conn.cursor()

    world_cup_id = resolve_world_cup_id(cur, 2026)
    forward_position_id = resolve_forward_position_id(cur)

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

    needed_stadiums = {STADIUMS[m[2]] for m in MATCHES}
    missing_stadiums = sorted(needed_stadiums - set(stadium_by_name))
    if missing_stadiums:
        raise SystemExit(f"Missing stadiums in DB: {missing_stadiums}")

    needed_teams = {resolve_country(m[3]) for m in MATCHES} | {
        resolve_country(m[4]) for m in MATCHES
    }
    missing_teams = sorted(needed_teams - set(team_by_country))
    if missing_teams:
        raise SystemExit(f"Missing teams in DB (WorldCup {world_cup_id}): {missing_teams}")

    # Prefer real squad forwards; keep Tournament Scorer only as fallback for empty squads.
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

    scorers_by_team = load_scorers_by_team(cur, world_cup_id)

    wiped = wipe_world_cup_matches(cur, world_cup_id)
    print(f"Wiped {wiped} prior match(es) for WorldCupId={world_cup_id} (2026 only).")

    inserted = 0
    goals_inserted = 0

    for kickoff, stage, venue_alias, home, away, home_goals, away_goals in MATCHES:
        stadium_id = stadium_by_name[STADIUMS[venue_alias]]
        team_one_id = team_by_country[resolve_country(home)]
        team_two_id = team_by_country[resolve_country(away)]

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

        for minute_offset in range(home_goals):
            scored_at = kickoff + timedelta(minutes=10 + minute_offset * 12)
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (pick_scorer(scorers_by_team, team_one_id, minute_offset), stats_one, scored_at),
            )
            goals_inserted += 1

        # Final a.e.t.: Ferran Torres 106' (not the generic away minute loop).
        if (
            stage == FINAL
            and home_goals == 0
            and away_goals == 1
            and resolve_country(home) == "Argentina"
            and resolve_country(away) == "Spain"
        ):
            torres_id = find_player_id(cur, team_two_id, "Ferran Torres")
            if torres_id is None:
                torres_id = pick_scorer(scorers_by_team, team_two_id, 0)
            scored_at = kickoff + timedelta(minutes=106)
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (torres_id, stats_two, scored_at),
            )
            goals_inserted += 1
        else:
            for minute_offset in range(away_goals):
                scored_at = kickoff + timedelta(minutes=15 + minute_offset * 12)
                cur.execute(
                    """
                    INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                    VALUES (%s, %s, %s, 0)
                    """,
                    (pick_scorer(scorers_by_team, team_two_id, minute_offset), stats_two, scored_at),
                )
                goals_inserted += 1

        inserted += 1

    conn.commit()
    cur.close()
    conn.close()
    print(f"Imported {inserted} matches with {goals_inserted} goals (WorldCupId={world_cup_id}).")


if __name__ == "__main__":
    main()
