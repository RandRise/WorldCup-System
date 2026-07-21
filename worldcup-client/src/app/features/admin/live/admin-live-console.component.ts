import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BetApiService } from '../../../core/api/bet-api.service';
import { CardApiService } from '../../../core/api/card-api.service';
import { GoalApiService } from '../../../core/api/goal-api.service';
import { MatchApiService } from '../../../core/api/match-api.service';
import { TeamStatsApiService } from '../../../core/api/team-stats-api.service';
import { TournamentApiService } from '../../../core/api/tournament-api.service';
import { MatchDetail, Player, ResolveBetsResult } from '../../../core/models/api.models';
import {
  PLACEHOLDER_SCORER_NAME,
  toHonestPlayerName,
} from '../../../core/utils/placeholder-scorer';

@Component({
  selector: 'app-admin-live-console',
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './admin-live-console.component.html',
  styleUrl: './admin-live-console.component.scss',
})
export class AdminLiveConsoleComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly matchApi = inject(MatchApiService);
  private readonly goalApi = inject(GoalApiService);
  private readonly cardApi = inject(CardApiService);
  private readonly statsApi = inject(TeamStatsApiService);
  private readonly betApi = inject(BetApiService);
  private readonly tournamentApi = inject(TournamentApiService);

  protected readonly isLoading = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly warningMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly match = signal<MatchDetail | null>(null);
  protected readonly teamOnePlayers = signal<Player[]>([]);
  protected readonly teamTwoPlayers = signal<Player[]>([]);
  protected readonly lastResolve = signal<ResolveBetsResult | null>(null);

  protected readonly matchId = computed(() => Number(this.route.snapshot.paramMap.get('matchId')));

  protected goalForm = {
    teamId: 0,
    playerId: 0,
    minute: 0,
    isOwnGoal: false,
  };

  protected cardForm = {
    teamId: 0,
    playerId: 0,
    minute: 0,
    cardType: 1,
  };

  protected statsForm = {
    teamOnePossession: 50,
    teamOneShots: 0,
    teamOneShotsOnTarget: 0,
    teamTwoPossession: 50,
    teamTwoShots: 0,
    teamTwoShotsOnTarget: 0,
  };

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    const matchId = this.matchId();
    if (!matchId) {
      this.errorMessage.set('Invalid match.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const match = await this.matchApi.getMatchById(matchId);
      this.match.set(match);

      if (match.teamOneId == null || match.teamTwoId == null) {
        this.teamOnePlayers.set([]);
        this.teamTwoPlayers.set([]);
        this.errorMessage.set(
          'Both teams must be set before recording live events. Wait for feeder matches to finish (auto-advance).',
        );
        return;
      }

      const [teamOnePlayers, teamTwoPlayers] = await Promise.all([
        this.tournamentApi.getPlayersByTeam(match.teamOneId),
        this.tournamentApi.getPlayersByTeam(match.teamTwoId),
      ]);
      this.teamOnePlayers.set(teamOnePlayers);
      this.teamTwoPlayers.set(teamTwoPlayers);

      this.goalForm.teamId = match.teamOneId;
      this.goalForm.playerId = this.playersForTeam(match.teamOneId)[0]?.id ?? 0;
      this.cardForm.teamId = match.teamOneId;
      this.cardForm.playerId = this.playersForTeam(match.teamOneId)[0]?.id ?? 0;

      const teamOneStats = match.teamOneStats;
      const teamTwoStats = match.teamTwoStats;
      if (teamOneStats && teamTwoStats) {
        this.statsForm = {
          teamOnePossession: teamOneStats.possession,
          teamOneShots: teamOneStats.shots,
          teamOneShotsOnTarget: teamOneStats.shotsOnTarget,
          teamTwoPossession: teamTwoStats.possession,
          teamTwoShots: teamTwoStats.shots,
          teamTwoShotsOnTarget: teamTwoStats.shotsOnTarget,
        };
      }
    } catch {
      this.errorMessage.set('Failed to load match.');
    } finally {
      this.isLoading.set(false);
    }
  }

  /** Squad picker for live events — excludes import/sync placeholder "Tournament Scorer". */
  playersForTeam(teamId: number): Player[] {
    const match = this.match();
    if (!match) {
      return [];
    }
    const players =
      teamId === match.teamOneId ? this.teamOnePlayers() : this.teamTwoPlayers();
    return players.filter(
      (player) => player.name !== PLACEHOLDER_SCORER_NAME && player.number !== 99,
    );
  }

  onGoalTeamChange(): void {
    const players = this.playersForTeam(this.goalForm.teamId);
    this.goalForm.playerId = players[0]?.id ?? 0;
  }

  onCardTeamChange(): void {
    const players = this.playersForTeam(this.cardForm.teamId);
    this.cardForm.playerId = players[0]?.id ?? 0;
  }

  async addGoal(): Promise<void> {
    const match = this.match();
    if (!match) {
      return;
    }

    this.clearMessages();
    try {
      const response = await this.goalApi.addGoal({
        matchId: match.id,
        teamId: this.goalForm.teamId,
        playerId: this.goalForm.playerId,
        minute: this.goalForm.minute,
        isOwnGoal: this.goalForm.isOwnGoal,
      });
      this.applyCommandResponse(response, 'Goal recorded.');
      await this.load();
      const refreshed = this.match();
      if (refreshed?.status === 'Finished') {
        await this.tryResolve();
      }
    } catch {
      this.errorMessage.set('Failed to record goal.');
    }
  }

  async deleteGoal(goalId: number): Promise<void> {
    this.clearMessages();
    try {
      const response = await this.goalApi.deleteGoal(goalId);
      this.applyCommandResponse(
        response,
        'Goal removed — bets re-scored if match finished.',
      );
      await this.load();
    } catch {
      this.errorMessage.set('Failed to delete goal.');
    }
  }

  async addCard(): Promise<void> {
    const match = this.match();
    if (!match) {
      return;
    }

    this.clearMessages();
    try {
      await this.cardApi.addCard({
        matchId: match.id,
        teamId: this.cardForm.teamId,
        playerId: this.cardForm.playerId,
        minute: this.cardForm.minute,
        cardType: this.cardForm.cardType,
      });
      this.message.set('Card recorded.');
      await this.load();
    } catch {
      this.errorMessage.set('Failed to record card.');
    }
  }

  async deleteCard(cardId: number): Promise<void> {
    this.clearMessages();
    try {
      await this.cardApi.deleteCard(cardId);
      this.message.set('Card removed.');
      await this.load();
    } catch {
      this.errorMessage.set('Failed to delete card.');
    }
  }

  async saveStats(): Promise<void> {
    const match = this.match();
    if (!match) {
      return;
    }

    this.clearMessages();
    if (match.teamOneId == null || match.teamTwoId == null) {
      this.errorMessage.set('Both teams must be assigned before updating stats.');
      return;
    }
    if (this.statsForm.teamOnePossession + this.statsForm.teamTwoPossession !== 100) {
      this.errorMessage.set('Possession must sum to 100%.');
      return;
    }

    const teamOneId: number = match.teamOneId;
    const teamTwoId: number = match.teamTwoId;

    try {
      // Sequential writes: TeamStatsService auto-balances the opposing possession row.
      // Parallel updates race and can persist splits that do not match the form.
      await this.statsApi.updateTeamStats({
        matchId: match.id,
        teamId: teamOneId,
        possession: this.statsForm.teamOnePossession,
        shots: this.statsForm.teamOneShots,
        shotsOnTarget: this.statsForm.teamOneShotsOnTarget,
      });
      await this.statsApi.updateTeamStats({
        matchId: match.id,
        teamId: teamTwoId,
        possession: this.statsForm.teamTwoPossession,
        shots: this.statsForm.teamTwoShots,
        shotsOnTarget: this.statsForm.teamTwoShotsOnTarget,
      });
      this.message.set('Team stats updated.');
      await this.load();
    } catch {
      this.errorMessage.set('Failed to update stats.');
    }
  }

  async tryResolve(): Promise<void> {
    const match = this.match();
    if (!match || match.status !== 'Finished') {
      return;
    }

    try {
      const result = await this.betApi.resolveBetsForMatch(match.id);
      this.lastResolve.set(result);
      if (!this.warningMessage()) {
        this.message.set(result.message);
      }
    } catch {
      if (!this.warningMessage()) {
        this.errorMessage.set('Could not resolve bets — match may still be in play or stats incomplete.');
      }
    }
  }

  allEvents(): { id: number; type: string; minute: number; label: string; teamName?: string | null }[] {
    const match = this.match();
    if (!match) {
      return [];
    }

    const events: { id: number; type: string; minute: number; label: string; teamName?: string | null }[] = [];

    for (const goal of match.teamOneStats?.goals ?? []) {
      const honestName = toHonestPlayerName(goal.playerName);
      events.push({
        id: goal.id,
        type: 'goal',
        minute: goal.minute,
        label: `${honestName ?? 'Goal'}${goal.isOwnGoal ? ' (OG)' : ''}`,
        teamName: match.teamOneName,
      });
    }
    for (const goal of match.teamTwoStats?.goals ?? []) {
      const honestName = toHonestPlayerName(goal.playerName);
      events.push({
        id: goal.id,
        type: 'goal',
        minute: goal.minute,
        label: `${honestName ?? 'Goal'}${goal.isOwnGoal ? ' (OG)' : ''}`,
        teamName: match.teamTwoName,
      });
    }
    for (const card of match.teamOneStats?.cards ?? []) {
      const honestName = toHonestPlayerName(card.playerName);
      const cardType = card.type ?? 'Card';
      events.push({
        id: card.id,
        type: 'card',
        minute: card.minute,
        label: honestName ? `${cardType} — ${honestName}` : cardType,
        teamName: match.teamOneName,
      });
    }
    for (const card of match.teamTwoStats?.cards ?? []) {
      const honestName = toHonestPlayerName(card.playerName);
      const cardType = card.type ?? 'Card';
      events.push({
        id: card.id,
        type: 'card',
        minute: card.minute,
        label: honestName ? `${cardType} — ${honestName}` : cardType,
        teamName: match.teamTwoName,
      });
    }

    return events.sort((left, right) => left.minute - right.minute);
  }

  private clearMessages(): void {
    this.message.set(null);
    this.warningMessage.set(null);
    this.errorMessage.set(null);
  }

  private applyCommandResponse(response: string, fallback: string): void {
    const text = response?.trim();
    if (text && text.includes('Warning:')) {
      this.warningMessage.set(text);
      return;
    }

    this.message.set(text && text.length > 0 ? text : fallback);
  }
}
