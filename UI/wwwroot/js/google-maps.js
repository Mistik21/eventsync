import { fetchApi } from "./session.js";

let maps;
let geocoder;

async function initializeGoogleMaps() {
    const response = await fetchApi("/api/maps/config", { cache: "no-store" });
    if (!response.ok || response.status === 204) {
        throw new Error("Google Maps недоступны.");
    }
    const { apiKey } = await response.json();
    if (!apiKey) {
        throw new Error("Google Maps недоступны.");
    }

    return new Promise((resolve, reject) => {
        const callback = "eventSyncGoogleMapsReady";
        const script = document.createElement("script");
        const url = new URL("https://maps.googleapis.com/maps/api/js");
        url.search = new URLSearchParams({
            key: apiKey,
            loading: "async",
            callback,
            language: "ru",
            auth_referrer_policy: "origin",
        });

        function cleanUp() {
            window.clearTimeout(timeout);
            delete window[callback];
        }
        function fail() {
            cleanUp();
            script.remove();
            reject(new Error("Не удалось открыть Google Maps."));
        }

        const timeout = window.setTimeout(fail, 15_000);
        window[callback] = () => {
            cleanUp();
            resolve(window.google.maps);
        };
        window.gm_authFailure = () => {
            fail();
            window.dispatchEvent(new Event("google-maps-unavailable"));
        };
        script.async = true;
        script.src = url.href;
        script.onerror = fail;
        document.head.append(script);
    });
}

export function loadGoogleMaps() {
    maps ??= initializeGoogleMaps();
    return maps;
}

export async function lookupGoogleAddress(location) {
    const maps = await loadGoogleMaps();
    geocoder ??= maps.importLibrary("geocoding").then(({ Geocoder }) => new Geocoder());
    const { results } = await (await geocoder).geocode({ location, fulfillOnZeroResults: true });
    return results[0]?.formatted_address ?? "";
}

export function parseCoordinates(value) {
    const match = value?.match(/^\s*([+-]?\d+(?:\.\d+)?)\s*,\s*([+-]?\d+(?:\.\d+)?)\s*$/);
    if (!match) {
        return null;
    }
    const lat = Number(match[1]);
    const lng = Number(match[2]);
    return Math.abs(lat) <= 90 && Math.abs(lng) <= 180 ? { lat, lng } : null;
}

export function formatCoordinates({ lat, lng }) {
    return `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
}
