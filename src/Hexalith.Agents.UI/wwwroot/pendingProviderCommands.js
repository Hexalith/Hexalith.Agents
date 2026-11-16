// Web Locks coordinate same-origin documents sharing a browser storage scope.
// Unavailable attempts leave no queued mutation behind another storage owner.
// An acquired callback writes synchronously; a delivered write can outlive caller refusal.
export async function compareAndSet(key, expected, replacement) {
    if (!globalThis.navigator?.locks) {
        throw new Error("Browser storage coordination is unavailable.");
    }
    return navigator.locks.request(`hexalith.agents.pending:${key}`, { ifAvailable: true }, lock => {
        if (!lock) {
            return false;
        }
        if (sessionStorage.getItem(key) !== expected) {
            return false;
        }
        sessionStorage.setItem(key, replacement);
        return true;
    });
}
