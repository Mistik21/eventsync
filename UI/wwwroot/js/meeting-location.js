import { formatCoordinates, loadMapGl, lookupAddress, parseCoordinates } from "./two-gis.js";

export function setupMeetingLocation(field) {
    const toggle = field.querySelector(".meeting-map-toggle");
    const toggleLabel = toggle.querySelector("span");
    const panel = field.querySelector(".meeting-map-panel");
    const mapElement = field.querySelector(".meeting-map");
    const confirm = field.querySelector(".meeting-map-confirm");
    const editAddress = field.querySelector(".meeting-edit-address");
    const summary = field.querySelector(".meeting-address");
    const address = summary?.querySelector(".meeting-address-text");
    const attribution = summary?.querySelector(".map-attribution");
    const status = field.querySelector(".meeting-location-status");
    const fallback = field.querySelector(".meeting-address-fallback");
    const manualAddress = fallback?.querySelector("input");
    let map;
    let mapLoading;
    let marker;
    let draft;
    let selected;
    let selectedAddress = Promise.resolve("");
    let editCoordinates = "";
    let choosing = false;

    function showStatus(message = "") {
        status.textContent = message;
        status.hidden = !message;
    }

    function showPanel(open) {
        choosing = open;
        const preview = Boolean(editAddress && selected && !open && !toggle.hidden);
        panel.hidden = !open && !preview;
        confirm.hidden = !open;
        mapElement.classList.toggle("meeting-map--preview", preview);
        if (summary) {
            summary.hidden = open || !selected;
        }
        toggle.setAttribute("aria-expanded", String(open));
        toggleLabel.textContent = open ? "Закрыть карту" : selected ? "Изменить точку" : "Выбрать на карте 2ГИС";
        if (preview && map) {
            map.invalidateSize();
            map.setCenter([selected.lng, selected.lat]);
            setDraft(selected);
        }
    }

    function useManualAddress() {
        toggle.hidden = true;
        showPanel(false);
        map?.destroy();
        map = undefined;
        marker = undefined;
        if (summary && fallback) {
            summary.hidden = true;
            fallback.hidden = false;
        }
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
        const initialCenter = selected ? [selected.lng, selected.lat] : [60.6057, 56.8389];
        map = new mapgl.Map(mapElement, {
            key: apiKey,
            center: initialCenter,
            zoom: selected ? 15 : 12,
            lang: "ru",
            disableZoomOnScroll: true,
            enableTrackResize: true,
        });
        marker = new mapgl.Marker(map, { coordinates: initialCenter, interactive: false });
        map.on("click", ({ lngLat: [lng, lat] }) => {
            if (choosing) {
                setDraft({ lat, lng });
            }
        });
        map.once("styleloaderror", useManualAddress);
    }

    toggle.addEventListener("click", async () => {
        if (choosing) {
            showPanel(false);
            showStatus();
            return;
        }

        showPanel(true);
        if (!map) {
            toggle.disabled = true;
            showStatus("Загружаем карту…");
            try {
                await (mapLoading ??= initializeMap());
            } catch {
                useManualAddress();
            } finally {
                toggle.disabled = false;
            }
            if (toggle.hidden) {
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
        const coordinates = formatCoordinates(point);
        if (editAddress) {
            editCoordinates = coordinates;
            editAddress.value = "";
            editAddress.placeholder = coordinates;
        } else {
            address.textContent = coordinates;
            attribution.hidden = true;
        }
        showPanel(false);
        toggle.focus();
        showStatus("Определяем адрес…");

        const lookup = lookupAddress(point).then(value => value.trim()).catch(() => "");
        selectedAddress = lookup;
        const formattedAddress = await lookup;
        if (selectedAddress !== lookup || (fallback && !fallback.hidden)) {
            return;
        }
        if (formattedAddress) {
            if (editAddress) {
                editAddress.value = formattedAddress;
            } else {
                address.textContent = formattedAddress;
                attribution.hidden = false;
            }
        }
        if (!choosing && !toggle.hidden) {
            showStatus(formattedAddress ? "" : "Адрес не найден. Будет сохранена выбранная точка.");
        }
    });

    editAddress?.addEventListener("input", () => {
        selected = null;
        editCoordinates = "";
        selectedAddress = Promise.resolve(editAddress.value.trim());
        editAddress.placeholder = "Введите адрес…";
        if (!choosing) {
            showPanel(false);
            if (!toggle.hidden) {
                showStatus();
            }
        }
    });

    return {
        setLocation(location) {
            editAddress.value = location.address ?? "";
            editCoordinates = location.coordinates ?? "";
            selected = parseCoordinates(editCoordinates);
            selectedAddress = Promise.resolve(editAddress.value);
            if (!editAddress.value && selected) {
                editAddress.placeholder = editCoordinates;
            }
            showPanel(false);
            if (selected) {
                mapLoading ??= initializeMap();
                void mapLoading.then(() => {
                    if (!choosing) {
                        showPanel(false);
                    }
                }).catch(useManualAddress);
            }
        },
        read() {
            if (choosing) {
                return null;
            }
            if (editAddress) {
                return selectedAddress.then(address => ({ address, coordinates: editCoordinates }));
            }
            if (!fallback.hidden) {
                const value = manualAddress.value.trim();
                return value ? Promise.resolve({ address: value, coordinates: selected ? formatCoordinates(selected) : "" }) : null;
            }
            if (!selected) {
                return null;
            }
            const coordinates = formatCoordinates(selected);
            return selectedAddress.then(address => ({ address, coordinates }));
        },
        focus() {
            if (choosing) {
                (confirm.disabled ? mapElement : confirm).focus();
            } else if (editAddress) {
                editAddress.focus();
            } else if (!fallback.hidden) {
                manualAddress.focus();
            } else {
                toggle.focus();
            }
        },
    };
}
