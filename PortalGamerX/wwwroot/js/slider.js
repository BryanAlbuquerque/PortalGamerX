document.addEventListener('DOMContentLoaded', () => {
    const slides = Array.from(document.querySelectorAll('.slide'));
    const prev = document.querySelector('.nav.prev');
    const next = document.querySelector('.nav.next');
    const dotsContainer = document.querySelector('.dots');
    const sliderBox = document.getElementById('sliderBox');

    if (!slides.length) return;

    let index = 0;
    let timer = null;
    const INTERVAL = 4500;

    // build dots
    slides.forEach((_, i) => {
        const btn = document.createElement('button');
        btn.addEventListener('click', () => show(i));
        dotsContainer.appendChild(btn);
    });
    const dots = Array.from(dotsContainer.children);

    function show(i) {
        index = (i + slides.length) % slides.length;
        slides.forEach((s, si) => s.classList.toggle('active', si === index));
        dots.forEach((d, di) => d.classList.toggle('active', di === index));
        resetTimer();
    }

    function prevSlide() { show(index - 1); }
    function nextSlide() { show(index + 1); }

    if (prev) prev.addEventListener('click', prevSlide);
    if (next) next.addEventListener('click', nextSlide);

    function start() { timer = setInterval(() => show(index + 1), INTERVAL); }
    function resetTimer() { if (timer) clearInterval(timer); start(); }

    show(0);
    start();

    // pause on hover/focus (accessibility)
    if (sliderBox) {
        sliderBox.addEventListener('mouseenter', () => clearInterval(timer));
        sliderBox.addEventListener('mouseleave', start);
    }
});
