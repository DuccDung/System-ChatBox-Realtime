// threads-ui.js
(function () {
    // Ưu tiên id="threadList". Nếu không có thì fallback ".thread-list"
    const threadList = document.getElementById("threadList")
    const scroller = document.getElementById("messageScroller");
    const peerName = document.getElementById("peerName");
    const peerAvatar = document.getElementById("peerAvatar");
    const peerStatus = document.getElementById("peerStatus");

    if (!threadList) {
        console.warn("Thread list not found (#threadList or .thread-list)");
        return;
    }

    // =========================
    // 1) CLICK TRONG THREAD LIST
    // =========================
    threadList.addEventListener("click", async (e) => {
        // A) Nếu click nút 3 chấm -> toggle menu và dừng
        const moreBtn = e.target.closest(".more-btn");
        if (moreBtn) {
            e.stopPropagation();

            const item = moreBtn.closest(".thread-item");
            if (!item) return;

            const menu = item.querySelector(".thread-menu");
            if (!menu) return;

            // Đóng menu của các thread khác
            closeAllMenusExcept(menu);

            // Toggle menu hiện tại
            menu.hidden = !menu.hidden;
            return;
        }

        // B) Nếu click vào 1 item -> mở conversation
        const item = e.target.closest(".thread-item");
        if (!item || !threadList.contains(item)) return;

        // Đóng tất cả menu khi click vào item (trừ khi click nút 3 chấm)
        closeAllMenusExcept(null);

        const conversationId = item.dataset.id; // data-id="@t.ConversationId"
        if (!conversationId) return;

        // 1) Set ACTIVE UI
        setActiveItem(item);

        // 2) Update header từ data-* (bạn đã add data-name, data-avatar)
        if (peerName) peerName.textContent = item.dataset.name || "Người dùng";
        if (peerAvatar) peerAvatar.src = item.dataset.avatar || peerAvatar.src;
        if (peerStatus) peerStatus.textContent = ""; // sau này fill presence

        // 3) (Optional) khi mở thì bỏ highlight/unread UI
        item.classList.remove("highlight");
        item.classList.remove("unread");
        const badge = item.querySelector(".unread-badge");
        if (badge) badge.remove();

        // 4) Load messages
        await loadMessages(conversationId);
    });

    // =========================
    // 2) CLICK NGOÀI -> ĐÓNG MENU
    // =========================
    document.addEventListener("click", (e) => {
        // nếu click không nằm trong threadList -> đóng menu
        if (!e.target.closest("#threadList") && !e.target.closest(".thread-list")) {
            closeAllMenusExcept(null);
        }
    });

    // =========================
    // HELPERS
    // =========================
    function closeAllMenusExcept(menuToKeep) {
        document.querySelectorAll(".thread-menu").forEach((m) => {
            if (m !== menuToKeep) m.hidden = true;
        });
    }

    function setActiveItem(item) {
        document.querySelectorAll(".thread-item.active").forEach((li) => {
            li.classList.remove("active");
        });
        item.classList.add("active");
    }

    // =========================
    // 3) LOAD MESSAGES (AJAX)
    // =========================
    async function loadMessages(conversationId) {
        if (!scroller) return;

        // UI loading
        scroller.innerHTML = `<div class="loading">Đang tải...</div>`;

        try {
            // TODO: đổi endpoint cho đúng backend bạn
            // Ví dụ: /Conversations/{id}/Messages (MVC) hoặc /api/conversations/{id}/messages
            const url = `/api/conversations/${conversationId}/messages?limit=50`;

            const res = await fetch(url, { credentials: "include" });
            if (!res.ok) throw new Error(await res.text());

            const messages = await res.json();
            renderMessages(messages);
        } catch (err) {
            console.error(err);
            scroller.innerHTML = `<div class="error">Không tải được tin nhắn.</div>`;
        }
    }

    // =========================
    // 4) RENDER MESSAGES
    // =========================
    function renderMessages(messages) {
        if (!scroller) return;
        scroller.innerHTML = "";

        // bạn set CURRENT_USER_ID từ Razor: <script>window.CURRENT_USER_ID=...</script>
        const currentUserId = window.CURRENT_USER_ID ?? 1;

        (messages || []).forEach((m) => {
            // tuỳ DTO của bạn, sửa field nếu khác
            const senderId = m.senderId ?? m.SenderId;
            const content = m.content ?? m.Content ?? "";
            const sentAt = m.sentAt ?? m.SentAt;

            const side = senderId === currentUserId ? "right" : "left";

            const wrap = document.createElement("div");
            wrap.className = "bubble-group";

            const msgEl = document.createElement("div");
            msgEl.className = "msg " + side;
            msgEl.textContent = content;

            // tooltip thời gian
            const tip = document.createElement("div");
            tip.className = "message-time-tooltip";
            tip.textContent = formatTime(sentAt);
            msgEl.appendChild(tip);

            wrap.appendChild(msgEl);
            scroller.appendChild(wrap);
        });

        scroller.scrollTop = scroller.scrollHeight;
    }

    function formatTime(value) {
        if (!value) return "";
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return "";
        const hh = d.getHours();
        const mm = String(d.getMinutes()).padStart(2, "0");
        return `${hh}:${mm}`;
    }
})();