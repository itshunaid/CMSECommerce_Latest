# TODO: Fix '_unitOfWork' does not exist in current context

## Tasks
- [x] Update ChatHub.cs constructor to inject IUnitOfWork and assign _unitOfWork
- [x] Replace _context usage with _unitOfWork.Repository<T>() in ProductService.cs
- [x] Replace _context usage with _unitOfWork.Repository<T>() in ValidationService.cs
- [x] Update DashboardController.cs constructor to inject IUnitOfWork and assign _unitOfWork
- [x] Update ProductQueryService.cs constructor to inject IUnitOfWork and assign _unitOfWork
- [x] Update UserStatusService.cs constructor to inject IUnitOfWork and assign _unitOfWork
- [x] Test compilation to ensure no errors
