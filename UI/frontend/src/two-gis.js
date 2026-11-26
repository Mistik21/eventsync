const apiKey = import.meta.env.VITE_2GIS_API_KEY?.trim();
let maps;

async function initializeMapGl() {
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
    maps ??= initializeMapGl().catch(error => {
        maps = undefined;
        throw error;
    });
    return maps;
}

export async function lookupAddress({ lat, lng }) {
    if (!apiKey) {
        throw new Error("Не удалось получить адрес из 2ГИС.");
    }

    const url = new URL("https://catalog.api.2gis.com/3.0/items/geocode");
    url.search = new URLSearchParams({
        lat: lat.toFixed(6),
        lon: lng.toFixed(6),
        fields: "items.adm_div,items.address",
        key: apiKey,
    }).toString();

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 2500);
    try {
        const response = await fetch(url, { cache: "no-store", signal: controller.signal });
        if (!response.ok) {
            throw new Error("Не удалось получить адрес из 2ГИС.");
        }

        const { meta, result } = await response.json();
        if (meta?.code === 404) {
            return "";
        }
        if (meta?.code !== 200) {
            throw new Error("Не удалось получить адрес из 2ГИС.");
        }

        const item = result?.items?.[0];
        return item?.full_name?.trim() || item?.address_name?.trim() || "";
    } finally {
        clearTimeout(timeout);
    }
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
