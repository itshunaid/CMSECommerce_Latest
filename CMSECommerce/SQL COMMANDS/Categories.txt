Delete from Categories;
-- Allow manual insertion of IDs
SET IDENTITY_INSERT Categories ON;

-- LEVEL 0: HEADINGS
INSERT INTO Categories (Id, Name, Slug, [Level], ParentId) VALUES
(1, 'Apparel & Fashion', 'apparel-fashion', 0, NULL),
(2, 'Food & Refreshments', 'food-refreshments', 0, NULL),
(3, 'Home & Industry', 'home-industry', 0, NULL),
(4, 'Health & Care', 'health-care', 0, NULL),
(5, 'Professional Services', 'professional-services', 0, NULL),
(6, 'Gems & Jewelry', 'gems-jewelry', 0, NULL),
(7, 'Gifts & Stationery', 'gifts-stationery', 0, NULL);

-- LEVEL 1: CATEGORIES
INSERT INTO Categories (Id, Name, Slug, [Level], ParentId) VALUES
(8, 'Clothing & Accessories', 'clothing-accessories', 1, 1),
(9, 'Dawoodi Bohra Specialty', 'dawoodi-bohra-specialty', 1, 1),
(10, 'Leather Products', 'leather-products', 1, 1),
(11, 'Textiles', 'textiles', 1, 1),
(12, 'Bakery & Confectionery', 'bakery-confectionery', 1, 2),
(13, 'Staples', 'staples', 1, 2),
(14, 'Specialty Foods', 'specialty-foods', 1, 2),
(15, 'Construction & Garden', 'construction-garden', 1, 3),
(16, 'Beauty & Skincare', 'beauty-skincare', 1, 4),
(17, 'Wellness', 'wellness', 1, 4),
(18, 'Education', 'education', 1, 5),
(19, 'Catering', 'catering', 1, 5),
(20, 'Precious Stones', 'precious-stones', 1, 6),
(21, 'Traditional Jewelry', 'traditional-jewelry', 1, 6),
(22, 'Office Supplies', 'office-supplies', 1, 7),
(23, 'Gifts', 'gifts', 1, 7);

-- LEVEL 2: SUB-CATEGORIES (Rectified Mappings)
INSERT INTO Categories (Id, Name, Slug, [Level], ParentId) VALUES
-- Under Clothing & Accessories (8)
(24, 'Men''s Clothing', 'mens-clothing', 2, 8),
(25, 'Women''s Clothing', 'womens-clothing', 2, 8),
(26, 'Kid''s Clothing', 'kids-clothing', 2, 8),
-- Under Dawoodi Bohra Specialty (9)
(27, 'Rida', 'rida', 2, 9),
(28, 'Jodi', 'jodi', 2, 9),
(29, 'Topi', 'topi', 2, 9),
(30, 'Masallah', 'masallah', 2, 9),
-- Under Leather Products (10)
(31, 'Belts', 'belts', 2, 10),
(32, 'Wallets', 'wallets', 2, 10),
(33, 'Handbags', 'handbags', 2, 10),
(34, 'Laptop Bags', 'laptop-bags', 2, 10),
-- Under Textiles (11)
(35, 'Fabrics', 'fabrics', 2, 11),
(36, 'Yarn', 'yarn', 2, 11),
(37, 'Thread', 'thread', 2, 11),
(38, 'Sewing Accessories', 'sewing-accessories', 2, 11),
-- Under Bakery & Confectionery (12)
(39, 'Cakes', 'cakes', 2, 12),
(40, 'Biscuits', 'biscuits', 2, 12),
(41, 'Cookies', 'cookies', 2, 12),
(42, 'Chocolates', 'chocolates', 2, 12),
-- Under Staples (13)
(43, 'Cereals', 'cereals', 2, 13),
(44, 'Food Grains', 'food-grains', 2, 13),
(45, 'Edible Oils', 'edible-oils', 2, 13),
-- Under Specialty Foods (14)
(46, 'Dry Fruits', 'dry-fruits', 2, 14),
(47, 'Honey', 'honey', 2, 14),
(48, 'Pickles', 'pickles', 2, 14),
(49, 'Jams', 'jams', 2, 14),
(50, 'Tea', 'tea', 2, 14),
(51, 'Coffee', 'coffee', 2, 14),
(52, 'Spices', 'spices', 2, 14),
-- Under Construction & Garden (15)
(53, 'Building Materials', 'building-materials', 2, 15),
(54, 'Interior Design', 'interior-design', 2, 15),
(55, 'Gardening Tools', 'gardening-tools', 2, 15),
-- Under Beauty & Skincare (16)
(56, 'Cosmetics', 'cosmetics', 2, 16),
(57, 'Soaps', 'soaps', 2, 16),
(58, 'Detergents', 'detergents', 2, 16),
(59, 'Herbal Supplements', 'herbal-supplements', 2, 16),
-- Under Wellness (17)
(60, 'Health Services', 'health-services', 2, 17),
(61, 'Wellness Products', 'wellness-products', 2, 17),
-- Under Education (18)
(62, 'Tutors', 'tutors', 2, 18),
(63, 'Skill Development', 'skill-development', 2, 18),
-- Under Catering (19)
(64, 'Event Catering', 'event-catering', 2, 19),
(65, 'Tiffin Services', 'tiffin-services', 2, 19),
-- Under Precious Stones (20)
(66, 'Loose Gemstones', 'loose-gemstones', 2, 20),
-- Under Traditional Jewelry (21)
(67, 'Gold & Silver Ornaments', 'gold-silver-ornaments', 2, 21),
-- Under Office Supplies (22)
(68, 'Stationery', 'stationery', 2, 22),
(69, 'Packaging', 'packaging', 2, 22),
-- Under Gifts (23)
(70, 'Corporate Gifts', 'corporate-gifts', 2, 23),
(71, 'Birthday Gifts', 'birthday-gifts', 2, 23),
(72, 'Anniversary Gifts', 'anniversary-gifts', 2, 23);

SET IDENTITY_INSERT Categories OFF;
Select * from Categories;