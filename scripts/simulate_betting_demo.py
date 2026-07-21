#!/usr/bin/env python3
"""
Demo: fake bettors + bets + resolve + KO scoring for World Cup 2030.

Flow:
  1. Ensure demo users (API register / reuse)
  2. Backfill bets on finished group matches (DB — kickoff already passed)
  3. Place bets on upcoming KO via API (CanBet)
  4. Admin ResolveBetsForWorldCup → points on finished groups
  5. Finish KO matches (shift kickoff to past, insert goals/stats) → advance + resolve
  6. With --to-final: play R32 → R16 → QF → SF → Final

Usage:
  python simulate_betting_demo.py
  python simulate_betting_demo.py --seed 42 --finish-r32 2
  python simulate_betting_demo.py --wipe-bets --to-final
  python simulate_betting_demo.py --api http://localhost:5055 --wipe-bets

Env:
  WC_DB, WC_API (default http://localhost:5055)
  WC_ADMIN_EMAIL / WC_ADMIN_PASSWORD (default admin@localhost / Admin123!)
"""

from __future__ import annotations

import argparse
import json
import os
import random
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Any

import psycopg2
from psycopg2.extensions import connection as PgConnection
from psycopg2.extensions import cursor as PgCursor

CONN = os.environ.get(
    "WC_DB",
    "host=localhost dbname=TestDatabase user=postgres password=Aa@123456",
)
API_BASE = os.environ.get("WC_API", "http://localhost:5055").rstrip("/")
ADMIN_EMAIL = os.environ.get("WC_ADMIN_EMAIL", "admin@localhost")
ADMIN_PASSWORD = os.environ.get("WC_ADMIN_PASSWORD", "Admin123!")
DEV_USER_EMAIL = os.environ.get("WC_USER_EMAIL", "user@localhost")
DEV_USER_PASSWORD = os.environ.get("WC_USER_PASSWORD", "User123!")

WC_YEAR = datetime(2030, 6, 13, tzinfo=timezone.utc)
MATCH_STAGE_GROUP = 0
MATCH_STAGE_ROUND_OF_16 = 1
MATCH_STAGE_QUARTER_FINAL = 2
MATCH_STAGE_SEMI_FINAL = 3
MATCH_STAGE_FINAL = 5
MATCH_STAGE_ROUND_OF_32 = 6
DEMO_PASSWORD = "Demo123!"

PLACEHOLDER_NAME = "Tournament Scorer"

STAGE_LABELS: dict[int, str] = {
    MATCH_STAGE_ROUND_OF_32: "R32",
    MATCH_STAGE_ROUND_OF_16: "R16",
    MATCH_STAGE_QUARTER_FINAL: "QF",
    MATCH_STAGE_SEMI_FINAL: "SF",
    MATCH_STAGE_FINAL: "Final",
}

KO_STAGE_ORDER: list[int] = [
    MATCH_STAGE_ROUND_OF_32,
    MATCH_STAGE_ROUND_OF_16,
    MATCH_STAGE_QUARTER_FINAL,
    MATCH_STAGE_SEMI_FINAL,
    MATCH_STAGE_FINAL,
]


@dataclass(frozen=True)
class DemoUser:
    name: str
    email: str
    password: str
    # Strategy for finished group matches: correct | home | away | draw | random
    group_strategy: str
    # Bias for upcoming KO: favorite (team one) | underdog (team two) | draw | random
    knockout_strategy: str


DEMO_USERS: list[DemoUser] = [
    DemoUser("Oracle Alice", "demo.alice@localhost", DEMO_PASSWORD, "correct", "favorite"),
    DemoUser("Home Bob", "demo.bob@localhost", DEMO_PASSWORD, "home", "favorite"),
    DemoUser("Away Cara", "demo.cara@localhost", DEMO_PASSWORD, "away", "underdog"),
    DemoUser("Draw Dan", "demo.dan@localhost", DEMO_PASSWORD, "draw", "draw"),
    DemoUser("Lucky Eve", "demo.eve@localhost", DEMO_PASSWORD, "random", "random"),
]


