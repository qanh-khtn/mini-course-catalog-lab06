(() => {
    const setTheme = (theme) => {
        const nextTheme = theme === "dark" ? "light" : "dark";
        document.body.classList.add("theme-animating");
        document.body.classList.toggle("theme-dark", theme === "dark");
        document.body.classList.toggle("theme-light", theme !== "dark");

        document.querySelectorAll("[data-theme-toggle]").forEach((toggle) => {
            toggle.dataset.nextTheme = nextTheme;
            toggle.title = theme === "dark" ? "Giao diện Sáng" : "Giao diện Tối";

            const thumb = toggle.querySelector(".switch-thumb");
            const track = toggle.querySelector(".switch-track");

            thumb?.classList.toggle("thumb-dark", theme === "dark");
            thumb?.classList.toggle("thumb-light", theme !== "dark");
            track?.classList.toggle("track-dark", theme === "dark");
            track?.classList.toggle("track-light", theme !== "dark");

            const url = new URL(toggle.href, window.location.origin);
            url.searchParams.set("theme", nextTheme);
            toggle.href = `${url.pathname}${url.search}${url.hash}`;
        });

        document.querySelectorAll("a[href]").forEach((link) => {
            if (link.matches("[data-theme-toggle]")) {
                return;
            }

            const url = new URL(link.href, window.location.origin);
            if (url.origin !== window.location.origin) {
                return;
            }

            if (url.pathname === "/" || url.pathname.startsWith("/Home") || url.pathname.startsWith("/Courses")) {
                url.searchParams.set("theme", theme);
                link.href = `${url.pathname}${url.search}${url.hash}`;
            }
        });

        const pageUrl = new URL(window.location.href);
        pageUrl.searchParams.set("theme", theme);
        window.history.replaceState({}, "", `${pageUrl.pathname}${pageUrl.search}${pageUrl.hash}`);

        window.setTimeout(() => {
            document.body.classList.remove("theme-animating");
        }, 520);
    };

    document.addEventListener("click", (event) => {
        const toggle = event.target.closest("[data-theme-toggle]");
        if (!toggle) {
            return;
        }

        event.preventDefault();
        setTheme(toggle.dataset.nextTheme === "dark" ? "dark" : "light");
    });
})();
