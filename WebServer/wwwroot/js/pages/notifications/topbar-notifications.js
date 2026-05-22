import { notificationService } from "../../services/notificationService.js";
import { connectWs } from "../../services/ws-client.js";

const avatarBtn = document.getElementById("avatarBtn");
const avatarDropdown = document.getElementById("avatarDropdown");
const logoutBtn = document.getElementById("logoutBtn");

const notificationMenu = document.getElementById("notificationMenu");
const notificationBtn = document.getElementById("notificationBtn");
const notificationPopup = document.getElementById("notificationPopup");
const notificationBadge = document.getElementById("notificationBadge");
const notificationList = document.getElementById("notificationList");
const notificationSummary = document.getElementById("notificationSummary");
const notificationMarkAllBtn = document.getElementById("notificationMarkAllBtn");

const DEFAULT_AVATAR = "/assets/images/avatar-default.png";
const MAX_NOTIFICATIONS = 50;

let notifications = [];
let unreadCount = 0;
let activeFilter = "all";
let hasLoadedList = false;
let isLoadingList = false;

initAvatarMenu();
initNotifications();

function initAvatarMenu() {
    if (!avatarBtn || !avatarDropdown) return;

    avatarBtn.addEventListener("click", (event) => {
        event.stopPropagation();
        closeNotifications();
        setAvatarOpen(avatarDropdown.hidden);
    });

    logoutBtn?.addEventListener("click", () => {
        setAvatarOpen(false);
        window.location.href = "/Auth/Logout";
    });
}

function initNotifications() {
    if (!notificationMenu || !notificationBtn || !notificationPopup || !notificationList) return;

    connectWs();
    loadUnreadCount();

    notificationBtn.addEventListener("click", async (event) => {
        event.stopPropagation();
        setAvatarOpen(false);

        if (notificationPopup.hidden) {
            openNotifications();
            return;
        }

        closeNotifications();
    });

    notificationMarkAllBtn?.addEventListener("click", async (event) => {
        event.stopPropagation();
        await markAllAsRead();
    });

    notificationPopup.addEventListener("click", async (event) => {
        event.stopPropagation();

        const filterBtn = event.target.closest("[data-notification-filter]");
        if (filterBtn) {
            setFilter(filterBtn.dataset.notificationFilter || "all");
            return;
        }

        const item = event.target.closest("[data-notification-id]");
        if (!item) return;

        const id = Number.parseInt(item.dataset.notificationId || "0", 10);
        const conversationId = Number.parseInt(item.dataset.conversationId || "0", 10);
        await markOneAsRead(id);
        openConversation(conversationId);
    });

    window.addEventListener("ws:message", (event) => {
        const message = event.detail;
        if (message?.type !== "notification") return;

        const notification = normalizeNotification(message.payload);
        if (!notification.id) return;

        upsertNotification(notification, { atTop: true });
        setUnreadCount(unreadCount + (notification.isRead ? 0 : 1));
        renderNotifications();
        pulseNotificationButton();
    });
}

document.addEventListener("click", (event) => {
    if (!event.target.closest(".avatar-menu")) {
        setAvatarOpen(false);
    }

    if (!event.target.closest("#notificationMenu")) {
        closeNotifications();
    }
});

document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape") return;

    setAvatarOpen(false);
    closeNotifications();
});

async function openNotifications() {
    notificationPopup.hidden = false;
    notificationBtn.setAttribute("aria-expanded", "true");

    if (!hasLoadedList) {
        await loadNotifications();
    } else {
        renderNotifications();
    }
}

function closeNotifications() {
    if (!notificationPopup || notificationPopup.hidden) return;

    notificationPopup.hidden = true;
    notificationBtn?.setAttribute("aria-expanded", "false");
}

function setAvatarOpen(isOpen) {
    if (!avatarBtn || !avatarDropdown) return;

    avatarDropdown.hidden = !isOpen;
    avatarBtn.setAttribute("aria-expanded", String(isOpen));
}

