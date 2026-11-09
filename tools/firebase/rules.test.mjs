// Rules check for tools/firebase/database.rules.json against the Realtime Database emulator.
// Run: npx firebase emulators:exec --only database --project solohero-5744f "node rules.test.mjs"
import fs from 'fs';
import { initializeTestEnvironment, assertSucceeds, assertFails } from '@firebase/rules-unit-testing';
import { ref, get, set, remove, serverTimestamp } from 'firebase/database';

const env = await initializeTestEnvironment({
  projectId: 'solohero-5744f',
  database: { host: '127.0.0.1', port: 9000, rules: fs.readFileSync('database.rules.json', 'utf8') },
});

const alice = env.authenticatedContext('alice').database();
const bob = env.authenticatedContext('bob').database();
const nobody = env.unauthenticatedContext().database();
const save = { dataVersion: 2, saveRevision: 0, gold: 10 };
let passed = 0;
let failed = 0;

async function check(name, promise) {
  try {
    await promise;
    passed++;
    console.log('ok   ' + name);
  } catch (e) {
    failed++;
    console.log('FAIL ' + name + ' :: ' + (e && e.message ? e.message.split('\n')[0] : e));
  }
}

const transfer = (uid, extra = {}) => ({ uid, save: JSON.stringify(save), createdAt: serverTimestamp(), ...extra });

// users/{uid}: own node only, v2 with the save's shape, delete the whole node.
await check('owner writes v2', assertSucceeds(set(ref(alice, 'users/alice/v2'), save)));
await check('owner reads own node', assertSucceeds(get(ref(alice, 'users/alice'))));
await check('v2 without saveRevision refused', assertFails(set(ref(alice, 'users/alice/v2'), { dataVersion: 2, gold: 1 })));
await check('other user cannot read', assertFails(get(ref(bob, 'users/alice/v2'))));
await check('other user cannot write', assertFails(set(ref(bob, 'users/alice/v2'), save)));
await check('signed-out cannot read', assertFails(get(ref(nobody, 'users/alice'))));
await check('owner cannot write outside v2', assertFails(set(ref(alice, 'users/alice/legacy'), 1)));
await check('root listing refused', assertFails(get(ref(alice, 'users'))));
await check('other user cannot delete', assertFails(remove(ref(bob, 'users/alice'))));
await check('owner deletes everything', assertSucceeds(remove(ref(alice, 'users/alice'))));

// transfers/{code}: create once with own uid and the server stamp; anyone signed in with the code reads it for 24 h.
const code = 'ABCDEFGH2345';
await check('owner issues a code', assertSucceeds(set(ref(alice, 'transfers/' + code), transfer('alice'))));
await check('taken code refused', assertFails(set(ref(bob, 'transfers/' + code), transfer('bob'))));
await check('someone else\'s uid refused', assertFails(set(ref(alice, 'transfers/BCDEFGHJ2345'), transfer('bob'))));
await check('lower case code refused', assertFails(set(ref(alice, 'transfers/abcdefgh2345'), transfer('alice'))));
await check('11 characters refused', assertFails(set(ref(alice, 'transfers/ABCDEFGH234'), transfer('alice'))));
await check('look-alike 0 refused', assertFails(set(ref(alice, 'transfers/ABCDEFGH2340'), transfer('alice'))));
await check('client createdAt refused', assertFails(set(ref(alice, 'transfers/CDEFGHJK2345'), transfer('alice', { createdAt: 12345 }))));
await check('extra field refused', assertFails(set(ref(alice, 'transfers/DEFGHJKL2345'), transfer('alice', { extra: 1 }))));
await check('oversized save refused', assertFails(set(ref(alice, 'transfers/EFGHJKLM2345'), transfer('alice', { save: 'x'.repeat(200001) }))));
await check('signed-in user with the code reads it', assertSucceeds(get(ref(bob, 'transfers/' + code))));
await check('signed-out cannot read a code', assertFails(get(ref(nobody, 'transfers/' + code))));
await check('codes cannot be listed', assertFails(get(ref(bob, 'transfers'))));
await check('unknown code refused', assertFails(get(ref(bob, 'transfers/NNNNNNNNNNNN'))));

