// home.js - Efectos para la página de inicio

document.addEventListener('DOMContentLoaded', function () {
    // Crear partículas animadas
    const particlesContainer = document.getElementById('particles');
    const particleCount = 25;

    for (let i = 0; i < particleCount; i++) {
        createParticle();
    }

    function createParticle() {
        const particle = document.createElement('div');
        particle.classList.add('particle');

        // Tamaño aleatorio
        const size = Math.random() * 8 + 2;
        particle.style.width = `${size}px`;
        particle.style.height = `${size}px`;

        // Posición aleatoria
        particle.style.left = `${Math.random() * 100}vw`;
        particle.style.top = `${Math.random() * 100}vh`;

        // Color aleatorio de la paleta pastel
        const colors = [
            'rgba(168, 216, 234, 0.8)',  // primary-pastel
            'rgba(170, 150, 218, 0.8)',  // secondary-pastel
            'rgba(252, 186, 211, 0.8)',  // accent-pastel
            'rgba(162, 210, 255, 0.8)'   // info-pastel
        ];
        const color = colors[Math.floor(Math.random() * colors.length)];
        particle.style.background = color;

        // Duración y delay de animación aleatorios
        const duration = Math.random() * 15 + 10;
        const delay = Math.random() * 5;
        particle.style.animationDuration = `${duration}s`;
        particle.style.animationDelay = `${delay}s`;

        particlesContainer.appendChild(particle);
    }

    // Efecto de escritura para el título
    const titulo = document.querySelector('.titulo');
    const originalText = titulo.textContent;
    titulo.textContent = '';

    let i = 0;
    function typeWriter() {
        if (i < originalText.length) {
            titulo.textContent += originalText.charAt(i);
            i++;
            setTimeout(typeWriter, 100);
        }
    }

    // Iniciar efecto de escritura después de un breve delay
    setTimeout(typeWriter, 500);

    // Efecto de partículas interactivas
    document.addEventListener('mousemove', function (e) {
        const particles = document.querySelectorAll('.particle');
        particles.forEach(particle => {
            const speed = parseFloat(particle.style.width) * 0.5;
            const x = (e.clientX * speed) / 250;
            const y = (e.clientY * speed) / 250;

            particle.style.transform = `translate(${x}px, ${y}px)`;
        });
    });

    // Efecto de respiración para el contenedor principal
    const heroContent = document.querySelector('.hero-content');
    setInterval(() => {
        heroContent.style.transform = 'scale(1.01)';
        setTimeout(() => {
            heroContent.style.transform = 'scale(1)';
        }, 2000);
    }, 4000);

    // Efecto de confeti al hacer hover en el botón
    const btnIngresar = document.querySelector('.btn-ingresar');
    btnIngresar.addEventListener('mouseenter', function () {
        createConfetti();
    });

    function createConfetti() {
        for (let i = 0; i < 15; i++) {
            const confetti = document.createElement('div');
            confetti.classList.add('particle');
            confetti.style.width = '8px';
            confetti.style.height = '8px';
            confetti.style.position = 'absolute';
            confetti.style.left = '50%';
            confetti.style.top = '50%';
            confetti.style.animation = `confettiFall ${Math.random() * 2 + 1}s ease-out forwards`;

            const colors = [
                'rgba(168, 216, 234, 0.9)',
                'rgba(170, 150, 218, 0.9)',
                'rgba(252, 186, 211, 0.9)',
                'rgba(162, 210, 255, 0.9)'
            ];
            confetti.style.background = colors[Math.floor(Math.random() * colors.length)];

            document.body.appendChild(confetti);

            setTimeout(() => {
                confetti.remove();
            }, 2000);
        }
    }

    // Agregar estilo para la animación de confeti
    const style = document.createElement('style');
    style.textContent = `
        @keyframes confettiFall {
            0% {
                transform: translate(0, 0) rotate(0deg);
                opacity: 1;
            }
            100% {
                transform: translate(${Math.random() * 200 - 100}px, 100vh) rotate(360deg);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);

    // Efecto de sonido sutil al interactuar (opcional)
    btnIngresar.addEventListener('click', function () {
        // Crear un sonido de click sutil
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);

        oscillator.frequency.value = 523.25; // Nota Do
        oscillator.type = 'sine';

        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);

        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.5);
    });
});