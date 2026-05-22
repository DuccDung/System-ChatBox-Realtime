import { profileService } from '../../services/profileService.js';

const btnEditProfile = document.getElementById('btnEditProfile');
const btnCancelEdit = document.getElementById('btnCancelEdit');
const btnCloseEdit = document.getElementById('btnCloseEdit');
const btnSaveProfile = document.getElementById('btnSaveProfile');
const editPanel = document.getElementById('editPanel');
const profileEditForm = document.getElementById('profileEditForm');
const formAlert = document.getElementById('profileFormAlert');
const btnEditAvatar = document.getElementById('btnEditAvatar');
const avatarInput = document.getElementById('avatarInput');
const coverInput = document.getElementById('coverInput');
const btnEditCover = document.querySelector('.profile-cover-edit-btn');
const btnShareProfile = document.getElementById('btnShareProfile');

const fields = {
    accountName: document.getElementById('accountName'),
    bio: document.getElementById('bio'),
    dateOfBirth: document.getElementById('dateOfBirth'),
    gender: document.getElementById('gender')
};

const ICON_EDIT = `
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M12 20h9"></path>
        <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z"></path>
    </svg>
`;

const ICON_CHECK = `
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M20 6L9 17l-5-5"></path>
    </svg>
`;

const ICON_IMAGE = `
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
        <circle cx="8.5" cy="8.5" r="1.5"></circle>
        <polyline points="21 15 16 10 5 21"></polyline>
    </svg>
`;

const TEXT = {
    unknownUser: 'Người dùng',
    empty: 'Chưa cập nhật',
    emptyBio: 'Chưa có giới thiệu',
    edit: 'Chỉnh sửa hồ sơ',
    closeEdit: 'Đóng chỉnh sửa',
    save: 'Lưu thay đổi',
    saving: 'Đang lưu...',
    saved: 'Đã lưu',
    uploading: 'Đang tải lên...',
    noChanges: 'Bạn chưa thay đổi thông tin nào.',
    saveSuccess: 'Hồ sơ đã được cập nhật.',
    saveError: 'Không thể cập nhật hồ sơ. Vui lòng thử lại.',
    slowServer: 'Máy chủ phản hồi chậm. Vui lòng kiểm tra lại sau ít giây.',
    discardConfirm: 'Bạn có thay đổi chưa lưu. Đóng form và bỏ thay đổi?'
};

const LIMITS = {
    nameMin: 2,
    nameMax: 80,
    bioMax: 280,
    avatarMb: 5,
    coverMb: 10
};

let savedProfile = normalizeProfile(readProfileFromForm());
let isSaving = false;
let closeTimer = 0;

initializeProfileEditor();

function initializeProfileEditor() {
    if (!profileEditForm || !editPanel) return;

    setEditPanelOpen(false, { focus: false });
    syncFormValues(savedProfile);
    updateCounters();
    updateSaveAvailability();

    btnEditProfile?.addEventListener('click', () => {
        if (editPanel.hidden) {
            openEditor();
        } else {
            requestCloseEditor();
        }
    });

    btnCancelEdit?.addEventListener('click', () => requestCloseEditor());
    btnCloseEdit?.addEventListener('click', () => requestCloseEditor());

    Object.values(fields).forEach((field) => {
        field?.addEventListener('input', () => {
            clearAlert();
            updateCounters();
            updateSaveAvailability({ showErrors: true });
        });

        field?.addEventListener('change', () => {
            clearAlert();
            updateCounters();
            updateSaveAvailability({ showErrors: true });
        });
    });

    profileEditForm.addEventListener('submit', handleProfileSubmit);

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && !editPanel.hidden) {
            requestCloseEditor();
        }
    });
}

