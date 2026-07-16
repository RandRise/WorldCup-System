BEGIN;

-- Repair incomplete AddMatchStage migration (history says applied; columns missing).
ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederMatchOneId" integer NULL;
ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederMatchTwoId" integer NULL;
ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederOneTakesLoser" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederTwoTakesLoser" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Match" ALTER COLUMN "TeamOneId" DROP NOT NULL;
ALTER TABLE "Match" ALTER COLUMN "TeamTwoId" DROP NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_Match_FeederMatchOneId" ON "Match" ("FeederMatchOneId");
CREATE INDEX IF NOT EXISTS "IX_Match_FeederMatchTwoId" ON "Match" ("FeederMatchTwoId");
CREATE INDEX IF NOT EXISTS "IX_Match_Stage" ON "Match" ("Stage");

-- Ensure World Cup 2026 row exists (reuse Id 1 when present).
INSERT INTO "WorldCups" ("Year")
SELECT TIMESTAMPTZ '2026-06-11 00:00:00+00'
WHERE NOT EXISTS (SELECT 1 FROM "WorldCups");

-- Resolve tournament id used for groups.
DO $$
DECLARE
    wc_id integer;
BEGIN
    SELECT "Id" INTO wc_id FROM "WorldCups" ORDER BY "Id" LIMIT 1;

    -- Create groups A-L if missing.
    INSERT INTO "Group" ("Name", "WorldCupId")
    SELECT g.name, wc_id
    FROM (VALUES
        ('A'), ('B'), ('C'), ('D'), ('E'), ('F'),
        ('G'), ('H'), ('I'), ('J'), ('K'), ('L')
    ) AS g(name)
    WHERE NOT EXISTS (
        SELECT 1 FROM "Group" existing
        WHERE existing."WorldCupId" = wc_id AND existing."Name" = g.name
    );
END $$;

-- Helper: map country display names used in groups to DB "Countries"."Name"
-- Create/update teams for FIFA World Cup 2026 groups.
DO $$
DECLARE
    wc_id integer;
    group_id integer;
    country_id integer;
    country_names text[];
    country_name text;
    aliases text[];
BEGIN
    SELECT "Id" INTO wc_id FROM "WorldCups" ORDER BY "Id" LIMIT 1;

    -- Each entry: group letter, then preferred country names (first match wins).
    FOREACH country_name IN ARRAY ARRAY[
        'A|Mexico',
        'A|South Korea',
        'A|South Africa',
        'A|Czech Republic|Czechia',
        'B|Canada',
        'B|Switzerland',
        'B|Qatar',
        'B|Bosnia and Herzegovina',
        'C|Brazil',
        'C|Morocco',
        'C|Scotland',
        'C|Haiti',
        'D|United States|USA',
        'D|Paraguay',
        'D|Australia',
        'D|Turkey|Türkiye|Turkiye',
        'E|Germany',
        'E|Ecuador',
        'E|Côte d''Ivoire|Ivory Coast|Cote d''Ivoire',
        'E|Curaçao|Curacao',
        'F|Netherlands',
        'F|Japan',
        'F|Tunisia',
        'F|Sweden',
        'G|Belgium',
        'G|Iran',
        'G|Egypt',
        'G|New Zealand',
        'H|Spain',
        'H|Uruguay',
        'H|Saudi Arabia',
        'H|Cape Verde|Cabo Verde',
        'I|France',
        'I|Senegal',
        'I|Norway',
        'I|Iraq',
        'J|Argentina',
        'J|Austria',
        'J|Algeria',
        'J|Jordan',
        'K|Portugal',
        'K|Colombia',
        'K|Uzbekistan',
        'K|DR Congo|Congo',
        'L|England',
        'L|Croatia',
        'L|Panama',
        'L|Ghana'
    ]
    LOOP
        aliases := string_to_array(country_name, '|');
        SELECT g."Id" INTO group_id
        FROM "Group" g
        WHERE g."WorldCupId" = wc_id AND g."Name" = aliases[1];

        IF group_id IS NULL THEN
            RAISE EXCEPTION 'Missing group % for WorldCup %', aliases[1], wc_id;
        END IF;

        country_id := NULL;
        FOR i IN 2..array_length(aliases, 1) LOOP
            SELECT c."Id" INTO country_id
            FROM "Countries" c
            WHERE c."Name" = aliases[i]
            LIMIT 1;

            IF country_id IS NOT NULL THEN
                EXIT;
            END IF;
        END LOOP;

        IF country_id IS NULL THEN
            RAISE EXCEPTION 'Missing country for aliases: %', country_name;
        END IF;

        IF EXISTS (SELECT 1 FROM "Team" t WHERE t."CountryId" = country_id) THEN
            UPDATE "Team"
            SET "GroupId" = group_id
            WHERE "CountryId" = country_id;
        ELSE
            INSERT INTO "Team" ("CountryId", "GroupId")
            VALUES (country_id, group_id);
        END IF;
    END LOOP;

    -- Remove empty placeholder groups (e.g. QF1-QF4) for this tournament.
    DELETE FROM "Group" g
    WHERE g."WorldCupId" = wc_id
      AND g."Name" NOT IN ('A','B','C','D','E','F','G','H','I','J','K','L')
      AND NOT EXISTS (SELECT 1 FROM "Team" t WHERE t."GroupId" = g."Id");
END $$;

COMMIT;

-- Verification
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'Match'
ORDER BY ordinal_position;

SELECT g."Name" AS group_name, c."Name" AS country
FROM "Team" t
JOIN "Group" g ON g."Id" = t."GroupId"
JOIN "Countries" c ON c."Id" = t."CountryId"
JOIN "WorldCups" w ON w."Id" = g."WorldCupId"
ORDER BY g."Name", c."Name";

SELECT g."Name", COUNT(t."Id") AS teams
FROM "Group" g
LEFT JOIN "Team" t ON t."GroupId" = g."Id"
GROUP BY g."Id", g."Name"
ORDER BY g."Name";
