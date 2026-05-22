import { chatService } from "../../services/chatService.js";
import { load } from "../../utils/helper.js";
import { openAppModal } from "../../utils/modal.js";
import { loadThreads } from "./threads.js";

const btnNewChat = document.getElementById("newMsgBtn");
const popup = document.getElementById("newChatPopup");
const emailInput = document.getElementById("newChatEmailInput");
const searchBtn = document.getElementById("newChatSearchBtn");
const primaryBtn = document.getElementById("newChatPrimaryBtn");
const resultsBox = document.getElementById("newChatResults");
const statusBox = document.getElementById("newChatStatus");
const groupFields = document.getElementById("newChatGroupFields");
const groupTitleInput = document.getElementById("newChatGroupTitle");
const selectedBox = document.getElementById("newChatSelectedMembers");
const selectedCount = document.getElementById("newChatSelectedCount");

const selectedGroupMembers = new Map();
let chatMode = "direct";
let selectedDirectUser = null;

btnNewChat?.addEventListener("click", () => {
    openNewChatPopup();
});

searchBtn?.addEventListener("click", async () => {
    await searchUser();
});

primaryBtn?.addEventListener("click", async () => {
    if (chatMode === "group") {
        await createGroup();
        return;
    }

    if (selectedDirectUser?.id) {
        await openConversationWithUser(selectedDirectUser.id);
    }
});

emailInput?.addEventListener("keydown", async (event) => {
    if (event.key !== "Enter") return;

    event.preventDefault();
    await searchUser();
});

document.addEventListener("click", async (event) => {
    if (!isNewChatOpen()) return;

    const closeTarget = event.target.closest("[data-new-chat-close]");
    if (closeTarget) {
        closeNewChatPopup();
        return;
    }

    const modeBtn = event.target.closest("[data-new-chat-mode]");
    if (modeBtn) {
        setChatMode(modeBtn.dataset.newChatMode);
        return;
    }

    const selectBtn = event.target.closest("[data-new-chat-select]");
    if (selectBtn) {
        const card = selectBtn.closest("[data-new-chat-user]");
        const user = readUserFromCard(card);
        if (!user) return;

        if (chatMode === "group") {
            addGroupMember(user);
        } else {
            selectDirectUser(user);
        }
        return;
    }

    const profileBtn = event.target.closest("[data-new-chat-profile]");
    if (profileBtn) {
        const card = profileBtn.closest("[data-new-chat-user]");
        const user = readUserFromCard(card);
        if (user?.id) await openPersonalByUserId(user.id);
        return;
    }

    const removeBtn = event.target.closest("[data-new-chat-remove]");
    if (removeBtn) {
        const userId = Number.parseInt(removeBtn.dataset.newChatRemove || "0", 10);
        selectedGroupMembers.delete(userId);
        renderSelectedGroupMembers();
        syncResultSelection();
    }
});

document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && isNewChatOpen()) {
        closeNewChatPopup();
    }
});

await loadThreads();

function openNewChatPopup() {
    selectedDirectUser = null;
    clearStatus();
    setChatMode("direct");
    setResultsEmpty("Nhập email để bắt đầu tìm kiếm.");
    popup.hidden = false;
    popup.setAttribute("aria-hidden", "false");
    document.body.style.overflow = "hidden";

    requestAnimationFrame(() => {
        emailInput?.focus();
    });
}

function closeNewChatPopup() {
    popup.hidden = true;
    popup.setAttribute("aria-hidden", "true");
    document.body.style.overflow = "";
    selectedDirectUser = null;
    selectedGroupMembers.clear();
    if (emailInput) emailInput.value = "";
    if (groupTitleInput) groupTitleInput.value = "";
    renderSelectedGroupMembers();
}

function isNewChatOpen() {
    return popup && !popup.hidden;
}

