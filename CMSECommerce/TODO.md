# TODO: Implement Model Classes with Proper Normalization and Relations

## Information Gathered
- Reviewed all model files: Address.cs, CartItem.cs, Category.cs, ChatMessage.cs, Order.cs, OrderDetail.cs, Page.cs, Product.cs, ProductReview.cs, SubscriberRequest.cs, SubscriptionRequest.cs, SubscriptionTier.cs, UnlockRequest.cs, User.cs, UserProfile.cs, UserStatusSetting.cs, UserStatusTracker.cs.
- DataContext.cs defines DbSets for these models, including Store (nested in UserProfile.cs).
- Current issues: Redundant data in OrderDetail.cs (ProductName, Image), nested Review in Product.cs vs separate ProductReview.cs, missing navigation properties in some models, lack of foreign key constraints.

## Plan
- Normalize models to reduce redundancy (e.g., remove ProductName, Image from OrderDetail, use ProductId navigation).
- Add proper foreign keys and navigation properties for relations (e.g., Order to User, OrderDetail to Product).
- Consolidate Review and ProductReview into one model.
- Ensure all models follow e-commerce relations: User-Address, User-Order, Order-OrderDetail, Product-Category, etc.
- Update DataContext.cs if needed for relations.

## Dependent Files to Edit
- Models/Product.cs: Remove nested Review, add relations.
- Models/ProductReview.cs: Ensure consistency.
- Models/Order.cs: Add UserId, User navigation.
- Models/OrderDetail.cs: Remove redundant fields, add ProductId, Product navigation.
- Models/Address.cs: Add User navigation.
- Models/ChatMessage.cs: Add Order navigation if needed.
- Models/Category.cs: Add Products navigation.
- Models/SubscriberRequest.cs: Add User navigation.
- Models/SubscriptionRequest.cs: Add User navigation.
- Models/UnlockRequest.cs: Add User navigation.
- Models/UserProfile.cs: Ensure Store relations.
- Infrastructure/DataContext.cs: Update OnModelCreating for relations if needed.

## Followup Steps
- Run migrations to update database schema. ✅
- Test relations in controllers and views.
- Verify no data loss during normalization.
