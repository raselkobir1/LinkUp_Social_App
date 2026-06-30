import { Component, inject, OnInit, OnDestroy, signal, ViewChild, ElementRef, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { VideoCallHubService } from '../../core/signalr/video-call-hub.service';
import { CallService } from '../../core/services/call.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-video-call',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './video-call.component.html',
  styleUrls: ['./video-call.component.scss']
})
export class VideoCallComponent implements OnInit, OnDestroy {
  @ViewChild('localVideo') localVideoRef!: ElementRef<HTMLVideoElement>;
  @ViewChild('remoteVideo') remoteVideoRef!: ElementRef<HTMLVideoElement>;
  @Input() callId!: string;

  private hub = inject(VideoCallHubService);
  private callSvc = inject(CallService);
  private router = inject(Router);

  callStatus = signal<'connecting' | 'active' | 'ended'>('connecting');
  isMuted = signal(false);
  isCameraOff = signal(false);
  isScreenSharing = signal(false);
  isVideoCall = signal(true);
  peerName = signal('');
  duration = signal(0);

  private durationTimer?: ReturnType<typeof setInterval>;
  private subs: Subscription[] = [];
  private pc?: RTCPeerConnection;
  private localStream?: MediaStream;
  private cameraTrack?: MediaStreamTrack;
  private peerId = '';
  private isCaller = false;
  private pendingCandidates: RTCIceCandidateInit[] = [];
  private remoteSet = false;
  private peerAccepted = false;          // caller: peer pressed Accept
  private offerMade = false;
  private pendingOffer?: RTCSessionDescriptionInit;  // callee: offer that arrived before pc was ready

  async ngOnInit(): Promise<void> {
    const ctx = this.callSvc.getContext(this.callId);
    if (!ctx) {
      // No context (e.g. opened directly) — nothing to negotiate.
      this.callStatus.set('ended');
      this.updateHook();
      return;
    }
    this.peerId = ctx.peerId;
    this.isCaller = ctx.isCaller;
    this.isVideoCall.set(ctx.video);
    this.isCameraOff.set(!ctx.video);
    this.peerName.set(ctx.peerName ?? '');
    this.updateHook();

    await this.hub.connect();
    this.wireSignaling();
    await this.initWebRTC();
  }

  private wireSignaling(): void {
    this.subs.push(
      // Caller: peer accepted → create and send the offer (once pc is ready).
      this.hub.callAccepted$.subscribe(({ callId }) => {
        if (callId === this.callId && this.isCaller) { this.peerAccepted = true; this.tryMakeOffer(); }
      }),

      // Callee: received the offer → answer it (buffer if pc not ready yet).
      this.hub.sdpOffer$.subscribe(({ sdp, fromUserId }) => {
        this.peerId = fromUserId || this.peerId;
        if (this.pc) this.handleOffer(sdp); else this.pendingOffer = sdp;
      }),

      // Caller: received the answer.
      this.hub.sdpAnswer$.subscribe(async ({ sdp }) => {
        if (!this.pc) return;
        await this.pc.setRemoteDescription(new RTCSessionDescription(sdp));
        this.remoteSet = true;
        await this.flushCandidates();
      }),

      this.hub.iceCandidate$.subscribe(async ({ candidate }) => {
        if (!this.pc) return;
        if (this.remoteSet) await this.pc.addIceCandidate(new RTCIceCandidate(candidate));
        else this.pendingCandidates.push(candidate);
      }),

      this.hub.callEnded$.subscribe(({ callId }) => { if (callId === this.callId) this.remoteHangup(); }),
      this.hub.callDeclined$.subscribe(({ callId }) => { if (callId === this.callId) this.remoteHangup(); }),

      this.hub.cameraToggled$.subscribe(() => {}),
      this.hub.micToggled$.subscribe(() => {})
    );
  }

  /** getUserMedia that survives a transient stall — race each attempt against a timeout, retry a few times. */
  private async getMedia(): Promise<MediaStream> {
    const constraints = { video: this.isVideoCall(), audio: true };
    const withTimeout = (p: Promise<MediaStream>, ms: number) =>
      Promise.race([p, new Promise<MediaStream>((_, rej) => setTimeout(() => rej(new Error('gum-timeout')), ms))]);
    let lastErr: unknown;
    for (let i = 0; i < 3; i++) {
      try { return await withTimeout(navigator.mediaDevices.getUserMedia(constraints), 5000); }
      catch (e) { lastErr = e; }
    }
    throw lastErr;
  }

