import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { VideoCallService } from '../../core/services/video-call.service';
import { CallService } from '../../core/services/call.service';
import { AuthService } from '../../core/services/auth.service';
import { CallHistory } from '../../core/models/call.model';

@Component({
  selector: 'app-call-history',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-6">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Calls</h1>

      @if (loading()) {
        <div class="flex justify-center py-16"><mat-spinner diameter="40"></mat-spinner></div>
      }

      <div class="space-y-1">
        @for (call of calls(); track call.id) {
          <div class="flex items-center gap-3 p-3 rounded-xl hover:bg-gray-50" data-testid="call-row">
            <div class="w-11 h-11 rounded-full flex items-center justify-center flex-shrink-0"
              [class]="iconBg(call)">
              <mat-icon class="text-white">{{ call.type === 'Group' ? 'groups' : (call.isIncoming ? 'call_received' : 'call_made') }}</mat-icon>
            </div>
            <div class="flex-1 min-w-0">
              <p class="font-semibold text-gray-800 truncate">{{ otherNames(call) }}</p>
              <p class="text-xs" [class.text-red-500]="call.status === 'Missed' || call.status === 'Declined'"
                [class.text-gray-400]="call.status !== 'Missed' && call.status !== 'Declined'">
                <mat-icon style="font-size:13px;height:13px;width:13px" class="align-middle">
                  {{ call.isIncoming ? 'call_received' : 'call_made' }}
                </mat-icon>
                {{ statusLabel(call) }} · {{ when(call) }}
              </p>
            </div>
            @if (firstOther(call); as peer) {
              <button mat-icon-button color="primary" (click)="callBack(peer.userId, peer.fullName, call.type === 'Group')"
                title="Call again">
                <mat-icon>{{ call.type === 'Group' ? 'groups' : 'videocam' }}</mat-icon>
              </button>
            }
          </div>
        }

        @if (!loading() && calls().length === 0) {
          <div class="text-center py-16 text-gray-400">
            <mat-icon class="text-5xl text-gray-200">call</mat-icon>
            <p class="mt-2">No calls yet</p>
          </div>
        }
      </div>
    </div>
  `
})
export class CallHistoryComponent implements OnInit {
  private callApi = inject(VideoCallService);
  private callSvc = inject(CallService);
  private auth = inject(AuthService);

  calls = signal<CallHistory[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.callApi.getHistory().subscribe({
      next: res => { if (res.success) this.calls.set(res.data.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private others(call: CallHistory) {
    const me = this.auth.currentUser()?.id;
    return call.participants.filter(p => p.userId !== me);
  }
  firstOther(call: CallHistory) { return this.others(call)[0]; }
  otherNames(call: CallHistory): string {
    const names = this.others(call).map(p => p.fullName).filter(Boolean);
    return names.length ? names.join(', ') : 'Unknown';
  }
  statusLabel(call: CallHistory): string {
    if (call.status === 'Missed') return 'Missed';
    if (call.status === 'Declined') return 'Declined';
    if (call.durationSeconds) {
      const m = Math.floor(call.durationSeconds / 60), s = call.durationSeconds % 60;
      return `${m}m ${s}s`;
    }
    return call.status;
  }
  iconBg(call: CallHistory): string {
    if (call.status === 'Missed' || call.status === 'Declined') return 'bg-red-500';
    return 'bg-green-500';
  }
  when(call: CallHistory): string {
    const d = new Date(call.startedAt || call.endedAt || Date.now());
    return d.toLocaleString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
  callBack(userId: string, name: string, group: boolean): void {
    this.callSvc.initiateCall(userId, 'video', name);
  }
}
