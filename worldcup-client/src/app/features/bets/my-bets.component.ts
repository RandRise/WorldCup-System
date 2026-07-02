import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../core/api/bet-api.service';
import { Bet } from '../../core/models/api.models';

@Component({
  selector: 'app-my-bets',
  imports: [DatePipe, RouterLink],
  templateUrl: './my-bets.component.html',
  styleUrl: './my-bets.component.scss',
})
export class MyBetsComponent implements OnInit {
  private readonly betApi = inject(BetApiService);

  protected readonly bets = signal<Bet[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly showActiveOnly = signal(false);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const rows = this.showActiveOnly()
        ? await this.betApi.getMyActiveBets()
        : await this.betApi.getMyBets();
      this.bets.set(rows);
    } catch {
      this.errorMessage.set('Failed to load your bets.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async toggleFilter(activeOnly: boolean): Promise<void> {
    this.showActiveOnly.set(activeOnly);
    await this.load();
  }
}
