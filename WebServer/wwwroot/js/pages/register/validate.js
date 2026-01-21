// Helpers
const $ = (id) => document.getElementById(id);
function setFieldError(fieldEl, errorEl, message) {
    fieldEl.classList.add("error");
    if (message) errorEl.textContent = message;
    errorEl.classList.add("show");
}
function clearFieldError(fieldEl, errorEl) {
    fieldEl.classList.remove("error");
    errorEl.classList.remove("show");
}

function setupEyeToggle(inputEl, fieldEl, btnEl) {
    function setShowing(state) {
        inputEl.type = state ? "text" : "password";
        btnEl.setAttribute("aria-pressed", String(state));
        btnEl.setAttribute(
            "aria-label",
            state ? "Ẩn mật khẩu" : "Hiện mật khẩu",
        );
        btnEl.classList.toggle("showing-pass", state);
    }

    function updateButtonVisibility() {
        if (inputEl.value.length > 0) {
            fieldEl.classList.add("has-text");
        } else {
            fieldEl.classList.remove("has-text");
            setShowing(false);
        }
    }

    inputEl.addEventListener("input", updateButtonVisibility);
    updateButtonVisibility();

    btnEl.addEventListener("click", () => {
        const isShowing = btnEl.classList.contains("showing-pass");
        setShowing(!isShowing);
    });
}

// Elements
const form = $("signup-form");

const nameField = $("name-field");
const nameInput = $("fullname");
const nameError = $("name-error");

const contactField = $("contact-field");
const contactInput = $("contact");
const contactError = $("contact-error");

const passField = $("pass-field");
const passInput = $("password");
const passError = $("pass-error");

const confirmField = $("confirm-field");
const confirmInput = $("confirm");
const confirmError = $("confirm-error");

// Eye toggles
setupEyeToggle(passInput, passField, $("toggle-pass"));
setupEyeToggle(confirmInput, confirmField, $("toggle-confirm"));

// Clear error on typing
nameInput.addEventListener("input", () =>
    clearFieldError(nameField, nameError),
);
contactInput.addEventListener("input", () =>
    clearFieldError(contactField, contactError),
);
passInput.addEventListener("input", () =>
    clearFieldError(passField, passError),
);
confirmInput.addEventListener("input", () =>
    clearFieldError(confirmField, confirmError),
);

// Simple validators
function isValidEmail(v) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
}
function isValidPhone(v) {
    // demo: chấp nhận 9-11 số
    return /^\d{9, 11}$/.test(v.replace(/\s/g, ""));
}

form.addEventListener("submit", (e) => {
    e.preventDefault();

    // reset previous errors (optional)
    clearFieldError(nameField, nameError);
    clearFieldError(contactField, contactError);
    clearFieldError(passField, passError);
    clearFieldError(confirmField, confirmError);

    const nameVal = nameInput.value.trim();
    const contactVal = contactInput.value.trim();
    const passVal = passInput.value;
    const confirmVal = confirmInput.value;

    let ok = true;

    if (!nameVal) {
        setFieldError(nameField, nameError, "Vui lòng nhập họ và tên.");
        ok = false;
    }

    if (
        !contactVal ||
        !(isValidEmail(contactVal) || isValidPhone(contactVal))
    ) {
        setFieldError(
            contactField,
            contactError,
            "Vui lòng nhập email hoặc số di động hợp lệ.",
        );
        ok = false;
    }

    if (!passVal || passVal.length < 8) {
        setFieldError(
            passField,
            passError,
            "Mật khẩu phải có ít nhất 8 ký tự.",
        );
        ok = false;
    }

    if (confirmVal !== passVal) {
        setFieldError(
            confirmField,
            confirmError,
            "Mật khẩu nhập lại không khớp.",
        );
        ok = false;
    }

    if (ok) {
        alert("Đăng ký thành công (demo)!");
        form.reset();

        // reset button visibility + hide states
        passField.classList.remove("has-text");
        confirmField.classList.remove("has-text");
        passInput.type = "password";
        confirmInput.type = "password";
        $("toggle-pass").classList.remove("showing-pass");
        $("toggle-confirm").classList.remove("showing-pass");
        $("toggle-pass").setAttribute("aria-pressed", "false");
        $("toggle-confirm").setAttribute("aria-pressed", "false");
        $("toggle-pass").setAttribute("aria-label", "Hiện mật khẩu");
        $("toggle-confirm").setAttribute("aria-label", "Hiện mật khẩu");
    }
});
