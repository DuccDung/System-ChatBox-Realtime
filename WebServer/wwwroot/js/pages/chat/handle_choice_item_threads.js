import { chatService } from "../../services/chatService.js";
import { subscribeConversation } from "../../services/ws-client.js";
import { openAppModal } from "../../utils/modal.js";
import { markThreadAsRead, markThreadAsUnread } from "./threads.js";

const threadList = document.getElementById("threadList");
const peerName = document.getElementById("peerName");
const peerAvatar = document.getElementById("peerAvatar");
const peerStatus = document.getElementById("peerStatus");
const btnGroupMembers = document.getElementById("btnGroupMembers");
const btnVoiceCall = document.getElementById("btnVoiceCall");
const btnVideoCall = document.getElementById("btnVideoCall");

console.log("threads-ui loaded", { threadList: !!threadList });

if (!threadList) {
    console.warn("Thread list not found (#threadList)");
} else {
    threadList.addEventListener("click", async (e) => {
        console.log("threadList click", e.target);

        const moreBtn = e.target.closest(".more-btn");
        if (moreBtn) {
            e.stopPropagation();
            const item = moreBtn.closest(".thread-item");
            if (!item) return;

            const menu = item.querySelector(".thread-menu");
            if (!menu) return;

            closeAllMenusExcept(menu);
            menu.hidden = !menu.hidden;
            return;
        }

        const groupMembersAction = e.target.closest(".js-group-members");
        if (groupMembersAction) {
            e.stopPropagation();
            const item = groupMembersAction.closest(".thread-item");
            closeAllMenusExcept(null);
            await openGroupMembers(item?.dataset.id);
            return;
        }

        const item = e.target.closest(".thread-item");
        if (!item || !threadList.contains(item)) return;

        closeAllMenusExcept(null);
        await openThread(item);
    });

    document.addEventListener("click", (e) => {
        if (!e.target.closest("#threadList") && !e.target.closest(".thread-list")) {
            closeAllMenusExcept(null);
        }
    });

    autoOpenFirstThreadWhenReady();
}

function closeAllMenusExcept(menuToKeep) {
    document.querySelectorAll(".thread-menu").forEach((m) => {
        if (m !== menuToKeep) m.hidden = true;
    });
}

function setActiveItem(item) {
    document.querySelectorAll(".thread-item.active").forEach((li) => li.classList.remove("active"));
    item.classList.add("active");
}

async function openThread(item) {
    const conversationId = item.dataset.id;
    if (!conversationId) return;

    const isGroup = item.dataset.isGroup === "true";
    const memberCount = Number.parseInt(item.dataset.memberCount || "0", 10);

    setActiveItem(item);

    if (peerName) peerName.textContent = item.dataset.name || "Người dùng";
    if (peerAvatar) peerAvatar.src = item.dataset.avatar || peerAvatar.src;
    if (peerStatus) peerStatus.textContent = isGroup && memberCount > 0 ? `${memberCount} thành viên` : "";

    if (btnGroupMembers) {
        btnGroupMembers.hidden = !isGroup;
        btnGroupMembers.dataset.conversationId = conversationId;
    }

    if (btnVoiceCall) btnVoiceCall.hidden = isGroup;
    if (btnVideoCall) btnVideoCall.hidden = isGroup;

    await loadMessages(conversationId);
    markThreadAsRead(conversationId, { applyFilters: false });
    await markConversationRead(conversationId);
    subscribeConversation(parseInt(conversationId, 10));
}

async function markConversationRead(conversationId) {
    try {
        await chatService.markRead(conversationId);
        markThreadAsRead(conversationId, { applyFilters: false });
    } catch (err) {
        console.error("mark read failed", err);
    }
}

async function openGroupMembers(conversationId) {
    if (!conversationId) return;

    try {
        const res = await chatService.getGroupMembersView(conversationId);
        openAppModal(res.data);
    } catch (err) {
        console.error(err);
        alert("Không thể tải danh sách thành viên nhóm.");
    }
}

btnGroupMembers?.addEventListener("click", async () => {
    await openGroupMembers(btnGroupMembers.dataset.conversationId);
});

