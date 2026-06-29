import { Component, inject, OnInit, OnDestroy, signal, ViewChild, ElementRef, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { VideoCallHubService } from '../../core/signalr/video-call-hub.service';
import { AuthService } from '../../core/services/auth.service';
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
  private auth = inject(AuthService);
  private router = inject(Router);

  callStatus = signal<'connecting' | 'active' | 'ended'>('connecting');
  isMuted = signal(false);
  isCameraOff = signal(false);
  isScreenSharing = signal(false);
  duration = signal(0);
  private durationTimer?: ReturnType<typeof setInterval>;
  private subs: Subscription[] = [];
  private pc?: RTCPeerConnection;
  private localStream?: MediaStream;

  ngOnInit(): void {
    this.initWebRTC();

    this.subs.push(
      this.hub.sdpOffer$.subscribe(async ({ sdp }) => {
        await this.pc?.setRemoteDescription(sdp);
        const answer = await this.pc?.createAnswer();
        await this.pc?.setLocalDescription(answer!);
        this.hub.sendSdpAnswer(this.callId, '', answer!);
      }),

      this.hub.sdpAnswer$.subscribe(async ({ sdp }) => {
        await this.pc?.setRemoteDescription(sdp);
      }),

      this.hub.iceCandidate$.subscribe(async ({ candidate }) => {
        await this.pc?.addIceCandidate(new RTCIceCandidate(candidate));
      }),

      this.hub.callAccepted$.subscribe(() => {
        this.callStatus.set('active');
        this.startDurationTimer();
      }),

      this.hub.callEnded$.subscribe(() => this.endCall()),
      this.hub.callDeclined$.subscribe(() => this.endCall())
    );
  }

  private async initWebRTC(): Promise<void> {
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
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
      };

      this.pc.onicecandidate = event => {
        if (event.candidate) {
          this.hub.sendIceCandidate(this.callId, '', event.candidate.toJSON());
        }
      };

      const offer = await this.pc.createOffer();
      await this.pc.setLocalDescription(offer);
      this.hub.sendSdpOffer(this.callId, '', offer);
    } catch {}
  }

  toggleMute(): void {
    const muted = !this.isMuted();
    this.localStream?.getAudioTracks().forEach(t => t.enabled = muted);
    this.isMuted.set(muted);
    this.hub.toggleMic(this.callId, !muted);
  }

  toggleCamera(): void {
    const off = !this.isCameraOff();
    this.localStream?.getVideoTracks().forEach(t => t.enabled = off);
    this.isCameraOff.set(off);
    this.hub.toggleCamera(this.callId, !off);
  }

  endCall(): void {
    this.hub.endCall(this.callId);
    this.cleanup();
    this.callStatus.set('ended');
    setTimeout(() => this.router.navigate(['/messages']), 2000);
  }

  private cleanup(): void {
    clearInterval(this.durationTimer);
    this.localStream?.getTracks().forEach(t => t.stop());
    this.pc?.close();
  }

  private startDurationTimer(): void {
    this.durationTimer = setInterval(() => this.duration.update(d => d + 1), 1000);
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
