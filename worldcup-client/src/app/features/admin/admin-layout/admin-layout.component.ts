import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
})
export class AdminLayoutComponent {
  protected readonly links = [
    { path: '/admin', label: 'Dashboard' },
    { path: '/admin/teams', label: 'Teams & squads' },
    { path: '/admin/schedule', label: 'Match schedule' },
  ];
}
