# SuperAdmin Dashboard Improvements - Approved Plan (User Focus: Dashboard + Real-time Monitoring)

Status: In Progress ✅

## Steps:

### 1. [IN PROGRESS] Create TODO.md ✅
   - Track approved plan progress.

### 2. Read/Understand Files ✅
   - SuperAdminDashboardViewModel.cs ✅
   - DashboardController.cs ✅
   - Dashboard/Index.cshtml ✅

### 3. Enhance DashboardController ✅
   - Add IMemoryCache for metrics (reduce DB load) ✅
   - Add PDF Export action (PlaywrightPdfController) 
   - Add SignalR integration for real-time pushes (extend existing ChatHub or new method).

### 4. Update ViewModel (if needed)
   - Add properties for real-time flags/cached expiry.

### 5. Enhance Dashboard View
   - Add real-time refresh: SignalR client or AJAX polling (30s interval).
   - PDF export button.
   - Loading spinners for charts/metrics.
   - Auto-update charts on new data.

### 6. Test & Update TODOs
   - dotnet build & run.
   - Verify real-time updates (simulate new order/activity).
   - Update TODO_SUPERADMIN_*.md files.
   - Mark complete.

Next Step: File analysis complete, proceed to enhancements.

