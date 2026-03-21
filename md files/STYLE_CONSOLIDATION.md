# Style Consolidation — amazon-design.css

Summary
- Consolidated view-level CSS variables and rules into `wwwroot/css/amazon-design.css` for a single source of truth.
- Removed inline `<style>` blocks from several views and the admin layout; those views now rely on `amazon-design.css`.
- Added CSS variable aliases (`--amz-*`) to preserve backward compatibility with older admin styles.
- Added a small JS smoke test (`wwwroot/js/style-smoke-test.js`) that runs on page load and performs basic visual checks (link color, product card aspect ratio, floating chat position). A small result badge is shown in the page corner.
- Deduplicated and harmonized color variables and naming (prefer `--amazon-*`), kept short aliases for compatibility.

Files changed
- Edited: `wwwroot/css/amazon-design.css` (appended consolidated variables + moved per-view styles)
- Edited: `Areas/Admin/Views/Shared/_Layout.cshtml` (removed inline styles, added link to `amazon-design.css`)
- Edited: `Views/Pages/Index.cshtml` (removed inline style block)
- Edited: `Views/Products/Product.cshtml` (removed Styles section)
- Edited: `Views/Products/Index.cshtml` (removed Styles section)
- Created: `wwwroot/js/style-smoke-test.js` (automated smoke test)
- Created: `STYLE_CONSOLIDATION.md` (this document)

Notes / How to revert
- To revert fully, reintroduce the removed per-view `<style>` blocks into their respective views. The admin layout previously contained the `:root` variables under `--amz-*`; the new file preserves aliases.

Future suggestions
- Consider adding automated visual regression tests (Puppeteer/Playwright snapshots) to CI using the smoke test as a starting point.
- Migrate more project-wide constants (fonts, spacing scales) into the central CSS.

