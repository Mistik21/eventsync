import { fetchApi } from "./session.js";

let maps;

async function initializeMapGl() {
    const response = await fetchApi("/api/maps/config", { cache: "no-store" });
    if (!response.ok || response.status === 204) {
        throw new Error("Карта 2ГИС недоступна.");
    }
    const { apiKey } = await response.json();
    if (!apiKey) {
        throw new Error("Карта 2ГИС недоступна.");
    }

    return new Promise((resolve, reject) => {
        const callback = "eventSyncMapGlReady";
        const script = document.createElement("script");

        function cleanUp() {
            window.clearTimeout(timeout);
            delete window[callback];
        }
        function fail() {
            cleanUp();
            script.remove();
            reject(new Error("Не удалось открыть карту 2ГИС."));
        }

        const timeout = window.setTimeout(fail, 15_000);
        window[callback] = () => {
            const mapgl = window.mapgl;
            if (!mapgl?.isSupported()) {
                fail();
                return;
            }
            cleanUp();
            resolve({ mapgl, apiKey });
        };
        script.async = true;
        script.src = `https://mapgl.2gis.com/api/js/v1?callback=${callback}`;
        script.onerror = fail;
        document.head.append(script);
    });
}

export function loadMapGl() {
    maps ??= initializeMapGl();
    return maps;
}

export async function lookupAddress({ lat, lng }) {
    const query = new URLSearchParams({ lat: lat.toFixed(6), lng: lng.toFixed(6) });
    const response = await fetchApi(`/api/maps/reverse?${query}`, { cache: "no-store" });
    if (!response.ok) {
        throw new Error("Не удалось получить адрес из 2ГИС.");
    }
    if (response.status === 204) {
        return "";
    }
    const { address } = await response.json();
    return address ?? "";
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
