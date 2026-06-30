import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CallService } from '../../core/services/call.service';
import { AuthService } from '../../core/services/auth.service';
import { CallHistoryItem } from '../../core/models/call.model';

@Component({
  selector: 'app-call-history',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-6">
      <h1 class="text-2xl font-bold text-gray-800 mb-4">Call history</h1>

      @if (loading()) {
        <div class="flex justify-center py-10"><mat-spinner diameter="40"></mat-spinner></div>
      } @else if (calls().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <mat-icon class="text-5xl text-gray-200">videocam_off</mat-icon>
          <p class="mt-2">No calls yet</p>
        </div>
      } @else {
        <div class="bg-white rounded-xl shadow-sm divide-y divide-gray-100">
          @for (call of calls(); track call.id) {
            <div class="flex items-center gap-3 p-4">
              <div class="w-10 h-10 rounded-full flex items-center justify-center"
                [class.bg-red-50]="call.status === 'Missed' || call.status === 'Declined'"
                [class.bg-green-50]="call.status !== 'Missed' && call.status !== 'Declined'">
                <mat-icon [class.text-red-500]="call.status === 'Missed' || call.status === 'Declined'"
                  [class.text-green-600]="call.status !== 'Missed' && call.status !== 'Declined'">
                  {{ call.isIncoming ? 'call_received' : 'call_made' }}
                </mat-icon>
              </div>
              <div class="flex-1 min-w-0">
                <p class="font-medium text-gray-800">{{ peerName(call) }}</p>
                <p class="text-xs text-gray-400 flex items-center gap-1">
                  <span>{{ call.type === 'Group' ? 'Group call' : (call.isIncoming ? 'Incoming' : 'Outgoing') }}</span>
                  <span>·</span>
                  <span>{{ call.status }}</span>
                  @if (call.durationSeconds) { <span>· {{ duration(call.durationSeconds) }}</span> }
                </p>
              </div>
              <div class="text-right">
                <p class="text-xs text-gray-400">{{ call.startedAt ? (call.startedAt | date:'MMM d, h:mm a') : '' }}</p>
                @if (peerId(call); as pid) {
                  <button mat-icon-button class="text-[#1877f2]" (click)="callBack(pid)" title="Call back">
                    <mat-icon>videocam</mat-icon>
                  </button>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class CallHistoryComponent implements OnInit {
  private callSvc = inject(CallService);
  private auth = inject(AuthService);

  calls = signal<CallHistoryItem[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.callSvc.getCallHistory().subscribe({
      next: res => { if (res.success) this.calls.set(res.data.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private others(call: CallHistoryItem) {
    const me = this.auth.currentUser()?.id;
    return call.participants.filter(p => p.userId !== me);
  }

  peerName(call: CallHistoryItem): string {
    const others = this.others(call);
    if (others.length === 0) return 'Unknown';
    if (call.type === 'Group') return others.map(p => p.fullName).join(', ');
    return others[0].fullName;
  }

  peerId(call: CallHistoryItem): string | null {
    if (call.type === 'Group') return null;
    const others = this.others(call);
    return others[0]?.userId ?? null;
  }

  duration(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  callBack(userId: string): void {
    this.callSvc.initiateCall(userId, 'OneToOne');
  }
}
