/*
    Sales Order database - SQL Server create script
    ------------------------------------------------
    Mirrors the structure of the application's XML store (App_Data/orders.xml), normalised
    into the six tables from the supplied ER diagram:

        OrderHeaderStatus 1───* OrderHeader *───1 Customers
                                     │
                                     1
                                     │
                                     *
        ProductTypes 1───* Products 1───* OrderLine

    Order Type (Normal / Staff / Mechanical / Perishable) has no lookup table in the ERD,
    so it is a column on OrderHeader constrained by a CHECK.

    Cost/Sales price are held on Products (current list price) AND on OrderLine (price at
    the time of the order), so changing a product's price does not rewrite old orders.

    Re-runnable: drops and recreates the database.
*/

USE master;
GO

IF DB_ID(N'SalesOrders') IS NOT NULL
BEGIN
    ALTER DATABASE SalesOrders SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SalesOrders;
END
GO

CREATE DATABASE SalesOrders;
GO

USE SalesOrders;
GO

/* ---------- Lookup tables ---------- */

CREATE TABLE dbo.OrderHeaderStatus
(
    OrderStatusId   INT            NOT NULL IDENTITY(1,1),
    StatusName      VARCHAR(20)    NOT NULL,
    CONSTRAINT PK_OrderHeaderStatus PRIMARY KEY (OrderStatusId),
    CONSTRAINT UQ_OrderHeaderStatus_StatusName UNIQUE (StatusName)
);

CREATE TABLE dbo.ProductTypes
(
    ProductTypeId   INT            NOT NULL IDENTITY(1,1),
    ProductTypeName VARCHAR(20)    NOT NULL,
    CONSTRAINT PK_ProductTypes PRIMARY KEY (ProductTypeId),
    CONSTRAINT UQ_ProductTypes_ProductTypeName UNIQUE (ProductTypeName)
);

/* ---------- Master data ---------- */

CREATE TABLE dbo.Customers
(
    CustomerId      INT            NOT NULL IDENTITY(1,1),
    CustomerName    NVARCHAR(100)  NOT NULL,
    CONSTRAINT PK_Customers PRIMARY KEY (CustomerId),
    CONSTRAINT UQ_Customers_CustomerName UNIQUE (CustomerName)
);

CREATE TABLE dbo.Products
(
    ProductId       INT            NOT NULL IDENTITY(1,1),
    ProductCode     VARCHAR(20)    NOT NULL,
    ProductTypeId   INT            NOT NULL,
    CostPrice       DECIMAL(18,2)  NOT NULL,
    SalesPrice      DECIMAL(18,2)  NOT NULL,
    CONSTRAINT PK_Products PRIMARY KEY (ProductId),
    CONSTRAINT UQ_Products_ProductCode UNIQUE (ProductCode),
    CONSTRAINT FK_Products_ProductTypes FOREIGN KEY (ProductTypeId) REFERENCES dbo.ProductTypes (ProductTypeId),
    CONSTRAINT CK_Products_CostPrice CHECK (CostPrice > 0),
    CONSTRAINT CK_Products_SalesPrice CHECK (SalesPrice > 0)
);

/* ---------- Orders ---------- */

CREATE TABLE dbo.OrderHeader
(
    OrderHeaderId   INT            NOT NULL IDENTITY(1,1),
    OrderNumber     VARCHAR(20)    NOT NULL,
    OrderType       VARCHAR(20)    NOT NULL,
    OrderStatusId   INT            NOT NULL,
    CustomerId      INT            NOT NULL,
    CreateDate      DATETIME2(0)   NOT NULL CONSTRAINT DF_OrderHeader_CreateDate DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_OrderHeader PRIMARY KEY (OrderHeaderId),
    CONSTRAINT UQ_OrderHeader_OrderNumber UNIQUE (OrderNumber),
    CONSTRAINT CK_OrderHeader_OrderType CHECK (OrderType IN ('Normal', 'Staff', 'Mechanical', 'Perishable')),
    CONSTRAINT FK_OrderHeader_OrderHeaderStatus FOREIGN KEY (OrderStatusId) REFERENCES dbo.OrderHeaderStatus (OrderStatusId),
    CONSTRAINT FK_OrderHeader_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId)
);

CREATE INDEX IX_OrderHeader_CreateDate ON dbo.OrderHeader (CreateDate);
CREATE INDEX IX_OrderHeader_OrderType ON dbo.OrderHeader (OrderType);

