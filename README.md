# Tree editor

A small ASP.NET Core application with Entity Framework Core, SQLite, and a browser UI. The API and UI run in one process; SQLite is created automatically on first start.

## Run

Install the .NET 7 SDK, then run from the repository root:

```sh
dotnet run --project TreeEditor/TreeEditor.csproj --urls http://localhost:5000
```

Open <http://localhost:5000>. The first run restores the required NuGet package and creates `TreeEditor/tree.db` with sample data. **Reset sample data** restores that data at any time.

## How it works

`nodes` has `id`, `parent_id`, and `value`. `parent_id` is a foreign key back to `nodes.id` with `ON DELETE CASCADE`; an index supports loading a branch. The sample data includes paths four levels deep, for example Work → Projects → Tree editor → API.

The server has three small layers inside one project: `Controllers` handles HTTP, `Services` validates and coordinates operations, and `Data` contains the EF Core context, repository, and sample data setup. There are no migrations or separate projects for this exercise.

DBTreeView requests one branch at a time, 20 rows per page. Loading a selected element returns its value, parent ID, and ancestor ID path. The browser caches only explicitly loaded elements. That path lets CachedTreeView place elements under the nearest loaded ancestor, even if elements were loaded out of order. Loading a missing ancestor later moves its descendants into place.

Edits, additions, and deletions stay in the browser until **Apply changes**. Apply sends all pending changes in one database transaction. Deleting a row cascades through the database, including descendants that were never cached. Cached descendants are shown as deleted and cannot be edited. Loaded elements stay in CachedTreeView after Apply, including deleted elements. Only Reset clears the cache.
