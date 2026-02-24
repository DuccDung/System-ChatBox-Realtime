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
    },
    async getConversationView(conversationId) {
        return await api_origin.get("/chat/conversation", {
            params: { conversationId }
        });
    },
    async sendTextMessage(conversationId, content, parentMessageId = null) {
        return await api_origin.post("/chat/send_message", {
            conversationId,
            content,
            parentMessageId
        });
    },
    async sendImageMessage(conversationId, file, parentMessageId = null) {
        const formData = new FormData();
        formData.append("conversationId", conversationId);
        formData.append("file", file);

        if (parentMessageId !== null) {
            formData.append("parentMessageId", parentMessageId);
        }

        return await api_origin.post("/chat/send_image", formData, {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        });
    }
};
