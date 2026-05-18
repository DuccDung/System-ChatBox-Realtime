// view.js - Public profile viewing functionality

import { profileService } from '../../services/profileService.js';

// Elements
const btnFollow = document.getElementById('btnFollow');

// Follow button handler
if (btnFollow) {
    btnFollow.addEventListener('click', async () => {
        // Placeholder: Thêm logic theo dõi ở đây
        // Có thể gọi API khi đã có feature friendship
        console.log('Follow button clicked');

        btnFollow.disabled = true;
        btnFollow.innerHTML = `
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 6L9 17l-5-5"></path>
            </svg>
            Đang xử lý...
        `;

        // Simulate delay
        await new Promise(resolve => setTimeout(resolve, 500));

        btnFollow.innerHTML = `
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 6L9 17l-5-5"></path>
            </svg>
            Đã theo dõi
        `;
    });
}

// Toast notification helper
function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `profile-toast ${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);

    setTimeout(() => toast.classList.add('show'), 10);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Share functionality
async function shareProfile() {
    const url = window.location.href;

    if (navigator.share) {
        try {
            await navigator.share({
                title: document.title,
                url: url
            });
        } catch (err) {
            if (err.name !== 'AbortError') {
                console.error('Share error:', err);
            }
        }
    } else {
        try {
            await navigator.clipboard.writeText(url);
            showToast('Đã sao chép liên kết hồ sơ!', 'success');
        } catch (err) {
            showToast('Không thể sao chép liên kết.', 'error');
        }
    }
}

console.log('Profile view.js loaded');