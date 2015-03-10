# iqtoolkit
Toolkit for building LINQ IQueryable providers. This is the official move of my IQToolkit project from CodePlex. 

IQToolkit is essential if you are building your own LINQ IQueryable provider. It contains common tools and source code 
you can apply to your own project.

In the toolkit you will find useful techniques for manipulating LINQ expression trees, implementing IQueryable providers, 
and a host of extensible components for building providers that target translation of LINQ expressions into SQL like languages.

Updates
-------
The IQToolkit solution and project files have been updated to VS2013 format and target .Net 4.5 runtime.

Building 
--------
To build the solution you'll first have to restore the nuget packages. You can do this by right-clicking on the solution
in the solution explorer in Visual Studio and accessing the nuget package manager.

Running Tests
-------------
You can only run the tests for the database providers and test databases you have access to on your machine. 

The Access, SqlServeCe and SQLite should work by default as they are based on direct file access to database files that are supplied with the tests.

The SQL Server (SqlClient) tests relies on the existence of SQL Server on your machine. There may sometimes be issues with detaching the supplied database file that may make the tests fail until resolved using an external tool.

The MySql tests require MySql to be installed and a copy of the Northwind database imported into it (via Access .mdb file) before the tests will work.

Each provider has a separate test.XXX.exe file (found in the bin folder) that can be run from the command line. Test.Access.exe runs the unit tests for the Access provider, etc.