function setChatMode(mode) {
    chatMode = mode === "group" ? "group" : "direct";
    selectedDirectUser = null;
    clearStatus();

    document.querySelectorAll("[data-new-chat-mode]").forEach((btn) => {
        btn.classList.toggle("is-active", btn.dataset.newChatMode === chatMode);
    });

    groupFields.hidden = chatMode !== "group";
    primaryBtn.textContent = chatMode === "group" ? "Tạo nhóm" : "Mở chat";
    if (emailInput) emailInput.placeholder = chatMode === "group" ? "Email thành viên" : "Nhập email người dùng";
    renderSelectedGroupMembers();
    syncPrimaryButton();
    syncResultSelection();
}

async function searchUser() {
    const email = emailInput?.value?.trim();
    clearStatus();

    if (!email) {
        setStatus("Bạn cần nhập email để tìm kiếm.");
        emailInput?.focus();
        return;
    }

    try {
        load(true);
        const res = await chatService.searchUsersByEmail(email);
        renderSearchResponse(res.data);
        load(false);
    } catch (error) {
        console.error(error);
        load(false);
        setResultsEmpty("Không thể tìm kiếm lúc này. Vui lòng thử lại.");
    }
}

function renderSearchResponse(html) {
    const parserHost = document.createElement("div");
    parserHost.innerHTML = html || "";

    const oldItem = parserHost.querySelector(".form_friends__item[data-user-id]");
    if (!oldItem) {
        const message = parserHost.textContent?.trim() || "Không tìm thấy người dùng nào.";
        setResultsEmpty(message);
        selectedDirectUser = null;
        syncPrimaryButton();
        return;
    }

    const user = {
        id: Number.parseInt(oldItem.dataset.userId || "0", 10),
        name: oldItem.dataset.userName || oldItem.querySelector(".form_friends__name")?.textContent?.trim() || "Người dùng",
        email: oldItem.dataset.userEmail || oldItem.querySelector(".form_friends__sub")?.textContent?.trim() || "",
        photo: oldItem.dataset.userPhoto || oldItem.querySelector("img")?.getAttribute("src") || "/assets/images/avatar-default.png"
    };

    resultsBox.innerHTML = renderUserCard(user);
    syncResultSelection();
}

function renderUserCard(user) {
    const selected = chatMode === "group" && selectedGroupMembers.has(user.id);
    return `
        <article class="chat-create-popup__result${selected ? " is-selected" : ""}"
                 data-new-chat-user
                 data-user-id="${user.id}"
                 data-user-name="${escapeAttr(user.name)}"
                 data-user-email="${escapeAttr(user.email)}"
                 data-user-photo="${escapeAttr(user.photo)}">
            <img class="chat-create-popup__avatar"
                 src="${escapeAttr(user.photo || "/assets/images/avatar-default.png")}"
                 alt=""
                 onerror="this.onerror=null;this.src='/assets/images/avatar-default.png';" />
            <div>
                <div class="chat-create-popup__result-name">${escapeHtml(user.name)}</div>
                <div class="chat-create-popup__result-email">${escapeHtml(user.email)}</div>
            </div>
            <div class="chat-create-popup__result-actions">
                <button class="chat-create-popup__result-action" type="button" data-new-chat-profile>
                    Hồ sơ
                </button>
                <button class="chat-create-popup__result-action is-primary" type="button" data-new-chat-select>
                    ${chatMode === "group" ? (selected ? "Đã chọn" : "Thêm") : "Chọn"}
                </button>
            </div>
        </article>
    `;
}

function readUserFromCard(card) {
    if (!card) return null;

    const id = Number.parseInt(card.dataset.userId || "0", 10);
    if (!id) return null;

    return {
        id,
        name: card.dataset.userName || "Người dùng",
        email: card.dataset.userEmail || "",
        photo: card.dataset.userPhoto || "/assets/images/avatar-default.png"
    };
}

function selectDirectUser(user) {
    selectedDirectUser = user;
    clearStatus();
    syncResultSelection();
    syncPrimaryButton();
}

