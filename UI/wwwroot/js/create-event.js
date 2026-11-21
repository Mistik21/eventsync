import { canDisplayCover } from "./events-list.js";
import { fetchApi, getSessionErrorMessage } from "./session.js";
import { setupMeetingLocation } from "./meeting-location.js";

const form = document.querySelector(".create-form");
const editing = form.dataset.mode === "edit";
const eventId = editing && window.location.pathname.match(/^\/event\/([\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12})\/edit\/?$/i)?.[1];
const eventPage = eventId ? `/html/event.html?id=${eventId}` : "/";
const api = eventId ? `/api/events/${eventId}` : "/api/events";
const fields = form.querySelector("fieldset");
const submit = form.querySelector(".create-submit");
const status = form.querySelector(".create-status");
const coverInput = form.querySelector("#event-cover");
const coverPicker = form.querySelector(".cover-picker");
const coverPreview = form.querySelector(".cover-preview");
const coverCaption = form.querySelector(".cover-caption");
const coverRemove = form.querySelector(".cover-remove");
const budgetInput = form.querySelector("#event-budget");
const meetingLocation = setupMeetingLocation(form.querySelector(".meeting-location"));
let previewUrl;
let savedCover = "";
let coverRemoved = false;
let savedEvent;
let savedStartInput;
let previousBudgetInput = "";

function setStatus(message = "", isError = true) {
    status.textContent = message;
    status.hidden = !message;
    status.classList.toggle("create-status--error", isError);
}

function updateCover(file, existingUrl = "") {
    if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
    }
    previewUrl = file ? URL.createObjectURL(file) : undefined;
    const url = previewUrl ?? existingUrl;
    if (url) {
        coverPreview.src = url;
    } else {
        coverPreview.removeAttribute("src");
    }
    coverPreview.hidden = !url;
    coverRemove.hidden = !url;
    coverPicker.classList.toggle("cover-picker--filled", Boolean(url));
    coverCaption.textContent = url ? (editing ? "Изменить обложку" : "Заменить обложку") : "Добавить обложку";
}

coverInput.addEventListener("change", () => {
    const file = coverInput.files[0];
    if (!file) {
        return;
    }
    setStatus();
    updateCover(file);
});

coverPreview.addEventListener("error", () => {
    if (!coverInput.files.length) {
        if (editing) {
            savedCover = "";
            updateCover();
        }
        return;
    }
    coverInput.value = "";
    updateCover(undefined, coverRemoved ? "" : savedCover);
    setStatus("Не удалось открыть обложку. Выберите JPEG, PNG или WebP.");
});

coverRemove.addEventListener("click", () => {
    coverRemoved = true;
    coverInput.value = "";
    updateCover();
    coverInput.focus();
});

async function encodeCover() {
    if (!coverInput.files.length) {
        return editing && !coverRemoved ? null : "";
    }

    try {
        await coverPreview.decode();
        const canvas = document.createElement("canvas");
        canvas.width = 840;
        canvas.height = 360;
        const context = canvas.getContext("2d");
        const scale = Math.max(canvas.width / coverPreview.naturalWidth, canvas.height / coverPreview.naturalHeight);
        const width = coverPreview.naturalWidth * scale;
        const height = coverPreview.naturalHeight * scale;
        context.fillStyle = "#fff";
        context.fillRect(0, 0, canvas.width, canvas.height);
        context.drawImage(coverPreview, (canvas.width - width) / 2, (canvas.height - height) / 2, width, height);
        const image = canvas.toDataURL("image/jpeg", 0.8);
        return image.length <= 220_000 ? image : canvas.toDataURL("image/jpeg", 0.6);
    } catch {
        throw new Error("Не удалось подготовить обложку. Выберите другое изображение.");
    }
}

