import { Match } from '../../core/models/api.models';

/** Filter chip values for the fixtures page. */
export type FixtureFilter = 'action' | 'open' | 'live' | 'finished' | 'all';

export const FIXTURE_FILTER_OPTIONS: { id: FixtureFilter; label: string }[] = [
  { id: 'action', label: 'Action' },
  { id: 'open', label: 'Open bets' },
  { id: 'live', label: 'Live' },
  { id: 'finished', label: 'Finished' },
  { id: 'all', label: 'All' },
];

export type FixtureBucket = 'open' | 'live' | 'finished';

export interface FixtureSections {
  open: Match[];
  live: Match[];
  finished: Match[];
}

/** Classify a match into open (scheduled), live, or finished. */
export function fixtureBucket(match: Match): FixtureBucket {
  if (match.status === 'Live') {
    return 'live';
  }
  if (match.status === 'Finished') {
    return 'finished';
  }
  return 'open';
}

/** Sort by kickoff ascending within a bucket. Bettable scheduled matches first in open. */
export function compareFixtures(a: Match, b: Match): number {
  if (a.canBet !== b.canBet) {
    return a.canBet ? -1 : 1;
  }
  return a.date.localeCompare(b.date);
}

/** Split and sort fixtures into Open → Live → Finished sections. */
export function groupFixtures(matches: Match[]): FixtureSections {
  const open: Match[] = [];
  const live: Match[] = [];
  const finished: Match[] = [];

  for (const match of matches) {
    const bucket = fixtureBucket(match);
    if (bucket === 'live') {
      live.push(match);
    } else if (bucket === 'finished') {
      finished.push(match);
    } else {
      open.push(match);
    }
  }

  open.sort(compareFixtures);
  live.sort(compareFixtures);
  finished.sort(compareFixtures);

  return { open, live, finished };
}

/** Matches visible for the active filter chip. */
export function filterFixtureSections(
  sections: FixtureSections,
  filter: FixtureFilter,
): FixtureSections {
  switch (filter) {
    case 'open':
      return { open: sections.open, live: [], finished: [] };
    case 'live':
      return { open: [], live: sections.live, finished: [] };
    case 'finished':
      return { open: [], live: [], finished: sections.finished };
    case 'action':
      return { open: sections.open, live: sections.live, finished: [] };
    case 'all':
    default:
      return sections;
  }
}

/** First match still open for betting (canBet), else null. */
export function nextBettableMatch(matches: Match[]): Match | null {
  const open = matches
    .filter((match) => match.canBet)
    .sort(compareFixtures);
  return open[0] ?? null;
}
