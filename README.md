# LNLDirectFix

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader/) mod for [Resonite](https://resonite.com/) headless servers running on Linux. This mod fixes LNL's local IP announcement logic.
## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader/).
1. Place [LNLDirectFix.dll](https://github.com/sjsanjsrh/LNLDirectFix/releases/latest/download/LNLDirectFix.dll) into your `rml_mods` folder. For a default Linux Steam install, this folder should be at `~/.steam/steam/steamapps/common/Resonite/rml_mods`. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. Set `announceLocalIPs` to `true` in the headless `Config.json` file as described below.
1. Start the headless server. If you want to verify that the mod is working, you can check your Resonite logs.

## Config.json

In the headless server's `Config.json`, set `announceLocalIPs` to `true`:

```json
{
  "announceLocalIPs": true
}
```

If `Config.json` already contains other settings, add the property with a comma after the preceding property.

## Description

LNLDirectFix fixes the Linux local IP announcement issue described in [Resonite-Issues/#2651](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/2651). It ensures that the correct port is advertised for local IPs, allowing clients to connect successfully.

Bug: On Linux, LNL creates two listeners:

1. `IPAddress.Any:PORT` binds successfully to `PORT` (for example, `41863`).
2. `192.168.x.x:PORT` cannot reuse the same port and receives a random port (for example, `45375`).

The second listener can publish an `lnl://192.168.x.x:45375` URL even though no service is listening on that port.

When `announceLocalIPs` is enabled, `World.cs` iterates `LocalUris` and registers them in `sessionURLs`. The per-IP listener's random port can then end up in `sessionURLs`, causing clients to connect to a port nobody is listening on.

Fix (`LocalUris` patch):
  - Any listener (`BoundToSpecificIP=false`): emit `lnl://` URLs for all local IPs using the actual bound port.
  - Per-IP listener (`BoundToSpecificIP=true`): emit nothing and suppress the broken random-port URL.
