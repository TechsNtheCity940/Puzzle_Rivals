# Unity Personal License Setup Guide

## Current Status

✅ **Workflow is correctly configured**  
✅ **Found existing .ulf file in repository**  
❌ **Missing UNITY_EMAIL and UNITY_PASSWORD secrets**

## Required Secrets

For Unity Personal license in CI/CD, you need to add these 3 secrets to your GitHub repo:

1. **UNITY_LICENSE** - Raw .ulf file content (XML)
2. **UNITY_EMAIL** - Your Unity account email
3. **UNITY_PASSWORD** - Your Unity account password

## Steps to Complete Setup

### Step 1: Activate Unity Personal License Locally

1. Install Unity Hub on your machine
2. Sign in with your Unity account (the one you'll use for CI secrets)
3. Install Unity 2022.3.21f1 (or use the version specified in the workflow)
4. **Activate your license** in Unity Hub (Personal license doesn't require payment)

### Step 2: Locate Your .ulf License File

After activation, find your license file:

**Windows:** `C:\ProgramData\Unity\Unity_lic.ulf`
**macOS:** `/Library/Application Support/Unity/Unity_lic.ulf`
**Linux:** `~/.local/share/unity3d/Unity/Unity_lic.ulf`

### Step 3: Get the Email and Password

You'll need to use the **Unity account credentials** that were used to activate the license.

### Step 4: Add Secrets to GitHub

1. Go to your repository: `https://github.com/TechsNtheCity940/Puzzle_Rivals`
2. Click **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add these secrets:

| Secret Name | Value |
|-------------|-------|
| `UNITY_LICENSE` | Paste the entire contents of your `.ulf` file |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

### Step 5: Verify Setup

After adding all 3 secrets, the workflow will automatically run on your next push, or you can trigger it manually via **Actions** → **WebGL Build** → **Run workflow**.

## Troubleshooting

### Error: "TimeStamp validation failed"
- The .ulf file is machine-bound and needs to be activated for the CI runner
- Solution: Use UNITY_EMAIL + UNITY_PASSWORD (GameCI will extract serial from .ulf)

### Error: "No valid Unity Editor license found"
- Missing or incorrect secrets
- Check that all 3 secrets are set correctly

### Error: "0 entitlements"
- Unity account doesn't have proper license
- Re-activate license in Unity Hub with your account

## Workflow Configuration

The workflow uses `game-ci/unity-builder@v4` which:
- Downloads and activates Unity in a Docker container
- Extracts the serial from the .ulf file automatically
- Activates Unity using: `unity-editor -serial <serial> -username <email> -password <password>`

## Current Workflow File

Location: `.github/workflows/webgl-deploy.yml`

```yaml
- name: Setup Unity
  uses: game-ci/unity-builder@v4
  env:
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
    UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
    UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  with:
    targetPlatform: WebGL
    unityVersion: 2022.3.21f1
    buildMethod: PuzzleRivalsWebGLDeployBuild.Build
    buildName: PuzzleRivals
    cacheKey: ${{ runner.os }}-${{ github.sha }}-${{ github.ref }}
```

## Next Action Required

**Please provide:**
1. Unity account email
2. Unity account password

Or activate Unity locally and set the 3 secrets yourself, then let me know when ready to test.