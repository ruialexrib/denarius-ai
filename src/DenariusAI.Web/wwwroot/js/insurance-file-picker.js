(() => {
    "use strict";

    const formatFileSize = bytes => {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    };

    document.querySelectorAll("[data-file-picker]").forEach(picker => {
        const input = picker.querySelector('input[type="file"]');
        const name = picker.querySelector("[data-file-name]");
        const meta = picker.querySelector("[data-file-meta]");
        const action = picker.querySelector("[data-file-action]");
        const defaultName = name?.textContent ?? "";
        const defaultMeta = meta?.textContent ?? "";

        input?.addEventListener("change", () => {
            const file = input.files?.[0];
            picker.classList.toggle("has-file", Boolean(file));
            if (name) name.textContent = file?.name ?? defaultName;
            if (meta) meta.textContent = file ? `${formatFileSize(file.size)} · PDF selecionado` : defaultMeta;
            if (action) action.textContent = file ? "Alterar" : "Escolher";
        });
    });
})();
