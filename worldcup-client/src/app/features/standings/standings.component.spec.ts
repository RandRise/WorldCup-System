import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { StandingsComponent } from './standings.component';
import { StandingsApiService } from '../../core/api/standings-api.service';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { Group, Standing } from '../../core/models/api.models';

describe('StandingsComponent', () => {
  let fixture: ComponentFixture<StandingsComponent>;
  let standingsApi: jasmine.SpyObj<StandingsApiService>;
  let groupsForSelectedWorldCup: Group[];

  const cupOneGroups: Group[] = [
    { id: 1, name: 'A', worldCupId: 1 },
    { id: 2, name: 'B', worldCupId: 1 },
  ];

  const cupTwoGroups: Group[] = [{ id: 10, name: 'A', worldCupId: 2 }];

  const sampleStandings: Standing[] = [
    {
      teamId: 1,
      teamName: 'Qatar',
      rank: 1,
      played: 3,
      won: 2,
      drawn: 0,
      lost: 1,
      goalsFor: 5,
      goalsAgainst: 3,
      goalDifference: 2,
      points: 6,
    },
  ];

  beforeEach(async () => {
    groupsForSelectedWorldCup = [...cupOneGroups];
    standingsApi = jasmine.createSpyObj<StandingsApiService>('StandingsApiService', [
      'getGroupStandings',
    ]);
    standingsApi.getGroupStandings.and.resolveTo(sampleStandings);

    await TestBed.configureTestingModule({
      imports: [StandingsComponent],
      providers: [
        provideRouter([]),
        { provide: StandingsApiService, useValue: standingsApi },
        {
          provide: WorldCupContextService,
          useValue: {
            groupsForSelectedWorldCup: () => groupsForSelectedWorldCup,
            selectedWorldCupId: () => 1,
            selectedWorldCup: () => null,
            selectedWorldCupLabel: () => 'FIFA World Cup 2026',
            worldCups: () => [],
            worldCupLabel: (worldCup: { year: string }) =>
              `FIFA World Cup ${new Date(worldCup.year).getUTCFullYear()}`,
            selectWorldCup: jasmine.createSpy('selectWorldCup'),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StandingsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads standings for the first group on init', () => {
    expect(standingsApi.getGroupStandings).toHaveBeenCalledWith(1);
    expect(fixture.componentInstance['standings']()).toEqual(sampleStandings);
  });

  it('loads standings when user selects a different group', async () => {
    standingsApi.getGroupStandings.calls.reset();

    fixture.componentInstance.selectGroup(cupOneGroups[1]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(standingsApi.getGroupStandings).toHaveBeenCalledWith(2);
  });

  it('selects the first group when tournament groups change', async () => {
    groupsForSelectedWorldCup = [...cupTwoGroups];
    fixture.destroy();
    fixture = TestBed.createComponent(StandingsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(standingsApi.getGroupStandings).toHaveBeenCalledWith(10);
  });
});
