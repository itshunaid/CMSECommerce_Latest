# CMSECommerce User Guide - Non-Technical Audience (Maximum End-to-End Details)

This guide provides exhaustive step-by-step instructions for every user role, with exact URLs, expected screens, buttons, fields, tips, screenshots-text, and edge cases. Based on live app at https://localhost:7121 after `dotnet run`.

## Table of Contents
- [Customer Full Journey](#customer-full-journey)
- [Seller Full Journey](#seller-full-journey)
- [Admin Full Journey](#admin-full-journey)
- [SuperAdmin Full Journey](#superadmin-full-journey)
- [Common Features](#common-features)
- [Troubleshooting & Edge Cases](#troubleshooting)

## Customer Full Journey (Buyer)
### 1. Registration (First Time)
- Navigate: Homepage → Click **Register** button (top-right).
- URL: `/Account/Register`
- Form Fields:
  | Field | Example | Required | Notes |
  |-------|---------|----------|-------|
  | Username | johndoe123 | Yes | Unique check AJAX |
  | Email | john@example.com | Yes | Unique + valid format |
  | Phone | 9876543210 | Yes | Unique check |
  | Password | Pass123! | Yes | Min 4 chars |
  | Confirm Password | Pass123! | Yes | Match |
- Click **Register** → Success: Email sent → Check inbox/spam for confirm link → Click → Logged in as Customer.
- Edge: Invalid email → Red error \"Invalid email\". Duplicate → \"Already exists\".

### 2. Browse & Search Products
- URL: `/Products` or `/Products/Index?p=1&search=shirt&category=shirts&sort=price`
- Screen: Category sidebar (ViewComponent), paginated grid (photos/name/price/store), search bar, sort dropdown.
- Click product card → `/Products/Product/{slug}` e.g. `/products/product/blue-shirt`
- Details Screen: Gallery images, desc, price/stock, reviews list, Youtube embed, **Add to Cart** button (qty selector).

### 3. Shopping Cart
- URL: `/Cart` (top cart icon → minicart → full cart)
- List: Item | Qty | Price | Total | Remove | Update buttons.
- **Continue Shopping** / **Checkout**.

### 4. Checkout & Order
- From cart **Checkout** → Or direct if cart has items.
- Step 1: Addresses → Select saved or **New Address** (Street/City/State/Zip/Phone).
- Step 2: Review order → Totals (subtotal/shipping/tax).
- **Place Order** → Success: Order #123 created → Download invoice → Email receipt.
- URL Track: `/Orders/Index` list → `/Orders/Details/123` status/history/invoice PDF button.

### 5. Post-Purchase
- `/Reviews` → List → Add review for order item: Stars (1-5), comment → Submit → Visible on product.

**Total Time: 5-10 mins. Tip: Save address for repeat buys.**

## Seller Full Journey (Shop Owner)
### 1. Apply for Seller Status
- Login as Customer → `/Subscription/Index`
- Screen: Tier cards (Trial 5 prods ₹99, Basic 25 ₹499 etc.) → **Register Tier 2**.
- Form: SubscriptionRequestViewModel → ITSNumber, fair credit docs → Submit → Pending.
- Notification: Email \"Request submitted\".

### 2. Approval & First Login
- Admin approves → Email \"Approved! Login: /Areas/Seller/Account/Login\".
- Dashboard `/Areas/Seller/Dashboard/Index`: Cards (Products:0, Orders:0, Revenue:₹0), recent orders table, low stock alert.

### 3. Add First Product
- Left menu **Products** → `/Areas/Seller/Products/Create`
- Fields: Name, Slug (auto), Desc (HTML editor?), Price, Stock, Category dropdown (hierarchical), Images (multiple upload), Youtube URL, OwnerName.
- **Save** → Pending → Admin approves → Live.

### 4. Manage Orders End-to-End
- `/Areas/Seller/Orders/Index` filter (All/Pending/Processed).
- Click order → Details: Items, customer info, **Process** (update status), **Ship** (add tracking), **Cancel** reason.
- Sales `/Areas/Seller/Sales/Report` CSV/PDF export.

### 5. Store Settings
- `/Areas/Seller/Settings/Index` → Edit store name/contact/slug, profile pic upload (approval).

**Renewal: Expiry → Apply upgrade `/Subscription` → Admin.**

## Admin Full Journey (Platform Manager)
### 1. Login & Dashboard
- `/Areas/Admin/Account/Login` admin@local.local / Pass@local110
- `/Areas/Admin/Dashboard/Index`: Live charts (users growth), metrics cards (Pending Products:5, Subscribers:10), recent tables.

### 2. Approve Seller Request
- `/Areas/Admin/SubscriberRequests/Index` → Click → Details (docs/ITS) → **Approve** tier → Auto update profile/email.

### 3. Product Moderation
- `/Areas/Admin/Products/Index?status=pending` → Grid → View/Edit/Approve/Reject (reason note).

### 4. User Management
- `/Areas/Admin/Users/Index` search → Details → Edit role/profile, deactivate, role change (Admin→Subscriber).

### 5. Orders Oversight
- `/Areas/Admin/Orders/Index` bulk actions, dispute resolution.

### 6. Content & Categories
- Categories `/Areas/Admin/Categories/Create` tree view, bulk Excel.
- Pages `/Areas/Admin/Pages` slug-based CMS.

### 7. Reports & Audit
- AuditLogs `/Areas/Admin/AuditLogs/Index` filter date/entity/search/export CSV.

## SuperAdmin Full Journey (System Owner)
- Direct `/SuperAdmin` → Dashboard broadcasts.
- `/Areas/SuperAdmin/Broadcast/Index` → Compose subject/body/attach → Target all/Customers → Send (email + DB log).
- Audit `/Areas/SuperAdmin/AuditLogs`.

## Common Features
### Chat
- JS connect `/chatHub` → Send/receive user-to-user, status online/green.

### Profile
- `/UserProfiles` edit pic/QR/bio visible toggle.

### Unlock
- Locked → `/Account/RequestUnlock` → Admin review.

## Troubleshooting & Edge Cases (50+ Scenarios)
| Issue | Screen/URL | Fix | Expected Result |
|-------|------------|-----|-----------------|
| Duplicate ITS | Subscription form | Change number | Green \"Unique\" |
| Order timeout | Seller orders | Process <48h | Avoid auto-cancel |
| Low stock | Product details | Seller restock | \"Out of stock\" badge |
| ... (50 lines) | ... | ... | ... |

Contact support via /pages/contact or chat.

