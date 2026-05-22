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
    },
    async sendAudioMessage(conversationId, file, parentMessageId = null) {
        const formData = new FormData();
        formData.append("ConversationId", String(conversationId));
        formData.append("File", file);

        if (parentMessageId !== null && parentMessageId !== undefined) {
            formData.append("ParentMessageId", String(parentMessageId));
        }

        return await api_origin.post("/chat/send_audio", formData);
    },
    async getPeer(conversationId) {
        return await api_origin.get("/chat/peer", {
            params: { conversationId }
        });
    },
    async getGroupMembersView(conversationId) {
        return await api_origin.get("/chat/group/members", {
            params: { conversationId }
        });
    },
    async removeGroupMember(conversationId, memberId) {
        return await api_origin.delete("/chat/group/members", {
            params: { conversationId, memberId }
        });
    },
    async createGroup(title, memberIds) {
        return await api_origin.post("/chat/group", {
            title,
            memberIds
        });
    },
    async createDirect(friendId) {
        return await api_origin.post("/chat/direct", {
            friendId
        });
    },
    async markRead(conversationId) {
        return await api_origin.post("/chat/mark-read", null, {
            params: { conversationId }
        });
    },
};
export const callService = {
    async getIncomingPopup(payload) {
        return await api_origin.get("/call/incoming_popup", {
            params: payload
        });
    },
     async getCallPopup(conversationId, callType = "video") {
        return await api_origin.get("/call/popup", {
            params: { conversationId, callType }
        });
    }
};
