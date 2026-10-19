# Sales Order Technical Test

An ASP.NET Core MVC (C#, .NET 7) application for capturing, updating, deleting and viewing sales
orders and their order lines. Data is stored in an XML file that has the same shape as the example
in the spec.

## Running it

Requirements: the .NET 7 SDK.

```bash
dotnet run --project src/SalesOrders.Web --launch-profile http
```

Open http://localhost:5292 and log in with one of these accounts:

| User | Password | Role |
|---|---|---|
| `admin` | `Admin@123` | Admin: all CRUD, plus adding users |
| `guest` | `Guest@123` | Guest: search and view only |

Run the tests:

```bash
dotnet test
```

## Requirements coverage

| # | Requirement | Where |
|---|---|---|
| 1 | MVC app with an XML data store that is updated on add, edit and delete | `Data/XmlOrderStore.cs` → `src/SalesOrders.Web/App_Data/orders.xml` |
| 1e | Input validation (for example, Quantity can't accept text) | DataAnnotations on `Models/SalesOrder.cs`, enforced in the browser by jQuery unobtrusive validation and on the server by `ModelState` |
| 2 | Capture/update screen for the header and its lines | `Views/Orders/Edit.cshtml` (used for both Create and Edit). Lines are added and removed in the page, and line numbers auto-increment from 1 |
| 3 | Header grid, with the line grid shown when an order is selected | `Views/Orders/Index.cshtml` |
| 3b/c | Add, edit and delete lines | A popup (`_LineModal.cshtml`) on the grid, plus the full Edit screen |
| 3d | Search headers by order number/customer, order type, and From/To create date | Search bar on the Orders page |
| 3e | Search lines by product code | Search box on the line grid |
| 4 | SQL Server create script | `database/CreateDatabase.sql` (ERD in `docs/ERD.md`) |
| 5 | Login, Admin and Guest roles, adding users | Cookie auth (`AccountController`), the `CanEdit` policy, and `UsersController` (admin only) |
| 6 | REST GET by order number | `GET /api/orders/{orderNumber}` or `GET /api/orders?orderNumber=…` (requires login; returns 401 otherwise) |
| 7 | Unit tests | `tests/SalesOrders.Tests`: covers adding and updating orders and lines, search, and the user store |
| 8 | Dog API | **Dog API** menu item: shows a random image, with an optional breed filter |

## Design notes

- **Storage.** The store is XML (`<SalesOrders><SalesOrder><OrderHeader/><OrderLine/>…`), so it
  can be compared directly with the spec's example. Each change reads the file, applies the
  change, and writes it back. A lock prevents two requests from writing at the same time. The new
  contents go to a temp file that then replaces the original, so the file can't be left half
  written. On first run, the file is seeded with the two example orders.
- **Line numbers** start at 1 for each order and continue from the highest existing line.
  Existing lines are never renumbered, so deleting a line leaves a gap.
- **Order Number** is the business key, so it can't be changed after an order is created.
- **Security.** Every page requires login. All add/edit/delete actions and user management
  require the Admin role, which is enforced on the server (not just by hiding buttons). Every
  POST checks an anti-forgery token. Passwords are hashed with ASP.NET Core Identity's
  `PasswordHasher` (PBKDF2) and stored in `App_Data/users.json`.
- **Culture** is fixed to en-US, so prices always use `.` as the decimal separator.
- To reset the data, stop the app and delete `src/SalesOrders.Web/App_Data/*`. Both files are
  re-created and re-seeded on the next start.

## Project layout

```
SalesOrders.sln
src/SalesOrders.Web/        MVC app (Controllers, Models, Views, Data, Services)
tests/SalesOrders.Tests/    xUnit tests
database/CreateDatabase.sql SQL Server schema + seed data
docs/ERD.md                 Completed ER diagram
```