document.addEventListener("click", async (e) => {
    const removeBtn = e.target.closest(".group_members__remove[data-remove-member]");
    if (!removeBtn) return;

    const modal = removeBtn.closest(".group_members");
    const conversationId = modal?.dataset.conversationId;
    const memberId = removeBtn.dataset.removeMember;
    const memberName = removeBtn.dataset.memberName || "thành viên này";

    if (!conversationId || !memberId) return;
    if (!confirm(`Xóa ${memberName} khỏi nhóm?`)) return;

    try {
        removeBtn.disabled = true;
        removeBtn.textContent = "Đang xóa...";

        await chatService.removeGroupMember(conversationId, memberId);
        await openGroupMembers(conversationId);

        const activeItem = document.querySelector(`.thread-item.active[data-id="${conversationId}"]`);
        if (activeItem?.dataset.memberCount) {
            const nextCount = Math.max(0, Number.parseInt(activeItem.dataset.memberCount, 10) - 1);
            activeItem.dataset.memberCount = String(nextCount);
            if (peerStatus) peerStatus.textContent = nextCount > 0 ? `${nextCount} thành viên` : "";
        }
    } catch (err) {
        console.error(err);
        alert("Không thể xóa thành viên. Chỉ trưởng nhóm mới có quyền thực hiện.");
        removeBtn.disabled = false;
        removeBtn.textContent = "Xóa";
    }
});

document.addEventListener("click", async (e) => {
    const markUnreadAction = e.target.closest(".thread-menu .js-mark-unread");
    if (!markUnreadAction) return;

    e.stopPropagation();

    const item = markUnreadAction.closest(".thread-item[data-id]");
    if (!item) return;

    if (item.dataset.unread === "true") {
        markThreadAsRead(item.dataset.id);
        try {
            await chatService.markRead(item.dataset.id);
        } catch (err) {
            console.error("mark read failed", err);
        }
    } else {
        markThreadAsUnread(item.dataset.id);
    }

    item.querySelector(".thread-menu")?.setAttribute("hidden", "");
});

    window.addEventListener("threads:visibility-changed", async () => {
        if (!threadList) return;

        const activeItem = threadList.querySelector(".thread-item.active");
        if (activeItem && !activeItem.hidden) return;

    activeItem?.classList.remove("active");

    const firstVisibleItem = Array.from(threadList.querySelectorAll(".thread-item[data-id]"))
        .find((item) => !item.hidden);

    if (firstVisibleItem) {
            await openThread(firstVisibleItem);
        }
    });

    window.addEventListener("chat:open-conversation", async (event) => {
        const conversationId = Number.parseInt(String(event.detail?.conversationId || "0"), 10);
        if (!conversationId) return;

        const item = threadList.querySelector(`.thread-item[data-id="${conversationId}"]`);
        if (item) {
            await openThread(item);
        }
    });

    async function loadMessages(conversationId) {
    const scroller = document.getElementById("messageScroller");
    if (!scroller) return;

    scroller.innerHTML = `<div class="loading">Đang tải...</div>`;

    try {
        const res = await chatService.getConversationView(conversationId);
        const html = res.data;

        const temp = document.createElement("div");
        temp.innerHTML = html;

        const newSection = temp.querySelector("#messageScroller");
        scroller.innerHTML = newSection ? newSection.innerHTML : html;
        if (newSection?.dataset.readReceiptAvatar) {
            scroller.dataset.readReceiptAvatar = newSection.dataset.readReceiptAvatar;
        } else {
            delete scroller.dataset.readReceiptAvatar;
        }
        if (newSection?.dataset.conversationId) {
            scroller.dataset.conversationId = newSection.dataset.conversationId;
        }
        if (newSection?.dataset.meId) {
            scroller.dataset.meId = newSection.dataset.meId;
        }

        scroller.scrollTop = scroller.scrollHeight;
    } catch (err) {
        console.error(err);
        scroller.innerHTML = `<div class="error">Không tải được tin nhắn.</div>`;
    }
}

function autoOpenFirstThreadWhenReady() {
    const maxWaitMs = 5000;
    const intervalMs = 50;
    const start = Date.now();

    const timer = setInterval(async () => {
        const pendingConversationId = Number.parseInt(window.sessionStorage.getItem("chat:openConversationId") || "0", 10);
        if (pendingConversationId) {
            const pendingItem = threadList?.querySelector(`.thread-item[data-id="${pendingConversationId}"]`);
            if (pendingItem && !pendingItem.hidden) {
                clearInterval(timer);
                window.sessionStorage.removeItem("chat:openConversationId");
                await openThread(pendingItem);
                return;
            }
        }

        const firstItem = Array.from(threadList?.querySelectorAll(".thread-item[data-id]") || [])
            .find((item) => !item.hidden);
        if (firstItem) {
            clearInterval(timer);

            const alreadyActive = threadList.querySelector(".thread-item.active");
            if (alreadyActive) return;

            await openThread(firstItem);
            return;
        }

        if (Date.now() - start > maxWaitMs) {
            clearInterval(timer);
            console.warn("autoOpenFirstThreadWhenReady: timeout - no thread items found");
        }
    }, intervalMs);
}
