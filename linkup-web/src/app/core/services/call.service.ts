import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { CallHistoryItem } from '../models/call.model';
import { VideoCallHubService } from '../signalr/video-call-hub.service';

export interface IncomingCall {
  callId: string;
  callerId: string;
  callType: string;
}

/** Context handed to the VideoCallComponent when it opens for a given call. */
export interface CallContext {
  callId: string;
  peerId: string;
  isCaller: boolean;
}

@Injectable({ providedIn: 'root' })
export class CallService {
  private hub = inject(VideoCallHubService);
  private router = inject(Router);
  private http = inject(HttpClient);

  /** Set while an incoming call is ringing (shown by the shell banner). */
  readonly incomingCall = signal<IncomingCall | null>(null);

  private contexts = new Map<string, CallContext>();

  /** Connect the signaling hub app-wide and start listening for incoming calls. */
  async init(): Promise<void> {
    await this.hub.connect();
    (window as any).__callHubConnected = true;
    this.hub.callInitiated$.subscribe(c =>
      this.incomingCall.set({ callId: c.callId, callerId: c.callerId, callType: c.callType }));
    // If the caller hangs up before we answer, clear the banner.
    this.hub.callEnded$.subscribe(({ callId }) => {
      if (this.incomingCall()?.callId === callId) this.incomingCall.set(null);
    });
  }

  /** Start an outgoing call to a user and open the call screen as the caller. */
  initiateCall(targetUserId: string, callType: 'OneToOne' | 'Group' = 'OneToOne'): void {
    const callId = crypto.randomUUID();
    this.contexts.set(callId, { callId, peerId: targetUserId, isCaller: true });
    this.hub.initiateCall(targetUserId, callId, callType);
    this.router.navigate(['/video-call', callId]);
  }

  /** Accept the ringing call and open the call screen as the callee. */
  acceptIncoming(): void {
    const call = this.incomingCall();
    if (!call) return;
    this.contexts.set(call.callId, { callId: call.callId, peerId: call.callerId, isCaller: false });
    this.hub.acceptCall(call.callerId, call.callId);
    this.incomingCall.set(null);
    this.router.navigate(['/video-call', call.callId]);
  }

  declineIncoming(): void {
    const call = this.incomingCall();
    if (!call) return;
    this.hub.declineCall(call.callerId, call.callId);
    this.incomingCall.set(null);
  }

  /** Retrieve (and consume) the context for a call the component is about to open. */
  getContext(callId: string): CallContext | undefined {
    return this.contexts.get(callId);
  }

  clearContext(callId: string): void {
    this.contexts.delete(callId);
  }

  getCallHistory(page = 1, pageSize = 30): Observable<ApiResponse<PagedResult<CallHistoryItem>>> {
    return this.http.get<ApiResponse<PagedResult<CallHistoryItem>>>(`${environment.apiUrl}/video-calls/history`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }
}
