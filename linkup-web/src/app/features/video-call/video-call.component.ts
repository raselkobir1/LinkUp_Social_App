import {
  Component, Directive, Input, inject, OnInit, OnDestroy, signal, ViewChild, ElementRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { VideoCallHubService } from '../../core/signalr/video-call-hub.service';
import { CallService } from '../../core/services/call.service';
import { FriendService } from '../../core/services/friend.service';
import { IceService } from '../../core/services/ice.service';
import { environment } from '../../../environments/environment';
import { Subscription } from 'rxjs';

/** Binds a MediaStream to a <video> element (Angular has no built-in srcObject binding). */
@Directive({ selector: '[appSrcObject]', standalone: true })
export class SrcObjectDirective {
  constructor(private el: ElementRef<HTMLVideoElement>) {}
  @Input() set appSrcObject(stream: MediaStream | null) {
    this.el.nativeElement.srcObject = stream ?? null;
  }
}

interface PeerConn {
  pc: RTCPeerConnection;
  pendingCandidates: RTCIceCandidateInit[];
  remoteSet: boolean;
}

interface RemoteTile {
  userId: string;
  stream: MediaStream | null;
}

@Component({
  selector: 'app-video-call',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, SrcObjectDirective],
  templateUrl: './video-call.component.html',
  styleUrls: ['./video-call.component.scss']
})
export class VideoCallComponent implements OnInit, OnDestroy {
  @ViewChild('localVideo') localVideoRef!: ElementRef<HTMLVideoElement>;
  @Input() callId!: string;

  private hub = inject(VideoCallHubService);
  private callSvc = inject(CallService);
  private friendSvc = inject(FriendService);
  private ice = inject(IceService);
  private router = inject(Router);

  callStatus = signal<'connecting' | 'active' | 'ended'>('connecting');
  isMuted = signal(false);
  isCameraOff = signal(false);
  isScreenSharing = signal(false);
  isVideoCall = signal(true);
  isGroup = signal(false);
  duration = signal(0);
  remoteTiles = signal<RemoteTile[]>([]);
  showInvite = signal(false);
  friends = signal<{ userId: string; fullName: string; profilePictureUrl?: string }[]>([]);

  private durationTimer?: ReturnType<typeof setInterval>;
  private noAnswerTimer?: ReturnType<typeof setTimeout>;
  private subs: Subscription[] = [];
  private conns = new Map<string, PeerConn>();
  private localStream?: MediaStream;
  private cameraTrack?: MediaStreamTrack;
  // STUN (+ TURN when Metered is configured); resolved once before joining the call.
  private iceServers: RTCIceServer[] = environment.iceServers;

  async ngOnInit(): Promise<void> {
    const ctx = this.callSvc.getContext(this.callId);
    if (!ctx) { this.callStatus.set('ended'); this.updateHook(); return; }
    this.isVideoCall.set(ctx.video);
    this.isGroup.set(ctx.isGroup);
    this.isCameraOff.set(!ctx.video);
    this.updateHook();

    // Resolve ICE servers (fetches TURN credentials when Metered is configured)
    // before any peer connection is created.
    this.iceServers = await this.ice.getIceServers();

    await this.hub.connect();
    this.wireSignaling();
    await this.initLocalMedia();
    // Join only after media + handlers are ready, so we never miss the participant list.
    this.hub.joinCall(this.callId);

    // No-answer fallback: if a 1:1 call never connects (callee offline or never
    // answered), end it instead of ringing forever.
    if (!this.isGroup()) {
      this.noAnswerTimer = setTimeout(() => {
        if (this.callStatus() === 'connecting') this.endLocally();
      }, 45000);
    }
  }

