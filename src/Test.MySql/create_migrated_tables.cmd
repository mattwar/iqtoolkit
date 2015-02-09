REM Workbench Table Data copy script
REM 
REM Execute this to copy table data from a source RDBMS to MySQL.
REM Edit the options below to customize it. You will need to provide passwords, at least.
REM 
REM Source DB:  (Microsoft Access)
REM Target DB: Mysql@localhost:3306


@ECHO OFF
REM Source and target DB passwords
set arg_source_password=
set arg_target_password=

IF [%arg_source_password%] == [] (
    IF [%arg_target_password%] == [] (
        ECHO WARNING: Both source and target RDBMSes passwords are empty. You should edit this file to set them.
    )
)
set arg_worker_count=2
REM Uncomment the following options according to your needs

REM Whether target tables should be truncated before copy
REM set arg_truncate_target=--truncate-target
REM Enable debugging output
REM set arg_debug_output=--log-level=debug3


REM Creation of file with table definitions for copytable

set table_file="%TMP%\wb_tables_to_migrate.txt"
TYPE NUL > "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Products by Category"	`Northwind`	`Products by Category`			"CategoryName", "ProductName", "QuantityPerUnit", "UnitsInStock", "Discontinued" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Order Details"	`Northwind`	`Order Details`			"OrderID", "ProductID", "UnitPrice", "Quantity", "Discount" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Alphabetical list of products"	`Northwind`	`Alphabetical list of products`			"ProductID", "ProductName", "SupplierID", "CategoryID", "QuantityPerUnit", "UnitPrice", "UnitsInStock", "UnitsOnOrder", "ReorderLevel", "Discontinued", "CategoryName" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Invoices"	`Northwind`	`Invoices`			"ShipName", "ShipAddress", "ShipCity", "ShipRegion", "ShipPostalCode", "ShipCountry", "CustomerID", "CustomerName", "Address", "City", "Region", "PostalCode", "Country", "Salesperson", "OrderID", "OrderDate", "RequiredDate", "ShippedDate", "ShipperName", "ProductID", "ProductName", "UnitPrice", "Quantity", "Discount", "ExtendedPrice", "Freight" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Orders"	`Northwind`	`Orders`			"OrderID", "CustomerID", "EmployeeID", "OrderDate", "RequiredDate", "ShippedDate", "ShipVia", "Freight", "ShipName", "ShipAddress", "ShipCity", "ShipRegion", "ShipPostalCode", "ShipCountry" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Products"	`Northwind`	`Products`			"ProductID", "ProductName", "SupplierID", "CategoryID", "QuantityPerUnit", "UnitPrice", "UnitsInStock", "UnitsOnOrder", "ReorderLevel", "Discontinued" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Category Sales for 1997"	`Northwind`	`Category Sales for 1997`			"CategoryName", "CategorySales" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"CustomerCustomerDemo"	`Northwind`	`CustomerCustomerDemo`			"CustomerID", "CustomerTypeID" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Customer and Suppliers by City"	`Northwind`	`Customer and Suppliers by City`			"City", "CompanyName", "ContactName", "Relationship" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Current Product List"	`Northwind`	`Current Product List`			"ProductID", "ProductName" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Products Above Average Price"	`Northwind`	`Products Above Average Price`			"ProductName", "UnitPrice" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Suppliers"	`Northwind`	`Suppliers`			"SupplierID", "CompanyName", "ContactName", "ContactTitle", "Address", "City", "Region", "PostalCode", "Country", "Phone", "Fax", "HomePage" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Order Subtotals"	`Northwind`	`Order Subtotals`			"OrderID", "Subtotal" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Summary of Sales by Year"	`Northwind`	`Summary of Sales by Year`			"ShippedDate", "OrderID", "Subtotal" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Employees"	`Northwind`	`Employees`			"EmployeeID", "LastName", "FirstName", "Title", "TitleOfCourtesy", "BirthDate", "HireDate", "Address", "City", "Region", "PostalCode", "Country", "HomePhone", "Extension", "Photo", "Notes", "ReportsTo", "PhotoPath" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Quarterly Orders"	`Northwind`	`Quarterly Orders`			"CustomerID", "CompanyName", "City", "Country" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Summary of Sales by Quarter"	`Northwind`	`Summary of Sales by Quarter`			"ShippedDate", "OrderID", "Subtotal" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Orders Qry"	`Northwind`	`Orders Qry`			"OrderID", "CustomerID", "EmployeeID", "OrderDate", "RequiredDate", "ShippedDate", "ShipVia", "Freight", "ShipName", "ShipAddress", "ShipCity", "ShipRegion", "ShipPostalCode", "ShipCountry", "CompanyName", "Address", "City", "Region", "PostalCode", "Country" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Sales by Category"	`Northwind`	`Sales by Category`			"CategoryID", "CategoryName", "ProductName", "ProductSales" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"TestMoney"	`Northwind`	`TestMoney`			"cola", "mon", "i", "dec" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Sales Totals by Amount"	`Northwind`	`Sales Totals by Amount`			"SaleAmount", "OrderID", "CompanyName", "ShippedDate" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Categories"	`Northwind`	`Categories`			"CategoryID", "CategoryName", "Description", "Picture" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"EmployeeTerritories"	`Northwind`	`EmployeeTerritories`			"EmployeeID", "TerritoryID" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Order Details Extended"	`Northwind`	`Order Details Extended`			"OrderID", "ProductID", "ProductName", "UnitPrice", "Quantity", "Discount", "ExtendedPrice" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Customers"	`Northwind`	`Customers`			"CustomerID", "CompanyName", "ContactName", "ContactTitle", "Address", "City", "Region", "PostalCode", "Country", "Phone", "Fax" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Territories"	`Northwind`	`Territories`			"TerritoryID", "TerritoryDescription", "RegionID" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Region"	`Northwind`	`Region`			"RegionID", "RegionDescription" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Shippers"	`Northwind`	`Shippers`			"ShipperID", "CompanyName", "Phone" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"CustomerDemographics"	`Northwind`	`CustomerDemographics`			"CustomerTypeID", "CustomerDesc" >> "%TMP%\wb_tables_to_migrate.txt"
ECHO 	"Product Sales for 1997"	`Northwind`	`Product Sales for 1997`			"CategoryName", "ProductName", "ProductSales" >> "%TMP%\wb_tables_to_migrate.txt"


wbcopytables.exe --odbc-source="DSN=nw" --target="root@localhost:3306" --source-password="%arg_source_password%" --target-password="%arg_target_password%" --table-file="%table_file%" --thread-count=%arg_worker_count% %arg_truncate_target% %arg_debug_output%

REM Removes the file with the table definitions
DEL "%TMP%\wb_tables_to_migrate.txt"