function addGroupMember(user) {
    selectedGroupMembers.set(user.id, user);
    if (emailInput) {
        emailInput.value = "";
        emailInput.focus();
    }
    renderSelectedGroupMembers();
    syncResultSelection();
    syncPrimaryButton();
}

function renderSelectedGroupMembers() {
    if (!selectedBox || !selectedCount) return;

    selectedCount.textContent = String(selectedGroupMembers.size);

    if (selectedGroupMembers.size === 0) {
        selectedBox.innerHTML = `<div class="chat-create-popup__empty">Chưa chọn thành viên.</div>`;
        return;
    }

    selectedBox.innerHTML = Array.from(selectedGroupMembers.values()).map((member) => `
        <span class="chat-create-popup__chip">
            <img src="${escapeAttr(member.photo || "/assets/images/avatar-default.png")}" alt="" onerror="this.onerror=null;this.src='/assets/images/avatar-default.png';" />
            <span class="chat-create-popup__chip-name">${escapeHtml(member.name)}</span>
            <button class="chat-create-popup__chip-remove"
                    type="button"
                    data-new-chat-remove="${member.id}"
                    aria-label="Bỏ chọn ${escapeAttr(member.name)}">x</button>
        </span>
    `).join("");
}

function syncResultSelection() {
    document.querySelectorAll("[data-new-chat-user]").forEach((card) => {
        const user = readUserFromCard(card);
        const action = card.querySelector("[data-new-chat-select]");
        if (!user || !action) return;

        const selected = chatMode === "group"
            ? selectedGroupMembers.has(user.id)
            : selectedDirectUser?.id === user.id;

        card.classList.toggle("is-selected", selected);
        action.textContent = chatMode === "group"
            ? (selected ? "Đã chọn" : "Thêm")
            : (selected ? "Đã chọn" : "Chọn");
    });
}

function syncPrimaryButton() {
    if (chatMode === "group") {
        primaryBtn.disabled = selectedGroupMembers.size === 0;
        return;
    }

    primaryBtn.disabled = !selectedDirectUser?.id;
}

async function createGroup() {
    const title = groupTitleInput?.value?.trim() || "";
    const memberIds = Array.from(selectedGroupMembers.keys());

    if (memberIds.length === 0) {
        setStatus("Bạn cần chọn ít nhất 1 thành viên để tạo nhóm.");
        return;
    }

    try {
        load(true);
        const res = await chatService.createGroup(title, memberIds);
        closeNewChatPopup();
        await loadThreads();
        load(false);
        selectThread(res?.data?.conversationId);
    } catch (error) {
        console.error(error);
        load(false);
        setStatus("Không thể tạo nhóm. Vui lòng kiểm tra thành viên và thử lại.");
    }
}

async function openConversationWithUser(userId) {
    try {
        load(true);
        const res = await chatService.createDirect(userId);
        closeNewChatPopup();
        await loadThreads();
        load(false);
        selectThread(res?.data?.conversationId);
    } catch (error) {
        console.error(error);
        load(false);
        setStatus("Không thể mở cuộc trò chuyện. Vui lòng thử lại.");
    }
}

async function openPersonalByUserId(userId) {
    if (!userId) return;

    try {
        load(true);
        const res = await chatService.getPersonalView(userId);
        closeNewChatPopup();
        openAppModal(res.data);
        load(false);
    } catch (e) {
        console.log(e);
        load(false);
        setStatus("Không thể tải thông tin cá nhân. Vui lòng thử lại.");
    }
}

function selectThread(conversationId) {
    if (!conversationId) return;

    queueMicrotask(() => {
        document.querySelector(`.thread-item[data-id="${conversationId}"]`)?.click();
    });
}

function setResultsEmpty(message) {
    resultsBox.innerHTML = `<div class="chat-create-popup__empty">${escapeHtml(message)}</div>`;
}

function setStatus(message) {
    statusBox.textContent = message;
    statusBox.hidden = false;
}

function clearStatus() {
    statusBox.textContent = "";
    statusBox.hidden = true;
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