class ApiClient:
    def __init__(self, base_url: str) -> None:
        self.base_url = base_url.rstrip("/")
        self.token: str | None = None

    def _request(
        self,
        method: str,
        path: str,
        body: dict[str, Any] | None = None,
        auth: bool = False,
    ) -> Any:
        data = None
        headers = {"Accept": "application/json"}
        if body is not None:
            data = json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json"
        if auth:
            if not self.token:
                raise RuntimeError("Not logged in")
            headers["Authorization"] = f"Bearer {self.token}"

        req = urllib.request.Request(
            f"{self.base_url}{path}",
            data=data,
            headers=headers,
            method=method,
        )
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                raw = resp.read()
                if not raw:
                    return None
                return json.loads(raw.decode("utf-8"))
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"{method} {path} → HTTP {exc.code}: {detail}") from exc

    def login(self, email: str, password: str) -> str:
        payload = self._request(
            "POST",
            "/User/Login",
            {"email": email, "password": password},
        )
        if not isinstance(payload, dict) or "token" not in payload:
            # Some builds return { token, expiration } or string
            raise RuntimeError(f"Unexpected login response: {payload!r}")
        self.token = str(payload["token"])
        return self.token

    def register(self, name: str, email: str, password: str) -> None:
        try:
            self._request(
                "POST",
                "/User/CreateNewUser",
                {"name": name, "email": email, "password": password},
            )
        except RuntimeError as exc:
            msg = str(exc).lower()
            if "already" in msg or "taken" in msg or "exist" in msg or "400" in msg:
                return
            raise

    def place_bet(self, match_id: int, is_draw: bool, team_id: int | None) -> None:
        body: dict[str, Any] = {"matchId": match_id, "isDraw": is_draw}
        if team_id is not None:
            body["teamId"] = team_id
        self._request("POST", "/Bet/PlaceBet", body, auth=True)

    def resolve_world_cup(self, world_cup_id: int) -> dict[str, Any]:
        result = self._request(
            "POST",
            f"/Bet/ResolveBetsForWorldCup/{world_cup_id}",
            {},
            auth=True,
        )
        return result if isinstance(result, dict) else {}

    def leaderboard(self, world_cup_id: int) -> list[dict[str, Any]]:
        result = self._request(
            "GET",
            f"/Bet/GetLeaderboard?worldCupId={world_cup_id}",
            auth=False,
        )
        return result if isinstance(result, list) else []

    def add_goal(self, match_id: int, team_id: int, player_id: int, minute: int) -> Any:
        return self._request(
            "POST",
            "/Goal/AddGoal",
            {
                "matchId": match_id,
                "teamId": team_id,
                "playerId": player_id,
                "minute": minute,
                "isOwnGoal": False,
            },
            auth=True,
        )


def find_world_cup_id(cur: PgCursor) -> int:
    cur.execute(
        'SELECT "Id" FROM "WorldCups" WHERE "Year" = %s ORDER BY "Id" LIMIT 1',
        (WC_YEAR,),
    )
    row = cur.fetchone()
    if not row:
        raise SystemExit("World Cup 2030 not found — run simulate_wc2030.py first.")
    return int(row[0])


def load_user_id(cur: PgCursor, email: str) -> int | None:
    cur.execute(
        'SELECT "Id" FROM "AspNetUsers" WHERE LOWER("Email") = LOWER(%s)',
        (email,),
    )
    row = cur.fetchone()
    return int(row[0]) if row else None


def wipe_bets_for_world_cup(cur: PgCursor, world_cup_id: int) -> int:
    cur.execute(
        """
        SELECT m."Id"
        FROM "Match" m
        WHERE m."TeamOneId" IN (
            SELECT t."Id" FROM "Team" t
            JOIN "Group" g ON g."Id" = t."GroupId"
            WHERE g."WorldCupId" = %s
        )
        """,
        (world_cup_id,),
    )
    match_ids = [int(r[0]) for r in cur.fetchall()]
    if not match_ids:
        return 0
    cur.execute(
        """
        DELETE FROM "BetResult"
        WHERE "BetId" IN (SELECT "Id" FROM "Bet" WHERE "MatchId" = ANY(%s))
        """,
        (match_ids,),
    )
    cur.execute('DELETE FROM "Bet" WHERE "MatchId" = ANY(%s)', (match_ids,))
    return len(match_ids)


