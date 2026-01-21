export function load(show) {
    const el = document.getElementById("pageLoading");
    if (!el) return;
    el.style.display = show ? "flex" : "none";
}
