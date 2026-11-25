import { profileUrl, useResource } from "./api";
import { Back, ResourceStatus } from "./ui";
import { getInitials } from "./current-user.js";
import "../../wwwroot/css/profile.css";

export default function Profile() {
    const resource = useResource(profileUrl);
    const profile = resource.data;
    return <main className="profile-screen">
        <header className="profile-header"><Back to="/" className="profile-home" label="Вернуться к моим событиям" /><h1>Профиль</h1></header>
        <ResourceStatus resource={resource} url={profileUrl} className="profile-status" loading="Загружаем профиль…" fallback="Не удалось загрузить профиль." />
        {profile && <>
            <section className="profile-card" aria-labelledby="profile-name">
                <div className="profile-avatar" aria-hidden="true">{getInitials(profile.name)}</div><h2 id="profile-name">{profile.name}</h2>
            </section>
            <dl className="profile-stats" aria-label="Статистика мероприятий">
                <div className="profile-stat"><dt>Посещено</dt><dd>{profile.attendedEventCount}</dd></div>
                <div className="profile-stat"><dt>Организовано</dt><dd>{profile.attendedOrganizerCount ?? "—"}</dd></div>
            </dl>
        </>}
    </main>;
}
