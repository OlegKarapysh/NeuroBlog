# NeuroBlog

A small article-posting app: write articles in HTML, browse what others post, and
hold threaded (deeply nested) conversations in the comments. There is **no
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
- Comment on articles and reply to comments to **deep nesting** (up to 250 levels).
- Open a collapsible comment section under each article.
- Comments and replies load **lazily, 10 at a time** (see [Comments at scale](#comments-at-scale)):
  "Show comments" / "Show more comments" for top-level comments, and per-comment
  "Show replies" / "Show more replies" for direct replies.
- Edit your own comments.
- Delete your own comments: a comment that **has replies** is kept and shown as
  *“This comment was deleted”* so the thread stays intact; a comment with **no
  replies** is removed entirely.
- Validation: comments 1–1000 chars, article body ≥ 1 char, replies nested at
  most 250 levels deep (see [Comments at scale](#comments-at-scale)).

### Comments at scale

Comments are never loaded all at once — the design assumes an article may have
millions of comments, many of them deeply nested:

- The list endpoints return **one page of 10**, newest first, and only ever load
  more when the user asks ("Show more comments" / "Show more replies").
- Replies are fetched per comment, **one level (direct replies) at a time**, so a
  deep thread only costs queries for the branches the user actually expands.
- "More pages exist?" is answered by fetching one extra row (`Take(pageSize + 1)`)
  instead of a `COUNT`, which avoids counting across millions of rows.
- Composite indexes back the exact query shapes:
  `(ArticleId, ParentCommentId, CreatedAt, Id)` for top-level comments and
  `(ParentCommentId, CreatedAt, Id)` for replies, so a page reads straight from
  the index.
- "First 100 comments, depth-first" comes in two flavours. The recursive-CTE
  endpoint (`first-page-dfs`) expands the whole article tree before limiting. The
  `first-page-path` endpoint instead reads a denormalized **materialized path**
  column: comment Ids are sequence-backed `bigint`s (so an Id both orders siblings
  oldest-first and uniquely identifies them), and each comment's `Path` is a
  `bytea` holding its parent's path plus its own Id as 8 big-endian bytes. `bytea`
  compares byte-by-byte, so the path order *is* depth-first pre-order and the query
  is a single `WHERE "ArticleId" = @a ORDER BY "Path" LIMIT 100` served straight
  from the `(ArticleId, Path)` index — no recursion, no over-read.
- The path costs only 8 bytes per nesting level. Reply depth is capped at **250**
  so the deepest path stays well under PostgreSQL's ~2704-byte B-tree key limit; a
  reply past that depth is rejected with `400`, which is why "unlimited" nesting is
  in practice bounded.

### Security note

Pasted article HTML is **sanitized server-side** (via
[HtmlSanitizer](https://github.com/mganss/HtmlSanitizer)) before it is stored or
rendered — `<script>`, inline event handlers and other dangerous markup are
stripped, which prevents stored XSS while still allowing rich formatting.
Comments are plain text and HTML-encoded on display.

Because there is no authentication, "ownership" is simply the username sent in an
`X-Username` header. This is intentionally trivial to spoof and is fine for a
no-auth demo; it is **not** a real authorization system.

CORS is wide open (any origin, header and method), so the API can be called from
anywhere — convenient for a demo, not something you'd ship as-is.

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
| GET    | `/api/articles/{id}/comments/first-page`| First 100 comments breadth-first (by depth, replies grouped under their parent) |
| GET    | `/api/articles/{id}/comments/first-page-dfs`| First 100 comments depth-first (each comment followed by its descendants); raw recursive CTE |
| GET    | `/api/articles/{id}/comments/first-page-path`| First 100 comments depth-first via the materialized `Path` column — a single indexed `ORDER BY "Path" LIMIT 100`, no recursion |
| GET    | `/api/comments/{id}/replies?page=N`     | One page (10) of a comment's direct replies |
| POST   | `/api/articles/{id}/comments`           | Add a comment / reply                |
| PUT    | `/api/comments/{id}`                    | Edit your comment                    |
| DELETE | `/api/comments/{id}`                    | Delete your comment (kept as "deleted" if it has replies, else removed) |

## Database migrations

Migrations live in `NeuroBlog.Server/Migrations`. To add another after changing
the model:

```bash
dotnet ef migrations add <Name> --project NeuroBlog.Server
```

Migrations are applied automatically on app startup.