async function loadEventForEditing() {
    document.getElementById("edit-event-back").href = eventPage;
    if (!eventId) {
        setStatus("Событие не выбрано. Вернитесь к списку.");
        return;
    }

    try {
        const response = await fetchApi(api, { cache: "no-store" });
        if (!response.ok) {
            const messages = {
                401: getSessionErrorMessage(),
                403: "Редактировать событие может только организатор.",
                404: "Событие не найдено.",
            };
            setStatus(messages[response.status] ?? "Не удалось загрузить событие.");
            return;
        }

        const details = await response.json();
        if (!details.isOrganizer) {
            setStatus("Редактировать событие может только организатор.");
            return;
        }
        if (details.collectionStatus !== 0) {
            setStatus("Сбор уже открыт. Редактирование недоступно.");
            return;
        }

        savedEvent = details;
        form.elements.title.value = details.title;
        form.elements.description.value = details.description;
        const start = new Date(details.eventStartTime);
        if (Number.isFinite(start.getTime())) {
            savedStartInput = new Date(start.getTime() - start.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
            form.elements.eventStartTime.value = savedStartInput;
        }
        budgetInput.value = String(details.budget);
        previousBudgetInput = budgetInput.value;
        savedCover = canDisplayCover(details.mainImageUrl) ? details.mainImageUrl : "";
        updateCover(undefined, savedCover);
        fields.hidden = false;
        meetingLocation.setLocation(details.eventLocation);
        fields.disabled = false;
        submit.hidden = false;
        submit.disabled = false;
        setStatus();
    } catch {
        setStatus("Не удалось загрузить событие. Обновите страницу.");
    }
}

if (editing) {
    const negativeSign = /[-−]/;
    budgetInput.addEventListener("keydown", event => {
        if (negativeSign.test(event.key)) {
            event.preventDefault();
        }
    });
    budgetInput.addEventListener("beforeinput", event => {
        if (negativeSign.test(event.data ?? "")) {
            event.preventDefault();
        }
    });
    budgetInput.addEventListener("paste", event => {
        if (negativeSign.test(event.clipboardData?.getData("text") ?? "")) {
            event.preventDefault();
        }
    });
    budgetInput.addEventListener("input", () => {
        if (budgetInput.value.startsWith("-")) {
            budgetInput.value = previousBudgetInput;
        } else {
            previousBudgetInput = budgetInput.value;
        }
    });
    void loadEventForEditing();
}

form.addEventListener("submit", async event => {
    event.preventDefault();
    if (submit.disabled) {
        return;
    }

    const values = new FormData(form);
    const title = values.get("title").trim();
    const description = values.get("description").trim();
    const startInput = values.get("eventStartTime");
    const start = new Date(startInput);
    if (!title || !description || !Number.isFinite(start.getTime())) {
        setStatus("Заполните название, описание и дату.");
        return;
    }

    const budgetValue = values.get("budget");
    const budget = editing && budgetValue === "" ? savedEvent.budget : Number(budgetValue);
    if (editing && (!Number.isInteger(budget) || budget < 0 || budget > Number(budgetInput.max))) {
        setStatus("Укажите неотрицательный бюджет в целых рублях.");
        budgetInput.focus();
        return;
    }

    const location = meetingLocation.read();
    if (!location) {
        setStatus("Укажите место встречи.");
        meetingLocation.focus();
        return;
    }

    fields.disabled = true;
    submit.disabled = true;
    submit.textContent = "Сохраняем…";
    setStatus();
    let saved = false;

    try {
        const [eventLocation, mainImageUrl] = await Promise.all([location, encodeCover()]);
        const response = await fetchApi(api, {
            method: editing ? "PATCH" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                title,
                description,
                eventStartTime: editing && startInput === savedStartInput ? savedEvent.eventStartTime : start.toISOString(),
                eventLocation,
                budget,
                mainImageUrl,
            }),
        });
        if (response.status === 401) {
            setStatus(getSessionErrorMessage());
            return;
        }
        if (response.status === 403) {
            setStatus(editing ? "Нет доступа к редактированию события." : "Нет доступа к созданию события.");
            return;
        }
        if (!response.ok) {
            setStatus(editing ? "Не удалось сохранить изменения. Проверьте данные и попробуйте снова."
                : "Не удалось подтвердить сохранение. Перед повтором проверьте «Мои события».");
            return;
        }
        saved = true;
        if (editing) {
            window.location.replace(eventPage);
        } else {
            window.location.assign("/");
        }
    } catch (error) {
        const message = error instanceof TypeError
            ? (editing ? "Не удалось связаться с сервером. Попробуйте ещё раз."
                : "Не удалось связаться с сервером. Перед повтором проверьте «Мои события».")
            : error.message;
        setStatus(message);
    } finally {
        if (!saved) {
            fields.disabled = false;
            submit.disabled = false;
            submit.textContent = "Сохранить";
        }
    }
});
