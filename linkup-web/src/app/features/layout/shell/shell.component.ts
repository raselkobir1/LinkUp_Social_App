import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SearchService } from '../../../core/services/search.service';
import { CallService } from '../../../core/services/call.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, CommonModule,
    MatIconModule, MatBadgeModule, MatMenuModule, MatButtonModule, MatTooltipModule, MatDividerModule
  ],
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss']
})
export class ShellComponent implements OnInit {
  auth = inject(AuthService);
  notifService = inject(NotificationService);
  callService = inject(CallService);
  private searchSvc = inject(SearchService);
  private router = inject(Router);

  searchQuery = signal('');

  ngOnInit(): void {
    // Connect the video-call signaling hub app-wide so incoming calls ring anywhere.
    this.callService.init().catch(() => {});
  }

  onSearch(event: Event): void {
    const q = (event.target as HTMLInputElement).value.trim();
    if (q) this.router.navigate(['/search'], { queryParams: { q } });
  }

  logout(): void {
    this.auth.logout().subscribe({ error: () => {} });
  }
}