async function loadUnreadCount() {
    try {
        setUnreadCount(await notificationService.getUnreadCount());
    } catch (error) {
        console.error("Load notification unread count failed", error);
    }
}

async function loadNotifications() {
    if (isLoadingList) return;

    isLoadingList = true;
    renderLoading();

    try {
        const data = await notificationService.getNotifications({
            limit: MAX_NOTIFICATIONS,
            unreadOnly: false
        });

        notifications = data.map(normalizeNotification).filter((item) => item.id);
        hasLoadedList = true;
        renderNotifications();
    } catch (error) {
        console.error("Load notifications failed", error);
        renderError();
    } finally {
        isLoadingList = false;
    }
}

async function markOneAsRead(id) {
    if (!id) return;

    const current = notifications.find((item) => item.id === id);
    if (!current || current.isRead) return;

    current.isRead = true;
    setUnreadCount(Math.max(0, unreadCount - 1));
    renderNotifications();

    try {
        const updated = normalizeNotification(await notificationService.markRead(id));
        upsertNotification(updated);
        renderNotifications();
    } catch (error) {
        current.isRead = false;
        setUnreadCount(unreadCount + 1);
        renderNotifications();
        console.error("Mark notification read failed", error);
    }
}

async function markAllAsRead() {
    if (unreadCount === 0) return;

    const previous = notifications.map((item) => ({ id: item.id, isRead: item.isRead }));

    notifications = notifications.map((item) => ({ ...item, isRead: true }));
    setUnreadCount(0);
    renderNotifications();

    try {
        await notificationService.markAllRead();
    } catch (error) {
        previous.forEach((state) => {
            const item = notifications.find((notification) => notification.id === state.id);
            if (item) item.isRead = state.isRead;
        });
        setUnreadCount(notifications.filter((item) => !item.isRead).length);
        renderNotifications();
        console.error("Mark all notifications read failed", error);
    }
}

function setFilter(filter) {
    activeFilter = filter === "unread" ? "unread" : "all";

    notificationPopup.querySelectorAll("[data-notification-filter]").forEach((button) => {
        button.classList.toggle("is-active", button.dataset.notificationFilter === activeFilter);
    });

    renderNotifications();
}

function renderNotifications() {
    if (!notificationList) return;

    const visible = activeFilter === "unread"
        ? notifications.filter((item) => !item.isRead)
        : notifications;

    updateSummary();

    if (visible.length === 0) {
        notificationList.innerHTML = `
            <div class="notification-popup__state">
                ${activeFilter === "unread" ? "Không có thông báo chưa đọc." : "Chưa có thông báo nào."}
            </div>
        `;
        return;
    }

    notificationList.innerHTML = visible.map(renderNotificationItem).join("");
}

function renderNotificationItem(notification) {
    const unreadClass = notification.isRead ? "" : " is-unread";
    const conversationAttr = notification.conversationId ? ` data-conversation-id="${notification.conversationId}"` : "";

    return `
        <button class="notification-item${unreadClass}"
                type="button"
                data-notification-id="${notification.id}"${conversationAttr}>
            <img class="notification-item__avatar"
                 src="${escapeAttr(notification.senderPhotoPath || DEFAULT_AVATAR)}"
                 alt=""
                 onerror="this.onerror=null;this.src='${DEFAULT_AVATAR}';" />
            <span class="notification-item__body">
                <span class="notification-item__content">${escapeHtml(notification.content)}</span>
                <span class="notification-item__meta">
                    ${escapeHtml(getNotificationLabel(notification.type))}
                    <span aria-hidden="true">·</span>
                    ${escapeHtml(relativeTime(notification.date))}
                </span>
            </span>
            <span class="notification-item__dot" aria-hidden="true"></span>
        </button>
    `;
}

function renderLoading() {
    if (!notificationList) return;

    updateSummary("Đang tải...");
    notificationList.innerHTML = `
        <div class="notification-popup__state">
            Đang tải thông báo...
        </div>
    `;
}

