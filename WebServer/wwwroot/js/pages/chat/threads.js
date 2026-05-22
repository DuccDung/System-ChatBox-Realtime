import { chatService } from "../../services/chatService.js";
import { load } from "../../utils/helper.js";

const threadsContainer = document.getElementById("threadList");
const threadSearchInput = document.getElementById("threadSearch");
const threadTabs = Array.from(document.querySelectorAll(".tabs .tab[data-tab]"));

const FILTER_STORAGE_KEY = "chat.threadFilter";
const UNREAD_STORAGE_PREFIX = "chat.unreadThreads";
const DEFAULT_FILTER = "all";

let currentFilter = readSavedFilter();

export async function loadThreads() {
    if (!threadsContainer) return;

    try {
        load(true);
        const res = await chatService.getThreadsView();
        threadsContainer.innerHTML = res.data;
        hydrateThreadStates();
        applyThreadFilters();
        load(false);
    } catch (e) {
        console.log(e);
        load(false);
        threadsContainer.innerHTML = `<div class="threads-empty">Không tải được danh sách cuộc trò chuyện.</div>`;
    }
}

export function applyThreadFilters() {
    if (!threadsContainer) return;

    syncTabUi();

    const searchTerm = (threadSearchInput?.value || "").trim().toLowerCase();
    const items = getThreadItems();

    for (const item of items) {
        const visible = matchesCurrentFilter(item) && matchesSearch(item, searchTerm);
        item.hidden = !visible;
    }

    syncEmptyState(items);
    window.dispatchEvent(new CustomEvent("threads:visibility-changed"));
}

export function markThreadAsUnread(conversationId) {
    const id = normalizeConversationId(conversationId);
    if (!id) return;

    const unreadIds = getUnreadThreadIds();
    unreadIds.add(id);
    saveUnreadThreadIds(unreadIds);

    const item = getThreadItem(id);
    if (item) {
        item.dataset.unread = "true";
        syncUnreadUi(item);
        applyThreadFilters();
    }
}

export function markThreadAsRead(conversationId) {
    const id = normalizeConversationId(conversationId);
    if (!id) return;

    const unreadIds = getUnreadThreadIds();
    unreadIds.delete(id);
    saveUnreadThreadIds(unreadIds);

    const item = getThreadItem(id);
    if (item) {
        item.dataset.unread = "false";
        syncUnreadUi(item);
        applyThreadFilters();
    }
}

export function updateThreadPreview(conversationId, { snippet, createdAt } = {}) {
    const item = getThreadItem(conversationId);
    if (!item) return;

    if (typeof snippet === "string") {
        item.dataset.snippet = snippet;
        const snippetNode = item.querySelector(".snippet");
        if (snippetNode) snippetNode.textContent = snippet;
    }

    if (createdAt) {
        const parsed = new Date(createdAt);
        if (!Number.isNaN(parsed.getTime())) {
            const timeNode = item.querySelector(".thread-time");
            if (timeNode) timeNode.textContent = toRelativeTime(parsed);
        }
    }
}

export function moveThreadToTop(conversationId) {
    const item = getThreadItem(conversationId);
    if (!item || !threadsContainer) return;

    threadsContainer.prepend(item);
}

function hydrateThreadStates() {
    const unreadIds = getUnreadThreadIds();

    for (const item of getThreadItems()) {
        const id = normalizeConversationId(item.dataset.id);
        item.dataset.unread = unreadIds.has(id) ? "true" : "false";
        syncUnreadUi(item);
    }
}

function syncUnreadUi(item) {
    const unread = item?.dataset.unread === "true";
    if (!item) return;

    item.classList.toggle("unread", unread);

    let badge = item.querySelector(".unread-badge");
    if (unread) {
        if (!badge) {
            badge = document.createElement("span");
            badge.className = "unread-badge";
            badge.setAttribute("aria-hidden", "true");
            item.appendChild(badge);
        }
    } else {
        badge?.remove();
    }
}