def match_actual_outcome(cur: PgCursor, match_id: int) -> tuple[int | None, bool]:
    """Return (winner_team_id or None if draw, is_draw). Uses Goal counts like BetService."""
    cur.execute(
        """
        SELECT ts."Id", ts."TeamId"
        FROM "TeamStats" ts
        WHERE ts."MatchId" = %s
        """,
        (match_id,),
    )
    rows = cur.fetchall()
    if len(rows) < 2:
        raise RuntimeError(f"Match {match_id} missing TeamStats")
    scores: dict[int, int] = {}
    for stats_id, team_id in rows:
        cur.execute(
            'SELECT COUNT(*) FROM "Goal" WHERE "TeamStatsId" = %s',
            (int(stats_id),),
        )
        scores[int(team_id)] = int(cur.fetchone()[0])
    teams = list(scores.keys())
    if scores[teams[0]] > scores[teams[1]]:
        return teams[0], False
    if scores[teams[1]] > scores[teams[0]]:
        return teams[1], False
    return None, True


def choose_group_prediction(
    strategy: str,
    team_one_id: int,
    team_two_id: int,
    actual_winner: int | None,
    rng: random.Random,
) -> tuple[bool, int | None]:
    if strategy == "correct":
        if actual_winner is None:
            return True, None
        return False, actual_winner
    if strategy == "home":
        return False, team_one_id
    if strategy == "away":
        return False, team_two_id
    if strategy == "draw":
        return True, None
    # random
    pick = rng.choice(["home", "away", "draw"])
    return choose_group_prediction(pick, team_one_id, team_two_id, actual_winner, rng)


def choose_ko_prediction(
    strategy: str,
    team_one_id: int,
    team_two_id: int,
    rng: random.Random,
) -> tuple[bool, int | None]:
    if strategy == "favorite":
        return False, team_one_id
    if strategy == "underdog":
        return False, team_two_id
    if strategy == "draw":
        return True, None
    pick = rng.choice(["favorite", "underdog", "draw"])
    return choose_ko_prediction(pick, team_one_id, team_two_id, rng)


def ensure_demo_users(api: ApiClient, cur: PgCursor) -> dict[str, int]:
    """Register demo users + ensure DevUser exists in map. Returns email → userId."""
    ids: dict[str, int] = {}

    # Dev user (seeded by API on startup)
    dev_id = load_user_id(cur, DEV_USER_EMAIL)
    if dev_id is None:
        print(f"WARNING: {DEV_USER_EMAIL} not found — start API once so DevUser seeds.")
    else:
        ids[DEV_USER_EMAIL] = dev_id

    for demo in DEMO_USERS:
        existing = load_user_id(cur, demo.email)
        if existing is None:
            api.register(demo.name, demo.email, demo.password)
            existing = load_user_id(cur, demo.email)
        if existing is None:
            raise SystemExit(f"Failed to create/find user {demo.email}")
        ids[demo.email] = existing
        print(f"  User {demo.email} Id={existing} ({demo.name})")

    return ids


def backfill_group_bets(
    cur: PgCursor,
    world_cup_id: int,
    user_ids: dict[str, int],
    rng: random.Random,
    max_matches: int | None,
) -> int:
    cur.execute(
        """
        SELECT m."Id", m."TeamOneId", m."TeamTwoId"
        FROM "Match" m
        JOIN "Team" t ON t."Id" = m."TeamOneId"
        JOIN "Group" g ON g."Id" = t."GroupId"
        WHERE g."WorldCupId" = %s
          AND m."Stage" = %s
          AND m."TeamOneId" IS NOT NULL
          AND m."TeamTwoId" IS NOT NULL
          AND m."Date" < NOW() AT TIME ZONE 'utc'
        ORDER BY m."Date", m."Id"
        """,
        (world_cup_id, MATCH_STAGE_GROUP),
    )
    matches = [(int(r[0]), int(r[1]), int(r[2])) for r in cur.fetchall()]
    if max_matches is not None:
        matches = matches[:max_matches]

    inserted = 0
    bettors: list[tuple[str, DemoUser | None]] = [
        (DEV_USER_EMAIL, None),
        *[(d.email, d) for d in DEMO_USERS],
    ]

    for match_id, team_one, team_two in matches:
        actual_winner, _is_draw = match_actual_outcome(cur, match_id)
        for email, demo in bettors:
            user_id = user_ids.get(email)
            if user_id is None:
                continue
            cur.execute(
                'SELECT 1 FROM "Bet" WHERE "UserId" = %s AND "MatchId" = %s',
                (user_id, match_id),
            )
            if cur.fetchone():
                continue

            if demo is None:
                # DevUser: mostly correct with some noise
                strategy = "correct" if rng.random() < 0.7 else "random"
            else:
                strategy = demo.group_strategy

            is_draw, team_id = choose_group_prediction(
                strategy, team_one, team_two, actual_winner, rng
            )
            cur.execute(
                """
                INSERT INTO "Bet" ("UserId", "MatchId", "TeamId", "IsDraw")
                VALUES (%s, %s, %s, %s)
                """,
                (user_id, match_id, None if is_draw else team_id, is_draw),
            )
            inserted += 1

    return inserted


