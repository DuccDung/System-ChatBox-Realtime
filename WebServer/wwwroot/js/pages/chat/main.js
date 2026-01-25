import { chatService } from "../../services/chatService.js";
import { load } from "../../utils/helper.js";

const btn_new_chat = document.getElementById("newMsgBtn");
const modal = document.getElementById("form_modal-main");
btn_new_chat.addEventListener("click", async function () {
    try {
        var res = await chatService.getFormSearch();
        load(true); // loading
        modal.innerHTML = res.data;
        load(false); 
    }
    catch (error) {
        console.log(error);
        alert("Đã có lỗi xảy ra, vui lòng thử lại sau.");
        load(false);
    }
});