function renderError() {
    if (!notificationList) return;

    updateSummary("Không tải được thông báo");
    notificationList.innerHTML = `
        <div class="notification-popup__state is-error">
            Không tải được thông báo. Vui lòng thử lại.
        </div>
    `;
}

function updateSummary(customText = null) {
    if (!notificationSummary) return;

    if (customText) {
        notificationSummary.textContent = customText;
        return;
    }

    notificationSummary.textContent = unreadCount > 0
        ? `${unreadCount} thông báo chưa đọc`
        : "Bạn đã đọc hết thông báo";

    if (notificationMarkAllBtn) {
        notificationMarkAllBtn.disabled = unreadCount === 0;
    }
}

function setUnreadCount(nextCount) {
    unreadCount = Math.max(0, Number.parseInt(String(nextCount || 0), 10));

    if (!notificationBadge) return;

    if (unreadCount === 0) {
        notificationBadge.hidden = true;
        notificationBadge.textContent = "0";
        return;
    }

    notificationBadge.hidden = false;
    notificationBadge.textContent = unreadCount > 99 ? "99+" : String(unreadCount);
}

function upsertNotification(notification, options = {}) {
    if (!notification.id) return;

    const existingIndex = notifications.findIndex((item) => item.id === notification.id);
    if (existingIndex >= 0) {
        notifications[existingIndex] = { ...notifications[existingIndex], ...notification };
        return;
    }

    if (options.atTop) {
        notifications.unshift(notification);
    } else {
        notifications.push(notification);
    }

    notifications = notifications
        .sort((a, b) => new Date(b.date || 0) - new Date(a.date || 0) || b.id - a.id)
        .slice(0, MAX_NOTIFICATIONS);
}

function normalizeNotification(value) {
    const raw = value?.notification || value || {};

    return {
        id: Number(raw.id ?? raw.Id ?? 0),
        type: String(raw.type ?? raw.Type ?? ""),
        content: String(raw.content ?? raw.Content ?? ""),
        senderId: Number(raw.senderId ?? raw.SenderId ?? 0),
        senderName: String(raw.senderName ?? raw.SenderName ?? ""),
        senderPhotoPath: String(raw.senderPhotoPath ?? raw.SenderPhotoPath ?? DEFAULT_AVATAR),
        consumerId: Number(raw.consumerId ?? raw.ConsumerId ?? 0),
        date: raw.date ?? raw.Date ?? null,
        isRead: Boolean(raw.isRead ?? raw.IsRead ?? false),
        conversationId: Number(raw.conversationId ?? raw.ConversationId ?? 0) || null
    };
}

function getNotificationLabel(type) {
    if (type === "chat.message") return "Tin nhắn";
    return "Thông báo";
}

function openConversation(conversationId) {
    if (!conversationId) return;

    closeNotifications();

    const threadItem = document.querySelector(`.thread-item[data-id="${conversationId}"]`);
    if (threadItem) {
        threadItem.click();
        return;
    }

    window.sessionStorage.setItem("chat:openConversationId", String(conversationId));

    if (!location.pathname.toLowerCase().includes("/home/main")) {
        window.location.href = "/Home/Main";
        return;
    }

    window.dispatchEvent(new CustomEvent("chat:open-conversation", {
        detail: { conversationId }
    }));
}

function pulseNotificationButton() {
    if (!notificationBtn) return;

    notificationBtn.classList.remove("has-new-notification");
    void notificationBtn.offsetWidth;
    notificationBtn.classList.add("has-new-notification");
}

function relativeTime(value) {
    if (!value) return "Vừa xong";

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "Vừa xong";

    const diffSeconds = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000));
    if (diffSeconds < 60) return "Vừa xong";

    const diffMinutes = Math.floor(diffSeconds / 60);
    if (diffMinutes < 60) return `${diffMinutes} phút trước`;

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) return `${diffHours} giờ trước`;

    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 7) return `${diffDays} ngày trước`;

    return date.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric"
    });
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function escapeAttr(value) {
    return escapeHtml(value);
}