async function handleProfileSubmit(event) {
    event.preventDefault();
    if (isSaving) return;

    const validation = validateForm({ showErrors: true });
    const hasChanges = !profilesEqual(savedProfile, validation.data);

    if (!validation.valid) {
        showAlert('error', validation.firstError || TEXT.saveError);
        updateSaveAvailability({ showErrors: true });
        return;
    }

    if (!hasChanges) {
        showAlert('info', TEXT.noChanges);
        updateSaveAvailability();
        return;
    }

    isSaving = true;
    setSaveButtonState('saving');
    setFormDisabled(true);
    clearAlert();

    try {
        const updatedProfile = normalizeProfile(
            await profileService.updateProfile(validation.data),
            { ...savedProfile, ...validation.data }
        );

        savedProfile = updatedProfile;
        updateProfileText(updatedProfile);
        syncFormValues(updatedProfile);
        updateCounters();
        resetValidation();
        showAlert('success', TEXT.saveSuccess);
        showToast(TEXT.saveSuccess, 'success');
        setSaveButtonState('saved');

        closeTimer = window.setTimeout(() => {
            closeEditor({ restoreValues: false });
        }, 700);
    } catch (error) {
        console.error('Update profile error:', error);
        const message = error.name === 'AbortError'
            ? TEXT.slowServer
            : error.message || TEXT.saveError;

        showAlert('error', message);
        showToast(message, 'error');
        setSaveButtonState('idle');
    } finally {
        isSaving = false;
        setFormDisabled(false);
        updateSaveAvailability({ showErrors: true });
    }
}

function openEditor() {
    window.clearTimeout(closeTimer);
    syncFormValues(savedProfile);
    updateCounters();
    resetValidation();
    clearAlert();
    setSaveButtonState('idle');
    setEditPanelOpen(true);
    updateSaveAvailability();
}

function requestCloseEditor() {
    if (isSaving) return;

    const currentProfile = getFormProfile();
    if (!profilesEqual(savedProfile, currentProfile) && !window.confirm(TEXT.discardConfirm)) {
        return;
    }

    closeEditor({ restoreValues: true });
}

function closeEditor({ restoreValues }) {
    window.clearTimeout(closeTimer);

    if (restoreValues) {
        syncFormValues(savedProfile);
        updateCounters();
    }

    resetValidation();
    clearAlert();
    setSaveButtonState('idle');
    setEditPanelOpen(false, { focus: false });
    updateSaveAvailability();
}

function setEditPanelOpen(isOpen, options = {}) {
    if (!editPanel) return;

    const { focus = true } = options;
    editPanel.hidden = !isOpen;
    editPanel.classList.toggle('is-open', isOpen);

    if (btnEditProfile) {
        btnEditProfile.setAttribute('aria-expanded', String(isOpen));
        btnEditProfile.classList.toggle('is-active', isOpen);
        btnEditProfile.innerHTML = isOpen
            ? `${ICON_CHECK}<span>${TEXT.closeEdit}</span>`
            : `${ICON_EDIT}<span>${TEXT.edit}</span>`;
    }

    if (isOpen && focus) {
        window.setTimeout(() => {
            editPanel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            fields.accountName?.focus();
        }, 40);
    }
}

btnEditAvatar?.addEventListener('click', () => {
    if (!btnEditAvatar.disabled) avatarInput?.click();
});

avatarInput?.addEventListener('change', async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    await handleImageUpload({
        file,
        input: avatarInput,
        button: btnEditAvatar,
        maxMb: LIMITS.avatarMb,
        selector: '.profile-avatar',
        fieldName: 'photoPath',
        defaultHtml: ICON_EDIT,
        upload: profileService.uploadAvatar,
        successMessage: 'Đã cập nhật ảnh đại diện.',
        errorMessage: 'Không thể upload ảnh đại diện.'
    });
});

btnEditCover?.addEventListener('click', () => {
    if (!btnEditCover.disabled) coverInput?.click();
});