def list_ready_knockout_matches(
    cur: PgCursor,
    world_cup_id: int,
    stage: int,
    *,
    unscored_only: bool = True,
) -> list[tuple[int, int, int, str, str]]:
    """Return (match_id, team_one_id, team_two_id, team_one_name, team_two_name)."""
    scored_filter = ""
    if unscored_only:
        scored_filter = """
          AND NOT EXISTS (
            SELECT 1 FROM "Goal" g
            JOIN "TeamStats" ts ON ts."Id" = g."TeamStatsId"
            WHERE ts."MatchId" = m."Id"
          )
        """
    cur.execute(
        f"""
        SELECT m."Id", m."TeamOneId", m."TeamTwoId", c1."Name", c2."Name"
        FROM "Match" m
        JOIN "Team" t1 ON t1."Id" = m."TeamOneId"
        JOIN "Countries" c1 ON c1."Id" = t1."CountryId"
        JOIN "Team" t2 ON t2."Id" = m."TeamTwoId"
        JOIN "Countries" c2 ON c2."Id" = t2."CountryId"
        JOIN "Group" g ON g."Id" = t1."GroupId"
        WHERE g."WorldCupId" = %s
          AND m."Stage" = %s
          AND m."TeamOneId" IS NOT NULL
          AND m."TeamTwoId" IS NOT NULL
          {scored_filter}
        ORDER BY m."Date", m."Id"
        """,
        (world_cup_id, stage),
    )
    return [
        (int(r[0]), int(r[1]), int(r[2]), str(r[3]), str(r[4]))
        for r in cur.fetchall()
    ]


def place_knockout_bets(
    api: ApiClient,
    cur: PgCursor,
    conn: PgConnection,
    world_cup_id: int,
    rng: random.Random,
    stage: int | None = None,
) -> int:
    stages = [stage] if stage is not None else [MATCH_STAGE_ROUND_OF_32]
    matches: list[tuple[int, int, int]] = []
    for st in stages:
        for match_id, team_one, team_two, _n1, _n2 in list_ready_knockout_matches(
            cur, world_cup_id, st, unscored_only=True
        ):
            # Ensure CanBet: kickoff must be in the future for API PlaceBet
            cur.execute(
                """
                UPDATE "Match"
                SET "Date" = GREATEST("Date", %s)
                WHERE "Id" = %s
                  AND "Date" <= NOW() AT TIME ZONE 'utc'
                """,
                (datetime.now(timezone.utc) + timedelta(days=2), match_id),
            )
            matches.append((match_id, team_one, team_two))

    conn.commit()
    if not matches:
        return 0

    placed = 0
    sessions: list[tuple[str, str, str]] = [
        (DEV_USER_EMAIL, DEV_USER_PASSWORD, "favorite"),
        *[(d.email, d.password, d.knockout_strategy) for d in DEMO_USERS],
    ]

    for email, password, strategy in sessions:
        api.login(email, password)
        for match_id, team_one, team_two in matches:
            is_draw, team_id = choose_ko_prediction(strategy, team_one, team_two, rng)
            try:
                api.place_bet(match_id, is_draw, team_id)
                placed += 1
            except RuntimeError as exc:
                if "already" in str(exc).lower():
                    continue
                print(f"  WARN PlaceBet {email} match {match_id}: {exc}")

    return placed


