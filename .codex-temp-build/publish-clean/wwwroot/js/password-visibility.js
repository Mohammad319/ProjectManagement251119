(function () {
    function setButtonState(button, isVisible) {
        const showLabel = button.getAttribute("data-show-label") || "Show password";
        const hideLabel = button.getAttribute("data-hide-label") || "Hide password";
        const label = isVisible ? hideLabel : showLabel;

        button.setAttribute("aria-label", label);
        button.setAttribute("title", label);
        button.setAttribute("aria-pressed", isVisible ? "true" : "false");

        const srOnly = button.querySelector(".sr-only");
        if (srOnly) {
            srOnly.textContent = label;
        }

        const showIcon = button.querySelector('[data-password-icon="show"]');
        const hideIcon = button.querySelector('[data-password-icon="hide"]');

        if (showIcon) {
            showIcon.classList.toggle("hidden", isVisible);
        }

        if (hideIcon) {
            hideIcon.classList.toggle("hidden", !isVisible);
        }
    }

    window.pmTogglePasswordVisibility = function (inputId, button) {
        const input = document.getElementById(inputId);
        if (!input || !button) {
            return;
        }

        const isVisible = input.type === "password";
        input.type = isVisible ? "text" : "password";
        setButtonState(button, isVisible);
    };
})();
