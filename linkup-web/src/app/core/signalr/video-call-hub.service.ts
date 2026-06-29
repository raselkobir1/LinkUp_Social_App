import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

export interface CallInfo {
  callId: string;
  callerId: string;
  callerName: string;
  callerProfilePicture?: string;
  callType: 'OneToOne' | 'Group';
}

@Injectable({ providedIn: 'root' })
export class VideoCallHubService implements OnDestroy {
  private hub!: HubConnection;

  readonly callInitiated$ = new Subject<CallInfo>();
  readonly callAccepted$ = new Subject<{ callId: string; userId: string }>();
  readonly callDeclined$ = new Subject<{ callId: string; userId: string }>();
  readonly callEnded$ = new Subject<{ callId: string }>();
  readonly iceCandidate$ = new Subject<{ callId: string; candidate: RTCIceCandidateInit; userId: string }>();
  readonly sdpOffer$ = new Subject<{ callId: string; sdp: RTCSessionDescriptionInit; userId: string }>();
  readonly sdpAnswer$ = new Subject<{ callId: string; sdp: RTCSessionDescriptionInit; userId: string }>();
  readonly cameraToggled$ = new Subject<{ userId: string; isOn: boolean }>();
  readonly micToggled$ = new Subject<{ userId: string; isOn: boolean }>();
  readonly screenShareStarted$ = new Subject<{ userId: string }>();
  readonly screenShareStopped$ = new Subject<{ userId: string }>();
  readonly connected$ = new BehaviorSubject(false);

  constructor(private auth: AuthService) {}

  async connect(): Promise<void> {
    const token = this.auth.getAccessToken();
    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/videocall`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('CallInitiated', (info: CallInfo) => this.callInitiated$.next(info));
    this.hub.on('CallAccepted', (d: { callId: string; userId: string }) => this.callAccepted$.next(d));
    this.hub.on('CallDeclined', (d: { callId: string; userId: string }) => this.callDeclined$.next(d));
    this.hub.on('CallEnded', (d: { callId: string }) => this.callEnded$.next(d));
    this.hub.on('IceCandidateReceived', (d: { callId: string; candidate: RTCIceCandidateInit; userId: string }) => this.iceCandidate$.next(d));
    this.hub.on('SdpOfferReceived', (d: { callId: string; sdp: RTCSessionDescriptionInit; userId: string }) => this.sdpOffer$.next(d));
    this.hub.on('SdpAnswerReceived', (d: { callId: string; sdp: RTCSessionDescriptionInit; userId: string }) => this.sdpAnswer$.next(d));
    this.hub.on('CameraToggled', (d: { userId: string; isOn: boolean }) => this.cameraToggled$.next(d));
    this.hub.on('MicToggled', (d: { userId: string; isOn: boolean }) => this.micToggled$.next(d));
    this.hub.on('ScreenShareStarted', (d: { userId: string }) => this.screenShareStarted$.next(d));
    this.hub.on('ScreenShareStopped', (d: { userId: string }) => this.screenShareStopped$.next(d));

    this.hub.onreconnected(() => this.connected$.next(true));
    this.hub.onclose(() => this.connected$.next(false));

    await this.hub.start();
    this.connected$.next(true);
  }

  initiateCall(receiverId: string, callType: string): void { this.hub?.invoke('InitiateCall', receiverId, callType); }
  acceptCall(callId: string): void { this.hub?.invoke('AcceptCall', callId); }
  declineCall(callId: string): void { this.hub?.invoke('DeclineCall', callId); }
  endCall(callId: string): void { this.hub?.invoke('EndCall', callId); }
  sendIceCandidate(callId: string, targetUserId: string, candidate: RTCIceCandidateInit): void { this.hub?.invoke('SendIceCandidate', callId, targetUserId, candidate); }
  sendSdpOffer(callId: string, targetUserId: string, sdp: RTCSessionDescriptionInit): void { this.hub?.invoke('SendSdpOffer', callId, targetUserId, sdp); }
  sendSdpAnswer(callId: string, targetUserId: string, sdp: RTCSessionDescriptionInit): void { this.hub?.invoke('SendSdpAnswer', callId, targetUserId, sdp); }
  toggleCamera(callId: string, isOn: boolean): void { this.hub?.invoke('ToggleCamera', callId, isOn); }
  toggleMic(callId: string, isOn: boolean): void { this.hub?.invoke('ToggleMic', callId, isOn); }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.connected$.next(false);
  }

  ngOnDestroy(): void { this.disconnect(); }
}
