# Remove demo seed & CSV import — Change Log

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Remove demo seed & CSV import

13 Jul 2026 — cleanup of unused demo tournament seeding and bulk CSV import. [← Changes Review](../changes-review.md)

### Removed

- Demo seed + clear-tournament API, services, scripts, tests
- Embedded `CitiesCsvData`, `StadiumCsvData`, `DemoSeedData`
- CSV import actions on Country / City / Stadium controllers
- Admin seed and reference Angular features (both client copies)
- `DevSeed:WorldCup2026` config (appsettings, Docker, `.env.example`)
- EPPlus package references

### Still available

- `GET Country/GetAllCountries`, `GET City/GetAllCities`, `GET Stadium/GetStadiums`
- `POST Stadium/AddStadium`, `POST Stadium/UpdateStadium` (Admin)
- Admin Teams & Match schedule (default admin landing: teams)
- Development Identity role users + player-position startup seed

### Impact

- Other domain APIs unchanged — they only need FK rows to exist in the DB
- Integration tests unaffected (countries endpoint may return an empty array)
- Fresh databases need a one-time insert of countries/cities/stadiums before Teams/Schedule dropdowns work
