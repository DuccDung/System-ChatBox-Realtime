import { authService } from "../../services/authService.js";
import { load } from "../../utils/helper.js";

const loginForm = document.getElementById("auth_form-login");
const passwordInput = document.getElementById("password");
const toggleBtn = document.getElementById("togglePassword");
const icon = toggleBtn.querySelector("i");
const btnSubmit = document.getElementById("form_login_btn_submit");

btnSubmit.addEventListener("click", async (event) => {
    event.preventDefault();

    const formData = new FormData(loginForm);
    const email = formData.get("email");
    const password = formData.get("password");
    const rememberMe = document.getElementById("rememberMe").checked;
    load(true);
    try {
        const res = await authService.login(email, password , rememberMe);

        if (res.status == 200) {
            window.localStorage.setItem("user", JSON.stringify(res.data));
            window.location.href = "/dashboard";
        } else {
            alert("Email và mật khẩu chưa đúng, vui lòng nhập lại!");
        }
    } catch (error) {
        alert("Email và mật khẩu chưa đúng, vui lòng nhập lại!");
    } finally {
        load(false);
    }
});

toggleBtn.addEventListener("click", () => {
    const isPassword = passwordInput.type === "password";
    passwordInput.type = isPassword ? "text" : "password";
    icon.classList.toggle("bi-eye");
    icon.classList.toggle("bi-eye-slash");
});