  private wireSignaling(): void {
    this.subs.push(
      // Newcomer: offer every participant already in the room.
      this.hub.existingParticipants$.subscribe(({ userIds }) => {
        userIds.forEach(id => { this.ensurePeer(id); this.makeOffer(id); });
      }),
      // Someone new joined — they will offer us; just show a placeholder tile.
      this.hub.participantJoined$.subscribe(({ userId }) => this.ensurePeer(userId)),
      this.hub.participantLeft$.subscribe(({ userId }) => this.removePeer(userId)),

      this.hub.sdpOffer$.subscribe(({ callId, sdp, fromUserId }) => {
        if (callId === this.callId) this.handleOffer(fromUserId, sdp);
      }),
      this.hub.sdpAnswer$.subscribe(({ callId, sdp, fromUserId }) => {
        if (callId === this.callId) this.handleAnswer(fromUserId, sdp);
      }),
      this.hub.iceCandidate$.subscribe(({ callId, candidate, fromUserId }) => {
        if (callId === this.callId) this.addIce(fromUserId, candidate);
      }),

      this.hub.callDeclined$.subscribe(({ callId }) => {
        // 1:1 invitee declined and nobody else is here → end.
        if (callId === this.callId && !this.isGroup() && this.conns.size === 0) this.endLocally();
      })
    );
  }

  // ── Peer connection management (mesh) ──
  private ensurePeer(userId: string): PeerConn {
    let entry = this.conns.get(userId);
    if (entry) return entry;

    // STUN + optional TURN, resolved in ngOnInit; WebRTC falls back to TURN
    // automatically when STUN can't establish a direct P2P path.
    const pc = new RTCPeerConnection({ iceServers: this.iceServers });
    entry = { pc, pendingCandidates: [], remoteSet: false };
    this.conns.set(userId, entry);
    this.remoteTiles.update(t => t.some(x => x.userId === userId) ? t : [...t, { userId, stream: null }]);

    this.localStream?.getTracks().forEach(track => pc.addTrack(track, this.localStream!));

    pc.ontrack = e => {
      this.remoteTiles.update(t => t.map(x => x.userId === userId ? { ...x, stream: e.streams[0] } : x));
      this.markActive();
    };
    pc.onicecandidate = e => {
      if (e.candidate) this.hub.sendIceCandidate(this.callId, userId, e.candidate.toJSON());
    };
    pc.onconnectionstatechange = () => {
      if (pc.connectionState === 'connected') this.markActive();
      this.updateHook();
    };
    this.updateHook();
    return entry;
  }

