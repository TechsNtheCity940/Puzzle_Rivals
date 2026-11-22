#!/bin/bash
# Unity Personal License Setup Guide for GitHub Actions
# Run this to prepare your Unity license for CI/CD

# Instructions:
# 1. Activate Unity on your local machine using Unity Hub
# 2. Run this script after activating

echo "=== Unity Personal License Setup ==="
echo ""

# Common locations for the .ulf file on Windows
echo "Step 1: Locate your Unity license file"
echo "Look for Unity_lic.ulf in one of these locations:"
echo "  Windows: C:\\ProgramData\\Unity\\Unity_lic.ulf"
echo "  Linux: ~/.local/share/unity3d/Unity/Unity_lic.ulf"
echo "  macOS: /Library/Application Support/Unity/Unity_lic.ulf"
echo ""

read -p "Enter the full path to your Unity_lic.ulf file: " LICENSE_FILE

if [ ! -f "$LICENSE_FILE" ]; then
    echo "Error: File not found: $LICENSE_FILE"
    exit 1
fi

echo ""
echo "Step 2: Read the .ulf file content"
ULF_CONTENT=$(cat "$LICENSE_FILE")

echo ""
echo "Step 3: Set up GitHub secrets"
echo "Now you need to add these 3 secrets to your GitHub repo:"
echo "  Repository → Settings → Secrets and variables → Actions → New repository secret"
echo ""
echo "Secret 1: UNITY_LICENSE"
echo "  Value: (the entire content of your .ulf file - it's XML)"
echo ""
echo "Secret 2: UNITY_EMAIL"
echo "  Value: (your Unity account email address)"
echo ""
echo "Secret 3: UNITY_PASSWORD"
echo "  Value: (your Unity account password)"
echo ""

echo "=========================================="
echo "ULF FILE CONTENT (copy everything above):"
echo "=========================================="
echo "$ULF_CONTENT"
echo "=========================================="
echo ""

# Optional: Extract serial number (starts with F for Personal)
echo "Your Unity Serial (starts with F for Personal):"
echo "$ULF_CONTENT" | grep -oP '(?<=<DeveloperData Value=")[A-Za-z0-9-]+(?=")' | head -1 || echo "Serial not found in ULF file"

echo ""
echo "Done! After setting the 3 secrets, the build should work."