CREATE TABLE dbo.OrderLine
(
    OrderLineId     INT            NOT NULL IDENTITY(1,1),
    OrderHeaderId   INT            NOT NULL,
    LineNumber      INT            NOT NULL,
    ProductId       INT            NOT NULL,
    CostPrice       DECIMAL(18,2)  NOT NULL,
    SalesPrice      DECIMAL(18,2)  NOT NULL,
    Quantity        INT            NOT NULL,
    CONSTRAINT PK_OrderLine PRIMARY KEY (OrderLineId),
    -- Line numbers restart at 1 for every order.
    CONSTRAINT UQ_OrderLine_OrderHeader_LineNumber UNIQUE (OrderHeaderId, LineNumber),
    CONSTRAINT FK_OrderLine_OrderHeader FOREIGN KEY (OrderHeaderId) REFERENCES dbo.OrderHeader (OrderHeaderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderLine_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_OrderLine_LineNumber CHECK (LineNumber >= 1),
    CONSTRAINT CK_OrderLine_CostPrice CHECK (CostPrice > 0),
    CONSTRAINT CK_OrderLine_SalesPrice CHECK (SalesPrice > 0),
    CONSTRAINT CK_OrderLine_Quantity CHECK (Quantity > 0)
);

CREATE INDEX IX_OrderLine_ProductId ON dbo.OrderLine (ProductId);
GO

/* ---------- Seed: dropdown values ---------- */

INSERT INTO dbo.OrderHeaderStatus (StatusName) VALUES ('New'), ('Processing'), ('Complete');
INSERT INTO dbo.ProductTypes (ProductTypeName) VALUES ('Apparel'), ('Parts'), ('Equipment'), ('Motor');
GO

/* ---------- Seed: the two example orders from the spec ---------- */

INSERT INTO dbo.Customers (CustomerName) VALUES (N'KFC'), (N'Nandos');

INSERT INTO dbo.Products (ProductCode, ProductTypeId, CostPrice, SalesPrice)
SELECT p.ProductCode, pt.ProductTypeId, p.CostPrice, p.SalesPrice
FROM (VALUES
        ('GSX837420', 'Parts',     13.54,  84.49),
        ('AVF697420', 'Apparel',   19.34, 105.99),
        ('YTS786663', 'Parts',      8.44,  49.22),
        ('UYT485911', 'Equipment', 41.57, 250.45),
        ('PLO879996', 'Apparel',   21.57,  65.45)
     ) AS p (ProductCode, ProductTypeName, CostPrice, SalesPrice)
JOIN dbo.ProductTypes pt ON pt.ProductTypeName = p.ProductTypeName;

INSERT INTO dbo.OrderHeader (OrderNumber, OrderType, OrderStatusId, CustomerId, CreateDate)
SELECT h.OrderNumber, h.OrderType, s.OrderStatusId, c.CustomerId, h.CreateDate
FROM (VALUES
        ('SO625144', 'Normal', 'New', N'KFC',    CAST('2018-03-08T13:52:21' AS DATETIME2(0))),
        ('SO625145', 'Normal', 'New', N'Nandos', CAST('2018-03-09T08:56:21' AS DATETIME2(0)))
     ) AS h (OrderNumber, OrderType, StatusName, CustomerName, CreateDate)
JOIN dbo.OrderHeaderStatus s ON s.StatusName = h.StatusName
JOIN dbo.Customers c ON c.CustomerName = h.CustomerName;

INSERT INTO dbo.OrderLine (OrderHeaderId, LineNumber, ProductId, CostPrice, SalesPrice, Quantity)
SELECT oh.OrderHeaderId, l.LineNumber, p.ProductId, l.CostPrice, l.SalesPrice, l.Quantity
FROM (VALUES
        ('SO625144', 1, 'GSX837420', 13.54,  84.49, 10),
        ('SO625144', 2, 'AVF697420', 19.34, 105.99, 25),
        ('SO625145', 1, 'YTS786663',  8.44,  49.22,  2),
        ('SO625145', 2, 'UYT485911', 41.57, 250.45, 12),
        ('SO625145', 3, 'PLO879996', 21.57,  65.45,  8)
     ) AS l (OrderNumber, LineNumber, ProductCode, CostPrice, SalesPrice, Quantity)
JOIN dbo.OrderHeader oh ON oh.OrderNumber = l.OrderNumber
JOIN dbo.Products p ON p.ProductCode = l.ProductCode;
GO

/* ---------- Convenience view: flattened like the XML ---------- */

CREATE VIEW dbo.vSalesOrders
AS
SELECT  oh.OrderNumber,
        oh.OrderType,
        s.StatusName      AS OrderStatus,
        c.CustomerName,
        oh.CreateDate,
        ol.LineNumber,
        p.ProductCode,
        pt.ProductTypeName AS ProductType,
        ol.CostPrice,
        ol.SalesPrice,
        ol.Quantity
FROM dbo.OrderHeader oh
JOIN dbo.OrderHeaderStatus s ON s.OrderStatusId = oh.OrderStatusId
JOIN dbo.Customers c         ON c.CustomerId = oh.CustomerId
LEFT JOIN dbo.OrderLine ol   ON ol.OrderHeaderId = oh.OrderHeaderId
LEFT JOIN dbo.Products p     ON p.ProductId = ol.ProductId
LEFT JOIN dbo.ProductTypes pt ON pt.ProductTypeId = p.ProductTypeId;
GO
