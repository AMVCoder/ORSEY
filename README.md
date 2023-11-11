<p align="center"> <img src="Titulo.png" alt="Logo"> </p>

# OdysseyNet

Welcome to OdysseyNet, a Micro Object-Relational Mapping (ORM) framework designed to provide a simple and efficient interface for database interactions in .NET applications. Our aim is to offer a lightweight and user-friendly tool that simplifies database operations without the overhead of traditional ORMs.

<h3><em> Project Status: In Development </em></h3>

This project is currently in active development. We are diligently working on adding new features, enhancing efficiency, and ensuring stability. As it is in its early stages, you may encounter frequent changes and significant improvements in the codebase.


<h3><em> Features and Functionality </em></h3>

Lightweight and Fast: Engineered to be minimally invasive and high-performing, ideal for projects that need a nimble ORM solution.
Easy to Use: An intuitive interface that makes integration with both existing and new projects straightforward.
Flexible: Supports a variety of database operations, including queries, inserts, updates, and deletions.

```csharp

IDatabaseConnection dbConnection = new SqlDatabaseConnection(connectionString);
QueryExecutor executor = new QueryExecutor(dbConnection);

Expression<Func<Users, object>> columns = e => new { e.Name, e.Email };
Expression<Func<Users, bool>> condition = e => e.Name != "orm" && e.Id == 2;

IEnumerable<Users> user1 = executor.Query<Users>(column: columns, where: condition);

```


<h3><em> Implementing JOIN Operations </em></h3>

Supports various types of JOIN operations, enabling you to combine rows from two or more tables based on a related column between them.

```csharp

IDatabaseConnection dbConnection = new SqlDatabaseConnection(connectionString);
QueryExecutor executor = new QueryExecutor(dbConnection);

JoinClause joinClause = new JoinClause();

joinClause.SetJoinType(JoinType.Left)
    .SetPrimaryTable("Customers")
    .SetSecondaryTable("Orders")
    .SetPrimaryKey("CustomerId")
    .SetForeignKey("CustomerID");

IEnumerable<Users> user1 = executor.Query<Users>(join: joinClause);

```



