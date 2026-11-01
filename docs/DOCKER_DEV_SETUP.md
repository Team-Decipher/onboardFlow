# onboardFlow — Docker Development Environment

Team: Decipher Group 28 · Repo: `Team-Decipher/onboardFlow`
Stack: Next.js + React + TypeScript · ASP.NET Core Web API (C#) + EF Core · PostgreSQL · Azure Blob Storage (Azurite emulator locally)

**Goal:** every team member runs the exact same toolchain versions through Docker, with one command (`docker compose up`), on Ubuntu, Windows (WSL2) or macOS. Nobody installs Node, .NET or PostgreSQL on their own machine.

> **Status of this document:** the files below were written and reviewed carefully but were **not executed** when this document was produced. Run the verification checklist in section 9 and fix anything that fails *before* telling the team it is ready.

---

## 0. Instructions for the AI agent (Antigravity)

Read this whole file first, then do sections 4 to 9 in order.

**Rules**

1. Do **not** install or run Node, npm, .NET or PostgreSQL on the host. Everything runs inside Docker containers. (Exception: `git` and `docker` on the host.)
2. Use the exact version tags in section 2. Never use `latest` for Node, .NET or PostgreSQL. Do not change a version tag without asking the user.
3. Never commit `.env`, secrets, `node_modules/`, `bin/`, `obj/` or `.next/`.
4. Never run `docker compose down -v`, `docker volume prune` or `docker system prune -a` without explicit user approval (they delete the database).
5. Create the files exactly as written in section 4. If something must differ, explain why in your reply.
6. Ports are bound to `127.0.0.1` on purpose. Do not change this to `0.0.0.0`.
7. Do not use Dev Containers (`.devcontainer/`). Plain Docker Compose + the host terminal is the supported path for this project.
8. Follow the monorepo structure in section 4.1 exactly. Do not add new top-level folders or split the project into several repositories. Create `docs/PROGRESS.md` (section 4.9) and keep it updated.
9. After finishing, report the result of every item in the section 9 checklist (pass/fail, with command output for failures).

---

## 1. How it works (short)

- **Image** = a recipe-built package (OS + Node or .NET + tools). **Container** = a running instance of it.
- The recipes (`Dockerfile`s) and `docker-compose.yml` live in Git. Everyone builds the same images from the same recipes and the same pinned version tags, so everyone gets the same environment.
- Your **source code stays on your machine** and is *mounted* into the containers. Edit in your IDE, and the container's dev server (`next dev`, `dotnet watch`) reloads automatically.
- You still open `http://localhost:3000`. The difference is that the servers behind it run in containers.
- Containers on the Compose network reach each other **by service name** (`db`, `backend`, `azurite`), not `localhost`.
- The project is a **monorepo**: one Git repository holds the frontend, backend, Docker setup and docs (section 4.1). One clone and one `docker compose up` give you the whole system.

---

## 2. Agreed versions (single source of truth)

| Component | Image tag | Notes |
|---|---|---|
| Node.js | `node:24-bookworm-slim` | Debian-based (glibc) so native npm modules behave the same as on Ubuntu hosts. Node 26 is also LTS now; stay on 24 unless the team agrees to move. |
| .NET SDK | `mcr.microsoft.com/dotnet/sdk:10.0` | .NET 10 LTS. `dotnet-ef` is pinned to major version 10. |
| PostgreSQL | `postgres:17` | If you ever move to 18+, the data mount path changes (see section 10). |
| Azurite | `mcr.microsoft.com/azure-storage/azurite` | After the first successful run, pin the tag shown by `docker compose images` and commit it. |

Changing any version = a pull request that the whole team reviews. After merging, everyone runs `docker compose up --build`.

---

## 3. Host prerequisites (each member)

Only **Git** and **Docker Engine with the Compose plugin** are needed.

### Ubuntu

Install Docker from Docker's official apt repository (check https://docs.docker.com/engine/install/ubuntu/ for the current steps):

```bash
sudo apt update
sudo apt install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# run docker without sudo (log out and back in, or run newgrp)
sudo usermod -aG docker $USER
newgrp docker

docker --version
docker compose version
docker run --rm hello-world
```

For an 8 GB machine, a swap file is a useful safety net (check `free -h` first):

```bash
sudo fallocate -l 4G /swapfile && sudo chmod 600 /swapfile
sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### Windows

Install WSL2 + Ubuntu and Docker Desktop (WSL2 backend). **Clone the repo inside the WSL filesystem** (`~/projects/onboardFlow`), not under `C:\`. Run all commands from the WSL terminal.

### macOS

Install Docker Desktop. In `.env`, enable the polling variables (section 4.6).

---

## 4. Files to create

### 4.1 Repository layout (monorepo)

One repository (`Team-Decipher/onboardFlow`) holds everything. The frontend, backend and docs are separate folders at the root, and a single Docker Compose setup runs the whole system.

```
onboardFlow/                          # repository root
├── frontend/                         # Next.js + React + TypeScript app
├── backend/                          # ASP.NET Core solution
│   ├── OnboardFlow.sln
│   └── src/
│       └── OnboardFlow.Api/          # Web API project (add tests/ next to src/ later)
├── docs/
│   ├── PROGRESS.md                   # tracks which tasks are completed (section 4.9)
│   └── DOCKER_DEV_SETUP.md           # this guide
├── docker/                           # Dockerfiles and entrypoint script
│   ├── backend.Dockerfile
│   ├── frontend.Dockerfile
│   └── frontend-entrypoint.sh
├── .github/workflows/                # optional (section 6)
│   └── publish-dev-images.yml
├── docker-compose.yml
├── docker-compose.ghcr.yml           # optional: use shared prebuilt images (section 6)
├── .env.example                      # committed
├── .env                              # NOT committed (each member's own copy)
├── .gitignore
├── .gitattributes
└── README.md
```

**Why a monorepo here**

- One pull request can change an API endpoint and the page that uses it together.
- One clone and one Docker setup for everyone.
- Docs and the progress tracker live next to the code they describe.
- The frontend (JavaScript) and backend (C#) use different toolchains, so no monorepo tooling (Nx, Turborepo) is needed. Plain folders are enough.
- Trade-off: CI would run everything on every change. Later, add `paths:` filters to the GitHub Actions workflows (for example run the frontend job only when `frontend/**` changes).

**Folder rules**

| Folder | Contains | Notes |
|---|---|---|
| `frontend/` | Only the Next.js app | Generated in section 5. Never run `npm` on the host. |
| `backend/` | Only the .NET solution and projects | Generated in section 5. Add xUnit projects under `backend/tests/`. |
| `docs/` | Markdown documentation and the progress tracker | Update `PROGRESS.md` in the same pull request as the work. |
| `docker/` | Dockerfiles and the frontend entrypoint script | Kept out of `frontend/` and `backend/` so the app folders stay clean for the project generators. |

Git does not track empty folders, so `frontend/` and `backend/` only appear in the repository once the generated projects are committed.

### 4.2 `docker-compose.yml`

```yaml
name: onboardflow

services:
  db:
    image: postgres:17
    restart: unless-stopped
    environment:
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: ${DB_NAME}
    ports:
      - "127.0.0.1:${DB_PORT:-5432}:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d ${DB_NAME}"]
      interval: 5s
      timeout: 5s
      retries: 10
    mem_limit: 512m

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    restart: unless-stopped
    command: >
      azurite-blob
      --blobHost 0.0.0.0
      --location /data
      --skipApiVersionCheck
    ports:
      - "127.0.0.1:${BLOB_PORT:-10000}:10000"
    volumes:
      - azurite:/data
    mem_limit: 256m

  backend:
    build:
      context: ./docker
      dockerfile: backend.Dockerfile
    user: "${HOST_UID:-1000}:${HOST_GID:-1000}"
    working_dir: /app
    depends_on:
      db:
        condition: service_healthy
      azurite:
        condition: service_started
    environment:
      HOME: /tmp
      DOTNET_CLI_HOME: /tmp
      ASPNETCORE_ENVIRONMENT: Development
      DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER: "1"
      DOTNET_WATCH_RESTART_ON_RUDE_EDIT: "1"
      DOTNET_USE_POLLING_FILE_WATCHER: ${DOTNET_USE_POLLING_FILE_WATCHER:-0}
      # Adapt these names to the keys your appsettings / code actually read.
      ConnectionStrings__Default: "Host=db;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
      Storage__BlobConnectionString: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;"
      Jwt__Key: ${JWT_KEY}
      Brevo__ApiKey: ${BREVO_API_KEY:-}
      Cors__AllowedOrigins__0: http://localhost:${FRONTEND_PORT:-3000}
    command:
      - dotnet
      - watch
      - run
      - --project
      - ${BACKEND_PROJECT:-.}
      - --urls
      - http://0.0.0.0:8080
    ports:
      - "127.0.0.1:${API_PORT:-5000}:8080"
    volumes:
      - ./backend:/app
      - nuget:/nuget
    mem_limit: 1500m

  frontend:
    build:
      context: ./docker
      dockerfile: frontend.Dockerfile
    user: "${HOST_UID:-1000}:${HOST_GID:-1000}"
    working_dir: /app
    depends_on:
      - backend
    environment:
      HOME: /tmp
      NEXT_TELEMETRY_DISABLED: "1"
      NODE_OPTIONS: --max-old-space-size=1024
      WATCHPACK_POLLING: ${WATCHPACK_POLLING:-}
      # Used by code running in the BROWSER:
      NEXT_PUBLIC_API_URL: http://localhost:${API_PORT:-5000}
      # Used by code running on the Next.js SERVER (server components, route handlers):
      API_URL_INTERNAL: http://backend:8080
    ports:
      - "127.0.0.1:${FRONTEND_PORT:-3000}:3000"
    volumes:
      - ./frontend:/app
    mem_limit: 1500m

volumes:
  pgdata:
  azurite:
  nuget:
```

### 4.3 `docker/backend.Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0

LABEL org.opencontainers.image.source="https://github.com/Team-Decipher/onboardFlow"

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_PACKAGES=/nuget

# Shared NuGet cache (mounted as a named volume) must be writable by a non-root user.
# dotnet-ef is pinned to the same major version as the SDK.
RUN mkdir -p /nuget && chmod 777 /nuget \
 && dotnet tool install dotnet-ef --version "10.*" --tool-path /usr/local/bin

WORKDIR /app
EXPOSE 8080
```

### 4.4 `docker/frontend.Dockerfile`

```dockerfile
FROM node:24-bookworm-slim

LABEL org.opencontainers.image.source="https://github.com/Team-Decipher/onboardFlow"

COPY frontend-entrypoint.sh /usr/local/bin/frontend-entrypoint.sh
RUN chmod +x /usr/local/bin/frontend-entrypoint.sh

WORKDIR /app
EXPOSE 3000
ENTRYPOINT ["/usr/local/bin/frontend-entrypoint.sh"]
```

### 4.5 `docker/frontend-entrypoint.sh`

Installs dependencies only when `package-lock.json` changed (so restarts are fast), then starts the dev server.

```sh
#!/bin/sh
set -e
cd /app

if [ ! -f package.json ]; then
  echo "No package.json in /app (the frontend/ folder). Create the Next.js app first (see README)." >&2
  exit 1
fi

HASH=""
[ -f package-lock.json ] && HASH=$(md5sum package-lock.json | cut -d' ' -f1)
STORED=$(cat node_modules/.lock-hash 2>/dev/null || true)

if [ ! -d node_modules ] || [ "$HASH" != "$STORED" ]; then
  echo "Installing dependencies..."
  if [ -f package-lock.json ]; then npm ci; else npm install; fi
  [ -n "$HASH" ] && echo "$HASH" > node_modules/.lock-hash
fi

exec npm run dev -- --hostname 0.0.0.0 --port 3000
```

### 4.6 `.env.example`

Each member copies this to `.env`. **`.env` is never committed.**

```dotenv
# ---------- Database ----------
DB_USER=onboard
DB_PASSWORD=change_me_locally
DB_NAME=onboardflow

# ---------- Ports on YOUR machine (change only if something else already uses them) ----------
FRONTEND_PORT=3000
API_PORT=5000
DB_PORT=5432
BLOB_PORT=10000

# ---------- Linux file ownership ----------
# Ubuntu/Linux: put the output of `id -u` and `id -g` here.
# Windows (WSL2) / macOS: leave as 1000.
HOST_UID=1000
HOST_GID=1000

# ---------- Backend ----------
# Folder of the API .csproj relative to backend/ (e.g. src/OnboardFlow.Api). Use . if the project is directly in backend/.
BACKEND_PROJECT=src/OnboardFlow.Api
# Development-only signing key, at least 32 characters. Use different keys in real environments (Azure Key Vault).
JWT_KEY=dev-only-change-me-use-at-least-32-characters
# Your own Brevo TEST key (leave empty if you are not working on email).
BREVO_API_KEY=

# ---------- File-watching (macOS, or Windows if the repo is NOT inside WSL) ----------
# Uncomment both lines if hot reload does not pick up your edits.
# WATCHPACK_POLLING=true
# DOTNET_USE_POLLING_FILE_WATCHER=1
```

### 4.7 `.gitignore`

```gitignore
# secrets
.env

# frontend
node_modules/
.next/
out/
coverage/

# backend
bin/
obj/
*.user
.vs/

# misc
.DS_Store
```

### 4.8 `.gitattributes`

Prevents Windows CRLF line endings from breaking shell scripts inside Linux containers.

```gitattributes
* text=auto eol=lf
*.sh text eol=lf
```

### 4.9 `docs/PROGRESS.md`

Tracks which tasks are completed. The phases below are examples only: replace them with the modules and deadlines from the interim report.

```markdown
# Project Progress

Legend: [ ] not started · [~] in progress · [x] done
Update this file in the same pull request as the work it describes.

## Phase 0: Setup
- [ ] Repo structure and Docker environment (owner: ___, PR: #__)
- [ ] Next.js and ASP.NET Core projects generated
- [ ] CORS, health endpoint, config keys wired
- [ ] CI pipeline (GitHub Actions)

## Phase 1: Authentication and users
- [ ] User entities and first migration
- [ ] Login and JWT
- [ ] Role-based access

## Phase 2: Client onboarding
- [ ] ...

## Phase 3: Document upload and workflow
- [ ] ...

## Phase 4: Notifications (Brevo)
- [ ] ...

## Phase 5: Testing and deployment
- [ ] ...

## Log
| Date | Task | Owner | PR | Notes |
|---|---|---|---|---|
|  |  |  |  |  |
```

Tips: one line per task with an owner, and update the file in the same pull request so reviewers can confirm the task is really done. If the team grows, GitHub Issues with a Projects board can replace the task list, with `PROGRESS.md` kept as a high-level summary.

---

## 5. One-time project bootstrap (done by ONE member, then pushed)

No Node or .NET on the host: generate the projects with throwaway containers that run as your own user.

```bash
git clone https://github.com/Team-Decipher/onboardFlow.git
cd onboardFlow
git checkout -b setup/docker

mkdir -p frontend backend docs docker
```

Then copy this guide to `docs/DOCKER_DEV_SETUP.md`, create `docs/PROGRESS.md` (section 4.9), and create the files from sections 4.2 to 4.8 (Dockerfiles and entrypoint go in `docker/`).

**Frontend (Next.js + TypeScript)**

```bash
docker run --rm -it \
  -u "$(id -u):$(id -g)" -e HOME=/tmp \
  -v "$PWD/frontend":/app -w /app \
  node:24-bookworm-slim \
  npx create-next-app@latest . --typescript --app --use-npm
```

Answer the prompts (TypeScript yes, App Router yes, npm). Commit the generated `package-lock.json`.

**Backend (ASP.NET Core Web API with controllers)**

```bash
docker run --rm \
  -u "$(id -u):$(id -g)" -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_NOLOGO=1 \
  -v "$PWD/backend":/app -w /app \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  sh -c "dotnet new sln -n OnboardFlow \
      && dotnet new webapi -n OnboardFlow.Api -o src/OnboardFlow.Api --use-controllers \
      && dotnet sln add src/OnboardFlow.Api"
```

Then set `BACKEND_PROJECT=src/OnboardFlow.Api` in `.env.example`.

**Wire the template to the container setup**

- **CORS:** the browser calls the API from `http://localhost:3000`, so the API must allow that origin (read `Cors:AllowedOrigins` from configuration, add `AddCors` and `UseCors`). Without this, browser requests fail with CORS errors.
- **HTTPS redirection:** inside the container the API listens on plain HTTP. Remove or guard `app.UseHttpsRedirection()` in Development to avoid redirect warnings.
- **Database and storage:** read `ConnectionStrings:Default` and `Storage:BlobConnectionString` from configuration (the environment variables in the compose file map to these keys).

Then commit everything (`docker/`, `docs/`, `docker-compose.yml`, `.env.example`, `.gitignore`, `.gitattributes`, `README.md`, `frontend/`, `backend/`) on the `setup/docker` branch and open a pull request. Teammates start only after it is merged into `main`.

---

## 6. Sharing the image with everyone (two options)

### Option A — build locally from the recipes (default, works with zero setup)

Every member runs `docker compose up --build`. Because the Dockerfiles and version tags are in Git, everyone gets identical environments. The first build takes a couple of minutes, then it is cached.

### Option B — one prebuilt image in GitHub Container Registry (GHCR)

Publish the two dev images once from GitHub Actions; everyone pulls the same image instead of building it. These images contain only the official Node/.NET toolchains plus `dotnet-ef`: **no source code and no secrets** (your code is mounted at runtime).

**`.github/workflows/publish-dev-images.yml`**

```yaml
name: Publish dev images

on:
  push:
    branches: [main]
    paths:
      - "docker/**"
  workflow_dispatch:

permissions:
  contents: read
  packages: write

jobs:
  publish:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        service: [backend, frontend]
    steps:
      - uses: actions/checkout@v4

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - uses: docker/setup-buildx-action@v3

      - uses: docker/build-push-action@v6
        with:
          context: ./docker
          file: ./docker/${{ matrix.service }}.Dockerfile
          push: true
          tags: |
            ghcr.io/team-decipher/onboardflow-${{ matrix.service }}-dev:latest
            ghcr.io/team-decipher/onboardflow-${{ matrix.service }}-dev:${{ github.sha }}
```

(Image names must be lowercase. Check the current major versions of these actions when you add the workflow.)

**`docker-compose.ghcr.yml`**

```yaml
services:
  backend:
    image: ghcr.io/team-decipher/onboardflow-backend-dev:latest
    pull_policy: always
  frontend:
    image: ghcr.io/team-decipher/onboardflow-frontend-dev:latest
    pull_policy: always
```

**Using it**

```bash
docker compose -f docker-compose.yml -f docker-compose.ghcr.yml up
```

**Access:** after the first workflow run, open the package on GitHub (organization → Packages) and either:
- set visibility to **Public** (simplest; fine because the images contain no secrets), or
- keep it private, make sure the package's access includes the team (the `LABEL org.opencontainers.image.source` line links it to the repo), and have each member run `docker login ghcr.io` with a personal access token that has `read:packages`.

If the pull fails, fall back to Option A. Both options use the same version tags.

---

## 7. Daily use (every member)

### First time on a new machine

```bash
git clone https://github.com/Team-Decipher/onboardFlow.git
cd onboardFlow
cp .env.example .env
# edit .env: set HOST_UID/HOST_GID from `id -u` / `id -g`, set your own DB password and keys
docker compose up --build
```

- Frontend: http://localhost:3000
- API: http://localhost:5000
- Apply database migrations (new terminal): see below.

### Commands

```bash
docker compose up -d                  # start everything in the background
docker compose logs -f backend        # follow one service's logs
docker compose ps                     # status
docker compose stop                   # stop, keep containers and data
docker compose down                   # remove containers, KEEP data volumes
docker compose up --build             # rebuild after a Dockerfile / version change
docker compose exec backend bash      # shell inside a container
docker stats                          # live RAM/CPU per container
```

Only start what you need to save RAM, e.g. `docker compose up db azurite backend`.

### Packages and tools: always run them inside the containers

The `node_modules` folder is shared between the host and the container, so **never run `npm install` on the host**. Always:

```bash
docker compose exec frontend npm install <package>        # commit package.json AND package-lock.json
docker compose exec frontend npm test                      # Jest
docker compose exec backend dotnet add src/OnboardFlow.Api package <PackageName>
docker compose exec backend dotnet test                    # xUnit
```

### Database and migrations

```bash
# create a migration (whoever changes an entity does this, then commits the generated files)
docker compose exec backend dotnet ef migrations add <Name> --project src/OnboardFlow.Api

# apply migrations (every member, after pulling)
docker compose exec backend dotnet ef database update --project src/OnboardFlow.Api

# open psql
docker compose exec db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

Start over with an empty database (**deletes your local data**):

```bash
docker compose down -v
docker compose up -d
```

---

## 8. Team rules

1. **Docker files are shared code.** Changes to `docker/`, `docker-compose.yml`, `.env.example` or any version tag go through a pull request.
2. After pulling changes that touch `docker/` or `package-lock.json`, run `docker compose up --build`.
3. **Migrations are committed.** One person creates it, everyone else applies it. Never edit the schema by hand.
4. **No secrets in Git.** `.env` stays local. Deployed environments use Azure Key Vault (per the project report).
5. **No shared database dumps through Git.** Use a committed seed script (or EF Core seed data) for common test data.
6. **Install packages only inside containers** (section 7).
7. **Use the same code path as CI.** GitHub Actions should build/test with the same images and versions.
8. **Keep `docs/PROGRESS.md` current.** Tick off or add your task in the same pull request as the work.
9. **Use feature branches and pull requests.** Branch names like `feature/<name>`, one reviewer per pull request. Protect `main` in GitHub (Settings, Branches, require a pull request before merging).
10. **Production images are separate.** The files here are for development only. For Azure App Service, create separate multi-stage production Dockerfiles.

---

## 9. Verification checklist (the agent must run all of these)

Run from a **fresh clone** in a different folder, with only `.env` created from `.env.example`.

| # | Check | Command | Expected |
|---|---|---|---|
| 1 | Compose file valid | `docker compose config -q` | no errors |
| 2 | Everything builds and starts | `docker compose up -d --build` then `docker compose ps` | all services `running`; `db` shows `healthy` |
| 3 | Frontend reachable | `curl -s -o /dev/null -w "%{http_code}" http://localhost:3000` | `200` |
| 4 | API reachable | `curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/openapi/v1.json` (or any real endpoint) | `200` |
| 5 | Backend can reach the DB | `docker compose exec backend dotnet ef database update --project $BACKEND_PROJECT` (or any migration) | succeeds |
| 6 | Frontend server can reach the API by service name | `docker compose exec frontend node -e "fetch('http://backend:8080/openapi/v1.json').then(r=>console.log(r.status))"` | `200` |
| 7 | Browser CORS works | call the API from a page on `localhost:3000` | no CORS error |
| 8 | Hot reload (frontend) | edit `frontend/app/page.tsx`, refresh the browser | change appears |
| 9 | Hot reload (backend) | edit a controller, watch `docker compose logs -f backend` | `dotnet watch` rebuilds |
| 10 | Files are owned by the host user | `ls -l frontend backend` | owner is you, **not** root |
| 11 | Data persists | `docker compose down && docker compose up -d` | database data still present |
| 12 | Nothing sensitive tracked | `git status` | no `.env`, `node_modules`, `bin`, `obj`, `.next` |
| 13 | Ports not exposed to the network | `ss -tlnp \| grep -E '3000\|5000\|5432\|10000'` | listening on `127.0.0.1` only |
| 14 | Repo structure matches section 4.1 | `ls` at the repo root, `ls docs` | `frontend/`, `backend/`, `docs/`, `docker/` exist; `docs/PROGRESS.md` exists |
| 15 | RAM sanity (8 GB machines) | `docker stats --no-stream` | total well under ~4 GB when idle |

If any check fails, fix the cause and update this document.

---

## 10. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `permission denied` on the Docker socket | You are not in the `docker` group: `sudo usermod -aG docker $USER`, then log out and in. |
| Files in `backend/` or `frontend/` owned by root | `HOST_UID` / `HOST_GID` missing or wrong in `.env`. Fix, then `sudo chown -R $USER:$USER frontend backend` once. |
| Port already in use | Another program uses it (often a local PostgreSQL on 5432). Change the port in `.env`. |
| Frontend shows API errors only on the server side | Server code must call `API_URL_INTERNAL` (`http://backend:8080`); only browser code uses `NEXT_PUBLIC_API_URL`. Inside the frontend container, `localhost` is the frontend itself. |
| Browser blocks API calls (CORS) | Configure CORS in the API for `http://localhost:3000` (section 5). |
| Backend cannot connect to DB | Use host `db`, not `localhost`. Check `docker compose ps` shows `db` healthy. |
| Edits do not trigger reload | macOS, or repo on a Windows drive: enable the polling lines in `.env`. On Windows keep the repo inside WSL. |
| Native module / `node_modules` errors | Someone ran `npm install` on the host. Run `docker compose down`, delete `frontend/node_modules`, `docker compose up`. |
| Azurite data missing after restart | Make sure `--location /data` is in the compose command and the `azurite` volume exists. |
| Postgres 18+ container fails with the old volume path | Postgres 18 changed its data directory layout; mount `pgdata` at `/var/lib/postgresql` instead of `/var/lib/postgresql/data`, and migrate data deliberately. |
| Shell script fails with `\r: command not found` | Windows line endings. Ensure `.gitattributes` is committed, then re-clone. |
| Out of memory / system freezes | Start fewer services, add swap (section 3), keep the `mem_limit` values, close browser tabs. |
| Disk filling up | `docker system df`, then `docker builder prune` and `docker image prune` (not `-a` unless you are fine re-downloading). |

---

## 11. Using this with Antigravity

- Open the repo folder as a normal workspace. Antigravity edits files on the host; the bind mounts make them visible to the containers immediately.
- Run Docker commands in Antigravity's terminal (host shell), e.g. `docker compose up -d`.
- Do **not** rely on Dev Containers inside Antigravity; support is currently unreliable. Plain Compose avoids the problem.
- Remind any agent working on the repo of the rules in section 0 (no host installs, pinned versions, never run destructive volume commands without approval).
