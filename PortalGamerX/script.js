let slideIndex = 0;
const slides = document.querySelectorAll('.slide');
const dots = document.querySelectorAll('.dot');
const slideInterval = 3000;

function showSlide(index) {
    slides.forEach((slide, i) => {
        slide.style.transform = `translateX(-${index * 100}%)`;
    });
    dots.forEach((dot, i) => {
        dot.classList.toggle('active', i === index);
    });
}

function nextSlide() {
    slideIndex = (slideIndex + 1) % slides.length;
    showSlide(slideIndex);
}

function goToSlide(index) {
    slideIndex = index;
    showSlide(slideIndex);
}

dots.forEach((dot, index) => {
    dot.addEventListener('click', () => goToSlide(index));
});

showSlide(slideIndex);
setInterval(nextSlide, slideInterval);

