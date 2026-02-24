import { chatService } from "../../services/chatService.js";

const msgInput = document.getElementById("msgInput");
const sendBtn = document.getElementById("sendBtn");

console.log("composer-ui loaded", { msgInput: !!msgInput, sendBtn: !!sendBtn });

if (!msgInput || !sendBtn) {
    console.warn("Composer elements not found (#msgInput or #sendBtn)");
} else {
    sendBtn.addEventListener("click", async (e) => {
        e.preventDefault();
        await handleSendText();
    });

    msgInput.addEventListener("keydown", async (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            await handleSendText();
        }
    });
}

/** conversationId lấy từ thread-item đang active (được set khi click thread list) */
function getActiveConversationId() {
    const active = document.querySelector(".thread-item.active");
    return active?.dataset?.id ? parseInt(active.dataset.id, 10) : null;
}

async function handleSendText() {
    const conversationId = getActiveConversationId();
    if (!conversationId) {
        alert("Bạn chưa chọn cuộc trò chuyện.");
        return;
    }

    const text = (msgInput.value || "").trim();
    if (!text) return;

    setSending(true);

    try {
        // Gửi lên WebServer => WebServer lấy senderId từ cookie/claims
        await chatService.sendTextMessage(conversationId, text, null);

        msgInput.value = "";
        await reloadMessages(conversationId);
        msgInput.focus();
    } catch (err) {
        console.error(err);
        alert("Gửi tin nhắn thất bại.");
    } finally {
        setSending(false);
    }
}

function setSending(isSending) {
    sendBtn.disabled = isSending;
    msgInput.disabled = isSending;
    if (isSending) sendBtn.classList.add("is-sending");
    else sendBtn.classList.remove("is-sending");
}

async function reloadMessages(conversationId) {
    const scroller = document.getElementById("messageScroller");
    if (!scroller) return;

    try {
        const res = await chatService.getConversationView(conversationId);
        const html = res.data;

        const temp = document.createElement("div");
        temp.innerHTML = html;

        const newSection = temp.querySelector("#messageScroller");
        scroller.innerHTML = newSection ? newSection.innerHTML : html;

        scroller.scrollTop = scroller.scrollHeight;
    } catch (err) {
        console.error(err);
        scroller.innerHTML = `<div class="error">Không tải được tin nhắn.</div>`;
    }
}