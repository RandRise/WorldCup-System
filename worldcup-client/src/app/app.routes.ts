import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin.guard';
import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';
import { ShellComponent } from './core/layout/shell/shell.component';
import { AdminLayoutComponent } from './features/admin/admin-layout/admin-layout.component';
import { AdminScheduleComponent } from './features/admin/schedule/admin-schedule.component';
import { AdminTeamsComponent } from './features/admin/teams/admin-teams.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { MyBetsComponent } from './features/bets/my-bets.component';
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
      { path: 'fixtures', component: FixturesComponent },
      { path: 'standings', component: StandingsComponent },
      { path: 'leaderboard', component: LeaderboardComponent },
      { path: 'bets', component: MyBetsComponent, canActivate: [authGuard] },
      {
        path: 'admin',
        component: AdminLayoutComponent,
        canActivate: [adminGuard],
        children: [
          { path: '', redirectTo: 'teams', pathMatch: 'full' },
          { path: 'teams', component: AdminTeamsComponent },
          { path: 'schedule', component: AdminScheduleComponent },
        ],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
