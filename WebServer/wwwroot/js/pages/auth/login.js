import { authService } from "../../services/authService.js";
import { load } from "../../utils/helper.js";

const loginForm = document.getElementById("auth_form-login");
const btnSubmit = document.getElementById("form_login_btn_submit");

const emailField = document.getElementById("email-field");
const emailInput = document.getElementById("email");
const emailError = document.getElementById("email-error");

const passField = document.getElementById("pass-field");
const passInput = document.getElementById("password");
const passError = document.getElementById("pass-error");

const rememberMeInput = document.getElementById("rememberMe");

const togglePassBtn = document.getElementById("toggle-pass");
const pageLoading = document.getElementById("pageLoading");

const eyeOpen = togglePassBtn?.querySelector(".eye-open");
const eyeClosed = togglePassBtn?.querySelector(".eye-closed");
// ===== Loading overlay (optional) =====
function setPageLoading(isLoading) {
    if (!pageLoading) return;
    pageLoading.style.display = isLoading ? "flex" : "none";
}

// ===== Error helpers =====
function showFieldError(fieldEl, errorEl, message) {
    if (!fieldEl || !errorEl) return;
    fieldEl.classList.add("error");
    errorEl.textContent = message || "Thông tin không hợp lệ.";
    errorEl.classList.add("show");
}

function clearFieldError(fieldEl, errorEl) {
    if (!fieldEl || !errorEl) return;
    fieldEl.classList.remove("error");
    errorEl.textContent = "";
    errorEl.classList.remove("show");
}

function clearAllErrors() {
    clearFieldError(emailField, emailError);
    clearFieldError(passField, passError);
}

function showLoginErrors({ email, password } = {}) {
    if (email) showFieldError(emailField, emailError, email);
    if (password) showFieldError(passField, passError, password);
}



function renderEyeIcon(isShowing) {
    if (!eyeOpen || !eyeClosed) return;

    // hidden là native => không bị CSS override, không flash
    eyeOpen.hidden = !isShowing;
    eyeClosed.hidden = isShowing;
}

function setShowing(state) {
    if (!passInput || !togglePassBtn) return;

    passInput.type = state ? "text" : "password";
    togglePassBtn.setAttribute("aria-pressed", String(state));
    togglePassBtn.setAttribute("aria-label", state ? "Ẩn mật khẩu" : "Hiện mật khẩu");

    togglePassBtn.classList.toggle("showing-pass", !!state);
    renderEyeIcon(!!state);
}

function updateButtonVisibility() {
    if (!passField || !passInput) return;

    const hasText = passInput.value.length > 0;
    passField.classList.toggle("has-text", hasText);

    if (!hasText) {
        // reset về ẩn mật khẩu + icon đúng
        setShowing(false);
    } else {
        // đảm bảo icon đúng ngay khi vừa hiện button
        const showing = togglePassBtn.classList.contains("showing-pass");
        renderEyeIcon(showing);
    }
}

// ===== Wire events cho password =====
if (passInput) {
    passInput.addEventListener("input", () => {
        updateButtonVisibility();
        clearFieldError(passField, passError);
    });
    updateButtonVisibility();
    setShowing(false);
}

if (togglePassBtn) {
    togglePassBtn.addEventListener("click", () => {
        const isShowing = togglePassBtn.classList.contains("showing-pass");
        setShowing(!isShowing);
    });
}

// ===== Validate=====
function validateBeforeLogin() {
    clearAllErrors();

    const email = (emailInput?.value || "").trim();
    const password = passInput?.value || "";

    let ok = true;

    if (!email) {
        showFieldError(emailField, emailError, "Vui lòng nhập email hoặc số di động.");
        ok = false;
    }

    if (!password) {
        showFieldError(passField, passError, "Vui lòng nhập mật khẩu.");
        ok = false;
    }

    return ok;
}

// ===== Wire events =====
if (passInput) {
    passInput.addEventListener("input", () => {
        updateButtonVisibility();
        clearFieldError(passField, passError); // gõ lại thì ẩn lỗi pass
    });
    updateButtonVisibility();
}

if (togglePassBtn) {
    togglePassBtn.addEventListener("click", () => {
        const isShowing = togglePassBtn.classList.contains("showing-pass");
        setShowing(!isShowing);
    });
}

if (emailInput) {
    emailInput.addEventListener("input", () => {
        clearFieldError(emailField, emailError); // gõ lại thì ẩn lỗi email
    });
}

if (btnSubmit) {
    btnSubmit.addEventListener("click", async () => {
        if (!validateBeforeLogin()) return;

        try {
            // loading theo style bạn
            load(true);
            setPageLoading(true);
            btnSubmit.disabled = true;

            const formData = new FormData(loginForm);
            const email = formData.get("email");
            const password = formData.get("password");
            const rememberMe = rememberMeInput?.checked ?? false;

            const res = await authService.login(email, password, rememberMe);

            if (res && res.status === 200) {
                window.localStorage.setItem("user", JSON.stringify(res.data));
                window.location.href = "/dashboard";
            } else {
                showLoginErrors({
                    email: "Email hoặc số di động bạn nhập không kết nối với tài khoản nào. Hãy tìm tài khoản của bạn và đăng nhập.",
                    password: "Mật khẩu bạn nhập không đúng. Vui lòng thử lại."
                });
            }
        } catch (error) {
            showLoginErrors({
                email: "Email hoặc số di động bạn nhập không kết nối với tài khoản nào. Hãy tìm tài khoản của bạn và đăng nhập.",
                password: "Mật khẩu bạn nhập không đúng. Vui lòng thử lại."
            });
        } finally {
            btnSubmit.disabled = false;
            setPageLoading(false);
            load(false);
        }
    });
}
window.TalkyLoginUI = {
    showLoginErrors,
    clearAllErrors
};
