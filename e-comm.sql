-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Jun 02, 2026 at 04:00 AM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `e-comm`
--

DELIMITER $$
--
-- Procedures
--
CREATE DEFINER=`root`@`localhost` PROCEDURE `adjust_stock` (IN `p_product_id` INT, IN `p_quantity_change` INT)   BEGIN
    UPDATE product 
    SET stock = stock + p_quantity_change 
    WHERE product_id = p_product_id;
END$$

--
-- Functions
--
CREATE DEFINER=`root`@`localhost` FUNCTION `calculate_vat` (`price` DECIMAL(10,2)) RETURNS DECIMAL(10,2) DETERMINISTIC BEGIN
    DECLARE vat_amount DECIMAL(10,2);
    SET vat_amount = price * 0.12;
    RETURN vat_amount;
END$$

DELIMITER ;

-- --------------------------------------------------------

--
-- Table structure for table `category`
--

CREATE TABLE `category` (
  `category_id` int(11) NOT NULL,
  `cat_name` varchar(100) NOT NULL,
  `descript` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

--
-- Dumping data for table `category`
--

INSERT INTO `category` (`category_id`, `cat_name`, `descript`) VALUES
(1, 'Electronics', 'Gadgets, devices, and accessories.'),
(2, 'Computers', 'Laptops, desktops, and computer parts.'),
(3, 'Smartphones', 'Mobile phones and related tech.'),
(4, 'Audio', 'Headphones, speakers, and sound systems.'),
(5, 'Wearables', 'Smartwatches and fitness trackers.'),
(6, 'Cameras', 'DSLRs, mirrorless, and action cameras.'),
(7, 'Gaming', 'Consoles, controllers, and video games.'),
(8, 'Home Appliances', 'Smart home devices and kitchenware.'),
(9, 'Storage', 'Hard drives, SSDs, and flash drives.'),
(10, 'Peripherals', 'Keyboards, mice, and monitors.');

-- --------------------------------------------------------

--
-- Table structure for table `order`
--

CREATE TABLE `order` (
  `order_id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `order_date` datetime DEFAULT current_timestamp(),
  `total_amount` decimal(10,2) DEFAULT 0.00,
  `status` enum('Pending','Paid','Shipped','Cancelled') DEFAULT 'Pending'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

--
-- Dumping data for table `order`
--

INSERT INTO `order` (`order_id`, `user_id`, `order_date`, `total_amount`, `status`) VALUES
(1, 3, '2026-02-20 10:30:00', 25.99, 'Paid'),
(2, 5, '2026-02-21 14:15:00', 899.00, 'Shipped'),
(3, 1, '2026-02-22 09:45:00', 725.49, 'Pending'),
(4, 7, '2026-02-22 11:00:00', 199.99, 'Paid'),
(5, 5, '2026-02-23 16:20:00', 49.95, 'Shipped'),
(6, 6, '2026-02-24 08:00:00', 1200.00, 'Paid'),
(7, 7, '2026-02-24 12:30:00', 499.99, 'Cancelled'),
(8, 4, '2026-02-25 10:00:00', 129.00, 'Pending'),
(9, 8, '2026-02-25 11:15:00', 319.98, 'Paid'),
(10, 10, '2026-02-25 13:45:00', 89.00, 'Pending');

-- --------------------------------------------------------

--
-- Table structure for table `order_item`
--

CREATE TABLE `order_item` (
  `orderitem_id` int(11) NOT NULL,
  `order_id` int(11) DEFAULT NULL,
  `product_id` int(11) DEFAULT NULL,
  `quantity` int(11) NOT NULL DEFAULT 1,
  `unit_price` decimal(10,2) NOT NULL
) ;

--
-- Dumping data for table `order_item`
--

INSERT INTO `order_item` (`orderitem_id`, `order_id`, `product_id`, `quantity`, `unit_price`) VALUES
(1, 1, 1, 1, 25.99),
(2, 2, 2, 1, 899.00),
(3, 3, 3, 1, 699.50),
(4, 3, 1, 1, 25.99),
(5, 4, 4, 1, 199.99),
(6, 5, 5, 1, 49.95),
(7, 6, 6, 1, 1200.00),
(8, 7, 7, 1, 499.99),
(9, 9, 9, 2, 159.99),
(10, 10, 10, 1, 89.00);

--
-- Triggers `order_item`
--
DELIMITER $$
CREATE TRIGGER `deleteEntry_orderItem` BEFORE DELETE ON `order_item` FOR EACH ROW begin
	if old.order_id is not null then
	update `order`
	set total_amount = total_amount - (old.quantity * old.unit_price)
	where order_id = old.order_id;
    
    update `product`
    set stock = stock + old.quantity
    where product_id = old.product_id;
	end if;
end
$$
DELIMITER ;
DELIMITER $$
CREATE TRIGGER `insertEntry_orderItem` AFTER INSERT ON `order_item` FOR EACH ROW begin
	update `order`
	set total_amount = total_amount + (new.quantity * new.unit_price)
	where order_id = new.order_id;

	update `product`
	set stock = stock - new.quantity
	where product_id = new.product_id;
end
$$
DELIMITER ;
DELIMITER $$
CREATE TRIGGER `updatedQuantityPrice_orderItem` AFTER UPDATE ON `order_item` FOR EACH ROW begin
	if (old.quantity <> new.quantity or old.unit_price <> new.unit_price) then
	update `order`
	set total_amount = (total_amount - (old.quantity * old.unit_price)) + (new.quantity * new.unit_price)
	where order_id = new.order_id;
    
    update `product`
    set stock = stock + (new.quantity - old.quantity)
    where product_id = new.product_id;
	end if;
    

end
$$
DELIMITER ;

-- --------------------------------------------------------

--
-- Table structure for table `product`
--

CREATE TABLE `product` (
  `product_id` int(11) NOT NULL,
  `category_id` int(11) DEFAULT NULL,
  `product_name` varchar(175) NOT NULL,
  `price` decimal(10,2) DEFAULT NULL,
  `stock` int(11) DEFAULT 0,
  `is_active` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

--
-- Dumping data for table `product`
--

INSERT INTO `product` (`product_id`, `category_id`, `product_name`, `price`, `stock`, `is_active`) VALUES
(1, 4, 'Noise Cancelling Headphones', 199.99, 85, 1),
(2, 1, 'Wireless Charging Pad', 25.99, 150, 1),
(3, 2, 'ProBook 14-inch Laptop', 899.00, 45, 1),
(4, 10, 'Mechanical RGB Keyboard', 89.00, 110, 1),
(5, 7, 'Next-Gen Gaming Console', 499.99, 5, 1),
(6, 9, '2TB External SSD', 159.99, 60, 1),
(7, 6, '4K Mirrorless Camera', 1200.00, 12, 1),
(8, 3, 'Galaxy Smartphone X', 699.50, 30, 1),
(9, 8, 'Smart Coffee Maker', 129.00, 25, 1),
(10, 5, 'Fitness Tracker Pro', 49.95, 200, 1);

-- --------------------------------------------------------

--
-- Table structure for table `user`
--

CREATE TABLE `user` (
  `user_id` int(11) NOT NULL,
  `username` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `pass_hash` varchar(255) NOT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `role` varchar(20) NOT NULL DEFAULT 'user'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

--
-- Dumping data for table `user`
--

INSERT INTO `user` (`user_id`, `username`, `email`, `pass_hash`, `created_at`, `is_active`, `role`) VALUES
(1, 'jdoe88', 'john.doe@email.com', '$2y$10$eImiTXuWVxjW72P', '2026-02-25 10:39:50', 1, 'admin'),
(2, 'alice_w', 'alice.wonder@email.com', '$2y$10$92IXUNpkjO0rOQ5', '2026-02-25 10:39:50', 1, 'user'),
(3, 'tech_pro', 'admin@techsolutions.com', '$2y$10$8K1p/aeBYf72uL9', '2026-02-25 10:39:50', 1, 'user'),
(4, 'm_smith', 'mark.smith@provider.net', '$2y$10$v7Nn/pW.Y6y2K1r', '2026-02-25 10:39:50', 1, 'user'),
(5, 'sarah_con', 's.connor@cyberdyne.com', '$2y$10$Qj8uK.m1Gz1X4eP', '2026-02-25 10:39:50', 1, 'user'),
(6, 'b_wayne', 'bruce@wayneent.com', '$2y$10$L9p1M.r2Hw3Y5tQ', '2026-02-25 10:39:50', 1, 'user'),
(7, 'diana_p', 'diana@themyscira.org', '$2y$10$Z3s4X.c5Vv6B7nN', '2026-02-25 10:39:50', 1, 'user'),
(8, 'p_parker', 'peter.p@dailybugle.com', '$2y$10$M8k9J.h0Gg1F2dS', '2026-02-25 10:39:50', 1, 'user'),
(9, 'tony_s', 'stark@starkindustries.io', '$2y$10$W1q2E.r3Tt4Y5uU', '2026-02-25 10:39:50', 1, 'user'),
(10, 'nat_roman', 'blackwidow@shield120.gov', '$2y$10$D4f5G.h6Jj7K8lL', '2026-02-25 10:39:50', 1, 'user'),
(11, 'admin', 'admin@ecomm.com', '$2a$10$SKI4O9SmD2cj3q8DGzIEq.hTrS4LQ1lKgHg/4/wj3bvENr3r/X3Mq', '2026-06-01 13:45:06', 1, 'admin'),
(12, 'erick', 'eri@camota.com', '$2a$10$eO9VTAuU20Wg9eWgQcAJROgW0R4wl/O9AbPoYE3A6mH3OEZd0X.Fi', '2026-06-01 14:42:54', 1, 'user'),
(13, 'jm', 'jm@co.com', '$2a$10$tC5H40kWrRPmlPS9Wzg5i.SL1mASmyUDQoIEdFfdpLc0nIUEj9sWC', '2026-06-01 14:46:20', 1, 'user'),
(14, 'joe', 'joe@coma.io', '$2a$10$d6VjZJuubrJFNsIXm9DghO9CehowYhOFNEgG1ks3xIyx/A4vsuM9G', '2026-06-01 15:07:31', 1, 'user'),
(15, 'jen', 'jen@12345.com', '$2a$10$gNxBT9ADwMeX/drCiRYyeuUUseVP.BlOKvk8SqbTRX51OgfQnywXm', '2026-06-01 15:09:13', 1, 'user'),
(16, 'news', 'new@new', '$2a$10$gpreBoPlZJsMwe3Vw/BzRO6dTxn7gO79OH5H70ysaZ2S05dwUCQRm', '2026-06-01 23:40:49', 1, 'user');

-- --------------------------------------------------------

--
-- Stand-in structure for view `vw_customer_order`
-- (See below for the actual view)
--
CREATE TABLE `vw_customer_order` (
`username` varchar(50)
,`order_id` int(11)
,`order_date` datetime
,`total_amount` decimal(10,2)
,`status` enum('Pending','Paid','Shipped','Cancelled')
);

-- --------------------------------------------------------

--
-- Stand-in structure for view `vw_product_catalog`
-- (See below for the actual view)
--
CREATE TABLE `vw_product_catalog` (
`product_id` int(11)
,`product_name` varchar(175)
,`cat_name` varchar(100)
,`price` decimal(10,2)
,`stock` int(11)
);

-- --------------------------------------------------------

--
-- Stand-in structure for view `vw_top_sellers`
-- (See below for the actual view)
--
CREATE TABLE `vw_top_sellers` (
`product_name` varchar(175)
,`total_sold` decimal(32,0)
);

-- --------------------------------------------------------

--
-- Structure for view `vw_customer_order`
--
DROP TABLE IF EXISTS `vw_customer_order`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `vw_customer_order`  AS SELECT `u`.`username` AS `username`, `o`.`order_id` AS `order_id`, `o`.`order_date` AS `order_date`, `o`.`total_amount` AS `total_amount`, `o`.`status` AS `status` FROM (`user` `u` join `order` `o` on(`u`.`user_id` = `o`.`user_id`)) ;

-- --------------------------------------------------------

--
-- Structure for view `vw_product_catalog`
--
DROP TABLE IF EXISTS `vw_product_catalog`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `vw_product_catalog`  AS SELECT `p`.`product_id` AS `product_id`, `p`.`product_name` AS `product_name`, `c`.`cat_name` AS `cat_name`, `p`.`price` AS `price`, `p`.`stock` AS `stock` FROM (`product` `p` join `category` `c` on(`p`.`category_id` = `c`.`category_id`)) ;

-- --------------------------------------------------------

--
-- Structure for view `vw_top_sellers`
--
DROP TABLE IF EXISTS `vw_top_sellers`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `vw_top_sellers`  AS SELECT `p`.`product_name` AS `product_name`, sum(`oi`.`quantity`) AS `total_sold` FROM (`product` `p` join `order_item` `oi` on(`p`.`product_id` = `oi`.`product_id`)) GROUP BY `p`.`product_id` ORDER BY sum(`oi`.`quantity`) DESC ;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `category`
--
ALTER TABLE `category`
  ADD PRIMARY KEY (`category_id`),
  ADD UNIQUE KEY `cat_name_UNIQUE` (`cat_name`);

--
-- Indexes for table `order`
--
ALTER TABLE `order`
  ADD PRIMARY KEY (`order_id`),
  ADD KEY `user_id_idx` (`user_id`);

--
-- Indexes for table `order_item`
--
ALTER TABLE `order_item`
  ADD PRIMARY KEY (`orderitem_id`),
  ADD KEY `order_id_idx` (`order_id`),
  ADD KEY `product_id_idx` (`product_id`);

--
-- Indexes for table `product`
--
ALTER TABLE `product`
  ADD PRIMARY KEY (`product_id`),
  ADD KEY `category_id_idx` (`category_id`);

--
-- Indexes for table `user`
--
ALTER TABLE `user`
  ADD PRIMARY KEY (`user_id`),
  ADD UNIQUE KEY `username_UNIQUE` (`username`),
  ADD UNIQUE KEY `email_UNIQUE` (`email`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `category`
--
ALTER TABLE `category`
  MODIFY `category_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `order`
--
ALTER TABLE `order`
  MODIFY `order_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `order_item`
--
ALTER TABLE `order_item`
  MODIFY `orderitem_id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `product`
--
ALTER TABLE `product`
  MODIFY `product_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `user`
--
ALTER TABLE `user`
  MODIFY `user_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=17;

-- --------------------------------------------------------
--
-- Table structure for table `inventory_adjustment`
-- Created by InventoryCount module
--
CREATE TABLE IF NOT EXISTS `inventory_adjustment` (
  `adj_id`       int(11)       NOT NULL AUTO_INCREMENT,
  `product_id`   int(11)       NOT NULL,
  `product_name` varchar(175)  NOT NULL,
  `old_stock`    int(11)       NOT NULL,
  `new_stock`    int(11)       NOT NULL,
  `reason`       varchar(100)  DEFAULT NULL,
  `note`         text          DEFAULT NULL,
  `adj_by`       varchar(50)   DEFAULT NULL,
  `adj_date`     datetime      DEFAULT current_timestamp(),
  PRIMARY KEY (`adj_id`),
  KEY `fk_adj_product` (`product_id`),
  CONSTRAINT `fk_adj_product` FOREIGN KEY (`product_id`) REFERENCES `product` (`product_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;


--
-- Constraints for dumped tables
--

--
-- Constraints for table `order`
--
ALTER TABLE `order`
  ADD CONSTRAINT `fk_userId` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `order_item`
--
ALTER TABLE `order_item`
  ADD CONSTRAINT `fk_orderId` FOREIGN KEY (`order_id`) REFERENCES `order` (`order_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_productId` FOREIGN KEY (`product_id`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION;

--
-- Constraints for table `product`
--
ALTER TABLE `product`
  ADD CONSTRAINT `fk_categoryId` FOREIGN KEY (`category_id`) REFERENCES `category` (`category_id`) ON DELETE SET NULL ON UPDATE NO ACTION;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
