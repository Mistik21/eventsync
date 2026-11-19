import { fetchApi } from "./session.js";

const form = document.querySelector(".create-form");
const fields = form.querySelector("fieldset");
const submit = form.querySelector(".create-submit");
const status = form.querySelector(".create-status");
const coverInput = form.querySelector("#event-cover");
const coverPicker = form.querySelector(".cover-picker");
const coverPreview = form.querySelector(".cover-preview");
const coverCaption = form.querySelector(".cover-caption");
const coverRemove = form.querySelector(".cover-remove");
const location = form.querySelector(".event-coordinates");
let previewUrl;

function setStatus(message = "", isError = true) {
    status.textContent = message;
    status.hidden = !message;
    status.classList.toggle("create-status--error", isError);
}

function updateCover(file) {
    if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
    }
    previewUrl = file ? URL.createObjectURL(file) : undefined;
    if (previewUrl) {
        coverPreview.src = previewUrl;
    } else {
        coverPreview.removeAttribute("src");
    }
    coverPreview.hidden = !previewUrl;
    coverRemove.hidden = !previewUrl;
    coverPicker.classList.toggle("cover-picker--filled", Boolean(previewUrl));
    coverCaption.textContent = file ? "Заменить обложку" : "Добавить обложку";
}

coverInput.addEventListener("change", () => {
    setStatus();
    updateCover(coverInput.files[0]);
});

coverPreview.addEventListener("error", () => {
    if (!coverInput.files.length) {
        return;
    }
    coverInput.value = "";
    updateCover();
    setStatus("Не удалось открыть обложку. Выберите JPEG, PNG или WebP.");
});

coverRemove.addEventListener("click", () => {
    coverInput.value = "";
    updateCover();
    coverInput.focus();
});

async function encodeCover() {
    if (!coverInput.files.length) {
        return "";
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

function readCoordinates(value) {
    if (!value.trim()) {
        return "";
    }
    const parts = value.split(",").map(part => part.trim());
    const [latitude, longitude] = parts.map(Number);
    if (parts.length !== 2 || parts.some(part => !/^[+-]?\d+(\.\d+)?$/.test(part)) ||
        Math.abs(latitude) > 90 || Math.abs(longitude) > 180) {
        throw new Error("Введите широту и долготу через запятую, например 56.8389, 60.6057.");
    }
    return `${latitude}, ${longitude}`;
}

form.addEventListener("submit", async event => {
    event.preventDefault();
    if (submit.disabled) {
        return;
    }

    const values = new FormData(form);
    const title = values.get("title").trim();
    const description = values.get("description").trim();
    const address = values.get("address").trim();
    const start = new Date(values.get("eventStartTime"));
    if (!title || !description || !address || !Number.isFinite(start.getTime())) {
        setStatus("Заполните название, описание, дату и место встречи.");
        return;
    }

    let coordinates;
    try {
        coordinates = readCoordinates(values.get("coordinates"));
    } catch (error) {
        location.open = true;
        form.querySelector("#event-coordinates").focus();
        setStatus(error.message);
        return;
    }

    fields.disabled = true;
    submit.disabled = true;
    submit.textContent = "Сохраняем…";
    setStatus();
    let saved = false;

    try {
        const mainImageUrl = await encodeCover();
        const response = await fetchApi("/api/events", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                title,
                description,
                eventStartTime: start.toISOString(),
                eventLocation: { address, coordinates },
                budget: Number(values.get("budget")),
                mainImageUrl,
            }),
        });
        if (response.status === 401 || response.status === 403) {
            setStatus("Не удалось восстановить сессию. Откройте приложение заново в Telegram.");
            return;
        }
        if (!response.ok) {
            setStatus("Не удалось подтвердить сохранение. Перед повтором проверьте «Мои события».");
            return;
        }
        saved = true;
        window.location.assign("/");
    } catch (error) {
        const message = error instanceof TypeError
            ? "Не удалось связаться с сервером. Перед повтором проверьте «Мои события»."
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
