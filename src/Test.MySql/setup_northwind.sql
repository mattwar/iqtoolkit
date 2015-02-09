-- ----------------------------------------------------------------------------
-- MySQL Workbench Migration
-- Migrated Schemata: Northwind
-- Source Schemata: Northwind
-- Created: Sun Feb 08 20:55:46 2015
-- ----------------------------------------------------------------------------

SET FOREIGN_KEY_CHECKS = 0;;

-- ----------------------------------------------------------------------------
-- Schema Northwind
-- ----------------------------------------------------------------------------
DROP SCHEMA IF EXISTS `Northwind` ;
CREATE SCHEMA IF NOT EXISTS `Northwind` ;

-- ----------------------------------------------------------------------------
-- Table Northwind.Suppliers
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Suppliers` (
  `SupplierID` INT(10) NULL,
  `CompanyName` VARCHAR(50) NULL,
  `ContactName` VARCHAR(50) NULL,
  `ContactTitle` VARCHAR(50) NULL,
  `Address` VARCHAR(50) NULL,
  `City` VARCHAR(50) NULL,
  `Region` VARCHAR(50) NULL,
  `PostalCode` VARCHAR(50) NULL,
  `Country` VARCHAR(50) NULL,
  `Phone` VARCHAR(50) NULL,
  `Fax` VARCHAR(50) NULL,
  `HomePage` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Current Product List
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Current Product List` (
  `ProductID` INT(10) NULL,
  `ProductName` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Alphabetical list of products
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Alphabetical list of products` (
  `ProductID` INT(10) NULL,
  `ProductName` LONGTEXT NULL,
  `SupplierID` INT(10) NULL,
  `CategoryID` INT(10) NULL,
  `QuantityPerUnit` LONGTEXT NULL,
  `UnitPrice` DECIMAL(19,4) NULL,
  `UnitsInStock` SMALLINT(5) NULL,
  `UnitsOnOrder` SMALLINT(5) NULL,
  `ReorderLevel` SMALLINT(5) NULL,
  `Discontinued` SMALLINT(5) NULL,
  `CategoryName` LONGTEXT NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Territories
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Territories` (
  `TerritoryID` VARCHAR(50) NULL,
  `TerritoryDescription` VARCHAR(50) NULL,
  `RegionID` INT(10) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.TestMoney
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`TestMoney` (
  `cola` INT(10) NULL,
  `mon` DECIMAL(19,4) NULL,
  `i` INT(10) NULL,
  `dec` DECIMAL(18,0) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Category Sales for 1997
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Category Sales for 1997` (
  `CategoryName` LONGTEXT NULL,
  `CategorySales` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Product Sales for 1997
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Product Sales for 1997` (
  `CategoryName` LONGTEXT NULL,
  `ProductName` LONGTEXT NULL,
  `ProductSales` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Sales Totals by Amount
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Sales Totals by Amount` (
  `SaleAmount` DECIMAL(19,4) NULL,
  `OrderID` INT(10) NULL,
  `CompanyName` LONGTEXT NULL,
  `ShippedDate` DATETIME NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Sales by Category
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Sales by Category` (
  `CategoryID` INT(10) NULL,
  `CategoryName` LONGTEXT NULL,
  `ProductName` LONGTEXT NULL,
  `ProductSales` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Customers
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Customers` (
  `CustomerID` VARCHAR(5) NULL,
  `CompanyName` VARCHAR(50) NULL,
  `ContactName` VARCHAR(50) NULL,
  `ContactTitle` VARCHAR(50) NULL,
  `Address` VARCHAR(50) NULL,
  `City` VARCHAR(50) NULL,
  `Region` VARCHAR(50) NULL,
  `PostalCode` VARCHAR(50) NULL,
  `Country` VARCHAR(50) NULL,
  `Phone` VARCHAR(50) NULL,
  `Fax` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Order Details
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Order Details` (
  `OrderID` INT(10) NULL,
  `ProductID` INT(10) NULL,
  `UnitPrice` DECIMAL(19,4) NULL,
  `Quantity` SMALLINT(5) NULL,
  `Discount` DOUBLE NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Order Subtotals
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Order Subtotals` (
  `OrderID` INT(10) NULL,
  `Subtotal` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Products Above Average Price
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Products Above Average Price` (
  `ProductName` LONGTEXT NULL,
  `UnitPrice` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Employees
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Employees` (
  `EmployeeID` INT(10) NULL,
  `LastName` VARCHAR(50) NULL,
  `FirstName` VARCHAR(50) NULL,
  `Title` VARCHAR(10) NULL,
  `TitleOfCourtesy` VARCHAR(10) NULL,
  `BirthDate` DATETIME NULL,
  `HireDate` DATETIME NULL,
  `Address` VARCHAR(50) NULL,
  `City` VARCHAR(50) NULL,
  `Region` VARCHAR(50) NULL,
  `PostalCode` VARCHAR(10) NULL,
  `Country` VARCHAR(50) NULL,
  `HomePhone` VARCHAR(50) NULL,
  `Extension` VARCHAR(10) NULL,
  `Photo` LONGBLOB NULL,
  `Notes` VARCHAR(250) NULL,
  `ReportsTo` INT(10) NULL,
  `PhotoPath` VARCHAR(80) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Categories
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Categories` (
  `CategoryID` INT(10) NULL,
  `CategoryName` VARCHAR(50) NULL,
  `Description` VARCHAR(50) NULL,
  `Picture` LONGBLOB NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Products by Category
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Products by Category` (
  `CategoryName` LONGTEXT NULL,
  `ProductName` LONGTEXT NULL,
  `QuantityPerUnit` LONGTEXT NULL,
  `UnitsInStock` SMALLINT(5) NULL,
  `Discontinued` SMALLINT(5) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Quarterly Orders
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Quarterly Orders` (
  `CustomerID` VARCHAR(5) NULL,
  `CompanyName` VARCHAR(50) NULL,
  `City` VARCHAR(50) NULL,
  `Country` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.CustomerCustomerDemo
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`CustomerCustomerDemo` (
  `CustomerID` VARCHAR(5) NULL,
  `CustomerTypeID` VARCHAR(10) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Region
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Region` (
  `RegionID` INT(10) NULL,
  `RegionDescription` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Orders Qry
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Orders Qry` (
  `OrderID` INT(10) NULL,
  `CustomerID` VARCHAR(5) NULL,
  `EmployeeID` INT(10) NULL,
  `OrderDate` DATETIME NULL,
  `RequiredDate` DATETIME NULL,
  `ShippedDate` DATETIME NULL,
  `ShipVia` INT(10) NULL,
  `Freight` DECIMAL(19,4) NULL,
  `ShipName` LONGTEXT NULL,
  `ShipAddress` LONGTEXT NULL,
  `ShipCity` LONGTEXT NULL,
  `ShipRegion` LONGTEXT NULL,
  `ShipPostalCode` LONGTEXT NULL,
  `ShipCountry` LONGTEXT NULL,
  `CompanyName` LONGTEXT NULL,
  `Address` LONGTEXT NULL,
  `City` LONGTEXT NULL,
  `Region` LONGTEXT NULL,
  `PostalCode` LONGTEXT NULL,
  `Country` LONGTEXT NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Customer and Suppliers by City
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Customer and Suppliers by City` (
  `City` LONGTEXT NULL,
  `CompanyName` LONGTEXT NULL,
  `ContactName` LONGTEXT NULL,
  `Relationship` LONGTEXT NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Products
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Products` (
  `ProductID` INT(10) NULL,
  `ProductName` VARCHAR(50) NULL,
  `SupplierID` INT(10) NULL,
  `CategoryID` INT(10) NULL,
  `QuantityPerUnit` VARCHAR(50) NULL,
  `UnitPrice` DECIMAL(19,4) NULL,
  `UnitsInStock` SMALLINT(5) NULL,
  `UnitsOnOrder` SMALLINT(5) NULL,
  `ReorderLevel` SMALLINT(5) NULL,
  `Discontinued` SMALLINT(5) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.EmployeeTerritories
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`EmployeeTerritories` (
  `EmployeeID` INT(10) NULL,
  `TerritoryID` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Invoices
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Invoices` (
  `ShipName` VARCHAR(50) NULL,
  `ShipAddress` VARCHAR(50) NULL,
  `ShipCity` VARCHAR(50) NULL,
  `ShipRegion` VARCHAR(50) NULL,
  `ShipPostalCode` VARCHAR(50) NULL,
  `ShipCountry` VARCHAR(50) NULL,
  `CustomerID` VARCHAR(5) NULL,
  `CustomerName` VARCHAR(50) NULL,
  `Address` VARCHAR(50) NULL,
  `City` VARCHAR(50) NULL,
  `Region` VARCHAR(50) NULL,
  `PostalCode` VARCHAR(50) NULL,
  `Country` VARCHAR(50) NULL,
  `Salesperson` VARCHAR(50) NULL,
  `OrderID` INT(10) NULL,
  `OrderDate` DATETIME NULL,
  `RequiredDate` DATETIME NULL,
  `ShippedDate` DATETIME NULL,
  `ShipperName` VARCHAR(50) NULL,
  `ProductID` INT(10) NULL,
  `ProductName` VARCHAR(50) NULL,
  `UnitPrice` DECIMAL(19,4) NULL,
  `Quantity` VARCHAR(50) NULL,
  `Discount` VARCHAR(50) NULL,
  `ExtendedPrice` DECIMAL(19,4) NULL,
  `Freight` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Order Details Extended
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Order Details Extended` (
  `OrderID` INT(10) NULL,
  `ProductID` INT(10) NULL,
  `ProductName` VARCHAR(50) NULL,
  `UnitPrice` DECIMAL(19,4) NULL,
  `Quantity` SMALLINT(5) NULL,
  `Discount` DOUBLE NULL,
  `ExtendedPrice` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Summary of Sales by Year
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Summary of Sales by Year` (
  `ShippedDate` DATETIME NULL,
  `OrderID` INT(10) NULL,
  `Subtotal` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Summary of Sales by Quarter
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Summary of Sales by Quarter` (
  `ShippedDate` DATETIME NULL,
  `OrderID` INT(10) NULL,
  `Subtotal` DECIMAL(19,4) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Shippers
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Shippers` (
  `ShipperID` INT(10) NULL,
  `CompanyName` VARCHAR(50) NULL,
  `Phone` VARCHAR(50) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.CustomerDemographics
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`CustomerDemographics` (
  `CustomerTypeID` VARCHAR(10) NULL,
  `CustomerDesc` VARCHAR(100) NULL);

-- ----------------------------------------------------------------------------
-- Table Northwind.Orders
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Northwind`.`Orders` (
  `OrderID` INT(10) NULL,
  `CustomerID` VARCHAR(5) NULL,
  `EmployeeID` INT(10) NULL,
  `OrderDate` DATETIME NULL,
  `RequiredDate` DATETIME NULL,
  `ShippedDate` DATETIME NULL,
  `ShipVia` INT(10) NULL,
  `Freight` DECIMAL(19,4) NULL,
  `ShipName` VARCHAR(50) NULL,
  `ShipAddress` VARCHAR(50) NULL,
  `ShipCity` VARCHAR(50) NULL,
  `ShipRegion` VARCHAR(50) NULL,
  `ShipPostalCode` VARCHAR(50) NULL,
  `ShipCountry` VARCHAR(50) NULL);
SET FOREIGN_KEY_CHECKS = 1;;
