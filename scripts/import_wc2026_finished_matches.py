#!/usr/bin/env python3
"""
Import finished FIFA World Cup 2026 matches (group stage + knockouts through 15 Jul 2026).

Sources: FIFA match centre / schedule page + Yahoo Sports results
(through Argentina 2-1 England SF). Final (Argentina vs Spain) is scheduled
separately for betting — not wiped/reloaded here as a finished result.

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
# Knockouts: Yahoo + FIFA venues; both SFs finished (Spain–France, England–Argentina).
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
]


def resolve_country(name: str) -> str:
    return COUNTRY_ALIASES.get(name, name)


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

    needed_stadiums = {STADIUMS[m[2]] for m in MATCHES}
    missing_stadiums = sorted(needed_stadiums - set(stadium_by_name))
    if missing_stadiums:
        raise SystemExit(f"Missing stadiums in DB: {missing_stadiums}")

    needed_teams = {resolve_country(m[3]) for m in MATCHES} | {
        resolve_country(m[4]) for m in MATCHES
    }
    missing_teams = sorted(needed_teams - set(team_by_country))
    if missing_teams:
        raise SystemExit(f"Missing teams in DB: {missing_teams}")

    # Placeholder scorers (Forward = 4)
    cur.execute(
        """
        INSERT INTO "Player" ("Name", "Number", "TeamId", "PositionId")
        SELECT 'Tournament Scorer', 99, t."Id", 4
        FROM "Team" t
        WHERE NOT EXISTS (
            SELECT 1 FROM "Player" p
            WHERE p."TeamId" = t."Id" AND p."Name" = 'Tournament Scorer'
        )
        """
    )

    cur.execute(
        """
        SELECT p."TeamId", p."Id"
        FROM "Player" p
        WHERE p."Name" = 'Tournament Scorer'
        """
    )
    scorer_by_team = {tid: pid for tid, pid in cur.fetchall()}

    # Clear previous schedule data so re-runs are idempotent.
    cur.execute('DELETE FROM "Goal"')
    cur.execute('DELETE FROM "Card"')
    cur.execute('DELETE FROM "BetResult"')
    cur.execute('DELETE FROM "Bet"')
    cur.execute('DELETE FROM "TeamStats"')
    cur.execute('DELETE FROM "Match"')

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
                (scorer_by_team[team_one_id], stats_one, scored_at),
            )
            goals_inserted += 1

        for minute_offset in range(away_goals):
            scored_at = kickoff + timedelta(minutes=15 + minute_offset * 12)
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (scorer_by_team[team_two_id], stats_two, scored_at),
            )
            goals_inserted += 1

        inserted += 1

    conn.commit()
    cur.close()
    conn.close()
    print(f"Imported {inserted} matches with {goals_inserted} goals.")


if __name__ == "__main__":
    main()