  private async initWebRTC(): Promise<void> {
    try {
      this.localStream = await this.getMedia();
      this.cameraTrack = this.localStream.getVideoTracks()[0];
      if (this.localVideoRef?.nativeElement) {
        this.localVideoRef.nativeElement.srcObject = this.localStream;
      }

      this.pc = new RTCPeerConnection({
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
      });

      this.localStream.getTracks().forEach(track => this.pc!.addTrack(track, this.localStream!));

      this.pc.ontrack = event => {
        if (this.remoteVideoRef?.nativeElement) {
          this.remoteVideoRef.nativeElement.srcObject = event.streams[0];
        }
        this.updateHook();
      };

      this.pc.onicecandidate = event => {
        if (event.candidate && this.peerId) {
          this.hub.sendIceCandidate(this.peerId, event.candidate.toJSON());
        }
      };

      this.pc.onconnectionstatechange = () => {
        const s = this.pc?.connectionState;
        if (s === 'connected') {
          if (this.callStatus() !== 'active') {
            this.callStatus.set('active');
            this.startDurationTimer();
          }
        } else if (s === 'failed' || s === 'closed') {
          this.callStatus.set('ended');
        }
        this.updateHook();
      };

      this.updateHook();
      // pc is now ready — resolve any signal that raced ahead of it.
      if (this.isCaller) this.tryMakeOffer();
      else if (this.pendingOffer) { const o = this.pendingOffer; this.pendingOffer = undefined; this.handleOffer(o); }
    } catch {
      // Media access denied / unavailable.
      this.callStatus.set('ended');
      this.updateHook();
    }
  }

  /** Caller: send the offer once BOTH the peer has accepted and pc exists. */
  private async tryMakeOffer(): Promise<void> {
    if (!this.isCaller || !this.peerAccepted || !this.pc || this.offerMade) return;
    this.offerMade = true;
    const offer = await this.pc.createOffer();
    await this.pc.setLocalDescription(offer);
    this.hub.sendSdpOffer(this.peerId, offer);
  }

  /** Callee: apply the caller's offer and answer it. */
  private async handleOffer(sdp: RTCSessionDescriptionInit): Promise<void> {
    if (!this.pc) return;
    await this.pc.setRemoteDescription(new RTCSessionDescription(sdp));
    this.remoteSet = true;
    await this.flushCandidates();
    const answer = await this.pc.createAnswer();
    await this.pc.setLocalDescription(answer);
    this.hub.sendSdpAnswer(this.peerId, answer);
  }

  private async flushCandidates(): Promise<void> {
    while (this.pendingCandidates.length && this.pc) {
      await this.pc.addIceCandidate(new RTCIceCandidate(this.pendingCandidates.shift()!));
    }
  }

  toggleMute(): void {
    const muted = !this.isMuted();
    this.localStream?.getAudioTracks().forEach(t => t.enabled = !muted);
    this.isMuted.set(muted);
    this.hub.toggleMic(this.callId, !muted);
  }

  toggleCamera(): void {
    const off = !this.isCameraOff();
    this.localStream?.getVideoTracks().forEach(t => t.enabled = !off);
    this.isCameraOff.set(off);
    this.hub.toggleCamera(this.callId, !off);
  }

  async toggleScreenShare(): Promise<void> {
    if (!this.pc) return;
    const sender = this.pc.getSenders().find(s => s.track?.kind === 'video');
    if (!sender) return;

    if (!this.isScreenSharing()) {
      try {
        const display = await (navigator.mediaDevices as any).getDisplayMedia({ video: true });
        const screenTrack: MediaStreamTrack = display.getVideoTracks()[0];
        await sender.replaceTrack(screenTrack);
        screenTrack.onended = () => this.toggleScreenShare();
        this.isScreenSharing.set(true);
        this.hub.startScreenShare(this.callId);
      } catch { /* user cancelled */ }
    } else {
      if (this.cameraTrack) await sender.replaceTrack(this.cameraTrack);
      this.isScreenSharing.set(false);
      this.hub.stopScreenShare(this.callId);
    }
  }

  endCall(): void {
    this.hub.endCall(this.callId);
    this.cleanup();
    this.callStatus.set('ended');
    this.updateHook();
    setTimeout(() => this.router.navigate(['/messages']), 1500);
  }

  private remoteHangup(): void {
    this.cleanup();
    this.callStatus.set('ended');
    this.updateHook();
    setTimeout(() => this.router.navigate(['/messages']), 1500);
  }

  private cleanup(): void {
    clearInterval(this.durationTimer);
    this.localStream?.getTracks().forEach(t => t.stop());
    this.pc?.close();
    this.callSvc.clearContext(this.callId);
  }

  private startDurationTimer(): void {
    this.durationTimer = setInterval(() => this.duration.update(d => d + 1), 1000);
  }

  /** Test/diagnostic hook so e2e tests can observe call state from the page. */
  private updateHook(): void {
    (window as any).__videoCall = {
      callId: this.callId,
      role: this.isCaller ? 'caller' : 'callee',
      video: this.isVideoCall(),
      status: this.callStatus(),
      pcState: this.pc?.connectionState ?? 'none',
      iceState: this.pc?.iceConnectionState ?? 'none',
      hasRemote: !!(this.remoteVideoRef?.nativeElement?.srcObject)
    };
  }

  get formattedDuration(): string {
    const d = this.duration();
    const m = Math.floor(d / 60).toString().padStart(2, '0');
    const s = (d % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
    this.cleanup();
  }
}
