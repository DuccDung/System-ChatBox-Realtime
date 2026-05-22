// profileService.js - API client for profile operations

export const profileService = {
    /**
     * Lấy thông tin profile công khai theo userId
     */
    async getPublicProfile(userId) {
        const response = await fetch(`/api/profile/${userId}`, {
            credentials: 'include'
        });
        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to get public profile: ${error}`);
        }
        return response.json();
    },

    /**
     * Lấy thông tin profile của user hiện tại
     */
    async getMyProfile() {
        const response = await fetch('/api/profile/me', {
            credentials: 'include'
        });
        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to get my profile: ${error}`);
        }
        return response.json();
    },

    /**
     * Cập nhật thông tin profile
     */
    async updateProfile(profileData) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const response = await fetchWithTimeout('/Profile/Me/Profile', {
            method: 'PUT',
            credentials: 'include',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                ...(token && { 'RequestVerificationToken': token })
            },
            body: JSON.stringify(profileData)
        }, 15000);

        return readJsonResponse(response, 'Không thể cập nhật hồ sơ.');
    },

    /**
     * Upload ảnh đại diện
     */
    async uploadAvatar(file) {
        const formData = new FormData();
        formData.append('avatar', file);

        const response = await fetchWithTimeout('/Profile/Me/Avatar', {
            method: 'POST',
            credentials: 'include',
            body: formData
        }, 30000);

        return readJsonResponse(response, 'Không thể upload ảnh đại diện.');
    },

    /**
     * Upload ảnh bìa
     */
    async uploadCover(file) {
        const formData = new FormData();
        formData.append('cover', file);

        const response = await fetchWithTimeout('/Profile/Me/Cover', {
            method: 'POST',
            credentials: 'include',
            body: formData
        }, 30000);

        return readJsonResponse(response, 'Không thể upload ảnh bìa.');
    },

    /**
     * Tìm kiếm user theo email
     */
    async searchByEmail(email) {
        const encodedEmail = encodeURIComponent(email.trim());
        const response = await fetch(`/api/profile/by-email?email=${encodedEmail}`, {
            credentials: 'include'
        });

        if (response.status === 404) {
            return null;
        }

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Search failed: ${error}`);
        }

        return response.json();
    }
};

async function fetchWithTimeout(url, options = {}, timeoutMs = 15000) {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => controller.abort(), timeoutMs);

    try {
        return await fetch(url, {
            ...options,
            signal: controller.signal
        });
    } finally {
        window.clearTimeout(timeoutId);
    }
}

async function readJsonResponse(response, fallbackMessage) {
    const contentType = response.headers.get('content-type') || '';
    const isJson = contentType.includes('application/json');
    const text = await response.text();
    let payload = null;

    if (text && isJson) {
        try {
            payload = JSON.parse(text);
        } catch {
            payload = null;
        }
    }

    if (!response.ok) {
        throw new Error(payload?.message || fallbackMessage);
    }

    return payload;
}
