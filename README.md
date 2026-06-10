# NeuroBlog

A small article-posting app: write articles in HTML, browse what others post, and
hold threaded (unlimited-depth) conversations in the comments. There is **no
authentication** — pick any username and start using it.

## Stack

| Layer    | Technology                                              |
|----------|---------------------------------------------------------|
| Frontend | Blazor WebAssembly **PWA** (.NET 10)                    |
| Backend  | ASP.NET Core (.NET 10) REST API                         |
| Database | PostgreSQL (Entity Framework Core + Npgsql)             |
| Packaging| Docker + Docker Compose                                 |

The ASP.NET Core server hosts the published Blazor WASM client **and** the API
from a single container, so the whole app is just two services: `app` + `postgres`.

```
NeuroBlog/          Blazor WebAssembly client (UI, PWA)
NeuroBlog.Server/   ASP.NET Core API + static host for the client
NeuroBlog.Shared/   DTOs and validation limits shared by both
```

## Features

- Pick any username (stored in `localStorage`); switch users any time.
- Create an article by pasting HTML; optional title.
- Browse articles by everyone, read full articles.
- Edit / delete your own articles.
- Comment on articles and reply to comments to **any depth**.
- Open a collapsible comment section under each article.
- Edit your own comments; delete your own comments (shown as
  *“This comment was deleted”*, with replies preserved).
- Validation: comments 1–1000 chars, article body ≥ 1 char.

### Security note

Pasted article HTML is **sanitized server-side** (via
[HtmlSanitizer](https://github.com/mganss/HtmlSanitizer)) before it is stored or
rendered — `<script>`, inline event handlers and other dangerous markup are
stripped, which prevents stored XSS while still allowing rich formatting.
Comments are plain text and HTML-encoded on display.

Because there is no authentication, "ownership" is simply the username sent in an
`X-Username` header. This is intentionally trivial to spoof and is fine for a
no-auth demo; it is **not** a real authorization system.

## Run with Docker (recommended)

```bash
docker compose up --build
```

Then open <http://localhost:8080>. The database schema is created automatically
on startup (EF Core migrations are applied, retrying while Postgres warms up).

Data persists in the `pgdata` Docker volume. To start fresh:

```bash
docker compose down -v
```

## Run locally (without Docker)

You need the .NET 10 SDK and a PostgreSQL instance.

1. Start Postgres (e.g. the bundled one):

   ```bash
   docker compose up -d postgres
   ```

   The bundled Postgres is published on host port **5433** (to avoid clashing
   with a local Postgres on 5432). The default connection string in
   `NeuroBlog.Server/appsettings.json` targets `localhost:5433` with
   user/password/db all `neuroblog`.

2. Run the server (it serves the client too):

   ```bash
   dotnet run --project NeuroBlog.Server
   ```

   Open the URL printed in the console (e.g. <http://localhost:5271>).

## API

All write endpoints require a non-empty `X-Username` header (the client sets it
automatically).

| Method | Route                                   | Description                          |
|--------|-----------------------------------------|--------------------------------------|
| GET    | `/api/articles`                         | List articles (summaries)            |
| GET    | `/api/articles/{id}`                    | Get one article                      |
| POST   | `/api/articles`                         | Create an article                    |
| PUT    | `/api/articles/{id}`                    | Edit your article                    |
| DELETE | `/api/articles/{id}`                    | Delete your article (cascades comments) |
| GET    | `/api/articles/{id}/comments?page=N`    | One page (10) of top-level comments, newest first |
| GET    | `/api/comments/{id}/replies?page=N`     | One page (10) of a comment's direct replies |
| POST   | `/api/articles/{id}/comments`           | Add a comment / reply                |
| PUT    | `/api/comments/{id}`                    | Edit your comment                    |
| DELETE | `/api/comments/{id}`                    | Soft-delete your comment             |

## Database migrations

The initial migration lives in `NeuroBlog.Server/Migrations`. To add another
after changing the model:

```bash
dotnet ef migrations add <Name> --project NeuroBlog.Server
```

Migrations are applied automatically on app startup.
