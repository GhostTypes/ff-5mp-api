# (WIP) FlashForge 5m Pro API
With the release of Orca-FlashForge, and new firmware, previous API's for the 5m no longer work.<br>
This API is created from reverse engineering the communication between Orca-FlashForge and the printer in lan-only mode.<br>
WAN mode goes through FlashCloud first, resulting in latency

# API Coverage
- Requesting all details Orca-FlashForge, FlashCloud, and PolarCloud access (print stats, machine info, etc)
- Setting z-offset and speed override
- Controlling chamber & cooling fan, as well as internal & external filtration
- Uploading GCode/3MF files (supports leveling before print, auto printing after upload)

# Todo
- Reverse engineering to see how sending direct G/MCode is done now (if it even is)
- Robust error handling
