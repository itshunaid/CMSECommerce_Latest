# Admin Quick Reference — CMSECommerce

Key URLs
- Admin area root: `/admin`
- Manage products: `/admin/products`
- Manage orders: `/admin/orders`
- Manage users: `/admin/users`

Common tasks
- Approve product: open product in admin list and click `Approve` (or `Set Pending` to revert)
- Reject product: click `Reject`, enter reason in modal, submit — owner will get email (if SMTP configured)
- View order details: open `/admin/orders` and click `Details` to view line items
- Toggle shipped: in `/admin/orders` change the shipped checkbox — the form auto-submits

Debugging tips
- No product image: check `wwwroot/media/products/` for files
- Email not sent: inspect SMTP config and logs from `Infrastructure/SmtpEmailSender`
- Roles missing: ensure `Program.cs` seeds `Admin` and `Customer` roles on startup

Notes
- Subscriber flow is removed; only `Admin` and `Customer` roles remain.
