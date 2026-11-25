import { useEffect, useSyncExternalStore } from "react";
import { fetchApi, getSessionErrorMessage } from "./session.js";

export const eventsUrl = "/api/events";
export const profileUrl = "/api/users/me";
export const eventApi = id => `${eventsUrl}/${encodeURIComponent(id)}`;
const entries = new Map();
const maxAge = 30_000;

export function message(error, fallback) {
    return {
        401: getSessionErrorMessage(),
        403: "У вас нет доступа к этому действию.",
        404: "Данные не найдены.",
        409: "Данные изменились. Обновите их и попробуйте ещё раз.",
    }[error?.status] ?? fallback;
}

async function request(url, options, initial) {
    const response = await fetchApi(url, options, initial);
    if (!response.ok) throw Object.assign(new Error(`HTTP ${response.status}`), { status: response.status });
    return response;
}

export async function readJson(url, options) {
    const initial = url === eventsUrl ? window.eventSyncEvents : undefined;
    if (initial) delete window.eventSyncEvents;
    return (await request(url, { cache: "no-store", ...options }, initial)).json();
}

export function write(url, method, body) {
    return request(url, {
        method,
        ...(body === undefined ? {} : { headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) }),
    });
}

function entryFor(url) {
    if (!entries.has(url)) {
        const listeners = new Set();
        const entry = { snapshot: {}, updated: 0, pending: null };
        entry.subscribe = listener => {
            listeners.add(listener);
            return () => listeners.delete(listener);
        };
        entry.getSnapshot = () => entry.snapshot;
        entry.update = snapshot => {
            entry.snapshot = snapshot;
            for (const listener of listeners) listener();
        };
        entries.set(url, entry);
    }
    return entries.get(url);
}

export function refresh(url, force = false) {
    if (!url) return Promise.resolve();
    const entry = entryFor(url);
    if (!force && entry.pending) return entry.pending;
    if (!force && Date.now() - entry.updated < maxAge) return Promise.resolve(entry.snapshot.data);

    const pending = readJson(url).then(data => {
        if (entry.pending === pending) {
            entry.updated = Date.now();
            entry.update({ data });
        }
        return data;
    }).catch(error => {
        if (entry.pending === pending) {
            const denied = [401, 403, 404].includes(error.status);
            entry.update({ data: denied ? undefined : entry.snapshot.data, error });
        }
    }).finally(() => {
        if (entry.pending === pending) entry.pending = null;
    });
    entry.pending = pending;
    entry.update({ data: entry.snapshot.data });
    return pending;
}

export function expire(url) {
    entryFor(url).updated = 0;
}

export function changeCached(url, change) {
    const entry = entryFor(url);
    entry.pending = null;
    entry.updated = 0;
    entry.update({ data: change(entry.snapshot.data) });
}

export function useResource(url, active = true) {
    const entry = entryFor(url);
    const state = useSyncExternalStore(entry.subscribe, entry.getSnapshot);
    useEffect(() => {
        if (active) void refresh(url);
    }, [url, active]);
    return state;
}
