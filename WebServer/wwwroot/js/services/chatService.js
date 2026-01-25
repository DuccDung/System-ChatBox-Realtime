import { api_origin } from "../core/api.js";

export const chatService = {
    async getFormSearch() {
        const res = await api_origin.get("/chat/search_view");
        return res;
    }
};
