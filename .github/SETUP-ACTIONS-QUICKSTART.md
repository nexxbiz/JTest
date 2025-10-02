# GitHub Actions Quick Setup Checklist

## ✅ **Essential Setup (5 minutes)**

### **1. Add NuGet API Key Secret**
- Go to: Repository → Settings → Secrets and variables → Actions
- Click: **New repository secret**
- Name: `NUGET_API_KEY`
- Value: Get from [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
- ⚠️ **Required for publishing stable releases**

### **2. Create Production Environment**
- Go to: Repository → Settings → Environments
- Click: **New environment**
- Name: `production`
- Add protection rules (optional but recommended)

### **3. Test Your Setup**
```bash
# Test development release (main branch)
git checkout main
git commit --allow-empty -m "test: trigger development build"
git push origin main

# Test stable release (version tag)
git tag v0.1.0-test
git push origin v0.1.0-test
```

## 🔑 **Secrets Reference**

| Secret Name | Required For | How to Get |
|-------------|-------------|------------|
| `NUGET_API_KEY` | Publishing to NuGet.org | [Create at nuget.org](https://www.nuget.org/account/apikeys) |
| `GITHUB_TOKEN` | GitHub releases | Automatically provided |

## 🌍 **Environment Reference**

| Environment | Used For | Configuration |
|-------------|----------|--------------|
| `production` | NuGet publishing | Optional: Add reviewers, wait timer |

## 🚀 **Workflow Triggers**

| Trigger | What Happens | Secrets Used |
|---------|-------------|-------------|
| PR to main | Build + Test only | None |
| Push to main | Development release | `GITHUB_TOKEN` |
| Version tag | Stable release + NuGet | `GITHUB_TOKEN`, `NUGET_API_KEY` |

## 📞 **Quick Help**

**Problem with setup?** Check the [complete setup guide](.github/SETUP-ACTIONS.md) or open an issue.

**Ready to release?** 
```bash
git tag v1.0.0
git push origin v1.0.0
```