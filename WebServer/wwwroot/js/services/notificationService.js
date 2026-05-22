import { api_origin } from "../core/api.js";

export const notificationService = {
    async getNotifications({ limit = 30, unreadOnly = false } = {}) {
        const res = await api_origin.get("/notifications", {
            params: { limit, unreadOnly }
        });
        return res.data || [];
    },

    async getUnreadCount() {
        const res = await api_origin.get("/notifications/unread-count");
        return Number(res.data?.unreadCount || 0);
    },

    async markRead(notificationId) {
        const res = await api_origin.post(`/notifications/${notificationId}/read`);
        return res.data;
    },

    async markAllRead() {
        const res = await api_origin.post("/notifications/read-all");
        return res.data;
    }
};
