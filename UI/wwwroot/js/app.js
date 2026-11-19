import { fetchCurrentUser, getInitials } from "./current-user.js";

async function signInWithTelegram() {
    const telegram = window.Telegram?.WebApp;
    telegram?.ready();
    telegram?.expand();

    const initData = telegram?.initData;
    if (!initData) {
        return;
    }

    const session = await fetch("/api/auth/session", { cache: "no-store" });
    if (session.ok) {
        return;
    }
    if (session.status !== 401) {
        throw new Error("Не удалось проверить сессию.");
    }

    const login = await fetch("/api/auth/telegram", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ initData }),
    });
    if (!login.ok) {
        throw new Error("Не удалось войти через Telegram.");
    }
}

async function loadProfileBadge() {
    const badge = document.querySelector(".profile-badge");
    const message = document.querySelector(".auth-error");

    try {
        await signInWithTelegram();
    } catch {
        message.textContent = "Не удалось войти через Telegram. Откройте приложение ещё раз.";
        message.hidden = false;
        return;
    }

    try {
        const response = await fetchCurrentUser();
        if (response.status === 401) {
            message.textContent = "Откройте приложение в Telegram, чтобы войти.";
            message.hidden = false;
            return;
        }
        if (!response.ok) {
            throw new Error("Не удалось загрузить профиль.");
        }

        const { name } = await response.json();
        badge.textContent = getInitials(name);
        badge.href = "/profile";
        badge.removeAttribute("aria-disabled");
    } catch {
        message.textContent = "Не удалось загрузить профиль.";
        message.hidden = false;
    }
}

void loadProfileBadge();
