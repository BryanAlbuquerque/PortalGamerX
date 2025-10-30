document.addEventListener("DOMContentLoaded", function () {
    const slides = document.querySelectorAll(".slide");
    const dotsContainer = document.getElementById("dots-container");
    let index = 0;

    if (!slides || slides.length === 0) return;

    // criar dots dinamicamente conforme número de slides
    slides.forEach((_, i) => {
        const dot = document.createElement("span");
        dot.classList.add("dot");
        dot.addEventListener("click", () => { showSlide(i); });
        dotsContainer.appendChild(dot);
    });

    const dots = dotsContainer.querySelectorAll(".dot");

    function showSlide(n) {
        slides.forEach(s => { s.style.display = "none"; s.classList.remove("fade"); });
        dots.forEach(d => d.classList.remove("active"));

        slides[n].style.display = "block";
        slides[n].classList.add("fade");
        dots[n].classList.add("active");
        index = n;
    }

    function nextSlide() {
        index = (index + 1) % slides.length;
        showSlide(index);
    }

    // inicia
    showSlide(0);
    setInterval(nextSlide, 4000);
});
