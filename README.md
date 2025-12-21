login credentials:
johnstaff@gmail.com 
John123!

alicestaff@gmail.com
Alicestaff1!

demo33228@gmail.com
Demo123!

assistant@gmail.com
Assistant1!

admin@gmail.com
Admin123!

staff@gmail.com 
Staff123!

# grocery-management
BMIT2023 Y2S2 assignment
feat: feature
fix: fix bug
chore: like import library
refactor: arg file

SQL query:
INSERT INTO Users (
    Id, 
    Name, 
    Email, 
    Password, 
    PhoneNum, 
    ResetToken, 
    ResetTokenExpiry, 
    LoginAttempts, 
    Locked, 
    Discriminator, 
    PhotoURL, 
    Salary, 
    AuthorizationLvl, 
    ManagerId
) VALUES 
('M001', 'System Administrator', 'admin@gmail.com', 'AQAAAAIAAYagAAAAEAcTHaT2HEeBythoxbJBjLJ0ERcELa2mKx6aQLWf026gjduYRtCJQ8TpA+IgT1FjfA==', '012-2981201', NULL, NULL, 0, NULL, 'Manager', NULL, NULL, NULL, NULL),
('M002', 'Assistant manager', 'assistant@gmail.com', 'AQAAAAIAAYagAAAAEG8jsvK33F+htTM+OtUcINrDWGPLdqzNd1qx93ASPqttNX+hZ2xJjaQsB0dXdqHbfg==', '012-2981201', NULL, NULL, 0, NULL, 'Manager', NULL, NULL, NULL, NULL),
('M003', 'demo', 'demo33228@gmail.com', 'AQAAAAIAAYagAAAAEF0ss8B96oQnDgJoUlJzRthxsu2GtVTFGtcowXSFuN3NkxGsf8cuE9v3gvgrhQ+JJQ==', '012-2981201', NULL, NULL, 0, NULL, 'Manager', NULL, NULL, NULL, NULL),
('S001', 'staff one', 'staff@gmail.com', 'AQAAAAIAAYagAAAAEJjH/PLrm4gHY6NfsvIQU4DaV4b1jFRGPBCtfYkdrNt9VxOS9cvnXY5+0JtmiquJng==', '011-2228711', NULL, NULL, 0, NULL, 'Staff', 'db24d6e026e74100817b8c1cecce1634.jpg', 10.00, 'CASHIER', 'M002'),
('S002', 'Alice', 'alicestaff@gmail.com', 'AQAAAAIAAYagAAAAEA0px1dxbTpOxE5hP61YWHDh42TEFjJkT5KjMWaaiBDqgXxbvu7C2yY08ZvwtQoQLg==', '012-8870119', NULL, NULL, 0, NULL, 'Staff', '3db875906dbc4b9dbfc21ff891aa252c.jpg', 12.00, 'INVENTORY', 'M001'),
('S003', 'John', 'johnstaff@gmail.com', 'AQAAAAIAAYagAAAAEDFRG1elEMzhdEKMLVpzOWflxjTqASs04tLOMH9VeyIih2uA2s50h9IEG2KI917E1Q==', '011-2228116', NULL, NULL, 0, NULL, 'Staff', '3ccff41e67fa4facb8b69a5e6c637e1e.jpg', 9.00, 'CLEANING', 'M002');

INSERT INTO Products (Id, Name, SellPrice, PhotoURL, category, WareHouseQty, StoreFrontQty) VALUES
('P00001', 'MILO', 15.50, 'milo.png', 'BEVERAGE', 100, 20),
('P00002', '100PLUS', 15.50, '100PLUS.png', 'BEVERAGE', 100, 20),
('P00003', 'MIMI', 15.50, 'mimi.png', 'SNACK', 100, 20);

INSERT INTO Supplier (Id, Name, SupplierType, Address, ContactNo)
VALUES 
('SUP001', 'Fresh Farms Co.', 'Wholesale', '123 Green Lane, Cameron Highlands', '60123456789'),
('SUP002', 'Global Drinks Ltd', 'Distributor', '45 Industrial Park, Shah Alam', '60198765432'),
('SUP003', 'Ocean Catch', 'Wholesale', 'Sector 5, Port Klang', '601112223334'),
('SUP004', 'Daily Grocers', 'White-Label', 'No 88, Jalan Ampang, Kuala Lumpur', '601222333444');

