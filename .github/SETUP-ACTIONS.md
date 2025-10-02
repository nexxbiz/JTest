# GitHub Actions Setup Guide

This guide helps you configure the necessary secrets, settings, and environments for the JTest GitHub Actions workflows to work properly.

## 📋 **Required Secrets and Settings**

### 🔑 **Repository Secrets**

You need to configure the following secrets in your GitHub repository settings:

#### **1. NuGet.org Publishing (Required for stable releases)**
- **Secret Name**: `NUGET_API_KEY`
- **Purpose**: Publish packages to NuGet.org for stable releases
- **Required for**: `ci-cd.yml` workflow (publish job)

#### **2. GitHub Token (Automatically available)**
- **Secret Name**: `GITHUB_TOKEN`
- **Purpose**: Create releases, upload artifacts, manage repository
- **Required for**: All workflows
- **Note**: This is automatically provided by GitHub, no setup needed

### 🌍 **Environment Configuration**

#### **Production Environment (Recommended)**
Set up a `production` environment for controlled releases:

1. Go to your repository → **Settings** → **Environments**
2. Click **New environment**
3. Name it `production`
4. Configure protection rules (optional but recommended):
   - **Required reviewers**: Add team members who should approve releases
   - **Wait timer**: Add delay before deployment (e.g., 5 minutes)
   - **Deployment branches**: Restrict to `main` branch and release tags

## 🛠️ **Step-by-Step Setup Instructions**

### **Step 1: Configure Repository Secrets**

1. **Navigate to Repository Settings**:
   - Go to your GitHub repository
   - Click **Settings** tab
   - In the left sidebar, click **Secrets and variables** → **Actions**

2. **Add NuGet API Key**:
   - Click **New repository secret**
   - Name: `NUGET_API_KEY`
   - Value: Your NuGet.org API key (see "How to Get NuGet API Key" below)
   - Click **Add secret**

### **Step 2: Create NuGet.org API Key**

