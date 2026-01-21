import { api_origin } from "../core/api.js";

export const authService = {
    async login(email, password, rememberMe) {
        const payload = { email, password, rememberMe: rememberMe };
        const res = await api_origin.post("/auth/login", payload);
        return res; // để bạn check res.status
        // hoặc: return res.data; (khuyên dùng hơn)
    }
};
