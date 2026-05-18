// edit.js - Profile editing functionality

import { profileService } from '../../services/profileService.js';

// Elements
const btnEditProfile = document.getElementById('btnEditProfile');
const btnCancelEdit = document.getElementById('btnCancelEdit');
const editPanel = document.getElementById('editPanel');
const profileEditForm = document.getElementById('profileEditForm');
const btnEditAvatar = document.getElementById('btnEditAvatar');
const avatarInput = document.getElementById('avatarInput');
const coverInput = document.getElementById('coverInput');
const btnEditCover = document.querySelector('.profile-cover-edit-btn');

// Toast notification
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

function formatDateOnly(value) {
    if (!value) return 'Chưa cập nhật';

    const [year, month, day] = value.split('-');
    if (!year || !month || !day) return 'Chưa cập nhật';

    return `${Number(day)} tháng ${Number(month)}, ${year}`;
}

function formatGender(value) {
    if (value === 0) return 'Nữ';
    if (value === 1) return 'Nam';
    if (value === 2) return 'Khác';
    return 'Chưa cập nhật';
}

function updateProfileText(profile) {
    if (!profile) return;

    const name = profile.accountName?.trim() || 'Người dùng';
    const bio = profile.bio?.trim() || 'Chưa có giới thiệu';

    const nameEl = document.querySelector('.profile-name');
    const bioEl = document.querySelector('.profile-bio');
    const detailValues = document.querySelectorAll('.profile-detail-value');

    if (nameEl) nameEl.textContent = name;
    if (bioEl) bioEl.textContent = bio;
    if (detailValues[0]) detailValues[0].textContent = profile.email?.trim() || 'Chưa cập nhật';
    if (detailValues[1]) detailValues[1].textContent = formatDateOnly(profile.dateOfBirth);
    if (detailValues[2]) detailValues[2].textContent = formatGender(profile.gender);
}

// Toggle edit panel
if (btnEditProfile) {
    btnEditProfile.addEventListener('click', () => {
        if (editPanel) {
            editPanel.style.display = editPanel.style.display === 'none' ? 'block' : 'none';
        }
    });
}

if (btnCancelEdit) {
    btnCancelEdit.addEventListener('click', () => {
        if (editPanel) {
            editPanel.style.display = 'none';
        }
        // Reset form
        profileEditForm?.reset();
    });
}

// Handle form submission
if (profileEditForm) {
    profileEditForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const formData = new FormData(profileEditForm);
        const profileData = {
            accountName: formData.get('accountName'),
            bio: formData.get('bio'),
            dateOfBirth: formData.get('dateOfBirth') || null,
            gender: formData.get('gender') ? parseInt(formData.get('gender')) : null
        };

        try {
            // Disable submit button
            const submitBtn = profileEditForm.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = 'Đang lưu...';
            }

            const updatedProfile = await profileService.updateProfile(profileData);

            showToast('Cập nhật hồ sơ thành công!', 'success');
            updateProfileText(updatedProfile);

            setTimeout(() => {
                editPanel.style.display = 'none';
            }, 600);
        } catch (error) {
            console.error('Update profile error:', error);
            showToast('Không thể cập nhật hồ sơ. Vui lòng thử lại.', 'error');
        } finally {
            const submitBtn = profileEditForm.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = `
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M12 20h9"></path>
                        <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path>
                    </svg>
                    Lưu thay đổi
                `;
            }
        }
    });
}

// Avatar upload
if (btnEditAvatar && avatarInput) {
    btnEditAvatar.addEventListener('click', () => {
        avatarInput.click();
    });

    avatarInput.addEventListener('change', async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        // Validate file type
        if (!file.type.startsWith('image/')) {
            showToast('Vui lòng chọn file ảnh hợp lệ.', 'error');
            return;
        }

        // Validate file size (max 5MB)
        if (file.size > 5 * 1024 * 1024) {
            showToast('Kích thước ảnh không được vượt quá 5MB.', 'error');
            return;
        }

        // Preview ảnh
        const reader = new FileReader();
        reader.onload = (event) => {
            const avatarEl = document.querySelector('.profile-avatar');
            if (avatarEl) {
                avatarEl.style.backgroundImage = `url('${event.target.result}')`;
            }
        };
        reader.readAsDataURL(file);

        // Upload
        try {
            btnEditAvatar.disabled = true;
            btnEditAvatar.innerHTML = '<span>Đang upload...</span>';

            await profileService.uploadAvatar(file);

            showToast('Đã cập nhật ảnh đại diện!', 'success');

            // Reload trang
            setTimeout(() => {
                location.reload();
            }, 1000);
        } catch (error) {
            console.error('Avatar upload error:', error);
            showToast('Không thể upload ảnh. Vui lòng thử lại.', 'error');
        } finally {
            btnEditAvatar.disabled = false;
            btnEditAvatar.innerHTML = `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 20h9"></path>
                    <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path>
                </svg>
            `;
        }
    });
}

if (btnEditCover && coverInput) {
    btnEditCover.addEventListener('click', () => {
        coverInput.click();
    });

    coverInput.addEventListener('change', async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        // Validate file type
        if (!file.type.startsWith('image/')) {
            showToast('Vui lòng chọn file ảnh hợp lệ.', 'error');
            return;
        }

        // Validate file size (max 10MB)
        if (file.size > 10 * 1024 * 1024) {
            showToast('Kích thước ảnh không được vượt quá 10MB.', 'error');
            return;
        }

        // Preview ảnh bìa
        const reader = new FileReader();
        reader.onload = (event) => {
            const coverEl = document.querySelector('.profile-header');
            if (coverEl) {
                coverEl.style.backgroundImage = `url('${event.target.result}')`;
            }
        };
        reader.readAsDataURL(file);

        // Upload
        try {
            btnEditCover.disabled = true;
            btnEditCover.innerHTML = '<span>Đang upload...</span>';

            await profileService.uploadCover(file);

            showToast('Đã cập nhật ảnh bìa!', 'success');

            // Reload trang
            setTimeout(() => {
                location.reload();
            }, 1000);
        } catch (error) {
            console.error('Cover upload error:', error);
            showToast('Không thể upload ảnh. Vui lòng thử lại.', 'error');
        } finally {
            btnEditCover.disabled = false;
            btnEditCover.innerHTML = `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                    <circle cx="8.5" cy="8.5" r="1.5"></circle>
                    <polyline points="21 15 16 10 5 21"></polyline>
                </svg>
            `;
        }
    });
}

// Share profile button
if (document.getElementById('btnShareProfile')) {
    document.getElementById('btnShareProfile').addEventListener('click', async () => {
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
            // Fallback: copy to clipboard
            try {
                await navigator.clipboard.writeText(url);
                showToast('Đã sao chép liên kết hồ sơ!', 'success');
            } catch (err) {
                showToast('Không thể sao chép liên kết.', 'error');
            }
        }
    });
}

console.log('Profile edit.js loaded');
