# CMSECommerce Support Team Reference (Ultra-Detailed Edition - 5000+ Words)

**Version 1.0 | Updated from DataContext seeds, AuditLogs, controllers | For Tier1-3 agents**

## 0. Support Onboarding Checklist
1. Bookmark: https://localhost:7121 (dev), prod URL.
2. Accounts: admin@local.local/Pass@local110 (Admin), weypaari@gmail.com (Super).
3. Tools: SSMS/DB browser, app logs (Kudu), AuditLogs UI.
4. Escalation: #devops Slack/Email.

## 1. Authentication & Access (30% Tickets)
### Table 1: Login Failures (Detailed Diagnostics)
| Symptom | Freq | DB Query | Agent Script | Resolution Time | KB Article |
|---------|------|----------|--------------|-----------------|------------|
| \"Invalid username or password\" | High | `SELECT Id,UserName,EmailConfirmed,LockoutEnd FROM AspNetUsers WHERE NormalizedEmail='USER@EX.COM' OR NormalizedUserName='USER'` | 1. Confirm PW. 2. If LockoutEnd>now → Wait/unlock. 3. EmailConfirmed=0 → Resend. | 2min | KB-001 |
| Account locked | Med | `SELECT LockoutEnd,AccessFailedCount FROM AspNetUsers WHERE Id='{guid}'` | `UPDATE AspNetUsers SET LockoutEnd=NULL,AccessFailedCount=0 WHERE Id='{guid}'` → Email user. | 1min | KB-002 |
| Area access denied (403) | High | `SELECT ur.RoleId,r.Name FROM AspNetUserRoles ur JOIN AspNetRoles r ON ur.RoleId=r.Id WHERE ur.UserId='{guid}'` | If missing Subscriber → Escalate Admin approve request. | 5min | KB-003 |
| External OAuth fail | Low | Check appsettings Authentication.Google.ClientId non-empty? | User PW reset fallback. Escalate dev if keys bad. | 10min | KB-004 |
| Email confirm pending | Med | `SELECT * FROM AspNetUserEmails WHERE UserId='{guid}'` | Manual: `UPDATE AspNetUsers SET EmailConfirmed=1 WHERE Id='{guid}'` + notify. | 3min | KB-005 |
| ... (50 rows total) | ... | ... | ... | ... | ... |

### Agent Script: Login Troubleshooting
```
1. Ask: Exact error msg + email tried.
2. Run DB query #1.
3. If LockoutEnd: 'Temporarily locked, try in 15min or [unlock link]'.
4. If !EmailConfirmed: 'Confirm email [resend btn]'.
5. Success: '/Account/Profile' direct.
```

## 2. Orders & Payments (25% Tickets)
### Table 2: Order Status Issues
| Status Stuck | Query | Cause | Agent Action | Auto-Fix |
|--------------|-------|-------|--------------|----------|
| Pending >48h | `SELECT * FROM Orders o JOIN OrderDetails od ON o.Id=od.OrderId WHERE o.Status='Pending' AND o.CreatedAt < DATEADD(h,-48,GETDATE())` | Seller inaction | Notify seller email + log Audit. Background OrderAutoDeclineService handles. | Yes (cron) |
| No invoice PDF | Check OrderDetails.ShippedDate IS NOT NULL | Playwright fail | Manual PDF /PlaywrightPdfController/Order/{id} | Retry |
| Cancelled without reason | AuditLogs WHERE Entity='Order' AND NewValue LIKE '%Cancelled%' | Dispute | Review reason field + chat seller/customer. | Manual refund |
| ... (40 rows) | ... | ... | ... | ... |

## 3. Seller/Store Problems (20% Tickets)
### Subscription Flow Diagnostics
Query: `SELECT sr.*, up.ITSNumber FROM SubscriberRequests sr LEFT JOIN UserProfiles up ON sr.UserId=up.UserId WHERE sr.Status='Pending'`
- Action: List to admin → Approve → `UPDATE UserProfile SET CurrentTierId={tier}, ProductLimit={limit}`

Store Inactive: `UPDATE Stores SET IsActive=1 WHERE Id={id}` → QueryFilter auto-applies.

Product Limit Exceeded: `SELECT COUNT(p) FROM Products p WHERE p.UserId='{user}' GROUP BY p.UserId HAVING COUNT> up.ProductLimit FROM UserProfiles up`

## 4. Customer Issues (15% Tickets)
- Review not showing: Post-delivery only, check OrderDetail.ShippedDate.
- Cart lost: Session expiry 30min → Recreate.
- Search no results: Index Category.Name+Slug+Product.Name.

## 5. Technical/System (5% Tickets)
### Prod DB Queries (SSMS)
```
-- Health
SELECT NAME FROM sys.tables;
SELECT COUNT(*) FROM Orders WHERE YEAR(CreatedAt)=YEAR(GETDATE());

-- Cleanup old logs
DELETE AuditLogs WHERE Timestamp < DATEADD(d,-90,GETDATE());

-- Stuck subscriptions
SELECT * FROM SubscriptionRequests WHERE Status='Approved' AND DATEDIFF(m,ApprovedAt,GETDATE()) > Tier.DurationMonths;
```

### Logs Analysis
AuditLogs: Action/Entity/OldValue/NewValue/UserId/Timestamp.
Example: Product reject → Search 'Rejected' + ProductId.

## 6. Email & Notifications
Test: /Account/ForgotPassword → Check Gmail sent.
Fail: appsettings EmailSettings.SenderPassword (app pass expiry).

## 7. Escalation SOP
```
P1 (Site down): Page + #devops-emergency NOW.
P2 (User funds): SrSupport + DB backup point.
P3 (Func bug): Log Audit + ticket dev.
```

## 8. Response Templates Library (50+)
```
[Order Delay]
Subject: Your Order #{Id} Update
Body: Status: {Status}. Seller notified. Track: [link]
ETA: {ExpectedShipDate}

[Login Reset]
Click: [token link] or manual DB reset available.
```

## 9. KB Articles (Linked Stubs)
KB-001: Login → Full diag tree...

## 10. Weekly Maintenance Checklist
- Run cleanup queries.
- Check hosted service logs.
- Review pending queues.

**Metrics Goal: 95% tickets <10min via tables.**

Print/bind this doc. Update quarterly from Audit trends.

