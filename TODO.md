# Task: Add email notifications (already exist) and WhatsApp button for product approve/reject on /admin/products/pendingproducts

## Steps to Complete (Approved Plan):

### 1. ✅ Update ProductListViewModel.cs
- Added WhatsAppUrl property.
- Add `public string WhatsAppUrl { get; set; }` property.

### 2. Update Areas/Admin/Controllers/ProductsController.cs
- In `PendingProducts()` action: For each product, fetch UserProfile.WhatsAppNumber, clean it (remove non-digits, prepend '91' if needed), construct `https://wa.me/{number}?text=...` (pre-filled msg with status), set in viewmodel.
- Enhance Approve/Reject: Already sends email; optionally log WhatsApp URL or improve msg.

### 3. ✅ Update Areas/Admin/Views/Products/Index.cshtml
- Added WhatsApp button in action-group for products with WhatsAppUrl (pending/rejected).
- In product card action-group (for non-Approved): Add WhatsApp button `<a href="@item.WhatsAppUrl" target="_blank" class="btn btn-success btn-sm"><i class="bi bi-whatsapp"></i> WA</a>`.

### 4. Test
- Submit product as seller → pending.
- Admin: Approve → check email + WA button opens chat.
- Admin: Reject → check email with reason + WA button.

### 5. Completion
- use attempt_completion

**Progress: 4/5**
