// LinkUp API smoke test — exercises the main flows end-to-end.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const BASE = 'https://localhost:5001/api/v1';

let pass = 0, fail = 0;
const results = [];
function check(name, ok, detail = '') {
  if (ok) { pass++; results.push(`  ✓ ${name}`); }
  else { fail++; results.push(`  ✗ ${name} ${detail}`); }
  return ok;
}

async function api(method, path, { token, body, raw } = {}) {
  const headers = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (body && !raw) headers['Content-Type'] = 'application/json';
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: raw ? body : (body ? JSON.stringify(body) : undefined),
  });
  let data = null;
  try { data = await res.json(); } catch { /* no body */ }
  return { status: res.status, data };
}

const stamp = Date.now();
const userA = { firstName: 'Alice', lastName: 'Anderson', email: `alice${stamp}@test.com`, userName: `alice${stamp}`, password: 'Passw0rd!', confirmPassword: 'Passw0rd!' };
const userB = { firstName: 'Bob', lastName: 'Brown', email: `bob${stamp}@test.com`, userName: `bob${stamp}`, password: 'Passw0rd!', confirmPassword: 'Passw0rd!' };

(async () => {
  console.log('=== AUTH ===');
  let r = await api('POST', '/auth/register', { body: userA });
  check('register user A', (r.status === 200 || r.status === 201) && r.data?.success, `(${r.status} ${r.data?.message || ''})`);
  let tokenA = r.data?.data?.accessToken;
  const idA = r.data?.data?.user?.id;

  r = await api('POST', '/auth/register', { body: userB });
  check('register user B', (r.status === 200 || r.status === 201) && r.data?.success, `(${r.status})`);
  let tokenB = r.data?.data?.accessToken;
  const idB = r.data?.data?.user?.id;

  r = await api('POST', '/auth/login', { body: { email: userA.email, password: userA.password } });
  check('login user A', r.status === 200 && !!r.data?.data?.accessToken, `(${r.status})`);

  r = await api('POST', '/auth/login', { body: { email: userA.email, password: 'wrong' } });
  check('login rejects bad password', r.status === 400 || r.status === 401, `(${r.status})`);

  r = await api('GET', '/posts/feed', {});
  check('protected endpoint rejects no-token', r.status === 401, `(${r.status})`);

  console.log('=== PROFILE ===');
  r = await api('GET', `/profile/${idA}`, { token: tokenA });
  check('get profile A', r.status === 200, `(${r.status})`);

  r = await api('PUT', '/profile', { token: tokenA, body: { bio: 'Hello world', location: 'NYC', website: 'https://alice.dev' } });
  check('update profile A', r.status === 200 || r.status === 204, `(${r.status} ${r.data?.message || ''})`);

  console.log('=== FRIENDS ===');
  r = await api('POST', '/friends/request', { token: tokenA, body: { receiverId: idB } });
  check('A sends friend request to B', (r.status === 200 || r.status === 201) && r.data?.success, `(${r.status} ${r.data?.message || ''})`);

  r = await api('GET', '/friends/requests/pending', { token: tokenB });
  check('B sees pending request', r.status === 200, `(${r.status})`);
  const reqId = r.data?.data?.items?.[0]?.id;

  if (reqId) {
    r = await api('PUT', `/friends/request/${reqId}/accept`, { token: tokenB });
    check('B accepts request', r.status === 200, `(${r.status} ${r.data?.message || ''})`);
  } else {
    check('B accepts request', false, '(no requestId found)');
  }

  r = await api('GET', '/friends', { token: tokenA });
  check('A lists friends', r.status === 200, `(${r.status})`);

  r = await api('GET', '/friends/suggestions', { token: tokenA });
  check('A gets friend suggestions', r.status === 200, `(${r.status})`);

  console.log('=== POSTS ===');
  // Send enum as STRING (as the Angular UI does) to verify JsonStringEnumConverter.
  r = await api('POST', '/posts', { token: tokenA, body: { content: 'My first post!', postType: 'Text', visibility: 'Public' } });
  check('A creates post (string enum)', r.status === 200 || r.status === 201, `(${r.status} ${r.data?.message || ''})`);
  const postId = r.data?.data?.id;
  check('post visibility returned as string', r.data?.data?.visibility === 'Public', `(got ${JSON.stringify(r.data?.data?.visibility)})`);

  r = await api('GET', '/posts/feed', { token: tokenA });
  check('A gets feed', r.status === 200, `(${r.status})`);

  if (postId) {
    r = await api('GET', `/posts/${postId}`, { token: tokenB });
    check('B reads post by id', r.status === 200, `(${r.status})`);
  }

  console.log('=== COMMENTS ===');
  let commentId;
  if (postId) {
    r = await api('POST', '/comments', { token: tokenB, body: { postId, content: 'Nice post!' } });
    check('B comments on post', r.status === 200 || r.status === 201, `(${r.status} ${r.data?.message || ''})`);
    commentId = r.data?.data?.id;

    r = await api('GET', `/comments/post/${postId}`, { token: tokenA });
    check('list comments for post', r.status === 200, `(${r.status})`);
    const c0 = r.data?.data?.items?.[0];
    check('comment exposes flat authorName', !!c0 && typeof c0.authorName === 'string' && c0.authorName.length > 0, `(got ${JSON.stringify(c0?.authorName)})`);
  }

  console.log('=== REACTIONS ===');
  if (postId) {
    r = await api('POST', '/reactions', { token: tokenB, body: { targetId: postId, targetType: 'Post', type: 1 } });
    check('B reacts to post', r.status === 200 || r.status === 201, `(${r.status} ${r.data?.message || ''})`);

    r = await api('GET', `/reactions/Post/${postId}`, { token: tokenA });
    check('get reaction counts', r.status === 200, `(${r.status})`);
  }

  console.log('=== CHAT ===');
  r = await api('GET', '/chats', { token: tokenA });
  check('A lists chats', r.status === 200, `(${r.status})`);

  r = await api('POST', '/chats/direct', { token: tokenA, body: { targetUserId: idB } });
  check('A starts direct chat with B', r.status === 200 || r.status === 201, `(${r.status} ${r.data?.message || ''})`);
  const chatId = r.data?.data?.id || r.data?.data?.chatId;
  check('chat list item exposes flat otherUserName', r.data?.data?.otherUserName === 'Bob Brown', `(got ${JSON.stringify(r.data?.data?.otherUserName)})`);

  if (chatId) {
    r = await api('POST', '/chats/messages', { token: tokenA, body: { chatId, content: 'Hey Bob!', messageType: 'Text' } });
    check('A sends message (string enum)', r.status === 200 || r.status === 201, `(${r.status} ${r.data?.message || ''})`);
    check('message exposes flat senderId', !!r.data?.data?.senderId && r.data?.data?.senderId === idA, `(got ${JSON.stringify(r.data?.data?.senderId)})`);
  }

  console.log('=== NOTIFICATIONS ===');
  r = await api('GET', '/notifications', { token: tokenB });
  check('B lists notifications', r.status === 200, `(${r.status})`);
  const notifs = r.data?.data?.items ?? [];
  const withSender = notifs.find(n => n.senderId);
  // If any notification has a sender, the flat senderName field should be present.
  check('notification exposes flat senderName (if any sender)', !withSender || typeof withSender.senderName === 'string', `(got ${JSON.stringify(withSender?.senderName)})`);

  console.log('=== SEARCH ===');
  r = await api('GET', `/search/users?q=alice`, { token: tokenB });
  check('search users', r.status === 200, `(${r.status})`);

  r = await api('GET', `/search?q=alice`, { token: tokenB });
  check('global search', r.status === 200, `(${r.status})`);

  console.log('=== ADMIN ===');
  const adminLogin = await api('POST', '/auth/login', { body: { email: 'admin@linkup.com', password: 'Admin@123' } });
  const adminToken = adminLogin.data?.data?.accessToken;

  r = await api('GET', '/admin/dashboard', { token: adminToken });
  check('admin dashboard', r.status === 200, `(${r.status})`);

  r = await api('GET', '/admin/users', { token: adminToken });
  check('admin lists users', r.status === 200, `(${r.status})`);

  r = await api('GET', '/admin/dashboard', { token: tokenA });
  check('admin endpoint forbidden for normal user', r.status === 403, `(${r.status})`);

  console.log('=== LOGOUT (object body) ===');
  // Re-login A to get a fresh refresh token, then logout with {refreshToken} object.
  const relog = await api('POST', '/auth/login', { body: { email: userA.email, password: userA.password } });
  r = await api('POST', '/auth/logout', { token: relog.data?.data?.accessToken, body: { refreshToken: relog.data?.data?.refreshToken } });
  check('logout accepts {refreshToken} object', r.status === 200, `(${r.status} ${r.data?.message || ''})`);

  console.log('\n=== RESULTS ===');
  console.log(results.join('\n'));
  console.log(`\nPASS: ${pass}  FAIL: ${fail}`);
  process.exit(fail > 0 ? 1 : 0);
})().catch(e => { console.error('FATAL', e); process.exit(2); });
