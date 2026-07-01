# Video Calling — SignalR, WebRTC, STUN & TURN (a plain-English guide)

This doc explains **how LinkUp's audio/video calling connects two people**, why calls
sometimes fail across different networks, and how we fixed that with STUN + TURN. It's
written as a learning guide — start at the top and read down.

> Written up from a real Q&A while building the feature, so it follows the natural
> questions you'd ask: *"Does it use SignalR?" → "Will it work across networks?" →
> "What's STUN/TURN?" → "Own server, free, or managed?" → "Hide the key on the backend."*

---

## 1. The core idea: two different jobs

A video call needs two separate things, and LinkUp uses a different technology for each:

| Job | Technology | What it does |
|-----|-----------|--------------|
| **Signaling** | **SignalR** (`VideoCallHub`) | Introduces the two browsers: who's calling, who answered, and the network handshake. |
| **Media** | **WebRTC** (browser-native) | Sends the actual audio/video **directly** between the two browsers (peer-to-peer). |

```
        Browser A                                     Browser B
   (VideoCallComponent)                          (VideoCallComponent)
        │                                              │
        │  ── SignalR (handshake messages only) ──►    │
        │      offer / answer / ICE                    │
        │◄─────── VideoCallHub (.NET server) ─────────►│
        │                                              │
        └════════ WebRTC P2P (actual video+audio) ═════┘
                 (the server never sees this stream)
```

**Key point:** SignalR is just the *postman* — it introduces the browsers to each other.
Once introduced, the video flows **directly** browser-to-browser over WebRTC. The server
never touches the video stream. There is **no third-party video library** (no PeerJS,
Agora, Twilio) — just the browser's built-in `RTCPeerConnection` and `getUserMedia`.

---

## 2. How SignalR does the signaling

The server hub (`src/Modules/VideoCall/Hubs/VideoCallHub.cs`) only **relays messages** —
no video data passes through it.

```csharp
// Ring the invited users
public async Task StartCall(string callId, string[] inviteeIds, string mediaType, bool isGroup)
{
    foreach (var id in inviteeIds)
        await Clients.Group(id).SendAsync("CallRinging", callId, Uid, mediaType, isGroup);
    ...
}

// Relay the WebRTC handshake (SDP/ICE) to one specific user — just pass it through
public Task SendSdpOffer(string callId, string targetUserId, string sdp) =>
    Clients.Group(targetUserId).SendAsync("SdpOfferReceived", callId, sdp, Uid);
```

The three things that get exchanged during the handshake:

- **SDP Offer** → *"my camera/mic run in this format — can you handle it?"*
- **SDP Answer** → *"yes, here are my settings."*
- **ICE Candidate** → *"you can reach me at this IP/port"* (network address).

---

## 3. How WebRTC does the media (browser-native)

In `linkup-web/src/app/features/video-call/video-call.component.ts`:

```typescript
// One RTCPeerConnection per peer — the browser's built-in WebRTC
const pc = new RTCPeerConnection({ iceServers: this.iceServers });

// Grab local camera/mic (also native)
this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: {...} });

// The remote video arrives here — directly P2P, NOT through the server
pc.ontrack = e => { /* show the remote stream */ };
```

---

## 4. The problem: will a call work across *different* networks?

Short answer: **usually yes, but not always.** It depends on the networks the two people
are on. This is where **STUN** and **TURN** come in.

### STUN — "what's my public address?"
Your computer sits behind a router with a **private** IP. STUN is a free service that tells
your browser its **public** IP as seen from the internet, so two peers can find each other
and connect **directly** (peer-to-peer, no relay). LinkUp uses Google's free public STUN.

✅ Works for most networks: home WiFi ↔ home WiFi, home ↔ mobile data, most cafés/offices.
❌ Fails on "hard" networks: **corporate LANs, mobile CGNAT, strict firewalls** — these
block direct connections, so STUN alone can't punch through (~20% of real-world cases).

### TURN — "relay it for me"
When a direct connection is impossible, **TURN** is a relay server: the audio/video flows
*through* it instead of peer-to-peer. It's the industry-standard fallback (WhatsApp, Google
Meet, etc. all use it). With TURN, connection success goes from ~80% → ~99%.

### How they work together (automatic)
You **don't** write "if STUN fails, use TURN" logic. WebRTC's ICE engine tries them in
order automatically, and TURN is only used as a last resort because it costs bandwidth:

```
1. Direct connection (same network)      ← tried first, fastest
2. STUN — public IP path (P2P across NAT) ← tried next
3. TURN — relay through a server          ← only if 1 & 2 fail
```

You just provide **both** in the `iceServers` list; WebRTC picks the best one that works.

---

## 5. Three ways to get TURN — which to choose

TURN needs a relay server somewhere. There are three approaches:

| | **STUN only** (free) | **Metered** (managed) ✅ *chosen* | **Own server** (self-host coturn) |
|---|---|---|---|
| What it is | Google's free public STUN, no TURN | Rent TURN from Metered's cloud | Run coturn on your own VPS |
| Cost | $0 forever | Free ~50GB/mo, then pay | ~$5/mo VPS (flat) |
| Server to run? | None | None (Metered runs it) | Yes — you manage it |
| Credit card? | No | No (free tier) | Yes (for the VPS) |
| Hard-NAT calls | ❌ often fail | ✅ work | ✅ work |
| Setup effort | Already done | ~2 min (paste key) | ~1 hour |
| Control | Google | Metered | You |

**Rule of thumb:**
- Just testing / normal networks → **STUN only** (free, already active).
- Want reliability with minimal effort → **Metered** (what we chose).
- High traffic or want full control → **self-host coturn**.

STUN alone is free but unreliable on hard networks. TURN fixes that — and TURN is either
*rented* (Metered) or *self-run* (coturn). Same job; the difference is who runs the server.

---

## 6. What LinkUp actually implements

We chose **Metered** (free tier) and fetch its credentials **server-side** so the API key
never ships in the browser bundle.

```
VideoCallComponent → IceService → GET /api/v1/video-calls/ice-servers  (JWT-protected)
                                        │
                                        └─ backend calls Metered with the secret key
                                           → returns STUN + TURN (fresh creds) to the browser
```

**Backend** (`src/Modules/VideoCall/`):
- `Managers/TurnCredentialService.cs` — calls Metered server-side; **falls back to
  STUN-only if Metered is unset or unreachable**, so a call is never blocked.
- `Controllers/VideoCallController.cs` — `GET /video-calls/ice-servers` endpoint.
- `Configuration/MeteredOptions.cs` — binds the `Metered` config section.
- Config lives in `appsettings.json` → `"Metered": { "AppName": "", "ApiKey": "" }`.

**Frontend** (`linkup-web/`):
- `core/services/ice.service.ts` — fetches the list from the backend, caches it for 1h
  (TURN creds are time-limited), falls back to a static STUN list on error.
- `features/video-call/video-call.component.ts` — resolves ICE servers once on init and
  passes them to every `RTCPeerConnection`.

### To enable TURN
1. Sign up free at **https://dashboard.metered.ca** (email only, no credit card).
2. Create an app → copy its **subdomain** and **API key**.
3. Put them in `appsettings.Development.json` (and `appsettings.json` for prod):
   ```json
   "Metered": { "AppName": "linkup", "ApiKey": "your-metered-api-key" }
   ```
4. Restart the API. Done — TURN fallback is live (up to 50GB/mo free).

> **Production tip:** don't commit the key. Use an environment variable —
> `Metered__ApiKey=...` and `Metered__AppName=...` are read automatically by .NET config.

### Verify TURN works
Use the [Trickle ICE test tool](https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/):
enter your TURN URL + credentials, click **Gather candidates**, and look for candidates of
type **`relay`** — that proves TURN is reachable and authenticating.

---

## 7. Current limitation (honest note)

Without Metered configured, LinkUp is **STUN-only** — great for demos and normal networks,
but calls between users behind corporate NAT / CGNAT / strict firewalls may fail to connect
(the 45-second no-answer timer then ends the call). Configuring Metered (or self-hosting
coturn) removes this limitation.

---

## TL;DR

- **SignalR** carries the *signaling*; **WebRTC** carries the *media*, peer-to-peer.
- **STUN** = free address discovery, works ~80% of the time. **TURN** = paid/self-run relay
  for the hard 20%.
- WebRTC auto-falls-back: **direct → STUN → TURN**. You just list both.
- LinkUp uses **Metered managed TURN**, fetched **server-side** so the API key stays secret,
  with a **STUN fallback** so calls never break.