function syncTabUi() {
    for (const tab of threadTabs) {
        tab.classList.toggle("active", tab.dataset.tab === currentFilter);
    }
}

function syncEmptyState(items) {
    const visibleCount = items.filter((item) => !item.hidden).length;
    const existing = threadsContainer.querySelector(".threads-filter-empty");

    if (visibleCount > 0 || items.length === 0) {
        existing?.remove();
        return;
    }

    const empty = existing || document.createElement("li");
    empty.className = "threads-empty threads-filter-empty";
    empty.textContent = getEmptyMessage();

    if (!existing) {
        threadsContainer.appendChild(empty);
    }
}

function getEmptyMessage() {
    switch (currentFilter) {
        case "unread":
            return "Không có cuộc trò chuyện chưa đọc.";
        case "groups":
            return "Không có cuộc trò chuyện nhóm.";
        default:
            return "Không tìm thấy cuộc trò chuyện phù hợp.";
    }
}

function matchesCurrentFilter(item) {
    switch (currentFilter) {
        case "unread":
            return item.dataset.unread === "true";
        case "groups":
            return item.dataset.isGroup === "true";
        default:
            return true;
    }
}

function matchesSearch(item, searchTerm) {
    if (!searchTerm) return true;

    const haystack = `${item.dataset.name || ""} ${item.dataset.snippet || ""}`.toLowerCase();
    return haystack.includes(searchTerm);
}

function getThreadItems() {
    return Array.from(threadsContainer?.querySelectorAll(".thread-item[data-id]") || []);
}

function getThreadItem(conversationId) {
    const id = normalizeConversationId(conversationId);
    if (!id) return null;

    return threadsContainer?.querySelector(`.thread-item[data-id="${id}"]`) || null;
}

function getUnreadThreadIds() {
    try {
        return new Set(JSON.parse(window.localStorage.getItem(getUnreadStorageKey()) || "[]"));
    } catch {
        return new Set();
    }
}

function saveUnreadThreadIds(ids) {
    window.localStorage.setItem(getUnreadStorageKey(), JSON.stringify(Array.from(ids)));
}

function getUnreadStorageKey() {
    return `${UNREAD_STORAGE_PREFIX}:${getCurrentUserId()}`;
}

function getCurrentUserId() {
    try {
        const raw = JSON.parse(window.localStorage.getItem("user") || "{}");
        return String(raw.accountId || raw.AccountId || "guest");
    } catch {
        return "guest";
    }
}

function readSavedFilter() {
    const saved = window.localStorage.getItem(FILTER_STORAGE_KEY);
    if (saved === "all" || saved === "unread" || saved === "groups") return saved;
    return DEFAULT_FILTER;
}

function saveCurrentFilter() {
    window.localStorage.setItem(FILTER_STORAGE_KEY, currentFilter);
}

function normalizeConversationId(value) {
    const id = Number.parseInt(String(value || "0"), 10);
    return id > 0 ? id : 0;
}

function toRelativeTime(value) {
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return "";

    const spanSeconds = Math.floor((Date.now() - date.getTime()) / 1000);
    if (spanSeconds < 60) return "Vừa xong";

    const spanMinutes = Math.floor(spanSeconds / 60);
    if (spanMinutes < 60) return `${spanMinutes} phút`;

    const spanHours = Math.floor(spanMinutes / 60);
    if (spanHours < 24) return `${spanHours} giờ`;

    const spanDays = Math.floor(spanHours / 24);
    if (spanDays < 2) return "Hôm qua";
    if (spanDays < 7) return `${spanDays} ngày`;

    return date.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" });
}

threadTabs.forEach((tab) => {
    tab.addEventListener("click", () => {
        const nextFilter = tab.dataset.tab || DEFAULT_FILTER;
        if (nextFilter !== "all" && nextFilter !== "unread" && nextFilter !== "groups") return;

        currentFilter = nextFilter;
        saveCurrentFilter();
        applyThreadFilters();
    });
});

threadSearchInput?.addEventListener("input", () => {
    applyThreadFilters();
});
