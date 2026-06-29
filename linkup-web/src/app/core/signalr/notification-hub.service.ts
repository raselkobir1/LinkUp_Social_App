import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { NotificationDto } from '../models/notification.model';
import { NotificationService } from '../services/notification.service';

@Injectable({ providedIn: 'root' })
export class NotificationHubService implements OnDestroy {
  private hub!: HubConnection;

  readonly notification$ = new Subject<NotificationDto>();
  readonly connected$ = new BehaviorSubject(false);

  constructor(private auth: AuthService, private notifService: NotificationService) {}

  async connect(): Promise<void> {
    const token = this.auth.getAccessToken();
    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/notification`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('ReceiveNotification', (n: NotificationDto) => {
      this.notification$.next(n);
      this.notifService.unreadCount.update(c => c + 1);
    });

    this.hub.on('NotificationCountUpdated', (count: number) => {
      this.notifService.unreadCount.set(count);
    });

    this.hub.onreconnected(() => this.connected$.next(true));
    this.hub.onclose(() => this.connected$.next(false));

    await this.hub.start();
    this.connected$.next(true);
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.connected$.next(false);
  }

  ngOnDestroy(): void { this.disconnect(); }
}
