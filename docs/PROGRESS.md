# Project Progress

Legend: [ ] not started · [~] in progress · [x] done
Update this file in the same pull request as the work it describes.

## Phase 0: Setup
- [x] Repo structure and Docker environment (owner: Decipher Group 28, PR: #setup/docker)
- [x] Next.js and ASP.NET Core projects generated
- [x] CORS, health endpoint, config keys wired
- [ ] CI pipeline (GitHub Actions)

## Phase 1: Authentication and users
- [x] User entities and first migration
- [x] Login and JWT
- [x] Role-based access

## Phase 2: Client onboarding
- [x] Dynamic Checklist Management & Case Creation

## Phase 3: Document upload and workflow
- [ ] Secure Document Upload & Version Retention
- [ ] Sequential Approval Pipelines & Delegation

## Phase 4: Notifications (Brevo)
- [ ] Automated reminders and alert dispatching

## Phase 5: Testing and deployment
- [ ] Testing, CI/CD, and production Dockerfiles

## Log
| Date | Task | Owner | PR | Notes |
|---|---|---|---|---|
| 2026-11-01 | Setup Docker dev environment | Decipher Group 28 | #setup/docker | Monorepo layout, Dockerfiles, compose, dev verification |
| 2026-11-17 | US-13: Create Checklist Template | Decipher Group 28 | feature/CODWP-11-us13-create-checklist | POST /api/checklist-templates, admin dashboard, Create modal |
| 2026-11-17 | US-18: Assign Reviewer to Item | Decipher Group 28 | feature/CODWP-15-us18-assign-reviewer | PUT .../reviewer endpoint, optimistic UI, role-gated |
| 2026-11-17 | US-19: Set Due Date & Due Days | Decipher Group 28 | feature/CODWP-16-us19-set-due-date | PUT .../due endpoint, DueDays domain property, isOverdue flag, TimeProvider |
