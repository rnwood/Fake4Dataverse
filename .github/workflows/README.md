# GitHub Actions Workflows

## Build and Publish Workflow

The `build-and-publish.yml` workflow handles building, testing, packaging, and publishing the Fake4Dataverse packages.

### Triggers

- **Push to main/master/dev branches**: Creates CI builds with version suffix `-ci-YYYYMMDD-N`
- **Pull requests**: Creates CI builds with version suffix `-ci-YYYYMMDD-N-prXXXX`
- **Tags**: Creates release builds using the semantic version from the tag

### Version Numbering

#### CI Builds (Main/Master Branch)
- Format: `4.0.0-ci-YYYYMMDD-N`
- Example: `4.0.0-ci-20251010-42`
- Published as prerelease to NuGet and GitHub Releases

#### PR Builds
- Format: `4.0.0-ci-YYYYMMDD-N-prXXXX`
- Example: `4.0.0-ci-20251010-42-pr123`
- Published as prerelease to NuGet and GitHub Releases

#### Release Builds (Tags)
- Format: Tag must be `X.Y.Z` or `X.Y.Z-prerelease`
- Examples: `4.0.0`, `4.1.0-beta1`, `4.2.0-rc.1`
- Published to NuGet and GitHub Releases
- Marked as prerelease if tag contains a prerelease suffix

### Creating a Release

To create a release build:

1. Create and push a tag with semantic version:
   ```bash
   git tag 4.0.1
   git push origin 4.0.1
   ```

2. For prerelease versions:
   ```bash
   git tag 4.1.0-beta1
   git push origin 4.1.0-beta1
   ```

### Required Secrets

The workflow requires the following GitHub secret to be configured:

- `NUGET_API_KEY`: API key for publishing to nuget.org

To configure secrets:
1. Go to repository Settings → Secrets and variables → Actions
2. Add the `NUGET_API_KEY` secret

### Published Artifacts

Each build publishes:
1. **NuGet packages** to nuget.org:
   - Fake4Dataverse.Abstractions
   - Fake4Dataverse.Core
   - Fake4Dataverse

2. **GitHub Release** with:
   - All NuGet packages as downloadable assets
   - Auto-generated release notes from commits
   - Proper prerelease tagging

### Build Process

1. Checkout code
2. Setup .NET 8.0
3. Determine version suffix based on trigger type
4. Restore dependencies
5. Build for net8.0 and net462 frameworks
6. Run tests for both frameworks
7. Pack NuGet packages with appropriate version suffix
8. Upload packages as artifacts
9. Publish to NuGet (non-PR builds only)
10. Create GitHub Release (non-PR builds only)
