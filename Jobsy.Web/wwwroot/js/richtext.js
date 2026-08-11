window.jobsyRichtext = {
    /**
     * Wrap the current selection in a textarea with before/after markup.
     * If nothing is selected, wraps the whole value (or inserts a placeholder).
     * Returns the new textarea value.
     */
    wrap: function (textarea, before, after, placeholder) {
        if (!textarea) {
            return null;
        }

        var start = textarea.selectionStart ?? 0;
        var end = textarea.selectionEnd ?? 0;
        var value = textarea.value ?? "";
        var selected = value.substring(start, end);

        if (!selected) {
            selected = placeholder || "";
        }

        var next = value.substring(0, start) + before + selected + after + value.substring(end);
        textarea.value = next;

        var cursor = start + before.length + selected.length;
        textarea.focus();
        textarea.setSelectionRange(cursor, cursor);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));

        return next;
    },

    /**
     * Prompt for a URL and wrap the selection in an <a> tag.
     */
    insertLink: function (textarea) {
        if (!textarea) {
            return null;
        }

        var url = window.prompt("Link-URL (https://…)", "https://");
        if (!url || !url.trim()) {
            return null;
        }

        url = url.trim();
        var start = textarea.selectionStart ?? 0;
        var end = textarea.selectionEnd ?? 0;
        var value = textarea.value ?? "";
        var selected = value.substring(start, end) || url;

        var before = '<a href="' + url.replace(/"/g, "&quot;") + '" target="_blank" rel="noopener">';
        var after = "</a>";
        // Vacancy copy may not contain links — still expose the helper for other screens,
        // but refuse to insert when the textarea is marked data-no-links="true".
        if (textarea.getAttribute("data-no-links") === "true") {
            window.alert("Links in de vacaturetekst zijn niet toegestaan.");
            return null;
        }
        var next = value.substring(0, start) + before + selected + after + value.substring(end);
        textarea.value = next;

        var cursor = start + before.length + selected.length;
        textarea.focus();
        textarea.setSelectionRange(cursor, cursor);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));

        return next;
    }
};
