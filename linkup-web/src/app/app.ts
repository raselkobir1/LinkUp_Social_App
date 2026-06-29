import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { NotificationHubService } from './core/signalr/notification-hub.service';
import { ChatHubService } from './core/signalr/chat-hub.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  styles: []
})
export class App implements OnInit {
  private auth = inject(AuthService);
  private notifHub = inject(NotificationHubService);
  private chatHub = inject(ChatHubService);

  async ngOnInit(): Promise<void> {
    if (this.auth.isLoggedIn()) {
      await Promise.all([this.notifHub.connect(), this.chatHub.connect()]);
    }
  }
}
