#!/usr/bin/env python3
"""
Seed Player rows for WC 2026 teams from a CSV file.

Resolves country display names (with aliases) → Team via Countries, then
inserts/updates Player by (TeamId, Number). Does not touch "Tournament Scorer"
(#99) rows used by match import / FIFA sync.

CSV columns (header required):
  country,number,name,position

  country  — display name or alias matching DB Countries.Name
  number   — jersey 1–98 (99 reserved for Tournament Scorer)
  name     — player display name (max 64 chars)
  position — Goalkeeper|Defender|Midfielder|Forward (or GK|DEF|MID|FWD)

Usage:
  python seed_wc2026_players.py
  python seed_wc2026_players.py --csv path/to/players.csv
  python seed_wc2026_players.py --dry-run

Optional:
  WC_DB="host=localhost dbname=TestDatabase user=postgres password=..."
"""

from __future__ import annotations

import argparse
import csv
import os
import sys
from pathlib import Path

import psycopg2

CONN = os.environ.get(
    "WC_DB",
    "host=localhost dbname=TestDatabase user=postgres password=Aa@123456",
)

PLACEHOLDER_NAME = "Tournament Scorer"
PLACEHOLDER_NUMBER = 99

# Country display name in CSV -> DB Countries.Name
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

POSITION_ALIASES = {
    "gk": "Goalkeeper",
    "goalkeeper": "Goalkeeper",
    "def": "Defender",
    "defender": "Defender",
    "mid": "Midfielder",
    "midfielder": "Midfielder",
    "fwd": "Forward",
    "forward": "Forward",
    "striker": "Forward",
}

DEFAULT_CSV = Path(__file__).resolve().parent / "data" / "wc2026_players.csv"


def resolve_country(name: str) -> str:
    cleaned = name.strip()
    return COUNTRY_ALIASES.get(cleaned, cleaned)


def resolve_position(raw: str) -> str:
    key = raw.strip().lower()
    if key not in POSITION_ALIASES:
        raise ValueError(
            f"Unknown position '{raw}'. Use Goalkeeper/Defender/Midfielder/Forward "
            "or GK/DEF/MID/FWD."
        )
    return POSITION_ALIASES[key]


def load_rows(csv_path: Path) -> list[tuple[str, int, str, str]]:
    if not csv_path.is_file():
        raise SystemExit(f"CSV not found: {csv_path}")

    rows: list[tuple[str, int, str, str]] = []
    with csv_path.open(newline="", encoding="utf-8-sig") as handle:
        # Strip blank lines and # comments before DictReader sees them.
        filtered_lines = []
        for line in handle:
            stripped = line.strip()
            if not stripped or stripped.startswith("#"):
                continue
            filtered_lines.append(line)

        reader = csv.DictReader(filtered_lines)
        required = {"country", "number", "name", "position"}
        if reader.fieldnames is None or required - {f.strip().lower() for f in reader.fieldnames}:
            raise SystemExit(
                "CSV must have header columns: country,number,name,position"
            )

        field_map = {f.strip().lower(): f for f in reader.fieldnames}

        for line_no, raw in enumerate(reader, start=2):
            country = (raw[field_map["country"]] or "").strip()
            number_text = (raw[field_map["number"]] or "").strip()
            name = (raw[field_map["name"]] or "").strip()
            position_raw = (raw[field_map["position"]] or "").strip()

            if not any([country, number_text, name, position_raw]):
                continue

            if not country or not number_text or not name or not position_raw:
                raise SystemExit(f"Row {line_no}: incomplete row {raw}")

            try:
                number = int(number_text)
            except ValueError as exc:
                raise SystemExit(f"Row {line_no}: invalid number '{number_text}'") from exc

            if number < 1 or number > 98:
                raise SystemExit(
                    f"Row {line_no}: number must be 1–98 (99 reserved for {PLACEHOLDER_NAME})"
                )

            if len(name) > 64:
                raise SystemExit(f"Row {line_no}: name exceeds 64 characters: {name}")

            if name == PLACEHOLDER_NAME:
                raise SystemExit(
                    f"Row {line_no}: cannot seed reserved name '{PLACEHOLDER_NAME}'"
                )

            try:
                position = resolve_position(position_raw)
            except ValueError as exc:
                raise SystemExit(f"Row {line_no}: {exc}") from exc

            rows.append((resolve_country(country), number, name, position))

    if not rows:
        raise SystemExit(f"No player rows found in {csv_path}")

    return rows


