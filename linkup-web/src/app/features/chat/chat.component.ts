import { Component, inject, OnInit, OnDestroy, signal, Input, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subscription } from 'rxjs';
import { ChatService } from '../../core/services/chat.service';
import { ChatHubService } from '../../core/signalr/chat-hub.service';
import { MediaService } from '../../core/services/media.service';
import { CallService } from '../../core/services/call.service';
import { AuthService } from '../../core/services/auth.service';
import { ChatListDto, MessageDto, MessageType } from '../../core/models/chat.model';
import { CreateGroupDialogComponent } from './create-group-dialog.component';
import { GroupInfoDialogComponent } from './group-info-dialog.component';

const EMOJIS = ['😀','😂','😍','😎','😢','😡','👍','🙏','🎉','❤️','🔥','😮','😭','🤔','👏','🙌','💯','😅','😴','🤝'];

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatMenuModule, MatProgressSpinnerModule, MatDialogModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;
  @Input() chatId?: string;

  private chatSvc = inject(ChatService);
  private chatHub = inject(ChatHubService);
  private mediaSvc = inject(MediaService);
  private callSvc = inject(CallService);
  private dialog = inject(MatDialog);
  auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  readonly emojis = EMOJIS;

  chats = signal<ChatListDto[]>([]);
  messages = signal<MessageDto[]>([]);
  activeChat = signal<ChatListDto | null>(null);
  newMessage = '';
  loading = signal(false);
  private subs: Subscription[] = [];
  private scrollToBottom = false;
  typingUsers = signal<Set<string>>(new Set());

  // Compose state
  showEmoji = signal(false);
  replyTo = signal<MessageDto | null>(null);
  uploading = signal(false);
  // Edit state
  editingMessageId = signal<string | null>(null);
  editContent = '';
  // Search state
  showSearch = signal(false);
  searchQuery = '';
  searchResults = signal<MessageDto[]>([]);
  // Voice recording
  recording = signal(false);
  private mediaRecorder: MediaRecorder | null = null;
  private recordedChunks: Blob[] = [];

  ngOnInit(): void {
    this.loadChatList();

    this.subs.push(
      this.chatHub.message$.subscribe(msg => {
        const active = this.activeChat();
        if (msg.chatId === active?.id) {
          // The server broadcasts to the whole chat group, including the sender,
          // who already appended this message from the REST response. appendMessage
          // dedups by id so a message is never shown twice (notably in group chats).
          this.appendMessage(msg);
          if (msg.senderId !== this.auth.currentUser()?.id) this.chatHub.markAsRead(msg.id, msg.chatId);
        }
        this.chats.update(list => list.map(c =>
          c.id === msg.chatId ? { ...c, lastMessage: msg.content, lastMessageAt: msg.createdAt, unreadCount: c.id === active?.id ? 0 : c.unreadCount + 1 } : c
        ));
      }),

      this.chatHub.messageEdited$.subscribe(msg => {
        this.messages.update(m => m.map(x => x.id === msg.id ? msg : x));
      }),

      this.chatHub.messageDeleted$.subscribe(({ messageId }) => {
        this.messages.update(m => m.map(x => x.id === messageId ? { ...x, isDeletedForEveryone: true, content: '' } : x));
      }),

      this.chatHub.messageRead$.subscribe(({ messageId }) => {
        this.messages.update(m => m.map(x => x.id === messageId ? { ...x, status: 'Read' } : x));
      }),

      this.chatHub.messageDelivered$.subscribe(({ messageId }) => {
        this.messages.update(m => m.map(x => x.id === messageId && x.status !== 'Read' ? { ...x, status: 'Delivered' } : x));
      }),

      this.chatHub.userOnline$.subscribe(({ userId, isOnline }) => {
        this.chats.update(list => list.map(c => c.otherUserId === userId ? { ...c, otherUserIsOnline: isOnline } : c));
        const active = this.activeChat();
        if (active && active.otherUserId === userId) this.activeChat.set({ ...active, otherUserIsOnline: isOnline });
      }),

      this.chatHub.userTyping$.subscribe(t => {
        if (t.chatId !== this.activeChat()?.id || t.userId === this.auth.currentUser()?.id) return;
        this.typingUsers.update(s => {
          const ns = new Set(s);
          t.isTyping ? ns.add(t.userId) : ns.delete(t.userId);
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

    this.clearCompose();
    this.showSearch.set(false);
    this.searchResults.set([]);
    this.loading.set(true);
    this.chatHub.joinChat(chatId);

    this.chatSvc.getMessages(chatId).subscribe({
      next: res => {
        if (res.success) {
          this.messages.set([...res.data.items].reverse());
          this.scrollToBottom = true;
          // Mark the latest inbound message as read.
          const lastInbound = [...res.data.items].find(m => m.senderId !== this.auth.currentUser()?.id);
          if (lastInbound) this.chatHub.markAsRead(lastInbound.id, chatId);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    // Reset unread badge for the opened chat.
    this.chats.update(list => list.map(c => c.id === chatId ? { ...c, unreadCount: 0 } : c));
  }

  sendMessage(): void {
    const chatId = this.activeChat()?.id;
    if (!this.newMessage.trim() || !chatId) return;
    const content = this.newMessage;
    const replyToMessageId = this.replyTo()?.id;
    this.newMessage = '';
    this.showEmoji.set(false);
    this.chatHub.sendTyping(chatId, false);

    this.chatSvc.sendMessage({ chatId, content, messageType: 'Text', replyToMessageId }).subscribe({
      next: res => { if (res.success) this.appendMessage(res.data); }
    });
    this.replyTo.set(null);
  }

  /** Append a message, ignoring duplicates (same id may arrive via REST and SignalR). */
  private appendMessage(msg: MessageDto): void {
    this.messages.update(m => m.some(x => x.id === msg.id) ? m : [...m, msg]);
    this.scrollToBottom = true;
  }

  addEmoji(emoji: string): void {
    this.newMessage += emoji;
  }

  setReply(msg: MessageDto): void {
    this.replyTo.set(msg);
  }
  clearReply(): void { this.replyTo.set(null); }

  isMine(msg: MessageDto): boolean {
    return msg.senderId === this.auth.currentUser()?.id;
  }

  startEdit(msg: MessageDto): void {
    this.editingMessageId.set(msg.id);
    this.editContent = msg.content;
  }
  cancelEdit(): void { this.editingMessageId.set(null); }
  saveEdit(msg: MessageDto): void {
    const content = this.editContent.trim();
    if (!content) return;
    this.chatSvc.editMessage(msg.id, content).subscribe({
      next: res => { if (res.success) { this.messages.update(m => m.map(x => x.id === msg.id ? res.data : x)); this.editingMessageId.set(null); } }
    });
  }

  deleteForMe(msg: MessageDto): void {
    this.chatSvc.deleteForMe(msg.id).subscribe({
      next: () => this.messages.update(m => m.filter(x => x.id !== msg.id))
    });
  }
  deleteForEveryone(msg: MessageDto): void {
    this.chatSvc.deleteForEveryone(msg.id).subscribe({
      next: () => this.messages.update(m => m.map(x => x.id === msg.id ? { ...x, isDeletedForEveryone: true, content: '' } : x))
    });
  }

  onAttachmentSelected(event: Event, kind: 'image' | 'video' | 'file'): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    (event.target as HTMLInputElement).value = '';
    const chatId = this.activeChat()?.id;
    if (!chatId) return;
    this.uploading.set(true);
    const upload$ = kind === 'video' ? this.mediaSvc.uploadVideo(file) : this.mediaSvc.uploadImage(file);
    upload$.subscribe({
      next: res => {
        this.uploading.set(false);
        if (!res.success) return;
        const messageType: MessageType = kind === 'image' ? 'Image' : kind === 'video' ? 'Video' : 'File';
        this.chatSvc.sendMessage({
          chatId, content: file.name, messageType, attachmentUrl: res.data.url, attachmentType: messageType,
          replyToMessageId: this.replyTo()?.id
        }).subscribe({
          next: r => { if (r.success) { this.appendMessage(r.data); this.replyTo.set(null); } }
        });
      },
      error: () => this.uploading.set(false)
    });
  }

  async toggleRecording(): Promise<void> {
    if (this.recording()) { this.stopRecording(); return; }
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.recordedChunks = [];
      this.mediaRecorder = new MediaRecorder(stream);
      this.mediaRecorder.ondataavailable = e => { if (e.data.size > 0) this.recordedChunks.push(e.data); };
      this.mediaRecorder.onstop = () => {
        stream.getTracks().forEach(t => t.stop());
        this.uploadVoice(new Blob(this.recordedChunks, { type: 'audio/webm' }));
      };
      this.mediaRecorder.start();
      this.recording.set(true);
    } catch { /* mic permission denied */ }
  }

  private stopRecording(): void {
    this.recording.set(false);
    this.mediaRecorder?.stop();
  }

  private uploadVoice(blob: Blob): void {
    const chatId = this.activeChat()?.id;
    if (!chatId || blob.size === 0) return;
    const file = new File([blob], `voice-message.webm`, { type: 'audio/webm' });
    this.uploading.set(true);
    this.mediaSvc.uploadVideo(file).subscribe({
      next: res => {
        this.uploading.set(false);
        if (!res.success) return;
        this.chatSvc.sendMessage({ chatId, content: 'Voice message', messageType: 'Voice', attachmentUrl: res.data.url, attachmentType: 'Voice' }).subscribe({
          next: r => { if (r.success) this.appendMessage(r.data); }
        });
      },
      error: () => this.uploading.set(false)
    });
  }

  toggleSearch(): void {
    this.showSearch.update(v => !v);
    if (!this.showSearch()) { this.searchQuery = ''; this.searchResults.set([]); }
  }

  runSearch(): void {
    const chatId = this.activeChat()?.id;
    const q = this.searchQuery.trim();
    if (!chatId || q.length < 1) { this.searchResults.set([]); return; }
    this.chatSvc.searchMessages(chatId, q).subscribe({
      next: res => { if (res.success) this.searchResults.set(res.data.items); }
    });
  }

  openCreateGroup(): void {
    const ref = this.dialog.open(CreateGroupDialogComponent, { width: '440px', maxWidth: '95vw' });
    ref.afterClosed().subscribe((created: ChatListDto | undefined) => {
      if (created) { this.loadChatList(); this.openChat(created.id); }
    });
  }

  openGroupInfo(): void {
    const chat = this.activeChat();
    if (!chat?.isGroup) return;
    const ref = this.dialog.open(GroupInfoDialogComponent, { data: { chatId: chat.id }, width: '460px', maxWidth: '95vw' });
    ref.afterClosed().subscribe((result: { left?: boolean; changed?: boolean } | undefined) => {
      if (result?.left) { this.activeChat.set(null); this.loadChatList(); }
      else if (result?.changed) this.loadChatList();
    });
  }

  private clearCompose(): void {
    this.newMessage = '';
    this.replyTo.set(null);
    this.showEmoji.set(false);
    this.editingMessageId.set(null);
  }

  onTyping(): void {
    const chatId = this.activeChat()?.id;
    if (chatId) this.chatHub.sendTyping(chatId, true);
  }

  startCall(video: boolean): void {
    const chat = this.activeChat();
    if (!chat || chat.isGroup || !chat.otherUserId) return;
    this.callSvc.initiateCall(chat.otherUserId, video ? 'video' : 'audio', chat.otherUserName);
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
