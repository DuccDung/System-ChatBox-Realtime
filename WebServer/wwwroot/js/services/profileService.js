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

        const response = await fetch('/api/profile/me', {
            method: 'PUT',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                ...(token && { 'RequestVerificationToken': token })
            },
            body: JSON.stringify(profileData)
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to update profile: ${error}`);
        }

        return response.json();
    },

    /**
     * Upload ảnh đại diện
     */
    async uploadAvatar(file) {
        const formData = new FormData();
        formData.append('avatar', file);

        const response = await fetch('/Profile/Me/Avatar', {
            method: 'POST',
            credentials: 'include',
            body: formData
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to upload avatar: ${error}`);
        }

        return response.json();
    },

    /**
     * Upload ảnh bìa
     */
    async uploadCover(file) {
        const formData = new FormData();
        formData.append('cover', file);

        const response = await fetch('/Profile/Me/Cover', {
            method: 'POST',
            credentials: 'include',
            body: formData
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to upload cover: ${error}`);
        }

        return response.json();
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