  private async makeOffer(userId: string): Promise<void> {
    const { pc } = this.ensurePeer(userId);
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);
    this.hub.sendSdpOffer(this.callId, userId, offer);
  }

  private async handleOffer(userId: string, sdp: RTCSessionDescriptionInit): Promise<void> {
    const entry = this.ensurePeer(userId);
    await entry.pc.setRemoteDescription(new RTCSessionDescription(sdp));
    entry.remoteSet = true;
    await this.flush(userId);
    const answer = await entry.pc.createAnswer();
    await entry.pc.setLocalDescription(answer);
    this.hub.sendSdpAnswer(this.callId, userId, answer);
  }

  private async handleAnswer(userId: string, sdp: RTCSessionDescriptionInit): Promise<void> {
    const entry = this.conns.get(userId);
    if (!entry) return;
    await entry.pc.setRemoteDescription(new RTCSessionDescription(sdp));
    entry.remoteSet = true;
    await this.flush(userId);
  }

  private async addIce(userId: string, candidate: RTCIceCandidateInit): Promise<void> {
    const entry = this.ensurePeer(userId);
    if (entry.remoteSet) await entry.pc.addIceCandidate(new RTCIceCandidate(candidate));
    else entry.pendingCandidates.push(candidate);
  }

  private async flush(userId: string): Promise<void> {
    const entry = this.conns.get(userId);
    if (!entry) return;
    while (entry.pendingCandidates.length) {
      await entry.pc.addIceCandidate(new RTCIceCandidate(entry.pendingCandidates.shift()!));
    }
  }

  private removePeer(userId: string): void {
    const entry = this.conns.get(userId);
    if (entry) { entry.pc.close(); this.conns.delete(userId); }
    this.remoteTiles.update(t => t.filter(x => x.userId !== userId));
    this.updateHook();
    // In a 1:1 call, the other party leaving ends the call.
    if (!this.isGroup() && this.conns.size === 0) this.endLocally();
  }

  private markActive(): void {
    if (this.callStatus() === 'connecting') {
      this.callStatus.set('active');
      clearTimeout(this.noAnswerTimer);
      this.durationTimer = setInterval(() => this.duration.update(d => d + 1), 1000);
    }
    this.updateHook();
  }

  /** getUserMedia that survives a transient stall — retry a few times. */
  private async initLocalMedia(): Promise<void> {
    const constraints = {
      video: this.isVideoCall(),
      // Browser-level noise cancellation / echo removal / auto gain.
      audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true }
    };
    const withTimeout = (p: Promise<MediaStream>, ms: number) =>
      Promise.race([p, new Promise<MediaStream>((_, rej) => setTimeout(() => rej(new Error('gum-timeout')), ms))]);
    for (let i = 0; i < 3; i++) {
      try { this.localStream = await withTimeout(navigator.mediaDevices.getUserMedia(constraints), 5000); break; }
      catch { if (i === 2) { this.callStatus.set('ended'); this.updateHook(); return; } }
    }
    this.cameraTrack = this.localStream!.getVideoTracks()[0];
    if (this.localVideoRef?.nativeElement) this.localVideoRef.nativeElement.srcObject = this.localStream!;
  }

  // ── Controls ──
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
    const senders = [...this.conns.values()].map(c => c.pc.getSenders().find(s => s.track?.kind === 'video')).filter(Boolean) as RTCRtpSender[];
    if (!this.isScreenSharing()) {
      try {
        const display = await (navigator.mediaDevices as any).getDisplayMedia({ video: true });
        const screenTrack: MediaStreamTrack = display.getVideoTracks()[0];
        await Promise.all(senders.map(s => s.replaceTrack(screenTrack)));
        screenTrack.onended = () => this.toggleScreenShare();
        this.isScreenSharing.set(true);
        this.hub.startScreenShare(this.callId);
      } catch { /* cancelled */ }
    } else {
      if (this.cameraTrack) await Promise.all(senders.map(s => s.replaceTrack(this.cameraTrack!)));
      this.isScreenSharing.set(false);
      this.hub.stopScreenShare(this.callId);
    }
  }

  // ── Invite more people (group) ──
  openInvite(): void {
    this.showInvite.set(true);
    if (this.friends().length === 0) {
      this.friendSvc.getFriends().subscribe({
        next: res => {
          if (!res.success) return;
          // Backend FriendDto carries the friend's data under `friendInfo`.
          this.friends.set(res.data.items.map((f: any) => ({
            userId: f.friendInfo?.id ?? f.userId,
            fullName: f.friendInfo?.fullName ?? f.fullName ?? 'Friend',
            profilePictureUrl: f.friendInfo?.profilePictureUrl
          })));
        }
      });
    }
  }

  invite(userId: string): void {
    this.hub.inviteToCall(this.callId, userId, this.isVideoCall() ? 'video' : 'audio');
    this.showInvite.set(false);
  }

  endCall(): void {
    this.hub.leaveCall(this.callId);
    this.endLocally();
  }

  private endLocally(): void {
    this.cleanup();
    this.callStatus.set('ended');
    this.updateHook();
    setTimeout(() => this.router.navigate(['/messages']), 1200);
  }

  private cleanup(): void {
    clearInterval(this.durationTimer);
    clearTimeout(this.noAnswerTimer);
    this.conns.forEach(c => c.pc.close());
    this.conns.clear();
    this.localStream?.getTracks().forEach(t => t.stop());
    this.callSvc.clearContext(this.callId);
  }

  private updateHook(): void {
    (window as any).__videoCall = {
      callId: this.callId,
      isGroup: this.isGroup(),
      video: this.isVideoCall(),
      status: this.callStatus(),
      peers: [...this.conns.entries()].map(([userId, c]) => ({
        userId, pcState: c.pc.connectionState, iceState: c.pc.iceConnectionState
      })),
      connectedCount: [...this.conns.values()].filter(c => c.pc.connectionState === 'connected').length
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
    this.hub.leaveCall(this.callId);
    this.cleanup();
  }
}