def ensure_placeholder_player(cur: PgCursor, team_id: int) -> int:
    cur.execute(
        """
        SELECT p."Id" FROM "Player" p
        WHERE p."TeamId" = %s
        ORDER BY CASE WHEN p."Name" = %s THEN 0 ELSE 1 END, p."Id"
        LIMIT 1
        """,
        (team_id, PLACEHOLDER_NAME),
    )
    row = cur.fetchone()
    if row:
        return int(row[0])

    cur.execute(
        'SELECT "Id" FROM "PlayerPositions" WHERE "Name" IN (\'Forward\', \'FWD\') LIMIT 1'
    )
    pos = cur.fetchone()
    if not pos:
        cur.execute('SELECT "Id" FROM "PlayerPositions" ORDER BY "Id" LIMIT 1')
        pos = cur.fetchone()
    if not pos:
        raise SystemExit("No PlayerPositions — start API once to seed positions.")
    pos_id = int(pos[0])
    cur.execute(
        """
        INSERT INTO "Player" ("Name", "Number", "TeamId", "PositionId")
        VALUES (%s, 99, %s, %s)
        RETURNING "Id"
        """,
        (PLACEHOLDER_NAME, team_id, pos_id),
    )
    return int(cur.fetchone()[0])


def finish_knockout_matches(
    cur: PgCursor,
    world_cup_id: int,
    stage: int,
    rng: random.Random,
    count: int | None = None,
) -> list[tuple[int, str, str, int, int]]:
    """
    Move kickoff to past, add TeamStats + goals (forced winner).
    Returns list of (match_id, home_name, away_name, home_goals, away_goals).
    """
    rows = list_ready_knockout_matches(cur, world_cup_id, stage, unscored_only=True)
    if count is not None:
        rows = rows[:count]

    finished: list[tuple[int, str, str, int, int]] = []
    kickoff = datetime.now(timezone.utc) - timedelta(hours=3)
    label = STAGE_LABELS.get(stage, str(stage))

    for match_id, team_one, team_two, name_one, name_two in rows:
        home_goals = rng.randint(0, 3)
        away_goals = rng.randint(0, 3)
        if home_goals == away_goals:
            if rng.random() < 0.5:
                home_goals += 1
            else:
                away_goals += 1

        cur.execute(
            'UPDATE "Match" SET "Date" = %s WHERE "Id" = %s',
            (kickoff, match_id),
        )

        cur.execute('SELECT "Id", "TeamId" FROM "TeamStats" WHERE "MatchId" = %s', (match_id,))
        existing = {int(r[1]): int(r[0]) for r in cur.fetchall()}
        # Drop stale stats if teams changed after feeder advance
        for stale_team_id, stale_stats_id in list(existing.items()):
            if stale_team_id not in (team_one, team_two):
                cur.execute('DELETE FROM "Goal" WHERE "TeamStatsId" = %s', (stale_stats_id,))
                cur.execute('DELETE FROM "TeamStats" WHERE "Id" = %s', (stale_stats_id,))
                del existing[stale_team_id]

        for team_id, points in (
            (team_one, 3 if home_goals > away_goals else 0),
            (team_two, 3 if away_goals > home_goals else 0),
        ):
            if team_id in existing:
                cur.execute(
                    'UPDATE "TeamStats" SET "Points" = %s WHERE "Id" = %s',
                    (points, existing[team_id]),
                )
            else:
                cur.execute(
                    """
                    INSERT INTO "TeamStats"
                        ("MatchId", "TeamId", "Possession", "Shots", "ShotsOnTarget", "Points")
                    VALUES (%s, %s, 50, 8, 3, %s)
                    RETURNING "Id"
                    """,
                    (match_id, team_id, points),
                )
                existing[team_id] = int(cur.fetchone()[0])

        player_one = ensure_placeholder_player(cur, team_one)
        player_two = ensure_placeholder_player(cur, team_two)

        for i in range(home_goals):
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (
                    player_one,
                    existing[team_one],
                    kickoff + timedelta(minutes=12 + i * 15),
                ),
            )
        for i in range(away_goals):
            cur.execute(
                """
                INSERT INTO "Goal" ("PlayerId", "TeamStatsId", "TimeScored", "IsOwnGoal")
                VALUES (%s, %s, %s, 0)
                """,
                (
                    player_two,
                    existing[team_two],
                    kickoff + timedelta(minutes=18 + i * 15),
                ),
            )

        finished.append((match_id, name_one, name_two, home_goals, away_goals))
        print(f"  [{label}] {name_one} {home_goals}-{away_goals} {name_two} (match {match_id})")

    return finished


