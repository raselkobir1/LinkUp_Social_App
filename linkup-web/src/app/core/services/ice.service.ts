import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

/**
 * Supplies the ICE servers (STUN + TURN) for WebRTC peer connections.
 *
 * The list is fetched from the backend (GET /video-calls/ice-servers), which returns
 * STUN plus Metered's TURN servers with fresh credentials. TURN relays media for peers
 * that can't connect directly (corporate NAT / CGNAT / strict firewalls); WebRTC uses
 * it only as a last resort. The TURN API key lives on the server, never in this bundle.
 * If the request fails, we fall back to STUN-only so a call is never blocked.
 */
@Injectable({ providedIn: 'root' })
export class IceService {
  private http = inject(HttpClient);

  private cached?: RTCIceServer[];
  private cachedAt = 0;
  // TURN credentials are time-limited; refetch after this window.
  private readonly TTL_MS = 60 * 60 * 1000; // 1 hour

  async getIceServers(): Promise<RTCIceServer[]> {
    if (this.cached && performance.now() - this.cachedAt < this.TTL_MS) {
      return this.cached;
    }

    try {
      const res = await firstValueFrom(
        this.http.get<ApiResponse<RTCIceServer[]>>(`${environment.apiUrl}/video-calls/ice-servers`)
      );
      if (!res.success || !res.data?.length) throw new Error('No ICE servers returned');
      this.cached = res.data;
      this.cachedAt = performance.now();
      return this.cached;
    } catch {
      // Never break a call if the backend is unreachable — STUN still covers most cases.
      return environment.iceServers;
    }
  }
}
