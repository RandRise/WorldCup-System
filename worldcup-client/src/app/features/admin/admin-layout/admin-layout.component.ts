import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
})
export class AdminLayoutComponent {
  protected readonly links = [
    { path: '/admin/seed', label: 'Demo seed' },
    { path: '/admin/reference', label: 'Reference data' },
    { path: '/admin/teams', label: 'Teams & squads' },
    { path: '/admin/schedule', label: 'Match schedule' },
  ];
}
