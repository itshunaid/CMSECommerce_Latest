// Lightweight tree selector that fetches /api/CategoriesApi/tree
// Render into a select dropdown with hierarchical options.

window.CategoryTreeSelector = (function () {
    async function fetchTree() {
        const res = await fetch('/api/CategoriesApi/tree');
        if (!res.ok) throw new Error('Failed to load categories');
        return await res.json();
    }

    function renderTreeToSelect(nodes, selectElement, level = 0) {
        nodes.forEach(n => {
            const option = document.createElement('option');
            option.value = n.id;
            option.textContent = '  '.repeat(level) + n.text;
            selectElement.appendChild(option);

            if (n.children && n.children.length > 0) {
                renderTreeToSelect(n.children, selectElement, level + 1);
            }
        });
    }

    async function init(selectSelector, hiddenInputSelector, options = {}) {
        const selectElement = document.querySelector(selectSelector);
        const hiddenInput = document.querySelector(hiddenInputSelector);
        if (!selectElement || !hiddenInput) return;

        try {
            // Clear existing options except the first one
            selectElement.innerHTML = '<option value="">-- Choose a category --</option>';

            const tree = await fetchTree();
            renderTreeToSelect(tree, selectElement, 0);

            selectElement.addEventListener('change', function () {
                hiddenInput.value = this.value;
            });
        }
        catch (err) {
            console.error('Failed to load category tree:', err);
            // Optionally show an error option
            const errorOption = document.createElement('option');
            errorOption.textContent = 'Error loading categories';
            errorOption.disabled = true;
            selectElement.appendChild(errorOption);
        }
    }

    return { init };
})();
