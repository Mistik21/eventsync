import { fetchCurrentUser, getInitials } from "./current-user.js";

async function loadProfile() {
    const status = document.querySelector("#profile-status");

    try {
        const response = await fetchCurrentUser();
        if (response.status === 401) {
            status.textContent = "Сессия завершилась. Вернитесь на главную, чтобы войти.";
            return;
        }
        if (response.status === 404) {
            status.textContent = "Профиль не найден.";
            return;
        }
        if (!response.ok) {
            throw new Error("Не удалось загрузить профиль.");
        }

        const { name, attendedEventCount } = await response.json();
        document.querySelector("#profile-avatar").textContent = getInitials(name);
        document.querySelector("#profile-name").textContent = name;
        document.querySelector("#profile-attended-count").textContent = attendedEventCount;
        document.querySelector("#profile-details").hidden = false;
        status.hidden = true;
    } catch {
        status.textContent = "Не удалось загрузить профиль.";
    }
}

void loadProfile();
