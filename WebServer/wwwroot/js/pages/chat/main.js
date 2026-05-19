import { chatService } from "../../services/chatService.js";
import { load } from "../../utils/helper.js";
import { openAppModal, closeAppModal } from "../../utils/modal.js";
import { loadThreads } from "./threads.js";

const btnNewChat = document.getElementById("newMsgBtn");
const selectedGroupMembers = new Map();
let chatMode = "direct";

btnNewChat?.addEventListener("click", async () => {
    try {
        load(true);
        const res = await chatService.getFormSearch();
        load(false);

        selectedGroupMembers.clear();
        chatMode = "direct";
        openAppModal(res.data);

        queueMicrotask(() => {
            wireCreateChatModal();
            document.getElementById("friendEmailInput")?.focus();
        });
    } catch (error) {
        console.log(error);
        load(false);
        alert("Đã có lỗi xảy ra, vui lòng thử lại sau.");
    }
});

await loadThreads();

function isFriendsModalOpen() {
    return !!document.querySelector(".form_friends");
}

function closeFriendsModal() {
    selectedGroupMembers.clear();
    closeAppModal();
}

function wireCreateChatModal() {
    setChatMode(chatMode);
    renderSelectedGroupMembers();
}

function setChatMode(mode) {
    chatMode = mode === "group" ? "group" : "direct";

    document.querySelectorAll("[data-chat-mode]").forEach((btn) => {
        btn.classList.toggle("is-active", btn.dataset.chatMode === chatMode);
    });

    const groupFields = document.getElementById("groupFields");
    const searchBtn = document.getElementById("friendSearchBtn");
    const createBtn = document.getElementById("groupCreateBtn");
    const title = document.getElementById("formFriendsTitle");
    const emailInput = document.getElementById("friendEmailInput");
    const results = document.getElementById("friendResults");

    if (groupFields) groupFields.hidden = chatMode !== "group";
    if (searchBtn) searchBtn.hidden = chatMode === "group";
    if (createBtn) createBtn.hidden = chatMode !== "group";
    if (title) title.textContent = chatMode === "group" ? "Tạo nhóm chat" : "Tạo đoạn chat";
    if (emailInput) emailInput.placeholder = chatMode === "group" ? "Email thành viên" : "Email";

    if (results && chatMode === "group" && !results.querySelector(".form_friends__item")) {
        results.innerHTML = `<div class="form_friends__empty">Tìm email rồi chọn thành viên để thêm vào nhóm.</div>`;
    }
}

async function doSearch() {
    const input = document.getElementById("friendEmailInput");
    const results = document.getElementById("friendResults");
    if (!input || !results) return;

    const email = input.value?.trim();
    if (!email) {
        results.innerHTML = `<div class="form_friends__empty">Nhập email để tìm kiếm.</div>`;
        return;
    }

    try {
        load(true);
        const res = await chatService.searchUsersByEmail(email);
        results.innerHTML = res.data;
        markSelectedResult();
        load(false);
    } catch (e) {
        console.log(e);
        results.innerHTML = `<div class="form_friends__empty">Có lỗi khi tìm kiếm.</div>`;
        load(false);
    }
}

function addGroupMemberFromItem(item) {
    const userId = Number.parseInt(item.dataset.userId || "0", 10);
    if (!userId) return;

    selectedGroupMembers.set(userId, {
        id: userId,
        name: item.dataset.userName || item.querySelector(".form_friends__name")?.textContent?.trim() || "Người dùng",
        email: item.dataset.userEmail || item.querySelector(".form_friends__sub")?.textContent?.trim() || "",
        photo: item.dataset.userPhoto || item.querySelector("img")?.getAttribute("src") || "/assets/images/avatar-default.png"
    });

    item.classList.add("is-selected");
    renderSelectedGroupMembers();

    const input = document.getElementById("friendEmailInput");
    if (input) {
        input.value = "";
        input.focus();
    }
}