await env.withSecurityRulesDisabled(async (ctx) => {
  await set(ref(ctx.database(), 'transfers/EXPRDEXPRD22'), { uid: 'alice', save: '{}', createdAt: Date.now() - 86400000 - 60000 });
});
await check('expired code refused', assertFails(get(ref(bob, 'transfers/EXPRDEXPRD22'))));
await check('new device uses the code up', assertSucceeds(remove(ref(bob, 'transfers/' + code))));
await check('deleting a missing code refused', assertFails(remove(ref(bob, 'transfers/' + code))));

// purchases/{sha256(token)} (D-183): created once by the buyer with granted false, flipped to true by the buyer only,
// read by the buyer only - a receipt replayed into another account finds it taken.
const k1 = 'a'.repeat(64);
const k2 = 'b'.repeat(64);
const claim = (uid, extra = {}) => ({ uid, product: 'gem_small', granted: false, at: serverTimestamp(), ...extra });
await check('buyer records a purchase', assertSucceeds(set(ref(alice, 'purchases/' + k1), claim('alice'))));
await check('buyer reads own record', assertSucceeds(get(ref(alice, 'purchases/' + k1))));
await check('another account cannot take the same purchase', assertFails(set(ref(bob, 'purchases/' + k1), claim('bob'))));
await check('another account cannot read it', assertFails(get(ref(bob, 'purchases/' + k1))));
await check('buyer cannot record it twice', assertFails(set(ref(alice, 'purchases/' + k1), claim('alice'))));
await check('buyer marks it granted', assertSucceeds(set(ref(alice, 'purchases/' + k1 + '/granted'), true)));
await check('granted cannot go back to false', assertFails(set(ref(alice, 'purchases/' + k1 + '/granted'), false)));
await check('another account cannot mark it', assertFails(set(ref(bob, 'purchases/' + k1 + '/granted'), true)));
await check('product cannot be changed', assertFails(set(ref(alice, 'purchases/' + k1 + '/product'), 'gem_large')));
await check('owner cannot be changed', assertFails(set(ref(alice, 'purchases/' + k1 + '/uid'), 'bob')));
await check('a record cannot be deleted', assertFails(remove(ref(alice, 'purchases/' + k1))));
await check('new record already granted refused', assertFails(set(ref(alice, 'purchases/' + k2), claim('alice', { granted: true }))));
await check('record for someone else refused', assertFails(set(ref(alice, 'purchases/' + k2), claim('bob'))));
await check('client time refused', assertFails(set(ref(alice, 'purchases/' + k2), claim('alice', { at: 12345 }))));
await check('extra field refused', assertFails(set(ref(alice, 'purchases/' + k2), claim('alice', { gems: 999 }))));
await check('key that is not a hash refused', assertFails(set(ref(alice, 'purchases/not-a-hash'), claim('alice'))));
await check('upper-case hex refused', assertFails(set(ref(alice, 'purchases/' + 'A'.repeat(64)), claim('alice'))));
await check('signed-out cannot record', assertFails(set(ref(nobody, 'purchases/' + k2), claim('alice'))));
await check('records cannot be listed', assertFails(get(ref(alice, 'purchases'))));
await check('a free key reads as empty (not a replay)', assertSucceeds(get(ref(bob, 'purchases/' + 'c'.repeat(64)))));
await check('the all-zero probe key reads as empty', assertSucceeds(get(ref(alice, 'purchases/' + '0'.repeat(64)))));
await check('signed-out cannot read even a free key', assertFails(get(ref(nobody, 'purchases/' + 'c'.repeat(64)))));
await check('a taken key stays unreadable to others', assertFails(get(ref(bob, 'purchases/' + k1))));

// config/events (D-156): every client reads the event schedule, nobody writes it (the console only).
await env.withSecurityRulesDisabled(async (ctx) => {
  await set(ref(ctx.database(), 'config/events/launch_test'), { template: 'launch', start: 1, end: 2 });
});
await check('signed-in user reads the event schedule', assertSucceeds(get(ref(alice, 'config/events'))));
await check('signed-out client reads the event schedule', assertSucceeds(get(ref(nobody, 'config/events'))));
await check('user cannot write the event schedule', assertFails(set(ref(alice, 'config/events/x'), { template: 'launch', start: 1, end: 2 })));
await check('user cannot replace the schedule', assertFails(set(ref(alice, 'config/events'), {})));
await check('other config stays closed', assertFails(get(ref(alice, 'config'))));

await env.cleanup();
console.log('passed ' + passed + ', failed ' + failed);
process.exit(failed === 0 ? 0 : 1);
