import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin.guard';
import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';
import { ShellComponent } from './core/layout/shell/shell.component';
import { AdminLayoutComponent } from './features/admin/admin-layout/admin-layout.component';
import { AdminHubComponent } from './features/admin/hub/admin-hub.component';
import { AdminLiveConsoleComponent } from './features/admin/live/admin-live-console.component';
import { AdminScheduleComponent } from './features/admin/schedule/admin-schedule.component';
import { AdminTeamsComponent } from './features/admin/teams/admin-teams.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { MyBetsComponent } from './features/bets/my-bets.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { BracketComponent } from './features/bracket/bracket.component';
import { FixturesComponent } from './features/fixtures/fixtures.component';
import { HomeComponent } from './features/home/home.component';
import { LeaderboardComponent } from './features/leaderboard/leaderboard.component';
import { StandingsComponent } from './features/standings/standings.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', component: HomeComponent },
      { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
      { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
      { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
      { path: 'fixtures', component: FixturesComponent },
      { path: 'bracket', component: BracketComponent },
      { path: 'standings', component: StandingsComponent },
      { path: 'leaderboard', component: LeaderboardComponent },
      { path: 'bets', component: MyBetsComponent, canActivate: [authGuard] },
      {
        path: 'admin',
        component: AdminLayoutComponent,
        canActivate: [adminGuard],
        children: [
          { path: '', component: AdminHubComponent },
          { path: 'teams', component: AdminTeamsComponent },
          { path: 'schedule', component: AdminScheduleComponent },
          { path: 'live/:matchId', component: AdminLiveConsoleComponent },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
