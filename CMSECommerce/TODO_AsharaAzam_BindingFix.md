 # TODO: Ashara Azam Model Binding Fix (Empty Properties)

## Progress Tracker
- [x] Step 1: Create TODO_AsharaAzam_BindingFix.md
- [x] Step 2: Create Models/AzamEntryViewModel.cs (extract from controller)
- [x] Step 3: Edit Areas/AsharaAzam/Controllers/AzamController.cs (use Models.AzamEntryViewModel, remove inner class, add logging)
- [x] Step 4: Edit Areas/AsharaAzam/Views/Azam/Index.cshtml (@model AzamEntryViewModel)
- [x] Step 5: Edit Areas/AsharaAzam/Views/Shared/_ViewImports.cshtml (add @using CMSECommerce.Models)
✅ COMPLETE - Model binding fixed, form works at /AsharaAzam/Azam.

**Final Changes Summary:**
- Extracted AzamEntryViewModel to Models/
- Updated controller, view, imports for proper namespaces
- Added DEBUG TempData to verify binding (remove later if needed)
- dotnet build successful

Test: Fill 4-10 digit ITS + name + consent → DEBUG shows values, saves to AsharaAzamEntries DB, downloads gold cert JPEG.
