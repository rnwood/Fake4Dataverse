# GitHub Actions Workflows

## Build and Publish Workflow

The `build-and-publish.yml` workflow handles building, testing, packaging, and publishing the Fake4Dataverse packages with automatic versioning.

### Quick Reference

| Build Type | Version Format | Published To | Example |
|-----------|----------------|--------------|---------|
| Main/Master | `4.0.0-ci-YYYYMMDD-N` | NuGet + GitHub Releases (prerelease) | `4.0.0-ci-20251010-42` |
| Pull Request | `4.0.0-ci-YYYYMMDD-N-prXXXX` | Artifacts only (not published) | `4.0.0-ci-20251010-42-pr123` |
| Release Tag | `X.Y.Z` | NuGet + GitHub Releases | `4.0.1` |
| Prerelease Tag | `X.Y.Z-prerelease` | NuGet + GitHub Releases (prerelease) | `4.1.0-beta1` |

### Creating a Release

To create a release build, push a semantic version tag:

```bash
# Full release
git tag 4.0.1
git push origin 4.0.1

# Prerelease
git tag 4.1.0-beta1
git push origin 4.1.0-beta1
```

### Required Secrets

Configure the following GitHub secret in repository Settings → Secrets and variables → Actions:
- `NUGET_API_KEY`: API key for publishing to nuget.org

### Published Packages

Each build creates three NuGet packages:
- Fake4Dataverse.Abstractions
- Fake4Dataverse.Core
- Fake4Dataverse

For detailed implementation information, see [IMPLEMENTATION.md](IMPLEMENTATION.md).