coverInput?.addEventListener('change', async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    await handleImageUpload({
        file,
        input: coverInput,
        button: btnEditCover,
        maxMb: LIMITS.coverMb,
        selector: '.profile-header',
        fieldName: 'photoBackground',
        defaultHtml: ICON_IMAGE,
        upload: profileService.uploadCover,
        successMessage: 'Đã cập nhật ảnh bìa.',
        errorMessage: 'Không thể upload ảnh bìa.'
    });
});

async function handleImageUpload(options) {
    const {
        file,
        input,
        button,
        maxMb,
        selector,
        fieldName,
        defaultHtml,
        upload,
        successMessage,
        errorMessage
    } = options;

    if (!validateImageFile(file, maxMb)) {
        input.value = '';
        return;
    }

    const target = document.querySelector(selector);
    const previousBackground = target?.style.backgroundImage || '';

    try {
        const previewUrl = await readImageAsDataUrl(file);
        setImageBackground(selector, previewUrl);
        setIconButtonLoading(button, true, defaultHtml);

        const updatedProfile = normalizeProfile(await upload(file), savedProfile);
        savedProfile = updatedProfile;
        updateProfileText(updatedProfile);

        const finalUrl = cacheBust(updatedProfile[fieldName] || previewUrl);
        setImageBackground(selector, finalUrl);
        showToast(successMessage, 'success');
    } catch (error) {
        console.error('Profile image upload error:', error);
        if (target) target.style.backgroundImage = previousBackground;
        showToast(error.message || errorMessage, 'error');
    } finally {
        setIconButtonLoading(button, false, defaultHtml);
        input.value = '';
    }
}

btnShareProfile?.addEventListener('click', async () => {
    const url = window.location.href;

    if (navigator.share) {
        try {
            await navigator.share({ title: document.title, url });
        } catch (error) {
            if (error.name !== 'AbortError') console.error('Share error:', error);
        }
        return;
    }

    try {
        await navigator.clipboard.writeText(url);
        showToast('Đã sao chép liên kết hồ sơ.', 'success');
    } catch {
        showToast('Không thể sao chép liên kết.', 'error');
    }
});

function readProfileFromForm() {
    return {
        accountName: fields.accountName?.value || '',
        bio: fields.bio?.value || '',
        dateOfBirth: fields.dateOfBirth?.value || null,
        gender: fields.gender?.value || null,
        email: document.querySelector('[data-profile-field="email"]')?.textContent?.trim() || ''
    };
}

function getFormProfile() {
    const genderValue = fields.gender?.value || '';

    return normalizeProfile({
        accountName: fields.accountName?.value?.trim() || '',
        bio: fields.bio?.value?.trim() || '',
        dateOfBirth: fields.dateOfBirth?.value || null,
        gender: genderValue === '' ? null : Number(genderValue),
        email: savedProfile.email
    });
}

function normalizeProfile(profile, fallback = {}) {
    return {
        accountName: String(getProfileValue(profile, 'accountName') ?? fallback.accountName ?? '').trim(),
        email: String(getProfileValue(profile, 'email') ?? fallback.email ?? '').trim(),
        bio: String(getProfileValue(profile, 'bio') ?? fallback.bio ?? '').trim(),
        dateOfBirth: normalizeDateValue(getProfileValue(profile, 'dateOfBirth') ?? fallback.dateOfBirth ?? null),
        gender: normalizeGenderValue(getProfileValue(profile, 'gender') ?? fallback.gender ?? null),
        photoPath: getProfileValue(profile, 'photoPath') ?? fallback.photoPath ?? '',
        photoBackground: getProfileValue(profile, 'photoBackground') ?? fallback.photoBackground ?? ''
    };
}

function getProfileValue(profile, camelName) {
    if (!profile) return null;
    const pascalName = camelName[0].toUpperCase() + camelName.slice(1);
    return profile[camelName] ?? profile[pascalName] ?? null;
}

function profilesEqual(left, right) {
    const a = normalizeProfile(left);
    const b = normalizeProfile(right);

    return a.accountName === b.accountName
        && a.bio === b.bio
        && a.dateOfBirth === b.dateOfBirth
        && a.gender === b.gender;
}

