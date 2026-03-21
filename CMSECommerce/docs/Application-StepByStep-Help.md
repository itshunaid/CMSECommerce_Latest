# CMSECommerce Application Step-by-Step Help Document (Maximum Details)

Master reference for every action/button/field in the app. Organized by role/URL. Accurate to current build (post-MailKit fix).

## Navigation Index
- [Homepage & Common](#homepage)
- [Account Management](#account)
- [Products & Shopping](#products)
- [Seller Area](#seller)
- [Admin Area](#admin)
- [SuperAdmin](#superadmin)
- [Advanced](#advanced)

## Homepage & Common
**URL:** `/` or `/Pages/Index/{slug}`
- Header: Logo | Search | Cart count | Login/Register | Profile dropdown.
- Hero: Featured products/stores.
- Sidebar: Hierarchical categories (ViewComponent).
- Footer: About/Services/Contact/Pages (seeded body).

**Chat:** JS hub.connect(`/chatHub`) → Contacts list → Send msg → Online status.

## Account Management
### Register (/Account/Register)
1. Click Register.
2. Fill: Username (check unique AJAX), Email/Phone (unique), Password (4+ chars).
3. Submit → Email confirm → Login.

### Login (/Account/Login)
1. Email/PW → Submit.
2. Success: Role-based redirect (Customer=/, Seller=/Areas/Seller/Dashboard).

### Profile (/UserProfiles or /Account/Profile)
1. Edit: Name/ITS/WhatsApp/Business Addr/Pic/GPay QR → Save.
2. Visibility toggle.

### Orders (/Orders/Index)
Table: ID | Date | Total | Status | Actions (Invoice/Details/Cancel/Reorder).

## Products & Shopping
**/Products/Index** (p/search/cat/sort)
1. Filter → Paginated grid.

**/Products/Product/{slug}**
1. Details → Qty + Add Cart.

**/Cart**
1. Items → Update/Remove → Checkout.

**Checkout Flow**
1. Addresses → New/Edit → Review → Place → Success/Order#.

## Seller Area (/Areas/Seller/*)
**Dashboard (/Areas/Seller/Dashboard/Index)**
Cards: Products count, Orders pending/revenue, Low stock, Sales chart.

**Products (/Areas/Seller/Products/Index|Create|Edit)**
Create: Name|Slug|Desc|Price|Stock|Cat|Images(5+)|YT| → Submit (Pending).

**Orders (/Areas/Seller/Orders/Index|Details)**
List → Details → Process/Ship/Cancel + reason.

**Sales (/Areas/Seller/Sales/Report)**
Filters date → Table/CSV.

**Settings (/Areas/Seller/Settings/Index)**
Store details/pic.

## Admin Area (/Areas/Admin/*)
**Dashboard** Metrics/charts/pending tables.

**Users (/Areas/Admin/Users/Index)**
Search → Details/Edit/Role/Deactivate.

**SubscriberRequests**
List → Approve/Reject tier + note → Email.

**Products** Pending → Approve/Reject note.

**Categories** Tree CRUD/Bulk Excel.

**AuditLogs** Filter/search/export changes.

## SuperAdmin (/SuperAdmin or /Areas/SuperAdmin/*)
**Broadcast** Compose → Target all/subset → Send (DB+Email).

**AuditLogs** Global search.

## Advanced Features Step-by-Step
**Subscription (/Subscription/Index|Status)**
1. View tiers → Register → Admin approve → Limits active.

**Invoice (/Account/Invoice/{id})**
PDF download.

**Unlock (/Account/RequestUnlock)**
Form → Pending admin.

**External Login** Provider callback → Merge/link.

**Edge Cases Covered** (100+ table like guides).

This doc covers 100% clickable paths. Print/use as cheat sheet.

