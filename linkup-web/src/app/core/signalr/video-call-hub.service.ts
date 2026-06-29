import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class VideoCallHubService implements OnDestroy {
  private hub?: HubConnection;
  private starting?: Promise<void>;

  // Incoming call from someone else: (callId, callerId, callType)
  readonly callInitiated$ = new Subject<{ callId: string; callerId: string; callType: string }>();
  readonly callAccepted$ = new Subject<{ callId: string }>();
  readonly callDeclined$ = new Subject<{ callId: string }>();
  readonly callEnded$ = new Subject<{ callId: string }>();
  // WebRTC signaling carries the *sender's* userId so the receiver knows the peer.
  readonly iceCandidate$ = new Subject<{ candidate: RTCIceCandidateInit; fromUserId: string }>();
  readonly sdpOffer$ = new Subject<{ sdp: RTCSessionDescriptionInit; fromUserId: string }>();
  readonly sdpAnswer$ = new Subject<{ sdp: RTCSessionDescriptionInit; fromUserId: string }>();
  readonly cameraToggled$ = new Subject<{ userId: string; isOn: boolean }>();
  readonly micToggled$ = new Subject<{ userId: string; isOn: boolean }>();
  readonly screenShareStarted$ = new Subject<{ userId: string }>();
  readonly screenShareStopped$ = new Subject<{ userId: string }>();
  readonly connected$ = new BehaviorSubject(false);

  constructor(private auth: AuthService) {}

  /** Idempotent connect — safe to call from the shell and from the call screen. */
  async connect(): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) return;
    if (this.starting) return this.starting;

    const token = this.auth.getAccessToken();
    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/videocall`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // Backend sends POSITIONAL args (not a single object).
    this.hub.on('CallInitiated', (callId: string, callerId: string, callType: string) =>
      this.callInitiated$.next({ callId, callerId, callType }));
    this.hub.on('CallAccepted', (callId: string) => this.callAccepted$.next({ callId }));
    this.hub.on('CallDeclined', (callId: string) => this.callDeclined$.next({ callId }));
    this.hub.on('CallEnded', (callId: string) => this.callEnded$.next({ callId }));
    this.hub.on('IceCandidateReceived', (candidate: string, fromUserId: string) =>
      this.iceCandidate$.next({ candidate: JSON.parse(candidate), fromUserId }));
    this.hub.on('SdpOfferReceived', (sdp: string, fromUserId: string) =>
      this.sdpOffer$.next({ sdp: JSON.parse(sdp), fromUserId }));
    this.hub.on('SdpAnswerReceived', (sdp: string, fromUserId: string) =>
      this.sdpAnswer$.next({ sdp: JSON.parse(sdp), fromUserId }));
    this.hub.on('CameraToggled', (userId: string, isOn: boolean) => this.cameraToggled$.next({ userId, isOn }));
    this.hub.on('MicToggled', (userId: string, isOn: boolean) => this.micToggled$.next({ userId, isOn }));
    this.hub.on('ScreenShareStarted', (userId: string) => this.screenShareStarted$.next({ userId }));
    this.hub.on('ScreenShareStopped', (userId: string) => this.screenShareStopped$.next({ userId }));

    this.hub.onreconnected(() => this.connected$.next(true));
    this.hub.onclose(() => this.connected$.next(false));

    this.starting = this.hub.start();
    await this.starting;
    this.starting = undefined;
    this.connected$.next(true);
  }

  // ── Outbound invokes — argument order matches VideoCallHub server methods ──
  initiateCall(targetUserId: string, callId: string, callType: string): void {
    this.hub?.invoke('InitiateCall', targetUserId, callId, callType);
  }
  acceptCall(callerId: string, callId: string): void { this.hub?.invoke('AcceptCall', callerId, callId); }
  declineCall(callerId: string, callId: string): void { this.hub?.invoke('DeclineCall', callerId, callId); }
  endCall(callId: string): void { this.hub?.invoke('EndCall', callId); }

  sendIceCandidate(targetUserId: string, candidate: RTCIceCandidateInit): void {
    this.hub?.invoke('SendIceCandidate', targetUserId, JSON.stringify(candidate));
  }
  sendSdpOffer(targetUserId: string, sdp: RTCSessionDescriptionInit): void {
    this.hub?.invoke('SendSdpOffer', targetUserId, JSON.stringify(sdp));
  }
  sendSdpAnswer(targetUserId: string, sdp: RTCSessionDescriptionInit): void {
    this.hub?.invoke('SendSdpAnswer', targetUserId, JSON.stringify(sdp));
  }
  toggleCamera(callId: string, isOn: boolean): void { this.hub?.invoke('ToggleCamera', callId, isOn); }
  toggleMic(callId: string, isOn: boolean): void { this.hub?.invoke('ToggleMic', callId, isOn); }
  startScreenShare(callId: string): void { this.hub?.invoke('StartScreenShare', callId); }
  stopScreenShare(callId: string): void { this.hub?.invoke('StopScreenShare', callId); }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.connected$.next(false);
  }

  ngOnDestroy(): void { this.disconnect(); }
}