1. **Create NuGet.org Account**:
   - Go to [https://www.nuget.org](https://www.nuget.org)
   - Sign up or log in with your account

2. **Generate API Key**:
   - Click your username → **API Keys**
   - Click **Create**
   - Configure the key:
     - **Key Name**: `JTest-GitHub-Actions`
     - **Expiration**: Set appropriate expiration (e.g., 365 days)
     - **Scopes**: Select **Push new packages and package versions**
     - **Glob Pattern**: `JTest.*` (to restrict to JTest packages only)
   - Click **Create**
   - **IMPORTANT**: Copy the API key immediately - it won't be shown again

3. **Add to GitHub Secrets**:
   - Follow Step 1 above to add the copied API key as `NUGET_API_KEY`

### **Step 3: Set Up Production Environment**

1. **Create Environment**:
   - Repository → **Settings** → **Environments**
   - Click **New environment**
   - Name: `production`

2. **Configure Protection Rules** (Recommended):
   ```
   ✅ Required reviewers: [Add team members]
   ✅ Wait timer: 5 minutes
   ✅ Deployment branches: Selected branches
      - Add rule: main
      - Add rule: refs/tags/v*
   ```

3. **Environment Secrets** (if needed):
   - Add any production-specific secrets here
   - These override repository secrets for production deployments

### **Step 4: Configure Branch Protection (Recommended)**

1. **Protect Main Branch**:
   - Repository → **Settings** → **Branches**
   - Click **Add rule** for `main` branch
   - Enable:
     ```
     ✅ Require status checks to pass before merging
     ✅ Require up-to-date branches before merging
     ✅ Require pull request reviews before merging
     ✅ Dismiss stale pull request approvals when new commits are pushed
     ```

2. **Add Required Status Checks**:
   - Add `build-and-test` job as required check
   - This ensures all tests pass before merging

## 🔧 **Workflow Configuration**

### **Current Workflow Setup**

The repository includes these workflows:

1. **`ci-cd.yml`** - Main CI/CD pipeline
   - **Triggers**: Push to main, PRs to main, version tags
   - **Secrets Used**: `GITHUB_TOKEN`, `NUGET_API_KEY`
   - **Environments**: `production` (for NuGet publishing)

2. **`build-and-test.yml`** - Simple build and test
   - **Triggers**: Push to main/develop, PRs to main
   - **Secrets Used**: None (basic build only)

3. **`release.yml`** - Legacy release workflow
   - **Triggers**: Version tags
   - **Secrets Used**: `GITHUB_TOKEN`, `NUGET_API_KEY` (commented out)
   - **Note**: May be superseded by `ci-cd.yml`

### **Workflow Behavior**

#### **On Pull Request**:
- Runs build and test
- No deployment or publishing

#### **On Push to Main**:
- Runs build and test
- Creates development release with auto-generated version
- Updates `development` tag with latest build
- **Does NOT** publish to NuGet.org

#### **On Version Tag Push** (e.g., `git tag v1.0.0 && git push origin v1.0.0`):
- Runs build and test
- Creates stable release on GitHub
- **Publishes to NuGet.org** (requires `NUGET_API_KEY`)
- Uses production environment (requires approval if configured)

## 🚀 **Testing Your Setup**

### **Test 1: Basic Build (No secrets required)**
1. Create a pull request with a small change
2. Verify the build and test workflow runs successfully
3. Check that all tests pass

### **Test 2: Development Release (GitHub token only)**
1. Merge a pull request to main
2. Verify the CI/CD workflow runs
3. Check that a development release is created/updated
4. Verify packages are attached to the release

### **Test 3: Stable Release (Requires NuGet API key)**
1. Create and push a version tag:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```
2. Verify the stable release is created
3. Check that packages are published to NuGet.org
4. Verify the release appears in GitHub releases

## ⚠️ **Security Best Practices**

### **API Key Security**
- **Never commit API keys** to your repository
- **Use specific scopes** - limit NuGet key to JTest packages only
- **Set expiration dates** - rotate keys regularly
- **Monitor usage** - check NuGet.org for unexpected activity

### **Environment Protection**
- **Require approvals** for production deployments
- **Limit deployment branches** to main and release tags
- **Use wait timers** to allow for last-minute cancellations

### **Branch Protection**
- **Require PR reviews** before merging to main
- **Require status checks** to ensure tests pass
- **Keep branches up to date** before merging

## 🛠️ **Troubleshooting**

### **Common Issues**

#### **"NUGET_API_KEY not found"**
- Verify the secret exists in repository settings
- Check the secret name matches exactly: `NUGET_API_KEY`
- Ensure the workflow has access to the secret

#### **"403 Forbidden" when publishing to NuGet**
- Check that your NuGet API key has push permissions
- Verify the key hasn't expired
- Ensure the package name isn't already taken by another user

#### **Development release not created**
- Check that the workflow completed successfully
- Verify `GITHUB_TOKEN` has proper permissions
- Check if there are any branch protection rules blocking the workflow

#### **Production environment not working**
- Ensure the environment is named exactly `production`
- Check that required reviewers are available and notified
- Verify the deployment branch rules allow the current branch/tag

### **Getting Help**

If you encounter issues:

1. **Check workflow logs**: Go to Actions tab → Click on the failed workflow → Review logs
2. **Verify secrets**: Ensure all required secrets are configured correctly
3. **Test incrementally**: Start with simple PR builds, then development releases, then stable releases
4. **Check permissions**: Ensure your GitHub account has admin access to configure secrets and environments

## 📚 **Additional Resources**

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [NuGet API Keys Guide](https://docs.microsoft.com/en-us/nuget/nuget-org/scoped-api-keys)
- [GitHub Environments Documentation](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
- [Branch Protection Rules](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/about-protected-branches)

---

**Need help?** Open an issue in the repository with the `help wanted` label and include:
- What you're trying to do
- Error messages or workflow logs
- Screenshots of your configuration