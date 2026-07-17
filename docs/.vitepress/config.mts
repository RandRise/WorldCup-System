import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'WorldCup System',
  description: 'Project planning, roadmap, and change reviews',
  cleanUrls: true,
  ignoreDeadLinks: true,
  themeConfig: {
    nav: [
      { text: 'Dashboard', link: '/' },
      { text: 'Roadmap', link: '/roadmap' },
      { text: 'Changes', link: '/changes-review' },
      { text: 'API Status', link: '/api-status' },
    ],
    sidebar: [
      {
        text: 'Project',
        items: [
          { text: 'Dashboard', link: '/' },
          { text: 'System Overview', link: '/overview' },
          { text: 'Completion Roadmap', link: '/roadmap' },
          { text: 'Dev Workflow', link: '/workflow' },
          { text: 'API Status', link: '/api-status' },
          { text: 'Data Model', link: '/data-model' },
          { text: 'Changes Review', link: '/changes-review' },
        ],
      },
      {
        text: 'Phase change logs',
        collapsed: false,
        items: [
          { text: 'Phase 1 — Identity', link: '/changes/phase-1-identity' },
          { text: 'Phase 2 — Tournament Setup', link: '/changes/phase-2-tournament-setup' },
          { text: 'Phase 3 — Match Lifecycle', link: '/changes/phase-3-match-lifecycle' },
          { text: 'Phase 4 — Betting', link: '/changes/phase-4-betting' },
          { text: 'Phase 5 — Frontend SPA', link: '/changes/phase-5-frontend-spa' },
          { text: 'Phase 6 — Quality & Ops', link: '/changes/phase-6-quality-ops' },
          { text: 'Phase 7 — Bugfixes', link: '/changes/phase-7-bugfixes' },
          { text: 'Phase 8 — Dashboards', link: '/changes/phase-8-dashboards' },
          { text: 'Phase 9 — Ops Close-Out', link: '/changes/phase-9-ops-closeout' },
          { text: 'Phase 10 — Match Sync (Task 1)', link: '/changes/phase-10-match-sync' },
          { text: 'Phase 10 — Fixtures UX (Task 2)', link: '/changes/phase-10-fixtures-ux' },
          { text: 'Phase 10 — Planned (Task 3)', link: '/changes/phase-10-planned' },
          { text: 'WC 2026 Live Data', link: '/changes/wc2026-live-data' },
          { text: 'Remove Demo Seed', link: '/changes/remove-demo-seed' },
        ],
      },
    ],
    search: {
      provider: 'local',
    },
    outline: {
      level: [2, 3],
    },
  },
})