def finish_r32_matches(
    cur: PgCursor,
    api: ApiClient,
    world_cup_id: int,
    count: int,
    rng: random.Random,
) -> list[int]:
    """Compat wrapper — finish first N R32 matches; returns match ids."""
    _ = api
    results = finish_knockout_matches(
        cur, world_cup_id, MATCH_STAGE_ROUND_OF_32, rng, count=count
    )
    return [match_id for match_id, *_ in results]


def play_knockout_to_final(
    cur: PgCursor,
    conn: PgConnection,
    api: ApiClient,
    world_cup_id: int,
    rng: random.Random,
    *,
    skip_resolve: bool = False,
) -> None:
    """Place bets → score → advance → resolve for each KO round through the Final."""
    for stage in KO_STAGE_ORDER:
        label = STAGE_LABELS[stage]
        ready = list_ready_knockout_matches(cur, world_cup_id, stage, unscored_only=True)
        if not ready:
            # Later rounds need feeders first; if still empty after prior rounds, wait/fail
            cur.execute(
                """
                SELECT COUNT(*) FROM "Match" m
                JOIN "Team" t ON t."Id" = COALESCE(m."TeamOneId", m."TeamTwoId")
                JOIN "Group" g ON g."Id" = t."GroupId"
                WHERE g."WorldCupId" = %s AND m."Stage" = %s
                """,
                (world_cup_id, stage),
            )
            # Count all stage matches via feeders belonging to this cup
            cur.execute(
                """
                SELECT COUNT(*) FROM "Match" m
                WHERE m."Stage" = %s
                  AND (
                    m."TeamOneId" IN (
                      SELECT t."Id" FROM "Team" t
                      JOIN "Group" g ON g."Id" = t."GroupId"
                      WHERE g."WorldCupId" = %s
                    )
                    OR m."FeederMatchOneId" IN (
                      SELECT m2."Id" FROM "Match" m2
                      JOIN "Team" t2 ON t2."Id" = m2."TeamOneId"
                      JOIN "Group" g2 ON g2."Id" = t2."GroupId"
                      WHERE g2."WorldCupId" = %s
                    )
                  )
                """,
                (stage, world_cup_id, world_cup_id),
            )
            print(f"\n=== {label}: no ready fixtures (skipping) ===")
            continue

        print(f"\n=== {label}: {len(ready)} match(es) ===")
        placed = place_knockout_bets(api, cur, conn, world_cup_id, rng, stage=stage)
        print(f"  Placed {placed} bet(s)")

        finished = finish_knockout_matches(cur, world_cup_id, stage, rng)
        conn.commit()
        if not finished:
            print(f"  WARN: nothing finished for {label}")
            continue

        advance_knockout_matches(api, [m[0] for m in finished])
        if not skip_resolve:
            api.login(ADMIN_EMAIL, ADMIN_PASSWORD)
            result = api.resolve_world_cup(world_cup_id)
            print(f"  Resolve: {result.get('message', result)}")

        if stage == MATCH_STAGE_FINAL and finished:
            _mid, home, away, hg, ag = finished[0]
            champion = home if hg > ag else away
            print(f"\n*** CHAMPION: {champion} ({home} {hg}-{ag} {away}) ***")

    print_leaderboard(api, world_cup_id)


def advance_knockout_matches(api: ApiClient, match_ids: list[int]) -> None:
    api.login(ADMIN_EMAIL, ADMIN_PASSWORD)
    for match_id in match_ids:
        try:
            api._request("POST", f"/Knockout/AdvanceFromMatch/{match_id}", {}, auth=True)
            print(f"  Advanced bracket from match {match_id}")
        except RuntimeError as exc:
            print(f"  WARN AdvanceFromMatch {match_id}: {exc}")


