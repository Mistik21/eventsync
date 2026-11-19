export function fetchCurrentUser() {
    return fetch("/api/users/me", { cache: "no-store" });
}

export function getInitials(name) {
    const letters = name.normalize().match(/\p{L}\p{M}*/gu) ?? [];
    return letters.slice(0, 2).map(letter => {
        const uppercase = letter.toLocaleUpperCase("ru-RU");
        return /^\p{L}\p{M}*$/u.test(uppercase) ? uppercase : letter;
    }).join("") || "?";
}
