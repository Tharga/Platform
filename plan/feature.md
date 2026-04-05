# Feature: github-actions-ci

**Originating branch:** develop
**Date started:** 2026-04-05

## Goal

Migrate CI/CD from Azure DevOps to GitHub Actions. Run in parallel with existing Azure DevOps pipeline until verified.

## Scope

### Phase 1: Build & Test (all branches)
- Build all projects
- Run all tests
- Triggered on push and PRs to `develop` and `master`

### Phase 2: Pack & Version
- Same versioning scheme: `majorMinor.patch` for releases, `majorMinor.patch-pre.N` for pre-releases
- `majorMinor` configured in workflow (currently `2.0`)
- Patch derived from latest GitHub release tag
- Pack all 4 NuGet packages with computed version

### Phase 3: Release & Publish
- On `master` merge: create GitHub Release with auto-generated notes from PR descriptions
- Push NuGet packages to nuget.org
- Pre-releases on `develop`: optional, gated behind manual approval (workflow_dispatch or environment approval)

### Parallel operation
- Keep `azure-pipelines.yml` untouched — both pipelines run side by side
- GitHub Actions does NOT push git tags (Azure DevOps still does that)
- Once verified, remove Azure DevOps pipeline in a separate step

## Acceptance criteria

- [ ] GitHub Actions workflow builds and tests on push/PR
- [ ] Version computed from latest release tag matches the `majorMinor.patch` / `majorMinor.patch-pre.N` scheme
- [ ] NuGet packages are packed with correct version
- [ ] GitHub Release created on master merge with release notes from PRs
- [ ] NuGet packages published to nuget.org on release
- [ ] Pre-release publishing requires manual approval
- [ ] Azure DevOps pipeline continues to work unchanged

## Done condition

All acceptance criteria met, user confirms the GitHub Actions pipeline works correctly.
