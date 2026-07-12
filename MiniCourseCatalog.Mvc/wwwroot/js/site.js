// Theme toggle via AJAX for smooth, reload-less transition
document.addEventListener("DOMContentLoaded", () => {
    const themeForm = document.querySelector(".navbar-theme-toggle form");
    if (themeForm) {
        themeForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            
            const currentTheme = document.documentElement.getAttribute("data-bs-theme") || "light";
            const newTheme = currentTheme === "light" ? "dark" : "light";
            
            // Add animating class to body to trigger smooth transitions
            document.body.classList.add("theme-animating");
            
            // Update cookie in the background
            try {
                await fetch(themeForm.action || "/Theme/Toggle", {
                    method: "POST",
                    body: new FormData(themeForm)
                });
            } catch (err) {
                console.error("Failed to save theme preference:", err);
            }

            const updateThemeUI = () => {
                // Update html attributes & classes
                document.documentElement.setAttribute("data-bs-theme", newTheme);
                document.documentElement.classList.remove(`theme-${currentTheme}`);
                document.documentElement.classList.add(`theme-${newTheme}`);

                // Update body classes
                document.body.classList.remove(`theme-${currentTheme}`);
                document.body.classList.add(`theme-${newTheme}`);

                // Update Switch Track
                const track = themeForm.querySelector(".switch-track");
                if (track) {
                    track.classList.remove(`track-${currentTheme}`);
                    track.classList.add(`track-${newTheme}`);
                }

                // Update Switch Thumb
                const thumb = themeForm.querySelector(".switch-thumb");
                if (thumb) {
                    thumb.classList.remove(`thumb-${currentTheme}`);
                    thumb.classList.add(`thumb-${newTheme}`);
                }

                // Update title & aria-label
                const button = themeForm.querySelector("button[type='submit']");
                if (button) {
                    const themeTitle = newTheme === "light" ? "Giao diện Tối" : "Giao diện Sáng";
                    button.setAttribute("title", themeTitle);
                    button.setAttribute("aria-label", themeTitle);
                }
            };

            // Use View Transitions API if supported (Chrome, Edge, etc.)
            if (document.startViewTransition) {
                document.startViewTransition(updateThemeUI);
            } else {
                updateThemeUI();
            }

            // Remove animating class after transition finishes to prevent hover lag
            setTimeout(() => {
                document.body.classList.remove("theme-animating");
            }, 500);
        });
    }
});
