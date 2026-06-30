import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { MessageDto } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatHubService implements OnDestroy {
  private hub!: HubConnection;

  readonly message$ = new Subject<MessageDto>();
  readonly messageEdited$ = new Subject<MessageDto>();
  readonly messageDeleted$ = new Subject<{ messageId: string }>();
  readonly messageRead$ = new Subject<{ messageId: string; userId: string }>();
  readonly messageDelivered$ = new Subject<{ messageId: string; userId: string }>();
  readonly userTyping$ = new Subject<{ chatId: string; userId: string; isTyping: boolean }>();
  readonly userOnline$ = new Subject<{ userId: string; isOnline: boolean }>();
  readonly connected$ = new BehaviorSubject(false);

  constructor(private auth: AuthService) {}

  async connect(): Promise<void> {
    const token = this.auth.getAccessToken();
    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/chat`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // NOTE: the server sends positional args (see ChatHub.cs / ChatManager.cs), not single objects.
    this.hub.on('ReceiveMessage', (msg: MessageDto) => this.message$.next(msg));
    this.hub.on('MessageEdited', (msg: MessageDto) => this.messageEdited$.next(msg));
    this.hub.on('MessageDeleted', (messageId: string) => this.messageDeleted$.next({ messageId }));
    this.hub.on('MessageRead', (messageId: string, userId: string) => this.messageRead$.next({ messageId, userId }));
    this.hub.on('MessageDelivered', (messageId: string, userId: string) => this.messageDelivered$.next({ messageId, userId }));
    this.hub.on('UserTyping', (chatId: string, userId: string, isTyping: boolean) => this.userTyping$.next({ chatId, userId, isTyping }));
    this.hub.on('UserOnline', (userId: string) => this.userOnline$.next({ userId, isOnline: true }));
    this.hub.on('UserOffline', (userId: string) => this.userOnline$.next({ userId, isOnline: false }));

    this.hub.onreconnected(() => this.connected$.next(true));
    this.hub.onclose(() => this.connected$.next(false));

    await this.hub.start();
    this.connected$.next(true);
  }

  joinChat(chatId: string): void { this.hub?.invoke('JoinChat', chatId); }
  leaveChat(chatId: string): void { this.hub?.invoke('LeaveChat', chatId); }
  sendTyping(chatId: string, isTyping: boolean): void { this.hub?.invoke('SendTypingIndicator', chatId, isTyping); }
  markAsRead(messageId: string, chatId: string): void { this.hub?.invoke('MarkAsRead', messageId, chatId); }
  markDelivered(messageId: string, chatId: string): void { this.hub?.invoke('MarkDelivered', messageId, chatId); }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.connected$.next(false);
  }

  ngOnDestroy(): void { this.disconnect(); }
}
