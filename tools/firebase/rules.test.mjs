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

await env.cleanup();
console.log('passed ' + passed + ', failed ' + failed);
process.exit(failed === 0 ? 0 : 1);
