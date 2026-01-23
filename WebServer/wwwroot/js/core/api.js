import axios from "https://cdn.jsdelivr.net/npm/axios@1.6.8/+esm";

export const api_origin = axios.create({
    baseURL: window.location.origin,
    withCredentials: true,
    headers: {
        "Content-Type": "application/json",
        "Accept": "application/json",
    },
});
