# Privacy Policy for TunesLayer

**Last Updated: January 24, 2026**

## Overview

TunesLayer ("we", "our", or "the app") is committed to protecting your privacy. This Privacy Policy explains how TunesLayer handles your data.

## Data Collection

### What We Collect

TunesLayer collects the following data **locally on your device only**:

1. **Listening Analytics**
   - Track titles, artists, and album names (from Windows Media Session)
   - Playback timestamps
   - Active application/game window titles
   - Session duration

2. **Application Settings**
   - Overlay position, size, and opacity preferences
   - Hotkey configurations
   - Theme selections
   - Integration preferences (Discord, OBS)

### What We DO NOT Collect

- ❌ No personal identification information
- ❌ No login credentials or authentication tokens
- ❌ No Spotify, Apple Music, or YouTube account data
- ❌ No payment information
- ❌ No browsing history
- ❌ No location data

## Data Storage

All data collected by TunesLayer is stored **locally** on your device in:
- `%AppData%\TunesLayer\settings.json` - Application settings
- `%AppData%\TunesLayer\analytics.db` - Listening analytics (SQLite database)

**No data is transmitted to external servers.** TunesLayer does not have analytics servers, telemetry endpoints, or cloud storage.

## Data Usage

The data collected is used exclusively for:
- Displaying your listening statistics in the Analytics dashboard
- Maintaining your application preferences
- Providing Discord Rich Presence (if enabled)
- Serving the OBS Now Playing widget (if enabled)

## Third-Party Services

### Windows Media Session (SMTC)

TunesLayer reads media information from the Windows System Media Transport Controls API. This is a standard Windows API that provides:
- Currently playing track information
- Playback state
- Album artwork thumbnails

This data is provided by Windows and originates from your music applications (Spotify, Apple Music, etc.). TunesLayer does not communicate with these applications directly.

### Discord Rich Presence (Optional)

If you enable Discord integration:
- TunesLayer sends currently playing track information to Discord via the Discord RPC protocol
- This requires a Discord application registered under your Discord Developer account
- You can disable this feature at any time in Settings

### OBS Widget (Optional)

If you enable the OBS widget:
- TunesLayer runs a local HTTP server on `localhost` (default port 5123)
- This server is only accessible from your computer
- No external network access is created

## Data Retention

- **Analytics data**: Retained for the number of days specified in Settings (default: 30 days)
- **Settings**: Retained until you uninstall the application
- You can export or delete your analytics data at any time from the Analytics tab

## Data Sharing

TunesLayer **does not share, sell, or transmit** your data to any third parties. All data remains on your local device.

## Your Rights

You have the right to:
- **Access** your data at any time (stored in `%AppData%\TunesLayer`)
- **Export** your analytics data (JSON format available in Analytics tab)
- **Delete** your data (uninstall the application or manually delete the TunesLayer folder)

## Children's Privacy

TunesLayer does not knowingly collect data from children under 13. The application does not require age verification as no data is transmitted externally.

## Changes to This Policy

We may update this Privacy Policy from time to time. Changes will be reflected in the "Last Updated" date above. Continued use of TunesLayer after changes constitutes acceptance of the updated policy.

## Contact

For privacy-related questions or concerns:
- GitHub Issues: https://github.com/yourusername/TunesLayer/issues
- Email: privacy@tuneslayer.com (if applicable)

## Legal Basis

TunesLayer operates as a local Windows utility application. Since no data is transmitted externally, GDPR and similar data protection regulations regarding data transmission do not apply. However, we respect your privacy and provide transparency about local data storage.

---

**Summary**: TunesLayer is a privacy-first application. All your data stays on your computer. We don't track you, we don't sell your data, and we don't send anything to external servers.
