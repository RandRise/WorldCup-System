# WorldCup System — Data Model

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Data Model

Entity relationships and table inventory. All entities live in `WorldCup-System/Data/Entities/`.

### Entity Relationship Diagram

### Domain Tables

| Table | Entity File | Key Relationships | API |
| --- | --- | --- | --- |
| `WorldCups` | WorldCup.cs | Has many Groups | Done |
| `Group` | Group.cs | Belongs to WorldCup; has Teams | Done |
| `Countries` | Country.cs | Has Cities; Team links to Country | Done |
| `City` | City.cs | Belongs to Country; has Stadiums | Done |
| `Stadium` | Stadium.cs | Belongs to City; hosts Matches | Done |
| `Team` | Team.cs | One per Country **per World Cup** (via Group); has Coach, Players | Done |
| `Coach` | Coach.cs | One per Team | Done |
| `Player` | Player.cs | Belongs to Team; has PlayerPosition; optional `ExternalPlayerId` (FIFA IdPlayer) | Done |
| `PlayerPositions` | PlayerPosition.cs | Lookup table for player roles | Done |
| `Match` | Match.cs | Two Teams (nullable for TBD knockout), Stadium, date/time, `MatchStage` (Group → RoundOf32 → … → Final), optional feeder match IDs | Done |
| `TeamStats` | TeamStats.cs | Per-match stats (possession, shots, points) | Done |
| `Goal` | Goal.cs | Scorer, minute, own-goal flag; linked to Match | Done |
| `Card` | Card.cs | Yellow/red card; linked to Match and Player | Done |
| `Standings` | — (computed) | Group table from match results; no DB table | Done |
| `Bet` | Bet.cs | User prediction on Match outcome | Done |
| `BetResult` | BetResult.cs | Points earned from a Bet (via `BetController`) | Done |
| `Users` | User.cs | Custom Identity user (Name, RefreshToken); nullable `CompanyId` (Phase 13) | Done |
| `Company` | Company.cs | Workplace pool; unique `InviteCode`; optional `Slug`; members via `User.CompanyId` — [Task 2](changes/phase-13-company-model.md) · [Task 3 API](changes/phase-13-company-api.md) | Done |

### Identity Tables (ASP.NET)

Managed by ASP.NET Identity alongside the custom `User` entity:

- `AspNetUsers`, `AspNetRoles`
- `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`
- `AspNetRoleClaims`

### Data Population Order

Populate once and keep rows in PostgreSQL. Respect foreign key dependencies (no CSV / demo-seed pipeline):

### Operational Notes

Demo tournament seeding (`DemoSeedService`, `SeedController`, embedded CSVs) and CSV bulk import endpoints were removed.
List APIs for countries, cities, and stadiums remain for Teams and Schedule UIs.

### Entity Details — Match Domain

### Match.cs

- Links two Teams (home/away)
- Assigned to a Stadium
- Has date/time for scheduling
- Parent for Goals, Cards, TeamStats
- Optional `ExternalMatchId` (FIFA IdMatch) and `ExternalStageId` (FIFA IdStage) for post-match sync / timeline URLs

### TeamStats.cs

- Per-team stats for a match
- Possession, shots, points
- Used for standings calculation

### Goal.cs

- Scoring Player reference
- Minute of goal
- Own-goal flag

### Card.cs

- Player who received card
- Yellow or red
- Minute of card

### Entity Details — Betting Domain

### Bet.cs

- User places prediction on Match
- Predicted outcome (home win, draw, away win, score, etc.)
- Created before match starts

### BetResult.cs

- One-to-one with Bet
- Points awarded after match resolution
- Aggregated for leaderboard