function validateForm(options = {}) {
    const { showErrors = false } = options;
    const data = getFormProfile();
    const errors = {};

    if (!data.accountName) {
        errors.accountName = 'Tên hiển thị không được để trống.';
    } else if (data.accountName.length < LIMITS.nameMin) {
        errors.accountName = `Tên hiển thị cần ít nhất ${LIMITS.nameMin} ký tự.`;
    } else if (data.accountName.length > LIMITS.nameMax) {
        errors.accountName = `Tên hiển thị không được vượt quá ${LIMITS.nameMax} ký tự.`;
    }

    if (data.bio.length > LIMITS.bioMax) {
        errors.bio = `Giới thiệu không được vượt quá ${LIMITS.bioMax} ký tự.`;
    }

    if (data.dateOfBirth && data.dateOfBirth > getTodayValue()) {
        errors.dateOfBirth = 'Ngày sinh không được lớn hơn ngày hiện tại.';
    }

    if (data.gender !== null && ![0, 1, 2].includes(data.gender)) {
        errors.gender = 'Giới tính không hợp lệ.';
    }

    setFieldError('accountName', errors.accountName, showErrors);
    setFieldError('bio', errors.bio, showErrors);
    setFieldError('dateOfBirth', errors.dateOfBirth, showErrors);
    setFieldError('gender', errors.gender, showErrors);

    return {
        data,
        errors,
        valid: Object.keys(errors).length === 0,
        firstError: Object.values(errors)[0] || ''
    };
}

function updateSaveAvailability(options = {}) {
    if (!btnSaveProfile) return;

    const validation = validateForm(options);
    const hasChanges = !profilesEqual(savedProfile, validation.data);

    btnSaveProfile.disabled = isSaving || !validation.valid || !hasChanges;
    btnSaveProfile.classList.toggle('is-disabled-clean', !isSaving && validation.valid && !hasChanges);
}

function syncFormValues(profile) {
    const data = normalizeProfile(profile);

    setInputValue('accountName', data.accountName);
    setInputValue('bio', data.bio);
    setInputValue('dateOfBirth', data.dateOfBirth || '');
    setInputValue('gender', data.gender === null ? '' : String(data.gender));
}

function updateProfileText(profile) {
    const data = normalizeProfile(profile);
    const name = data.accountName || TEXT.unknownUser;
    const bio = data.bio || TEXT.emptyBio;

    setText('.profile-name', name);
    setText('.profile-bio', bio);
    setText('[data-profile-field="email"]', data.email || TEXT.empty);
    setText('[data-profile-field="dateOfBirth"]', formatDateOnly(data.dateOfBirth));
    setText('[data-profile-field="gender"]', formatGender(data.gender));
}

function setSaveButtonState(state) {
    if (!btnSaveProfile) return;

    btnSaveProfile.classList.toggle('is-loading', state === 'saving');
    btnSaveProfile.classList.toggle('is-saved', state === 'saved');

    if (state === 'saving') {
        btnSaveProfile.innerHTML = `<span class="profile-btn-spinner" aria-hidden="true"></span><span>${TEXT.saving}</span>`;
        btnSaveProfile.disabled = true;
        return;
    }

    if (state === 'saved') {
        btnSaveProfile.innerHTML = `${ICON_CHECK}<span>${TEXT.saved}</span>`;
        btnSaveProfile.disabled = true;
        return;
    }

    btnSaveProfile.innerHTML = `${ICON_CHECK}<span>${TEXT.save}</span>`;
}

function setFormDisabled(disabled) {
    Object.values(fields).forEach((field) => {
        if (field) field.disabled = disabled;
    });

    if (btnCancelEdit) btnCancelEdit.disabled = disabled;
    if (btnCloseEdit) btnCloseEdit.disabled = disabled;
    if (btnEditProfile) btnEditProfile.disabled = disabled;
}

