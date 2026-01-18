window.ldScrollById = (id, deltaY) => {
    const el = document.getElementById(id);
    if (!el) return;
    el.scrollBy({ top: deltaY, left: 0, behavior: "smooth" });
};