function renderSelectedGroupMembers() {
    const box = document.getElementById("groupSelectedMembers");
    const count = document.getElementById("groupSelectedCount");
    if (!box || !count) return;

    count.textContent = String(selectedGroupMembers.size);

    if (selectedGroupMembers.size === 0) {
        box.innerHTML = `<div class="form_friends__empty">Chưa chọn thành viên.</div>`;
        return;
    }

    box.innerHTML = Array.from(selectedGroupMembers.values()).map(member => `
        <div class="form_friends__selected_chip" data-selected-member="${member.id}">
            <img src="${escapeAttr(member.photo)}" alt="" onerror="this.onerror=null;this.src='/assets/images/avatar-default.png';" />
            <span>${escapeHtml(member.name)}</span>
            <button class="form_friends__selected_remove"
                    type="button"
                    data-remove-selected="${member.id}"
                    aria-label="Bỏ chọn ${escapeAttr(member.name)}">×</button>
        </div>
    `).join("");
}

function markSelectedResult() {
    document.querySelectorAll(".form_friends__item[data-user-id]").forEach((item) => {
        const userId = Number.parseInt(item.dataset.userId || "0", 10);
        item.classList.toggle("is-selected", selectedGroupMembers.has(userId));
    });
}

async function createGroup() {
    const titleInput = document.getElementById("groupTitleInput");
    const title = titleInput?.value?.trim() || "";
    const memberIds = Array.from(selectedGroupMembers.keys());

    if (memberIds.length === 0) {
        alert("Bạn cần chọn ít nhất 1 thành viên để tạo nhóm.");
        document.getElementById("friendEmailInput")?.focus();
        return;
    }

    try {
        load(true);
        const res = await chatService.createGroup(title, memberIds);
        closeFriendsModal();
        await loadThreads();
        load(false);

        const conversationId = res?.data?.conversationId;
        if (conversationId) {
            queueMicrotask(() => {
                document.querySelector(`.thread-item[data-id="${conversationId}"]`)?.click();
            });
        }
    } catch (error) {
        console.error(error);
        load(false);
        alert("Không thể tạo nhóm. Vui lòng kiểm tra thành viên và thử lại.");
    }
}

async function openPersonalByUserId(userId) {
    if (!userId) return;

    try {
        load(true);
        const res = await chatService.getPersonalView(userId);
        openAppModal(res.data);
        load(false);
    } catch (e) {
        console.log(e);
        load(false);
        alert("Không thể tải thông tin cá nhân. Vui lòng thử lại.");
    }
}

document.addEventListener("click", async (e) => {
    if (!isFriendsModalOpen()) return;

    const modeBtn = e.target.closest("[data-chat-mode]");
    if (modeBtn) {
        setChatMode(modeBtn.dataset.chatMode);
        return;
    }

    if (e.target.closest(".form_friends__icon_btn")) {
        closeFriendsModal();
        return;
    }

    if (e.target.closest(".form_friends__btn--ghost")) {
        closeFriendsModal();
        return;
    }

    if (e.target.closest("#friendSearchBtn")) {
        await doSearch();
        return;
    }

    if (e.target.closest("#groupCreateBtn")) {
        await createGroup();
        return;
    }

    const removeSelected = e.target.closest("[data-remove-selected]");
    if (removeSelected) {
        const id = Number.parseInt(removeSelected.dataset.removeSelected || "0", 10);
        selectedGroupMembers.delete(id);
        renderSelectedGroupMembers();
        markSelectedResult();
        return;
    }

    const item = e.target.closest(".form_friends__item[data-user-id]");
    if (item) {
        const userId = item.dataset.userId;
        if (chatMode === "group") {
            addGroupMemberFromItem(item);
        } else {
            openPersonalByUserId(userId);
        }
    }
});

document.addEventListener("keydown", async (e) => {
    if (!isFriendsModalOpen()) return;

    if (e.key === "Escape") {
        closeFriendsModal();
        return;
    }

    if (e.key === "Enter") {
        const active = document.activeElement;
        if (active?.id === "friendEmailInput") {
            e.preventDefault();
            await doSearch();
        }

        if (active?.id === "groupTitleInput") {
            e.preventDefault();
            await createGroup();
        }
    }
});

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
