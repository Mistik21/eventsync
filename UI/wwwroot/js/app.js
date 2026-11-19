import { fetchCurrentUser, getInitials } from "./current-user.js";

window.Telegram?.WebApp?.expand();

async function loadProfileBadge() {
    const badge = document.querySelector(".profile-badge");
    const message = document.querySelector(".auth-error");

    try {
        const response = await fetchCurrentUser();
        if (response.status === 401) {
            message.textContent = "Для входа откройте приложение заново в Telegram.";
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
