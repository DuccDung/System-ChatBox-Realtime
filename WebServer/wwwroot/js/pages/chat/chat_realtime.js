import { subscribeConversation, connectWs } from "../../services/ws-client.js";

const scroller = document.getElementById("messageScroller");

console.log("chat-realtime loaded");

// đảm bảo websocket được connect
connectWs();

// ==============================
// LẤY conversation đang active
// ==============================
function getActiveConversationId() {
    const active = document.querySelector(".thread-item.active");
    if (!active) return null;
    return parseInt(active.dataset.id, 10);
}

// ==============================
// SUBSCRIBE KHI THREAD ACTIVE ĐỔI
// ==============================
export function subscribeActiveConversation() {
    const conversationId = getActiveConversationId();
    if (!conversationId) return;

    console.log("Subscribing to conversation:", conversationId);
    subscribeConversation(conversationId);
}

// ==============================
// LẮNG NGHE MESSAGE TỪ WS
// ==============================
window.addEventListener("ws:message", (event) => {
    const msg = event.detail;

    if (!msg || msg.type !== "message") return;

    const activeConversationId = getActiveConversationId();
    if (!activeConversationId) return;

    // nếu message không thuộc conversation đang mở -> bỏ qua
    if (msg.conversationId !== activeConversationId) return;

    appendMessageToScroller(msg.payload);
});

// ==============================
// APPEND MESSAGE VÀO UI
// ==============================
function appendMessageToScroller(message) {
    if (!scroller) return;

    // meId lấy từ data attribute của scroller (đúng với view của bạn)
    const meId = parseInt(scroller.dataset.meId || "0", 10);
    const senderId = message?.sender?.accountId ?? message?.sender?.AccountId; // phòng trường hợp casing
    const isMe = senderId === meId;

    const sideClass = isMe ? "right" : "left";

    // createdAt có thể là string ISO -> parse về Date
    const curTime = parseDate(message.createdAt || message.CreatedAt);
    const now = new Date();

    // lấy message time của bubble cuối cùng đang render (để quyết định divider/spacing)
    const lastMsgTime = getLastMessageTime();

    // divider nếu cách nhau > 15 phút
    if (lastMsgTime && needDivider(lastMsgTime, curTime)) {
        const divider = document.createElement("div");
        divider.className = "message-timestamp-divider";
        divider.textContent = dividerLabel(curTime, now);
        scroller.appendChild(divider);
    }

    // spacing class
    const spacing = spacingClass(lastMsgTime, curTime);

    // bubble-group
    const group = document.createElement("div");
    group.className = `bubble-group ${spacing}`.trim();

    // msg bubble
    const msgDiv = document.createElement("div");
    msgDiv.className = `msg ${sideClass}`;

    // content (text)
    // (vì bạn đang render @m.Content trực tiếp, mình escape để tránh XSS)
    msgDiv.appendChild(document.createTextNode(message.content ?? message.Content ?? ""));

    // tooltip time HH:mm
    const tip = document.createElement("div");
    tip.className = "message-time-tooltip";
    tip.textContent = tooltipTime(curTime);

    msgDiv.appendChild(tip);
    group.appendChild(msgDiv);
    scroller.appendChild(group);

    // auto scroll xuống cuối
    scroller.scrollTop = scroller.scrollHeight;
}

/* ================= Helpers giống Razor ================= */

function parseDate(v) {
    if (!v) return new Date();
    // nếu là Date rồi
    if (v instanceof Date) return v;

    // ISO string thường parse được luôn
    const d = new Date(v);
    if (!isNaN(d.getTime())) return d;

    // fallback
    return new Date();
}

function tooltipTime(dt) {
    const hh = String(dt.getHours()).padStart(2, "0");
    const mm = String(dt.getMinutes()).padStart(2, "0");
    return `${hh}:${mm}`;
}

function dividerLabel(dt, now) {
    const dayDiff = Math.floor((stripTime(now) - stripTime(dt)) / (24 * 60 * 60 * 1000));
    const time = tooltipTime(dt);

    if (dayDiff === 0) return time;
    if (dayDiff === 1) return `Hôm qua, ${time}`;
    if (dayDiff < 7) return `${dayDiff} ngày trước, ${time}`;

    const dd = String(dt.getDate()).padStart(2, "0");
    const mo = String(dt.getMonth() + 1).padStart(2, "0");
    const yy = dt.getFullYear();
    return `${dd}/${mo}/${yy}, ${time}`;
}

function stripTime(d) {
    return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

function spacingClass(prevTime, curTime) {
    if (!prevTime) return "";
    const diffMs = curTime - prevTime;

    // < 60s => tight
    if (diffMs < 60 * 1000) return "spacing-tight";

    // >= 1 phút => spaced
    return "spacing-spaced";
}

function needDivider(prevTime, curTime) {
    if (!prevTime) return false;
    return (curTime - prevTime) > 15 * 60 * 1000;
}

/**
 * Lấy thời gian của message bubble cuối cùng đã render trong scroller
 * dựa vào tooltip "HH:mm". Nếu không parse được thì return null.
 */
function getLastMessageTime() {
    // tìm bubble cuối: .msg .message-time-tooltip
    const lastTip = scroller.querySelector(".msg:last-of-type .message-time-tooltip")
        || scroller.querySelector(".bubble-group:last-of-type .message-time-tooltip");

    if (!lastTip) return null;

    const t = (lastTip.textContent || "").trim(); // "HH:mm"
    const m = /^(\d{2}):(\d{2})$/.exec(t);
    if (!m) return null;

    const hh = parseInt(m[1], 10);
    const mm = parseInt(m[2], 10);

    // ⚠️ Không có ngày ở tooltip, nên ta gán ngày hôm nay làm gần đúng.
    // Nếu bạn muốn chuẩn tuyệt đối, mình sẽ lưu lastMessageTime vào biến global khi load messages.
    const base = new Date();
    base.setHours(hh, mm, 0, 0);
    return base;
}

// ==============================
// ESCAPE HTML TRÁNH XSS
// ==============================
function escapeHtml(text) {
    const div = document.createElement("div");
    div.innerText = text;
    return div.innerHTML;
}