import { Match } from '../../core/models/api.models';
import {
  FIXTURE_FILTER_OPTIONS,
  compareFixtures,
  filterFixtureSections,
  fixtureBucket,
  groupFixtures,
  nextBettableMatch,
} from './fixture-sections';

function match(partial: Partial<Match> & Pick<Match, 'id' | 'date' | 'status'>): Match {
  return {
    stadiumId: 1,
    canBet: false,
    ...partial,
  };
}

describe('fixture-sections', () => {
  const openBettable = match({
    id: 1,
    date: '2026-07-20T18:00:00Z',
    status: 'Scheduled',
    canBet: true,
    teamOneName: 'A',
  });
  const openTbd = match({
    id: 2,
    date: '2026-07-19T15:00:00Z',
    status: 'Scheduled',
    canBet: false,
    teamOneName: 'TBD',
  });
  const live = match({
    id: 3,
    date: '2026-07-18T16:00:00Z',
    status: 'Live',
    teamOneScore: 1,
    teamTwoScore: 0,
  });
  const finishedEarly = match({
    id: 4,
    date: '2026-07-10T12:00:00Z',
    status: 'Finished',
    teamOneScore: 2,
    teamTwoScore: 1,
  });
  const finishedLate = match({
    id: 5,
    date: '2026-07-12T12:00:00Z',
    status: 'Finished',
    teamOneScore: 0,
    teamTwoScore: 0,
  });

  it('exposes filter chip options in UI order', () => {
    expect(FIXTURE_FILTER_OPTIONS.map((option) => option.id)).toEqual([
      'action',
      'open',
      'live',
      'finished',
      'all',
    ]);
  });

  it('classifies matches by status', () => {
    expect(fixtureBucket(openBettable)).toBe('open');
    expect(fixtureBucket(live)).toBe('live');
    expect(fixtureBucket(finishedEarly)).toBe('finished');
  });

  it('buckets unknown status values as open', () => {
    const postponed = match({
      id: 8,
      date: '2026-07-21T12:00:00Z',
      status: 'Postponed',
    });
    expect(fixtureBucket(postponed)).toBe('open');
  });

  it('groups into Open → Live → Finished with date order', () => {
    const sections = groupFixtures([finishedLate, live, openTbd, finishedEarly, openBettable]);

    expect(sections.open.map((m) => m.id)).toEqual([1, 2]);
    expect(sections.live.map((m) => m.id)).toEqual([3]);
    expect(sections.finished.map((m) => m.id)).toEqual([4, 5]);
  });

  it('returns empty sections for an empty match list', () => {
    expect(groupFixtures([])).toEqual({ open: [], live: [], finished: [] });
  });

  it('puts bettable open matches before non-bettable when dates differ', () => {
    expect(compareFixtures(openBettable, openTbd)).toBeLessThan(0);
  });

  it('sorts by kickoff ascending when canBet is equal', () => {
    const earlier = match({
      id: 6,
      date: '2026-07-18T12:00:00Z',
      status: 'Scheduled',
      canBet: true,
    });
    const later = match({
      id: 7,
      date: '2026-07-22T12:00:00Z',
      status: 'Scheduled',
      canBet: true,
    });
    expect(compareFixtures(earlier, later)).toBeLessThan(0);
    expect(compareFixtures(later, earlier)).toBeGreaterThan(0);
  });

  it('filters Action to open + live only', () => {
    const all = groupFixtures([openBettable, live, finishedEarly]);
    const action = filterFixtureSections(all, 'action');

    expect(action.open.length).toBe(1);
    expect(action.live.length).toBe(1);
    expect(action.finished.length).toBe(0);
  });

  it('filters single buckets', () => {
    const all = groupFixtures([openBettable, live, finishedEarly]);

    expect(filterFixtureSections(all, 'open').live.length).toBe(0);
    expect(filterFixtureSections(all, 'open').finished.length).toBe(0);
    expect(filterFixtureSections(all, 'open').open.map((m) => m.id)).toEqual([1]);

    expect(filterFixtureSections(all, 'live').open.length).toBe(0);
    expect(filterFixtureSections(all, 'live').finished.length).toBe(0);
    expect(filterFixtureSections(all, 'live').live.map((m) => m.id)).toEqual([3]);

    expect(filterFixtureSections(all, 'finished').open.length).toBe(0);
    expect(filterFixtureSections(all, 'finished').live.length).toBe(0);
    expect(filterFixtureSections(all, 'finished').finished.map((m) => m.id)).toEqual([4]);
  });

  it('filters All to keep every section', () => {
    const all = groupFixtures([openBettable, live, finishedEarly]);
    const filtered = filterFixtureSections(all, 'all');

    expect(filtered.open.map((m) => m.id)).toEqual([1]);
    expect(filtered.live.map((m) => m.id)).toEqual([3]);
    expect(filtered.finished.map((m) => m.id)).toEqual([4]);
  });

  it('finds the next bettable match by kickoff', () => {
    const later = match({
      id: 9,
      date: '2026-07-25T18:00:00Z',
      status: 'Scheduled',
      canBet: true,
    });
    expect(nextBettableMatch([finishedEarly, later, openBettable, live])?.id).toBe(1);
    expect(nextBettableMatch([finishedEarly, live])).toBeNull();
  });

  it('ignores non-bettable matches when finding next bet', () => {
    expect(nextBettableMatch([openTbd, finishedEarly, live])).toBeNull();
    expect(nextBettableMatch([])).toBeNull();
  });
});