INSERT INTO Inventories (Id, ExpiryDate, ProductId, StaffId, SupplierId, Status, CheckoutId) VALUES
('INV00001A', '2025-12-31', 'P00001', 'S001', 'SUP001', 'AVAILABLE', NULL),
('INV00001B', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV00002A', '2025-12-31', 'P00001', 'S001', 'SUP001', 'AVAILABLE', NULL),
('INV00002B', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV00003A', '2025-12-31', 'P00001', 'S001', 'SUP001', 'AVAILABLE', NULL),
('INV00003B', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV00004A', '2026-01-27', 'P00002', 'S001', 'SUP002', 'AVAILABLE', NULL),
('INV00004B', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV00005A', '2026-01-27', 'P00002', 'S001', 'SUP002', 'AVAILABLE', NULL),
('INV00005B', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV00006A', '2026-01-27', 'P00002', 'S001', 'SUP002', 'AVAILABLE', NULL),
('INV00006B', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV00007A', '2026-01-27', 'P00002', 'S001', 'SUP002', 'AVAILABLE', NULL),
('INV00007B', '2026-02-26', 'P00001', 'S001', 'SUP004', 'AVAILABLE', NULL),
('INV00008B', '2026-02-26', 'P00001', 'S001', 'SUP004', 'AVAILABLE', NULL),
('INV00009B', '2026-02-26', 'P00001', 'S001', 'SUP004', 'AVAILABLE', NULL),
('INV99995A', '2026-01-27', 'P00002', 'S001', 'SUP002', 'AVAILABLE', NULL),
('INV99996A', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV99997A', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV99998A', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL),
('INV99999A', '2026-03-24', 'P00003', 'S001', 'SUP003', 'AVAILABLE', NULL);

INSERT INTO ProcurementRecords (
    Id, 
    Quantity, 
    TotalPrice, 
    Status,
    PaymentStatus, 
    ProcurementDateTime, 
    StatusUpdateDateTime, 
    ProductID, 
    SupplierID
)
VALUES 
('PR000002', 6, 93.00, 'Ordered', 'Unpaid',  '2025-12-17 16:01:25', '2025-12-17 16:01:25', 'P00001', 'SUP001'),
('PR000003', 9, 139.50, 'Ordered', 'Paid', '2025-12-17 17:00:00', '2025-12-17 17:00:00', 'P00002', 'SUP001');


INSERT INTO Expenses (
    Id, 
    Type, 
    Details, 
    Date, 
    Amount, 
    ManagerId
)
VALUES
('EX0001', 'Utilities', 'Electricity bill', '2025-12-31 00:00:00', 300.00, 'M002'),
('EX0002', 'Office Supplies', 'Ergonomic chairs and stationery', '2025-12-18 09:15:00', 1250.50, 'M002'),
('EX0003', 'Salary', 'June Salary for S001', '2025-12-20 14:30:00', 1800.00, 'M002');

INSERT INTO Checkout (
    Id, 
    CustomerId, 
    InventoryId, 
    Total, 
    Date, 
    Status, 
    StatusUpdateDate, 
    PaymentMethod, 
    StaffId
)
VALUES 
('CH0001', 'CUST0052', 'INV00001A', 150.75, '2025-12-20 10:30:00', 'Completed', '2025-12-20 10:45:00', 'Credit Card', 'S001'),
('CH0002', 'CUST0018', 'INV00001B', 85.00, '2025-12-20 11:15:00', 'Pending', '2025-12-20 11:15:00', 'Cash', 'S001');

INSERT INTO AttendanceRecords (Id, StaffId, Date, CheckInTime, CheckOutTime, Status)
VALUES 
('ATT00002', 'S001', '2025-12-22', '07:11:00', '21:13:00', 'ATTEND'),
('ATT00003', 'S001', '2025-12-23', '07:11:00', '21:13:00', 'ATTEND'),
('ATT00004', 'S001', '2025-12-24', '07:14:00', '21:13:00', 'ATTEND'),
('ATT00005', 'S001', '2025-12-25', '07:14:00', '21:13:00', 'ATTEND'),
('ATT00006', 'S001', '2025-12-26', '07:14:00', '21:13:00', 'ATTEND'),
('ATT00007', 'S001', '2025-12-27', '07:14:00', '21:13:00', 'ATTEND'),
('ATT00008', 'S001', '2025-12-29', '09:14:00', NULL,       'ATTEND'),
('ATT00009', 'S001', '2025-12-31', '09:14:00', NULL,       'ATTEND'),
('ATT00010', 'S002', '2025-12-22', '08:00:00', '17:00:00', 'ATTEND'),
('ATT00011', 'S002', '2025-12-23', '08:05:00', '17:15:00', 'ATTEND'),
('ATT00012', 'S002', '2025-12-24', '09:30:00', '18:00:00', 'LATE'),
('ATT00013', 'S002', '2025-12-25', NULL,       NULL,       'LEAVE'),
('ATT00014', 'S002', '2025-12-26', '08:00:00', '17:00:00', 'ATTEND'),
('ATT00015', 'S003', '2025-12-22', '07:55:00', '16:55:00', 'ATTEND'),
('ATT00016', 'S003', '2025-12-23', NULL,       NULL,       'ABSENT'),
('ATT00017', 'S003', '2025-12-24', '10:00:00', '19:00:00', 'LATE'),
('ATT00018', 'S003', '2025-12-25', '08:00:00', '17:00:00', 'ATTEND'),
('ATT00019', 'S003', '2025-12-26', '08:15:00', '17:30:00', 'ATTEND');

