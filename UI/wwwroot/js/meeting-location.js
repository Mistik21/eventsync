import { formatCoordinates, loadGoogleMaps, lookupGoogleAddress } from "./google-maps.js";

export function setupMeetingLocation(field) {
    const toggle = field.querySelector(".meeting-map-toggle");
    const toggleLabel = toggle.querySelector("span");
    const panel = field.querySelector(".meeting-map-panel");
    const mapElement = field.querySelector(".meeting-map");
    const confirm = field.querySelector(".meeting-map-confirm");
    const center = field.querySelector(".meeting-map-center");
    const summary = field.querySelector(".meeting-address");
    const address = summary.querySelector(".meeting-address-text");
    const attribution = summary.querySelector(".google-maps-attribution");
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
        toggleLabel.textContent = open ? "Закрыть карту" : selected ? "Изменить точку" : "Выбрать на Google Maps";
    }

    function useManualAddress() {
        showPanel(false);
        summary.hidden = true;
        toggle.hidden = true;
        fallback.hidden = false;
        showStatus("Google Maps сейчас недоступны. Введите адрес вручную.");
    }

    function setDraft(point) {
        draft = point;
        marker.draw();
        confirm.disabled = !draft;
    }

    async function initializeMap() {
        const maps = await loadGoogleMaps();
        const { Map, OverlayView } = await maps.importLibrary("maps");
        if (!fallback.hidden) {
            return;
        }
        map = new Map(mapElement, {
            center: { lat: 56.8389, lng: 60.6057 },
            zoom: 12,
            gestureHandling: "cooperative",
            clickableIcons: false,
            mapTypeControl: false,
            streetViewControl: false,
            fullscreenControl: false,
        });
        const pin = document.createElement("span");
        pin.className = "meeting-map-pin";
        pin.setAttribute("aria-hidden", "true");

        class MeetingPin extends OverlayView {
            onAdd() {
                this.getPanes().overlayLayer.append(pin);
            }
            draw() {
                const position = draft && this.getProjection()?.fromLatLngToDivPixel(draft);
                pin.hidden = !position;
                if (position) {
                    pin.style.left = `${position.x}px`;
                    pin.style.top = `${position.y}px`;
                }
            }
            onRemove() {
                pin.remove();
            }
        }

        marker = new MeetingPin();
        marker.setMap(map);
        map.addListener("click", event => {
            if (event.latLng) {
                setDraft(event.latLng);
            }
        });
    }

    window.addEventListener("google-maps-unavailable", useManualAddress);

    toggle.addEventListener("click", async () => {
        if (!panel.hidden) {
            showPanel(false);
            showStatus();
            return;
        }

        showPanel(true);
        if (!map) {
            toggle.disabled = true;
            center.disabled = true;
            showStatus("Загружаем карту…");
            try {
                await initializeMap();
            } catch {
                useManualAddress();
            } finally {
                toggle.disabled = false;
                center.disabled = false;
            }
            if (!fallback.hidden) {
                return;
            }
        }

        setDraft(selected);
        if (selected) {
            map.setCenter(selected);
        }
        showStatus("Нажмите на карту, чтобы поставить метку.");
    });

    center.addEventListener("click", () => setDraft(map.getCenter()));

    confirm.addEventListener("click", async () => {
        const point = draft;
        if (!point) {
            return;
        }
        selected = point;
        address.textContent = formatCoordinates(point.toJSON());
        attribution.hidden = true;
        showPanel(false);
        toggle.focus();
        showStatus("Определяем адрес…");

        let formattedAddress;
        try {
            formattedAddress = await lookupGoogleAddress(point);
        } catch {
            formattedAddress = "";
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
                return value ? { address: value, coordinates: selected ? formatCoordinates(selected.toJSON()) : "" } : null;
            }
            if (!selected || !panel.hidden) {
                return null;
            }
            // Only the point placed by the user is persisted. Each viewer gets a fresh address lookup.
            return { address: "", coordinates: formatCoordinates(selected.toJSON()) };
        },
        focus() {
            if (!fallback.hidden) {
                manualAddress.focus();
            } else if (!panel.hidden) {
                (confirm.disabled ? center : confirm).focus();
            } else {
                toggle.focus();
            }
        },
    };
}