function setIconButtonLoading(button, isLoading, defaultHtml) {
    if (!button) return;

    button.disabled = isLoading;
    button.classList.toggle('is-loading', isLoading);
    button.innerHTML = isLoading
        ? '<span class="profile-btn-spinner" aria-hidden="true"></span>'
        : defaultHtml;
}

function validateImageFile(file, maxMb) {
    if (!file.type.startsWith('image/')) {
        showToast('Vui lòng chọn file ảnh hợp lệ.', 'error');
        return false;
    }

    if (file.size > maxMb * 1024 * 1024) {
        showToast(`Kích thước ảnh không được vượt quá ${maxMb}MB.`, 'error');
        return false;
    }

    return true;
}

function readImageAsDataUrl(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (event) => resolve(event.target.result);
        reader.onerror = () => reject(new Error('Không thể đọc file ảnh.'));
        reader.readAsDataURL(file);
    });
}

function setImageBackground(selector, url) {
    const el = document.querySelector(selector);
    if (el && url) el.style.backgroundImage = `url('${url}')`;
}

function cacheBust(url) {
    if (!url || url.startsWith('data:')) return url;
    const join = url.includes('?') ? '&' : '?';
    return `${url}${join}v=${Date.now()}`;
}

function normalizeDateValue(value) {
    if (!value) return null;
    const dateValue = String(value).substring(0, 10);
    return /^\d{4}-\d{2}-\d{2}$/.test(dateValue) ? dateValue : null;
}

function normalizeGenderValue(value) {
    if (value === null || value === undefined || value === '') return null;
    const gender = Number(value);
    return Number.isInteger(gender) ? gender : null;
}

function formatDateOnly(value) {
    if (!value) return TEXT.empty;

    const [year, month, day] = normalizeDateValue(value)?.split('-') || [];
    if (!year || !month || !day) return TEXT.empty;

    return `${Number(day)} tháng ${Number(month)}, ${year}`;
}

function formatGender(value) {
    const gender = normalizeGenderValue(value);
    if (gender === 0) return 'Nữ';
    if (gender === 1) return 'Nam';
    if (gender === 2) return 'Khác';
    return TEXT.empty;
}

function updateCounters() {
    updateCounter('accountName', LIMITS.nameMax);
    updateCounter('bio', LIMITS.bioMax);
}

function updateCounter(fieldId, max) {
    const field = fields[fieldId];
    const counter = document.querySelector(`[data-count-for="${fieldId}"]`);
    if (!field || !counter) return;

    counter.textContent = `${field.value.length}/${max}`;
    counter.classList.toggle('is-over', field.value.length > max);
}

function setFieldError(fieldId, message, show) {
    const field = fields[fieldId];
    const errorEl = document.getElementById(`${fieldId}Error`);
    const hasError = Boolean(show && message);

    field?.classList.toggle('is-invalid', hasError);
    field?.setAttribute('aria-invalid', String(hasError));

    if (errorEl) {
        errorEl.textContent = hasError ? message : '';
    }
}

function resetValidation() {
    Object.keys(fields).forEach((fieldId) => setFieldError(fieldId, '', false));
}

function showAlert(type, message) {
    if (!formAlert) return;

    formAlert.hidden = false;
    formAlert.textContent = message;
    formAlert.className = `profile-form-alert ${type}`;
}

function clearAlert() {
    if (!formAlert) return;

    formAlert.hidden = true;
    formAlert.textContent = '';
    formAlert.className = 'profile-form-alert';
}

function setText(selector, value) {
    const el = document.querySelector(selector);
    if (el) el.textContent = value;
}

function setInputValue(id, value) {
    const input = document.getElementById(id);
    if (input) input.value = value ?? '';
}

function getTodayValue() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `profile-toast ${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);

    window.setTimeout(() => toast.classList.add('show'), 10);
    window.setTimeout(() => {
        toast.classList.remove('show');
        window.setTimeout(() => toast.remove(), 300);
    }, 3000);
}
