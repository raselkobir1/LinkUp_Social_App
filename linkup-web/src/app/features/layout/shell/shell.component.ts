import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SearchService } from '../../../core/services/search.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, CommonModule,
    MatIconModule, MatBadgeModule, MatMenuModule, MatButtonModule, MatTooltipModule
  ],
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss']
})
export class ShellComponent {
  auth = inject(AuthService);
  notifService = inject(NotificationService);
  private searchSvc = inject(SearchService);
  private router = inject(Router);

  searchQuery = signal('');

  onSearch(event: Event): void {
    const q = (event.target as HTMLInputElement).value.trim();
    if (q) this.router.navigate(['/search'], { queryParams: { q } });
  }

  logout(): void {
    this.auth.logout().subscribe({ error: () => {} });
  }
}
