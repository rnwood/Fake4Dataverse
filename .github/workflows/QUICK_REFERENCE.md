# Quick Reference for Releases

## Creating Different Types of Releases

### Standard Release (Stable)
When you're ready to release a stable version:

```bash
git tag 4.0.1
git push origin 4.0.1
```

This will:
- Build version `4.0.1`
- Publish to NuGet as a stable release
- Create a GitHub Release marked as "Latest"
- Include auto-generated release notes

### Beta Release
For testing with early adopters:

```bash
git tag 4.1.0-beta1
git push origin 4.1.0-beta1
```

This will:
- Build version `4.1.0-beta1`
- Publish to NuGet as a prerelease
- Create a GitHub Release marked as "Pre-release"

### Release Candidate
When you're close to a release:

```bash
git tag 4.1.0-rc.1
git push origin 4.1.0-rc.1
```

This will:
- Build version `4.1.0-rc.1`
- Publish to NuGet as a prerelease
- Create a GitHub Release marked as "Pre-release"

### Alpha/Development Release
For internal testing:

```bash
git tag 4.2.0-alpha.1
git push origin 4.2.0-alpha.1
```

## CI Builds (Automatic)

### On Every Commit to Main
Automatically creates:
- Version: `4.0.0-ci-YYYYMMDD-N` (e.g., `4.0.0-ci-20251010-42`)
- Published to NuGet as prerelease
- GitHub Release created automatically

### On Every Pull Request
Automatically creates:
- Version: `4.0.0-ci-YYYYMMDD-N-prXXXX` (e.g., `4.0.0-ci-20251010-42-pr123`)
- **NOT published** to NuGet
- Packages available as GitHub Actions artifacts only

## Version Numbering Guidelines

Follow Semantic Versioning (SemVer):

- **Major version** (X.0.0): Breaking changes
- **Minor version** (X.Y.0): New features, backward compatible
- **Patch version** (X.Y.Z): Bug fixes, backward compatible

Examples:
- `4.0.0` → `4.0.1`: Bug fix release
- `4.0.0` → `4.1.0`: New feature release
- `4.0.0` → `5.0.0`: Breaking change release

## Checking Build Status

1. Go to the Actions tab in GitHub
2. Look for the "Build and Publish" workflow
3. Click on a workflow run to see details

## Viewing Published Packages

### NuGet
Visit: https://www.nuget.org/packages/Fake4Dataverse/

### GitHub Releases
Visit: https://github.com/rnwood/Fake4Dataverse/releases

## Troubleshooting

### Tag Already Exists
If you accidentally pushed a tag and need to recreate it:

```bash
# Delete local tag
git tag -d 4.0.1

# Delete remote tag
git push origin :refs/tags/4.0.1

# Create new tag
git tag 4.0.1
git push origin 4.0.1
```

### Build Failed
1. Check the Actions tab for error messages
2. Verify the tag follows the pattern `X.Y.Z` or `X.Y.Z-prerelease`
3. Check that NUGET_API_KEY secret is configured

### Package Not Published
Make sure:
- The build completed successfully
- You're not on a pull request (PRs don't publish)
- The NUGET_API_KEY secret is valid

## Configuration

### Required Secrets
Set in Settings → Secrets and variables → Actions:

| Secret Name | Description | Where to Get |
|------------|-------------|--------------|
| `NUGET_API_KEY` | API key for NuGet.org | https://www.nuget.org/account/apikeys |

### Permissions
The workflow requires these permissions (already configured):
- `contents: write` - For creating GitHub Releases
- `packages: write` - For publishing packages
