import { formatCoordinates, loadMapGl, lookupAddress } from "./two-gis.js";

export function setupMeetingLocation(field) {
    const toggle = field.querySelector(".meeting-map-toggle");
    const toggleLabel = toggle.querySelector("span");
    const panel = field.querySelector(".meeting-map-panel");
    const mapElement = field.querySelector(".meeting-map");
    const confirm = field.querySelector(".meeting-map-confirm");
    const summary = field.querySelector(".meeting-address");
    const address = summary.querySelector(".meeting-address-text");
    const attribution = summary.querySelector(".map-attribution");
    const status = field.querySelector(".meeting-location-status");
    const fallback = field.querySelector(".meeting-address-fallback");
    const manualAddress = fallback.querySelector("input");
    let map;
    let marker;
    let draft;
    let selected;

    function showStatus(message = "") {
        status.textContent = message;
        status.hidden = !message;
    }

    function showPanel(open) {
        panel.hidden = !open;
        summary.hidden = open || !selected;
        toggle.setAttribute("aria-expanded", String(open));
        toggleLabel.textContent = open ? "Закрыть карту" : selected ? "Изменить точку" : "Выбрать на карте 2ГИС";
    }

    function useManualAddress() {
        showPanel(false);
        map?.destroy();
        map = undefined;
        marker = undefined;
        summary.hidden = true;
        toggle.hidden = true;
        fallback.hidden = false;
        showStatus("Карта 2ГИС сейчас недоступна. Введите адрес вручную.");
    }

    function setDraft(point) {
        draft = point;
        if (point) {
            marker.setCoordinates([point.lng, point.lat]);
            marker.show();
        } else {
            marker.hide();
        }
        confirm.disabled = !draft;
    }

    async function initializeMap() {
        const { mapgl, apiKey } = await loadMapGl();
        const initialCenter = [60.6057, 56.8389];
        map = new mapgl.Map(mapElement, {
            key: apiKey,
            center: initialCenter,
            zoom: 12,
            lang: "ru",
            disableZoomOnScroll: true,
            enableTrackResize: true,
        });
        marker = new mapgl.Marker(map, { coordinates: initialCenter, interactive: false });
        map.on("click", ({ lngLat: [lng, lat] }) => setDraft({ lat, lng }));
        map.once("styleloaderror", useManualAddress);
    }

    toggle.addEventListener("click", async () => {
        if (!panel.hidden) {
            showPanel(false);
            showStatus();
            return;
        }

        showPanel(true);
        if (!map) {
            toggle.disabled = true;
            showStatus("Загружаем карту…");
            try {
                await initializeMap();
            } catch {
                useManualAddress();
            } finally {
                toggle.disabled = false;
            }
            if (!fallback.hidden) {
                return;
            }
        }

        map.invalidateSize();
        setDraft(selected);
        if (selected) {
            map.setCenter([selected.lng, selected.lat]);
        }
        showStatus("Нажмите на карту, чтобы поставить метку.");
    });

    confirm.addEventListener("click", async () => {
        const point = draft;
        if (!point) {
            return;
        }
        selected = point;
        address.textContent = formatCoordinates(point);
        attribution.hidden = true;
        showPanel(false);
        toggle.focus();
        showStatus("Определяем адрес…");

        let formattedAddress;
        try {
            formattedAddress = await lookupAddress(point);
        } catch {
            if (selected === point && panel.hidden && fallback.hidden) {
                showStatus("Не удалось определить адрес. Будет сохранена выбранная точка.");
            }
            return;
        }
        if (selected !== point) {
            return;
        }
        if (formattedAddress) {
            address.textContent = formattedAddress;
            attribution.hidden = false;
        }
        if (panel.hidden && fallback.hidden) {
            showStatus(formattedAddress ? "" : "Адрес не найден. Будет сохранена выбранная точка.");
        }
    });

    return {
        read() {
            if (!fallback.hidden) {
                const value = manualAddress.value.trim();
                return value ? { address: value, coordinates: selected ? formatCoordinates(selected) : "" } : null;
            }
            if (!selected || !panel.hidden) {
                return null;
            }
            // Only the point placed by the user is persisted. Each viewer gets a fresh address lookup.
            return { address: "", coordinates: formatCoordinates(selected) };
        },
        focus() {
            if (!fallback.hidden) {
                manualAddress.focus();
            } else if (!panel.hidden) {
                (confirm.disabled ? mapElement : confirm).focus();
            } else {
                toggle.focus();
            }
        },
    };
}
