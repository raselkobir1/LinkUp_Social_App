import { Component, inject, OnInit, OnDestroy, signal, Input, computed, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subscription } from 'rxjs';
import { ChatService } from '../../core/services/chat.service';
import { ChatHubService } from '../../core/signalr/chat-hub.service';
import { AuthService } from '../../core/services/auth.service';
import { ChatListDto, MessageDto } from '../../core/models/chat.model';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;
  @Input() chatId?: string;

  private chatSvc = inject(ChatService);
  private chatHub = inject(ChatHubService);
  auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  chats = signal<ChatListDto[]>([]);
  messages = signal<MessageDto[]>([]);
  activeChat = signal<ChatListDto | null>(null);
  newMessage = '';
  loading = signal(false);
  private subs: Subscription[] = [];
  private scrollToBottom = false;
  typingUsers = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.loadChatList();

    this.subs.push(
      this.chatHub.message$.subscribe(msg => {
        if (msg.chatId === this.activeChat()?.id) {
          this.messages.update(m => [...m, msg]);
          this.scrollToBottom = true;
          this.chatHub.markAsRead(msg.id);
        }
        this.chats.update(list => list.map(c =>
          c.id === msg.chatId ? { ...c, lastMessage: msg.content, lastMessageAt: msg.createdAt, unreadCount: c.id === this.activeChat()?.id ? 0 : c.unreadCount + 1 } : c
        ));
      }),

      this.chatHub.userTyping$.subscribe(t => {
        if (t.chatId !== this.activeChat()?.id || t.userId === this.auth.currentUser()?.id) return;
        this.typingUsers.update(s => {
          const ns = new Set(s);
          t.isTyping ? ns.add(t.userName) : ns.delete(t.userName);
          return ns;
        });
      })
    );

    if (this.chatId) this.openChat(this.chatId);
  }

  loadChatList(): void {
    this.chatSvc.getChatList().subscribe({
      next: res => { if (res.success) this.chats.set(res.data); }
    });
  }

  openChat(chatId: string): void {
    const chat = this.chats().find(c => c.id === chatId);
    if (chat) this.activeChat.set(chat);

    this.loading.set(true);
    this.chatHub.joinChat(chatId);

    this.chatSvc.getMessages(chatId).subscribe({
      next: res => {
        if (res.success) {
          this.messages.set([...res.data.items].reverse());
          this.scrollToBottom = true;
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  sendMessage(): void {
    const chatId = this.activeChat()?.id;
    if (!this.newMessage.trim() || !chatId) return;
    const content = this.newMessage;
    this.newMessage = '';
    this.chatHub.sendTyping(chatId, false);

    this.chatSvc.sendMessage({ chatId, content, messageType: 'Text' }).subscribe({
      next: res => {
        if (res.success) {
          this.messages.update(m => [...m, res.data]);
          this.scrollToBottom = true;
        }
      }
    });
  }

  onTyping(): void {
    const chatId = this.activeChat()?.id;
    if (chatId) this.chatHub.sendTyping(chatId, true);
  }

  ngAfterViewChecked(): void {
    if (this.scrollToBottom) {
      try {
        this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
      } catch {}
      this.scrollToBottom = false;
    }
  }

  timeAgo(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    const chatId = this.activeChat()?.id;
    if (chatId) this.chatHub.leaveChat(chatId);
  }
}
