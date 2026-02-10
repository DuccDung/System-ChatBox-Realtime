import { api_origin } from "../core/api.js";

export const chatService = {
    async getFormSearch() {
        return await api_origin.get("/chat/search_view");
    },

    async searchUsersByEmail(email) {
        return await api_origin.get("/chat/search_user", {
            params: { email }
        });
    },
    async getPersonalView(userId) {
        return await api_origin.get("/chat/personal", {
            params: { userId }
        });
    },
    async getThreadsView() {
        return await api_origin.get("/chat/threads"); // HTML partial
    }
};
