#!/bin/bash

# APK Anti-Split Tool - Sample Usage Examples
# Make sure the tool is compiled and available in your PATH

echo ""
echo "======================================================"
echo "APK Anti-Split Tool - Sample Usage Examples"
echo "======================================================"
echo ""

# Example 1: Basic merge with default output
echo "Example 1: Basic merge with default output"
echo "Command: ./ApkAntiSplit MyGame.xapk"
echo ""
# ./ApkAntiSplit MyGame.xapk

# Example 2: Merge with custom output path
echo "Example 2: Merge with custom output path"
echo "Command: ./ApkAntiSplit MyGame.xapk --output ~/Games/MyGame_merged.apk"
echo ""
# ./ApkAntiSplit MyGame.xapk --output ~/Games/MyGame_merged.apk

# Example 3: Display APK information only
echo "Example 3: Display APK information only"
echo "Command: ./ApkAntiSplit MyGame.xapk --info"
echo ""
# ./ApkAntiSplit MyGame.xapk --info

# Example 4: Extract APKs without merging
echo "Example 4: Extract APKs without merging"
echo "Command: ./ApkAntiSplit MyGame.xapk --extract ~/ExtractedAPKs"
echo ""
# ./ApkAntiSplit MyGame.xapk --extract ~/ExtractedAPKs

# Example 5: Merge with custom keystore
echo "Example 5: Merge with custom keystore"
echo "Command: ./ApkAntiSplit MyGame.xapk --keystore release.jks --output signed_game.apk"
echo ""
# ./ApkAntiSplit MyGame.xapk --keystore release.jks --output signed_game.apk

# Example 6: Verbose output for debugging
echo "Example 6: Verbose output for debugging"
echo "Command: ./ApkAntiSplit MyGame.xapk --verbose"
echo ""
# ./ApkAntiSplit MyGame.xapk --verbose

# Example 7: Specify Android SDK path
echo "Example 7: Specify Android SDK path"
echo "Command: ./ApkAntiSplit MyGame.xapk --android-sdk ~/Library/Android/sdk"
echo ""
# ./ApkAntiSplit MyGame.xapk --android-sdk ~/Library/Android/sdk

# Example 8: Batch processing multiple files
echo "Example 8: Batch processing multiple files"
echo ""
echo "for file in *.xapk; do"
echo "    echo \"Processing \$file...\""
echo "    ./ApkAntiSplit \"\$file\" --output \"merged_\${file%.xapk}.apk\""
echo "done"
echo ""

# Example 9: Process with error handling
echo "Example 9: Process with error handling"
echo ""
echo "#!/bin/bash"
echo "XAPK_FILE=\"MyGame.xapk\""
echo "OUTPUT_FILE=\"MyGame_merged.apk\""
echo ""
echo "if [[ -f \"\$XAPK_FILE\" ]]; then"
echo "    echo \"Processing \$XAPK_FILE...\""
echo "    if ./ApkAntiSplit \"\$XAPK_FILE\" --output \"\$OUTPUT_FILE\" --verbose; then"
echo "        echo \"✅ Success! Merged APK created: \$OUTPUT_FILE\""
echo "        echo \"File size: \$(du -h \"\$OUTPUT_FILE\" | cut -f1)\""
echo "    else"
echo "        echo \"❌ Error: Failed to merge APK\""
echo "        exit 1"
echo "    fi"
echo "else"
echo "    echo \"❌ Error: XAPK file not found: \$XAPK_FILE\""
echo "    exit 1"
echo "fi"
echo ""

# Example 10: Advanced batch processing with logging
echo "Example 10: Advanced batch processing with logging"
echo ""
echo "LOG_FILE=\"merge_log_\$(date +%Y%m%d_%H%M%S).txt\""
echo "SUCCESS_COUNT=0"
echo "FAILURE_COUNT=0"
echo ""
echo "for xapk in *.xapk; do"
echo "    if [[ -f \"\$xapk\" ]]; then"
echo "        output=\"merged_\${xapk%.xapk}.apk\""
echo "        echo \"Processing \$xapk...\" | tee -a \"\$LOG_FILE\""
echo "        "
echo "        if ./ApkAntiSplit \"\$xapk\" --output \"\$output\" 2>&1 | tee -a \"\$LOG_FILE\"; then"
echo "            echo \"✅ Success: \$output\" | tee -a \"\$LOG_FILE\""
echo "            ((SUCCESS_COUNT++))"
echo "        else"
echo "            echo \"❌ Failed: \$xapk\" | tee -a \"\$LOG_FILE\""
echo "            ((FAILURE_COUNT++))"
echo "        fi"
echo "        echo \"\" | tee -a \"\$LOG_FILE\""
echo "    fi"
echo "done"
echo ""
echo "echo \"Summary: \$SUCCESS_COUNT successful, \$FAILURE_COUNT failed\" | tee -a \"\$LOG_FILE\""
echo ""

echo "======================================================"
echo "To run these examples:"
echo "1. Make sure ApkAntiSplit is compiled and executable"
echo "2. Set Android SDK environment variables"
echo "3. Have sample XAPK files available"
echo "4. Make this script executable: chmod +x sample_usage.sh"
echo "======================================================"
echo ""