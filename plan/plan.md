# Plan: github-actions-ci

## Steps

### Phase 1: Build & Test
1. [x] Create `.github/workflows/build.yml` — build + test on push/PR to develop and master

### Phase 2: Pack & Version
2. [x] Add version computation step — read `majorMinor` from workflow, derive patch from latest GitHub release tag
3. [x] Add pack step — pack 4 NuGet packages with computed version
4. [x] Upload packages as release artifacts via `gh release create`

### Phase 3: Release & Publish
5. [x] Create `.github/workflows/release.yml` — triggered on master push, creates GitHub Release with auto-generated PR notes
6. [x] Add NuGet publish step — push packages to nuget.org (requires NUGET_API_KEY secret)
7. [x] Create `.github/workflows/prerelease.yml` — manual dispatch with confirmation, creates pre-release

### Verification
8. [ ] Push to develop, verify build + test runs
9. [ ] Create PR, verify checks appear
10. [ ] Verify Azure DevOps pipeline still works
11. [ ] Commit