def print_leaderboard(api: ApiClient, world_cup_id: int) -> None:
    board = api.leaderboard(world_cup_id)
    print(f"\n=== Leaderboard (WorldCup {world_cup_id}) ===")
    if not board:
        print("  (empty)")
        return
    for entry in board[:15]:
        print(
            f"  #{entry.get('rank')} {entry.get('userName')} "
            f"— {entry.get('totalPoints')} pts "
            f"({entry.get('resolvedBets')} resolved)"
        )


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="WC 2030 betting demo seed")
    parser.add_argument("--api", default=API_BASE, help="API base URL")
    parser.add_argument("--seed", type=int, default=42, help="RNG seed")
    parser.add_argument(
        "--wipe-bets",
        action="store_true",
        help="Delete existing bets for WC 2030 before seeding",
    )
    parser.add_argument(
        "--group-matches",
        type=int,
        default=None,
        help="Limit finished group matches to bet on (default: all 72)",
    )
    parser.add_argument(
        "--finish-r32",
        type=int,
        default=2,
        help="How many R32 matches to score now (default 2; ignored with --to-final)",
    )
    parser.add_argument(
        "--to-final",
        action="store_true",
        help="Play full knockout path R32→R16→QF→SF→Final with bets each round",
    )
    parser.add_argument(
        "--skip-resolve",
        action="store_true",
        help="Do not call ResolveBetsForWorldCup",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> None:
    args = parse_args(argv)
    rng = random.Random(args.seed)
    api = ApiClient(args.api)

    print(f"API={args.api}")
    print("Checking health...")
    try:
        urllib.request.urlopen(f"{args.api}/health", timeout=10)
    except Exception as exc:
        raise SystemExit(f"API not reachable at {args.api}: {exc}") from exc

    conn: PgConnection = psycopg2.connect(CONN)
    cur: PgCursor = conn.cursor()
    try:
        world_cup_id = find_world_cup_id(cur)
        print(f"WorldCup 2030 Id={world_cup_id}")

        if args.wipe_bets:
            n = wipe_bets_for_world_cup(cur, world_cup_id)
            conn.commit()
            print(f"Wiped bets for matches scoped to WC 2030 (touched {n} match ids)")

        print("Ensuring demo users...")
        user_ids = ensure_demo_users(api, cur)
        conn.commit()

        print("Backfilling bets on finished group matches (DB)...")
        group_bets = backfill_group_bets(
            cur, world_cup_id, user_ids, rng, args.group_matches
        )
        conn.commit()
        print(f"  Inserted {group_bets} group-stage bet(s)")

        if not args.skip_resolve:
            print("Admin resolve finished group matches...")
            api.login(ADMIN_EMAIL, ADMIN_PASSWORD)
            result = api.resolve_world_cup(world_cup_id)
            print(
                f"  {result.get('message', result)} "
                f"(matchesProcessed={result.get('matchesProcessed')}, "
                f"totalResolved={result.get('totalResolvedCount')})"
            )

        print_leaderboard(api, world_cup_id)

        if args.to_final:
            play_knockout_to_final(
                cur,
                conn,
                api,
                world_cup_id,
                rng,
                skip_resolve=args.skip_resolve,
            )
        else:
            print("Placing bets on upcoming R32 (API)...")
            ko_bets = place_knockout_bets(api, cur, conn, world_cup_id, rng)
            print(f"  Placed {ko_bets} R32 bet(s)")

            if args.finish_r32 > 0:
                print(f"\nFinishing {args.finish_r32} R32 match(es) for live resolve...")
                finished = finish_r32_matches(
                    cur, api, world_cup_id, args.finish_r32, rng
                )
                conn.commit()
                if finished:
                    advance_knockout_matches(api, finished)
                    if not args.skip_resolve:
                        api.login(ADMIN_EMAIL, ADMIN_PASSWORD)
                        result = api.resolve_world_cup(world_cup_id)
                        print(f"  Post-KO resolve: {result.get('message', result)}")
                    print_leaderboard(api, world_cup_id)

        print("\n=== Demo logins ===")
        print(f"  Admin:  {ADMIN_EMAIL} / {ADMIN_PASSWORD}")
        print(f"  User:   {DEV_USER_EMAIL} / {DEV_USER_PASSWORD}")
        print(f"  Demos:  demo.alice@localhost ... demo.eve@localhost / {DEMO_PASSWORD}")
        print("  SPA: select World Cup 2030 -> /standings /fixtures /bets /leaderboard /bracket")
        print("  Admin: /admin/schedule or hub -> Resolve bets / live console for more KO scores")
    except Exception:
        conn.rollback()
        raise
    finally:
        cur.close()
        conn.close()


if __name__ == "__main__":
    main()
