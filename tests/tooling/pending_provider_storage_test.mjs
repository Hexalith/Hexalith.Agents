import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Import the shipped module unchanged; each import models a distinct JS runtime.
const source = await readFile(new URL('../../src/Hexalith.Agents.UI/wwwroot/pendingProviderCommands.js', import.meta.url), 'utf8');
const load = suffix => import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}#${suffix}`);
const first = await load('first');
const second = await load('second');
const values = new Map();
const held = new Set();
let deny = false;
Object.defineProperty(globalThis, 'navigator', { configurable: true, value: { locks: {
    async request(name, options, action) {
        assert.equal(options.ifAvailable, true);
        if (held.has(name)) return action(null);
        held.add(name);
        try { return action({ name }); }
        finally { held.delete(name); }
    }
} } });
globalThis.sessionStorage = {
    getItem(key) { if (deny) throw new Error('denied'); return values.get(key) ?? null; },
    setItem(key, value) { if (deny) throw new Error('denied'); values.set(key, value); }
};

test('separate module instances compare exact absent, corrupt and valid bytes under one browser lock', async () => {
    values.clear();
    const outcomes = await Promise.all([
        first.compareAndSet('scope', null, '["first"]'),
        second.compareAndSet('scope', null, '["second"]')
    ]);
    assert.deepEqual(outcomes, [true, false]);
    assert.equal(values.get('scope'), '["first"]');
    assert.equal(await second.compareAndSet('scope', '["first"]', '["first","second"]'), true);
    assert.equal(await first.compareAndSet('scope', '["first"]', '[]'), false);
    assert.equal(values.get('scope'), '["first","second"]');
    values.set('scope', 'null');
    assert.equal(await first.compareAndSet('scope', null, '[]'), false);
    assert.equal(values.get('scope'), 'null');
});

for (const transition of ['cancel', 'authentication']) {
    test(`held lock ${transition} refusal cannot leave a late browser mutation`, async () => {
        values.clear();
        const name = 'hexalith.agents.pending:alice';
        held.add(name);
        // Model the caller abandoning/refusing interop while another owner holds
        // the lock; an unavailable attempt must leave no work in a browser queue.
        const pending = first.compareAndSet('alice', null, '["identity"]');
        if (transition === 'authentication') values.set('bob', '["other"]');
        assert.equal(await pending, false);
        held.delete(name);
        await new Promise(resolve => setImmediate(resolve));
        assert.equal(values.has('alice'), false);
        assert.equal(values.get('bob'), transition === 'authentication' ? '["other"]' : undefined);
    });
}

test('a synchronous write already delivered to the captured scope remains observable', async () => {
    values.clear();
    assert.equal(await first.compareAndSet('alice', null, '["identity"]'), true);
    // A later caller cancellation/authentication refusal cannot undo these bytes.
    values.set('bob', '["other"]');
    assert.equal(values.get('alice'), '["identity"]');
    assert.equal(values.get('bob'), '["other"]');
});

test('storage denial and missing browser coordination refuse without replacing a lock', async () => {
    values.set('scope', '["retained"]');
    deny = true;
    await assert.rejects(first.compareAndSet('scope', '["retained"]', '[]'), /denied/);
    deny = false;
    const locks = navigator.locks;
    navigator.locks = null;
    await assert.rejects(second.compareAndSet('scope', '["retained"]', '[]'), /unavailable/);
    navigator.locks = locks;
    assert.equal(values.get('scope'), '["retained"]');
});
