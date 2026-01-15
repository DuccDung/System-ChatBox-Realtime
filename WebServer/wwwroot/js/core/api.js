import axios from "https://cdn.jsdelivr.net/npm/axios@1.6.8/dist/axios.min.js";

const api_origin = axios.create({
    baseURL: window.location.origin, // use domain origin
    timeout: 15000,
    withCredentials: true,
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
    },
});