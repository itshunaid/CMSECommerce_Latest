# Weypaari Project Platform User Guide

Welcome to the Weypaari Project Platform! Imagine this as a big online marketplace, like a digital mall where people can buy and sell things. It's designed to make it easy for everyone involved—whether you're running the show, selling items, or shopping—to work together smoothly. This guide will walk you through everything step by step, like a friendly tour guide. We'll explain things in simple words, with examples, so even if you're new to online shopping or selling, you'll feel confident.

## Table of Contents
1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Admin User Guide](#admin-user-guide)
4. [Seller User Guide](#seller-user-guide)
5. [Customer User Guide](#customer-user-guide)
6. [Troubleshooting](#troubleshooting)
7. [FAQs](#faqs)
8. [Common Terms Explained](#common-terms-explained)

## Introduction
The Weypaari Platform is like a busy online store where three types of people play important roles:
- **Admin**: Think of them as the store manager. They keep everything running smoothly, approve new sellers, check products, and handle any problems. It's like being the boss who makes sure the mall is safe and fun for everyone.
- **Seller**: These are the shop owners. They set up their own little stores, add products to sell, and handle orders. For example, if you make handmade jewelry, you'd be a seller listing your items here.
- **Customer**: These are the shoppers. They browse products, add them to a virtual cart, buy them, and leave reviews. It's just like going to a regular online shop like Amazon.

Everyone starts by creating an account and logging in. Admins are special—they're usually invited or assigned by other admins. The platform is built using modern web technology (don't worry, you don't need to know the details), with separate sections for admins and sellers to keep things organized.

Why use this platform? It's made for communities, like groups sharing goods, with features to help everyone succeed. Whether you're selling homemade goods or buying unique items, this guide will help you navigate it easily.

## Getting Started
Ready to join? Here's how to get started, step by step. It's like signing up for a new app on your phone.

1. **Open the Website**: Go to the platform's web address (URL) in your web browser. You'll see the homepage with options like "Register" or "Login."
   
2. **Create an Account**: Click the "Register" button. Fill in your details like name, email, and a password. Choose your role—if you're not an admin, select "Customer" for now. (Admins are usually added by existing ones.) Tip: Use a strong password with letters, numbers, and symbols to keep your account safe.

3. **Confirm Your Email**: After registering, check your email for a confirmation link. Click it to activate your account. If you don't see it, check your spam folder.

4. **Log In**: Go back to the site and click "Login." Enter your email and password. If you forget your password, click "Forgot Password" and follow the steps to reset it.

5. **Explore**: Once logged in, you'll see a dashboard or homepage based on your role. If you're a customer wanting to sell, you can apply later.

Common issues: If the site doesn't load, try refreshing your browser or using a different one. Make sure you're connected to the internet. If you get stuck, note the error message and check the Troubleshooting section below.

## Admin User Guide

As an admin, you're like the captain of the ship. Your job is to keep the platform fair, safe, and running well. You'll approve new sellers, check products, and fix issues. Let's break it down with examples and tips.

### 1. Login
- Go to the special admin login page (usually something like `/Areas/Admin/Account/Login`—your browser's address bar will show this).
- Type in your admin username and password.
- Click "Login." If it works, you'll be taken to your admin dashboard. Tip: Always log out when done to protect your account.

### 2. Dashboard Overview
This is your control center. It's like a car's dashboard showing how the platform is doing. You'll see numbers like:
- Total Users: How many people are signed up.
- Total Products: All items for sale.
- Pending Product Requests: New items waiting for your approval.
- Total Orders: How many purchases have been made.
- Categories Count: How many product groups there are (like "Clothing" or "Electronics").
- Pending Subscriber Requests: Sellers waiting to join.
- Deactivated Stores Count: Shops that are temporarily closed.
- Recent Orders: Latest buys.
- Sellers with Declined Orders: Shops with canceled purchases.

Use this to spot problems early. For example, if "Pending Subscriber Requests" is high, you might need to approve more sellers. Check it daily to keep things smooth.

### 3. Managing Users
- Go to the users page (`/Areas/Admin/Users/Index`).
- You'll see a list of everyone signed up. Click on a name to view details.
- You can change their role (e.g., make a customer a seller), edit info, or deactivate accounts if needed (like if someone is misbehaving).
- For new sign-ups, approve or reject them. Example: If someone registers as a seller but doesn't provide enough info, reject and explain why.

Tip: Be fair—only deactivate if rules are broken. Always explain your decisions.

### 4. Managing Products
- Visit the products page (`/Areas/Admin/Products/Index`).
- See all products. Some are "Pending" (waiting for approval), "Active" (live for sale), or "Rejected."
- Click to approve or reject. For rejections, add a note like "Photo is blurry—please upload a clearer one."
- Edit details if needed, like fixing a wrong price, or remove bad products.

Example: A seller adds a toy but it's unsafe—reject it and tell them to fix it. Warning: Check for scams or fake items.

### 5. Managing Orders
- Go to orders (`/Areas/Admin/Orders/Index`).
- View all purchases: who bought what, from which seller, status (e.g., "Processed," "Shipped," "Cancelled"), and dates.
- Update status if needed. For disputes, talk to the buyer and seller.

Tip: If an order is stuck, contact the seller. Encourage quick shipping to keep customers happy.

### 6. Managing Categories
- Head to categories (`/Areas/Admin/Categories/Index`).
- Add new groups (e.g., "Home Goods"), edit names, or delete unused ones.
- Keep them organized so customers can find things easily. Example: If there are too many "Misc" categories, create specifics like "Books" and "Art."

### 7. Managing Sellers (Subscriber Requests)
- Check pending requests (`/Areas/Admin/SubscriberRequests/Index`).
- Review applications: Look at details like their ID number or credit info.
- Approve to let them sell, or reject with feedback. Assign a "tier" (like a membership level) based on their plan.
- For approved sellers, manage their shops—activate or deactivate if issues arise.

Example: A baker wants to sell cakes—approve if they have good reviews. Tip: Verify info to prevent fraud.

### 8. System Settings and Pages
- Edit pages like "About Us" or "Contact" at `/Areas/Admin/Pages/Index`.
- Moderate profile pictures at `/Areas/Admin/UsersProfile/Index`—remove inappropriate ones.
- Adjust settings, like email notifications.

### End-to-End Workflow for Admin
1. Log in and check the dashboard for quick updates.
2. Approve waiting sellers and products.
3. Fix any order problems.
4. Organize users and categories.
5. Review deactivated shops and profiles regularly.

Remember, your role is to help everyone succeed. Be patient and clear in communications.

## Seller User Guide

Sellers, you're the heart of the marketplace! You create and manage your own shop. Think of it as setting up a stall at a fair. Here's how, with tips to make selling easy.

### 1. Registration and Subscription
- First, sign up as a customer (see Getting Started).
- Then, apply to be a seller at `/Subscription/Index`. Fill out a form with your details, like an ID number or credit info.
- Submit and wait for admin approval—it might take a day or two.

Tip: Prepare good photos and honest info to get approved faster. Example: If you're selling crafts, mention your experience.

### 2. Login
- Use the seller login (`/Areas/Seller/Account/Login`).
- Enter your details and access your seller dashboard.

### 3. Dashboard Overview
Your dashboard (`/Areas/Seller/Dashboard/Index`) shows your shop's health:
- How many products you have.
- Items low on stock (so you can restock).
- Orders waiting, processed, or canceled.
- Recent sales.

Use it to see what's selling and what needs attention. Check it often to stay on top.

### 4. Managing Products
- Go to products (`/Areas/Seller/Products/Index`).
- Add new items: Name, description, price, stock amount, photos, and category. Example: For a shirt, add sizes and colors.
- Edit or update stock. If required, submit for admin approval.

Tip: Use clear, high-quality photos. Describe items honestly to avoid rejections. Warning: Don't overprice—customers will leave reviews.

### 5. Managing Orders
- View orders (`/Areas/Seller/Orders/Index`).
- See what customers bought from you. Update status: "Process" (pack it), "Ship" (send it), or "Cancel."
- If not handled quickly, orders might auto-cancel—act fast!

Example: A customer orders shoes—process within 24 hours. Tip: Communicate with buyers if delays happen.

### 6. Store Settings
- Update your shop profile (`/Areas/Seller/UserProfiles/Index`).
- Change details, set your store as active or inactive, and add profile pictures.

### End-to-End Workflow for Seller
1. Get approved as a seller.
2. Log in and list your products.
3. Watch the dashboard for orders and low stock.
4. Fulfill orders quickly.
5. Update your store and respond to reviews.

Success tip: Engage with customers—good reviews bring more buyers!

## Customer User Guide

Shopping here is fun and easy, like browsing a store app. As a customer, you can find, buy, and review products. Let's make it simple.

### 1. Registration and Login
- Register at `/Account/Register` with your info.
- Log in at `/Account/Login`.

Tip: Keep your password safe and update your email if it changes.

### 2. Browsing Products
- Visit the products page (`/Products/Index`) to see everything.
- Use the category menu (like a store directory) to filter, e.g., "Electronics."
- Search by typing keywords, like "red dress."

Example: Looking for books? Click "Books" category and search "mystery."

### 3. Product Details and Cart
- Click a product to see details (`/Products/Details/{id}`)—photos, price, reviews.
- Add to your cart (`/Cart/Index`). You can change quantities or remove items.
- See totals and shipping costs.

Tip: Compare prices and read reviews before buying.

### 4. Checkout and Orders
- From the cart, click "Checkout."
- Enter your address and payment info (use secure methods like credit cards).
- Confirm the order—you'll get a receipt.
- Track past orders at `/Orders/Index`.

Safety tip: Only buy from trusted sellers. If something's wrong, contact support.

### 5. Managing Profile and Reviews
- Edit your profile (`/UserProfiles/Index`) for updates.
- Leave reviews (`/Reviews/Index`) after buying—share your experience.

### End-to-End Workflow for Customer
1. Sign up and log in.
2. Browse and search for items.
3. Add to cart and buy.
4. Track orders and review products.
5. Update your account as needed.

Enjoy shopping! Remember, honest reviews help everyone.

## Troubleshooting
Problems happen, but here's how to fix common ones simply.

- **Can't Log In**: Double-check username/password. Try "Forgot Password." If locked out, contact support.
- **Order Delays**: Email the seller or admin for updates. Sellers, check your dashboard.
- **Product Not Approved**: Sellers, see admin feedback and resubmit. Admins, provide clear reasons.
- **Payment Errors**: Verify card details. Try again or use another method. Contact your bank if needed.
- **Site Slow or Broken**: Refresh the page or clear browser cache. Try a different device.
- **Forgot How to Do Something**: Re-read this guide or search the FAQs.

If stuck, note what you're doing and the error, then ask for help via the Contact page.

## FAQs
- **How do I become a Seller?** Sign up as a customer, then apply at Subscription. Wait for approval.
- **What if my product is rejected?** Fix the issues (like better photos) and resubmit. Admins will explain why.
- **Can I cancel an order?** Customers can request it; sellers/admins decide. Do it quickly.
- **How to contact support?** Use the Contact page or chat feature. Be polite and detailed.
- **Is my info safe?** Yes, we use security measures. Don't share passwords.
- **How do reviews work?** After buying, leave a rating and comment. Sellers see them to improve.
- **What are subscription tiers?** Levels for sellers, like basic or premium, with different features.
- **Can I change my role?** Admins can adjust; otherwise, apply for changes.

## Common Terms Explained
- **Dashboard**: Your personal control panel showing key info.
- **Pending**: Waiting for approval or action.
- **Order Status**: Where your purchase is (e.g., shipped means it's on the way).
- **Category**: A group for products, like "Food" or "Clothes."
- **Subscription**: A plan for sellers to join and sell.
- **Profile**: Your account details and settings.
- **Cart**: Virtual basket for items before buying.
- **Checkout**: The final step to pay and confirm purchase.

This updated guide should make using Weypaari easy and enjoyable for everyone. If things change, check back for updates. Happy using the platform!