def main() -> None:
    parser = argparse.ArgumentParser(description="Seed WC 2026 players from CSV.")
    parser.add_argument(
        "--csv",
        type=Path,
        default=DEFAULT_CSV,
        help=f"Path to players CSV (default: {DEFAULT_CSV})",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate CSV and resolve teams without writing.",
    )
    args = parser.parse_args()

    players = load_rows(args.csv)
    print(f"Loaded {len(players)} player rows from {args.csv}")

    conn = psycopg2.connect(CONN)
    cur = conn.cursor()

    cur.execute('SELECT "Id", "Name" FROM "PlayerPositions"')
    position_by_name = {name: pid for pid, name in cur.fetchall()}
    for needed in ("Goalkeeper", "Defender", "Midfielder", "Forward"):
        if needed not in position_by_name:
            raise SystemExit(
                f"Missing PlayerPositions row '{needed}'. Start the API once to seed positions."
            )

    cur.execute(
        """
        SELECT t."Id", c."Name"
        FROM "Team" t
        JOIN "Countries" c ON c."Id" = t."CountryId"
        """
    )
    team_by_country = {name: tid for tid, name in cur.fetchall()}

    needed_countries = sorted({country for country, _, _, _ in players})
    missing_countries = [c for c in needed_countries if c not in team_by_country]
    if missing_countries:
        raise SystemExit(
            "Missing teams for countries (run seed-wc2026-groups.sql first):\n  "
            + "\n  ".join(missing_countries)
        )

    # Detect duplicate (country, number) in CSV
    seen: set[tuple[str, int]] = set()
    for country, number, name, _position in players:
        key = (country, number)
        if key in seen:
            raise SystemExit(f"Duplicate jersey in CSV: {country} #{number} ({name})")
        seen.add(key)

    inserted = 0
    updated = 0
    unchanged = 0

    for country, number, name, position_name in players:
        team_id = team_by_country[country]
        position_id = position_by_name[position_name]

        cur.execute(
            """
            SELECT "Id", "Name", "PositionId"
            FROM "Player"
            WHERE "TeamId" = %s AND "Number" = %s
            """,
            (team_id, number),
        )
        existing = cur.fetchone()

        if existing is None:
            if args.dry_run:
                inserted += 1
                continue
            cur.execute(
                """
                INSERT INTO "Player" ("Name", "Number", "TeamId", "PositionId")
                VALUES (%s, %s, %s, %s)
                """,
                (name, number, team_id, position_id),
            )
            inserted += 1
            continue

        player_id, existing_name, existing_position_id = existing
        if existing_name == PLACEHOLDER_NAME or number == PLACEHOLDER_NUMBER:
            raise SystemExit(
                f"Jersey #{number} on {country} is reserved for {PLACEHOLDER_NAME} "
                f"(player id {player_id}). Use 1–98."
            )

        if existing_name == name and existing_position_id == position_id:
            unchanged += 1
            continue

        if args.dry_run:
            updated += 1
            continue

        cur.execute(
            """
            UPDATE "Player"
            SET "Name" = %s, "PositionId" = %s
            WHERE "Id" = %s
            """,
            (name, position_id, player_id),
        )
        updated += 1

    if args.dry_run:
        conn.rollback()
        print(
            f"Dry run OK — would insert {inserted}, update {updated}, "
            f"leave unchanged {unchanged}."
        )
    else:
        conn.commit()
        print(
            f"Seeded players — inserted {inserted}, updated {updated}, "
            f"unchanged {unchanged}."
        )

    cur.close()
    conn.close()


if __name__ == "__main__":
    try:
        main()
    except psycopg2.Error as exc:
        print(f"Database error: {exc}", file=sys.stderr)
        raise SystemExit(1) from exc
