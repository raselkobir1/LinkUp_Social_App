import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class VideoCallHubService implements OnDestroy {
  private hub?: HubConnection;
  private starting?: Promise<void>;

  // Incoming ring
  readonly callRinging$ = new Subject<{ callId: string; callerId: string; mediaType: string; isGroup: boolean }>();
  readonly callDeclined$ = new Subject<{ callId: string; fromUserId: string }>();
  // Mesh membership
  readonly existingParticipants$ = new Subject<{ userIds: string[] }>();
  readonly participantJoined$ = new Subject<{ userId: string }>();
  readonly participantLeft$ = new Subject<{ userId: string }>();
  // WebRTC signaling (tagged with the sender so the client routes to the right peer)
  readonly sdpOffer$ = new Subject<{ callId: string; sdp: RTCSessionDescriptionInit; fromUserId: string }>();
  readonly sdpAnswer$ = new Subject<{ callId: string; sdp: RTCSessionDescriptionInit; fromUserId: string }>();
  readonly iceCandidate$ = new Subject<{ callId: string; candidate: RTCIceCandidateInit; fromUserId: string }>();
  // Media state
  readonly cameraToggled$ = new Subject<{ userId: string; isOn: boolean }>();
  readonly micToggled$ = new Subject<{ userId: string; isOn: boolean }>();
  readonly screenShareStarted$ = new Subject<{ userId: string }>();
  readonly screenShareStopped$ = new Subject<{ userId: string }>();
  readonly connected$ = new BehaviorSubject(false);

  constructor(private auth: AuthService) {}

  /** Idempotent connect — safe to call from the shell and the call screen. */
  async connect(): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) return;
    if (this.starting) return this.starting;

    const token = this.auth.getAccessToken();
    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/videocall`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('CallRinging', (callId, callerId, mediaType, isGroup) =>
      this.callRinging$.next({ callId, callerId, mediaType, isGroup }));
    this.hub.on('CallDeclined', (callId, fromUserId) => this.callDeclined$.next({ callId, fromUserId }));
    this.hub.on('ExistingParticipants', (userIds: string[]) => this.existingParticipants$.next({ userIds }));
    this.hub.on('ParticipantJoined', (userId: string) => this.participantJoined$.next({ userId }));
    this.hub.on('ParticipantLeft', (userId: string) => this.participantLeft$.next({ userId }));
    this.hub.on('SdpOfferReceived', (callId, sdp, fromUserId) =>
      this.sdpOffer$.next({ callId, sdp: JSON.parse(sdp), fromUserId }));
    this.hub.on('SdpAnswerReceived', (callId, sdp, fromUserId) =>
      this.sdpAnswer$.next({ callId, sdp: JSON.parse(sdp), fromUserId }));
    this.hub.on('IceCandidateReceived', (callId, candidate, fromUserId) =>
      this.iceCandidate$.next({ callId, candidate: JSON.parse(candidate), fromUserId }));
    this.hub.on('CameraToggled', (userId, isOn) => this.cameraToggled$.next({ userId, isOn }));
    this.hub.on('MicToggled', (userId, isOn) => this.micToggled$.next({ userId, isOn }));
    this.hub.on('ScreenShareStarted', (userId) => this.screenShareStarted$.next({ userId }));
    this.hub.on('ScreenShareStopped', (userId) => this.screenShareStopped$.next({ userId }));

    this.hub.onreconnected(() => this.connected$.next(true));
    this.hub.onclose(() => this.connected$.next(false));

    this.starting = this.hub.start();
    await this.starting;
    this.starting = undefined;
    this.connected$.next(true);
  }

  // ── Outbound invokes (argument order matches VideoCallHub) ──
  startCall(callId: string, inviteeIds: string[], mediaType: string, isGroup: boolean): void {
    this.hub?.invoke('StartCall', callId, inviteeIds, mediaType, isGroup);
  }
  inviteToCall(callId: string, inviteeId: string, mediaType: string): void {
    this.hub?.invoke('InviteToCall', callId, inviteeId, mediaType);
  }
  joinCall(callId: string): void { this.hub?.invoke('JoinCall', callId); }
  leaveCall(callId: string): void { this.hub?.invoke('LeaveCall', callId); }
  declineCall(callId: string, callerId: string): void { this.hub?.invoke('DeclineCall', callId, callerId); }

  sendSdpOffer(callId: string, targetUserId: string, sdp: RTCSessionDescriptionInit): void {
    this.hub?.invoke('SendSdpOffer', callId, targetUserId, JSON.stringify(sdp));
  }
  sendSdpAnswer(callId: string, targetUserId: string, sdp: RTCSessionDescriptionInit): void {
    this.hub?.invoke('SendSdpAnswer', callId, targetUserId, JSON.stringify(sdp));
  }
  sendIceCandidate(callId: string, targetUserId: string, candidate: RTCIceCandidateInit): void {
    this.hub?.invoke('SendIceCandidate', callId, targetUserId, JSON.stringify(candidate));
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
