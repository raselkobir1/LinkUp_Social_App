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

  async init(): Promise<void> {
    await this.hub.connect();
    (window as any).__callHubConnected = true;
    this.hub.callRinging$.subscribe(c =>
      this.incomingCall.set({ callId: c.callId, callerId: c.callerId, mediaType: c.mediaType, isGroup: c.isGroup }));
    this.hub.participantLeft$.subscribe(() => { /* handled in call screen */ });
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
    this.contexts.set(call.callId, { callId: call.callId, isGroup: call.isGroup, video: call.mediaType !== 'audio', invitees: [] });
    this.incomingCall.set(null);
    this.router.navigate(['/video-call', call.callId]);
  }

  declineIncoming(): void {
    const call = this.incomingCall();
    if (!call) return;
    this.hub.declineCall(call.callId, call.callerId);
    this.incomingCall.set(null);
  }

  getContext(callId: string): CallContext | undefined { return this.contexts.get(callId); }
  clearContext(callId: string): void { this.contexts.delete(callId); }
}
