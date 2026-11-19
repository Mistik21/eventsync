let renewal;
let sessionVersion = 0;

window.Telegram?.WebApp?.ready();

async function signInWithTelegram() {
    const initData = window.Telegram?.WebApp?.initData;
    if (!initData) {
        return false;
    }

    const response = await fetch("/api/auth/telegram", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ initData }),
    });

    if (response.status === 401) {
        return false;
    }
    if (!response.ok) {
        throw new Error("Не удалось войти через Telegram.");
    }

    sessionVersion++;
    return true;
}

function renewSession() {
    renewal ??= signInWithTelegram().finally(() => {
        renewal = undefined;
    });
    return renewal;
}

export async function fetchApi(input, options) {
    const request = new Request(input, options);
    const version = sessionVersion;
    const response = await fetch(request.clone());

    if (response.status !== 401) {
        return response;
    }

    if (version === sessionVersion && !(await renewSession())) {
        return response;
    }

    return fetch(request);
}
