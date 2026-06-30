import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { VideoCallHubService } from '../signalr/video-call-hub.service';

export interface IncomingCall {
  callId: string;
  callerId: string;
  mediaType: string;  // 'video' | 'audio'
  isGroup: boolean;
}

export interface CallContext {
  callId: string;
  isGroup: boolean;
  video: boolean;
  invitees: { id: string; name?: string }[];
}

@Injectable({ providedIn: 'root' })
export class CallService {
  private hub = inject(VideoCallHubService);
  private router = inject(Router);

  readonly incomingCall = signal<IncomingCall | null>(null);
  private contexts = new Map<string, CallContext>();

  // Ringtone (generated with the Web Audio API — no asset/CSP dependency).
  private audioCtx: AudioContext | null = null;
  private ringInterval: ReturnType<typeof setInterval> | null = null;
  private ringTimeout: ReturnType<typeof setTimeout> | null = null;

  async init(): Promise<void> {
    await this.hub.connect();
    (window as any).__callHubConnected = true;
    this.hub.callRinging$.subscribe(c => {
      this.incomingCall.set({ callId: c.callId, callerId: c.callerId, mediaType: c.mediaType, isGroup: c.isGroup });
      this.startRingtone();
    });
    this.hub.participantLeft$.subscribe(() => { /* handled in call screen */ });
  }

  /** Loop a phone-style ringtone while a call is incoming. */
  private startRingtone(): void {
    if (this.ringInterval) return;
    try {
      const Ctor = window.AudioContext || (window as any).webkitAudioContext;
      if (!Ctor) return;
      this.audioCtx = this.audioCtx ?? new Ctor();
      if (this.audioCtx!.state === 'suspended') this.audioCtx!.resume();
    } catch { return; }

    const burst = () => {
      const ctx = this.audioCtx;
      if (!ctx) return;
      const tone = (freq: number, offset: number) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = 'sine';
        osc.frequency.value = freq;
        const t = ctx.currentTime + offset;
        gain.gain.setValueAtTime(0.0001, t);
        gain.gain.exponentialRampToValueAtTime(0.3, t + 0.04);
        gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.4);
        osc.connect(gain).connect(ctx.destination);
        osc.start(t);
        osc.stop(t + 0.42);
      };
      tone(480, 0);     // classic two-tone ring
      tone(440, 0.5);
    };
    burst();
    this.ringInterval = setInterval(burst, 3000);
    // Auto-stop after 35s if unanswered (treat as a missed call).
    this.ringTimeout = setTimeout(() => { this.stopRingtone(); this.incomingCall.set(null); }, 35000);
  }

  private stopRingtone(): void {
    if (this.ringInterval) { clearInterval(this.ringInterval); this.ringInterval = null; }
    if (this.ringTimeout) { clearTimeout(this.ringTimeout); this.ringTimeout = null; }
  }

  /** Start a 1:1 call. */
  initiateCall(targetUserId: string, mode: 'video' | 'audio' = 'video', peerName?: string): void {
    const callId = crypto.randomUUID();
    this.contexts.set(callId, { callId, isGroup: false, video: mode === 'video', invitees: [{ id: targetUserId, name: peerName }] });
    this.hub.startCall(callId, [targetUserId], mode, false);
    this.router.navigate(['/video-call', callId]);
  }

  /** Start a group call with multiple invitees. */
  initiateGroupCall(invitees: { id: string; name?: string }[], mode: 'video' | 'audio' = 'video'): void {
    const callId = crypto.randomUUID();
    this.contexts.set(callId, { callId, isGroup: true, video: mode === 'video', invitees });
    this.hub.startCall(callId, invitees.map(i => i.id), mode, true);
    this.router.navigate(['/video-call', callId]);
  }

  acceptIncoming(): void {
    const call = this.incomingCall();
    if (!call) return;
    this.stopRingtone();
    this.contexts.set(call.callId, { callId: call.callId, isGroup: call.isGroup, video: call.mediaType !== 'audio', invitees: [] });
    this.incomingCall.set(null);
    this.router.navigate(['/video-call', call.callId]);
  }

  declineIncoming(): void {
    const call = this.incomingCall();
    if (!call) return;
    this.stopRingtone();
    this.hub.declineCall(call.callId, call.callerId);
    this.incomingCall.set(null);
  }

  getContext(callId: string): CallContext | undefined { return this.contexts.get(callId); }
  clearContext(callId: string): void { this.contexts.delete(callId); }
}
