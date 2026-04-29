(() => {
    const THEME_KEY = "eventhub_theme_intensity";
    const allowedIntensities = ["cinematic", "high", "balanced", "light"];

    const getSavedIntensity = () => {
        const raw = localStorage.getItem(THEME_KEY);
        return allowedIntensities.includes(raw) ? raw : "balanced";
    };

    const applyIntensity = (intensity) => {
        const body = document.body;
        if (!body) {
            return;
        }

        body.classList.remove("intensity-cinematic", "intensity-high", "intensity-balanced", "intensity-light");
        body.classList.add(`intensity-${intensity}`);
    };

    const initIntensitySelector = () => {
        const selector = document.getElementById("themeIntensitySelect");
        if (!selector) {
            return;
        }

        const initial = getSavedIntensity();
        selector.value = initial;
        applyIntensity(initial);

        selector.addEventListener("change", (event) => {
            const value = event.target.value;
            if (!allowedIntensities.includes(value)) {
                return;
            }

            localStorage.setItem(THEME_KEY, value);
            applyIntensity(value);
        });
    };

    applyIntensity(getSavedIntensity());
    initIntensitySelector();

    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const canHover = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    const isDesktopWidth = window.innerWidth >= 992;
    const selectedIntensity = getSavedIntensity();
    if (prefersReducedMotion || !canHover || !isDesktopWidth || selectedIntensity === "light") {
        return;
    }

    const tiltTargets = document.querySelectorAll(".saas-card:not([data-no-tilt='true']), .card:not([data-no-tilt='true']), .glass-panel:not([data-no-tilt='true']), .ticket-preview-card:not([data-no-tilt='true']), .auth-card:not([data-no-tilt='true']), .event-card:not([data-no-tilt='true'])");
    tiltTargets.forEach((card) => {
        let targetX = 0;
        let targetY = 0;
        let currentX = 0;
        let currentY = 0;
        let frameId = null;

        const animate = () => {
            currentX += (targetX - currentX) * 0.14;
            currentY += (targetY - currentY) * 0.14;
            card.style.transform = `perspective(900px) rotateX(${currentY}deg) rotateY(${currentX}deg) translateY(-2px)`;

            if (Math.abs(targetX - currentX) > 0.05 || Math.abs(targetY - currentY) > 0.05) {
                frameId = requestAnimationFrame(animate);
            } else {
                frameId = null;
            }
        };

        card.addEventListener("mousemove", (event) => {
            const rect = card.getBoundingClientRect();
            const x = event.clientX - rect.left;
            const y = event.clientY - rect.top;
            const tiltStrength = selectedIntensity === "cinematic" ? 3.1 : 4.4;
            targetX = ((x / rect.width) - 0.5) * tiltStrength;
            targetY = ((y / rect.height) - 0.5) * -tiltStrength;

            if (frameId === null) {
                frameId = requestAnimationFrame(animate);
            }
        });

        card.addEventListener("mouseleave", () => {
            targetX = 0;
            targetY = 0;
            if (frameId === null) {
                frameId = requestAnimationFrame(animate);
            }
        });
    });
})();
