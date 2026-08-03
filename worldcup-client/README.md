# WorldcupClient

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.27.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

### Development

`npm start` / `ng serve` uses the **development** configuration and `src/environments/environment.development.ts` (`apiUrl: http://localhost:5055`).

### Production

1. Set the public API base in `src/environments/environment.ts` (`apiUrl` — no trailing slash). The checked-in value is a `REPLACE_ME` placeholder so a prod build cannot silently keep localhost.
2. Build:

```bash
npm ci
npm run build
```

Artifacts land in `dist/worldcup-client/browser/` — serve that folder as static files (or behind a reverse proxy with SPA fallback to `index.html`). Do not use `ng serve` for production.

Detail: `../docs/changes/phase-14-spa-prod.md` (nested git docs; VitePress twin under workspace `docs/changes/`).

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
