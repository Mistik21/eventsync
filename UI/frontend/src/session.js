let renewal;
let verifiedInitData;

async function signInWithTelegram() {
    if (!window.Telegram?.WebApp) await window.eventSyncTelegram;
    const initData = window.Telegram?.WebApp?.initData;
    if (!initData) {
        return;
    }

    const response = await fetch("/api/auth/telegram", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ initData }),
    });

    if (response.status === 401) {
        return;
    }
    if (!response.ok) {
        throw new Error("Не удалось войти через Telegram.");
    }

    verifiedInitData = initData;
    return initData;
}

function renewSession() {
    renewal ??= signInWithTelegram().finally(() => {
        renewal = undefined;
    });
    return renewal;
}

export async function fetchApi(input, options, initialResponse) {
    const request = new Request(input, options);
    const response = (await initialResponse) ?? await fetch(request.clone());

    if (response.status !== 401) {
        return response;
    }

    const url = new URL(request.url);
    if (url.origin !== window.location.origin || !url.pathname.startsWith("/api/")) {
        return response;
    }

    const initData = verifiedInitData ?? await renewSession();
    if (!initData) {
        return response;
    }

    request.headers.set("X-EventSync-Telegram-Init-Data", initData);
    return fetch(request, { redirect: "error" });
}

export function getSessionErrorMessage() {
    return window.Telegram?.WebApp?.initData
        ? "Не удалось подтвердить вход через Telegram. Попробуйте обновить страницу."
        : "Откройте EventSync кнопкой запуска мини-приложения в боте Telegram.";
}
