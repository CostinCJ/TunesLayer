# Discord Developer Terms of Service - Compliance Review

**Reviewed**: January 24, 2026  
**TunesLayer Version**: 1.0  
**Discord Developer ToS**: https://discord.com/developers/docs/policies-and-agreements/developer-terms-of-service

---

## Summary

TunesLayer's Discord Rich Presence integration has been reviewed for compliance with Discord's Developer Terms of Service.

## How TunesLayer Uses Discord

TunesLayer uses **Discord Rich Presence (RPC)** to display currently playing music information in a user's Discord status.

### What We Send to Discord:
- Track title
- Artist name
- Source application (Spotify, Apple Music, etc.)
- Timestamps (when track started playing)

### What We DON'T Do:
- ❌ No user authentication or OAuth
- ❌ No access to Discord user data
- ❌ No message sending or bot functionality
- ❌ No data collection from Discord
- ❌ No Discord API calls (only RPC protocol)

---

## Compliance Checklist

### ✅ User Privacy
- **Requirement**: Respect user privacy and data
- **Compliance**: TunesLayer only sends music metadata that the user is already listening to. No personal data is collected or transmitted.

### ✅ Optional Feature
- **Requirement**: Don't force users to connect Discord
- **Compliance**: Discord integration is **opt-in** and can be disabled in Settings at any time.

### ✅ No Spam or Abuse
- **Requirement**: Don't spam or abuse Discord services
- **Compliance**: Rich Presence updates only when tracks change (typically every 3-5 minutes). No excessive API calls.

### ✅ Accurate Representation
- **Requirement**: Don't misrepresent your application
- **Compliance**: TunesLayer accurately displays as "TunesLayer" in Discord. We don't impersonate other applications.

### ✅ No Monetization Restrictions
- **Requirement**: Discord RPC has no monetization restrictions
- **Compliance**: TunesLayer's use of Discord RPC for Pro tier features is permitted.

### ✅ User Control
- **Requirement**: Users must be able to control what's shared
- **Compliance**: Users can enable/disable Discord integration in Settings. When disabled, no data is sent to Discord.

### ✅ Application Registration
- **Requirement**: Must register a Discord Application
- **Compliance**: TunesLayer requires users to create their own Discord Application ID (or we provide a default). This ensures transparency.

---

## Discord Application Setup

TunesLayer requires a Discord Application ID to function. Users have two options:

1. **Use Default ID** (provided by TunesLayer)
   - App ID: `1464593089812238368`
   - Registered under TunesLayer's Discord Developer account
   - Displays as "TunesLayer" in Discord

2. **Bring Your Own ID** (advanced users)
   - Users can register their own Discord Application
   - Provides full control over branding and assets

---

## Assets Uploaded to Discord

The following assets are uploaded to the Discord Application for Rich Presence:

- `tuneslayer_logo` - TunesLayer logo (large image)
- `spotify_icon` - Spotify icon (small image)
- `apple_music_icon` - Apple Music icon (small image)
- `youtube_music_icon` - YouTube Music icon (small image)
- `music_icon` - Generic music icon (small image)

**Note**: These icons are used for display purposes only and do not imply endorsement by the respective music services.

---

## Rate Limiting

Discord RPC has built-in rate limiting. TunesLayer respects these limits by:
- Only updating presence when tracks change
- Not sending updates more than once per second
- Using the official `DiscordRichPresence` library which handles rate limiting

---

## Data Retention

TunesLayer does not store any Discord-related data. Rich Presence information is sent directly to Discord and not retained locally.

---

## Conclusion

**TunesLayer's Discord integration is compliant** with Discord's Developer Terms of Service:

- ✅ Uses only Rich Presence (RPC) protocol
- ✅ No authentication or user data access
- ✅ Opt-in feature with user control
- ✅ No spam or abuse
- ✅ Accurate representation
- ✅ Respects rate limits

---

## References

- Discord Developer Portal: https://discord.com/developers/applications
- Discord RPC Documentation: https://discord.com/developers/docs/rich-presence/how-to
- DiscordRichPresence Library: https://github.com/Lachee/discord-rpc-csharp

---

**Reviewed by**: TunesLayer Development Team  
**Next Review**: Before major version updates
