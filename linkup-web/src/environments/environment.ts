export const environment = {
  production: true,
  apiUrl: 'https://localhost:5001/api/v1',
  hubUrl: 'https://localhost:5001/hubs',
  cloudinaryCloudName: 'your-cloud-name',

  // WebRTC ICE servers. The full list (STUN + Metered TURN) is fetched at call time
  // from the backend (GET /video-calls/ice-servers) so the TURN API key stays secret.
  // This static STUN list is only the fallback if that request fails.
  iceServers: [
    { urls: 'stun:stun.l.google.com:19302' }
  ] as RTCIceServer[]
};
