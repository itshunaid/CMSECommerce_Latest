# Order-Specific Chat Implementation

## Tasks
- [x] Add GetOrderContacts method to ChatHub.cs
- [x] Update _ChatWidget.cshtml to display contacts list
- [x] Add JavaScript logic to load and display order contacts
- [x] Test chat functionality between buyers and sellers
- [x] Ensure proper status updates

## Information Gathered
- SignalR-based chat with ChatHub, ChatMessage model, and chat widget
- User online/offline status tracked via UserStatusService and UserStatusTracker
- Orders link buyers to sellers via OrderDetail.ProductOwner
- Products have OwnerId for sellers
- Chat widget exists but needs modification for order-based contacts

## Plan
- Add GetOrderContacts method to ChatHub: Returns contacts with online status based on orders
- Modify _ChatWidget.cshtml to display contacts list with online/offline indicators
- Add JavaScript to load order-based contacts on chat open and update status
- Ensure private messaging and history work for these contacts

## Dependent Files
- Hubs/ChatHub.cs: Add GetOrderContacts method
- Views/Shared/_ChatWidget.cshtml: Update HTML for contacts list
- wwwroot/js/chat-widget-init.js: Add logic to load and display contacts
