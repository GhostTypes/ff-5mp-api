# FlashForge 5M (Pro) API

>  ⚠️ Important Update: A cross-platform TypeScript rewrite is now available [here](https://github.com/GhostTypes/ff-5mp-api-ts), with bug fixes and more features, and support for more printers. This will recive minimal updates.


## 🔍 Overview

- This API provides robust local network control for **FlashForge 5M & 5M Pro printers** 🤖.
- It was developed by reverse-engineering the LAN-only communication between Orca-FlashForge and the printer, addressing issues where previous APIs became non-functional after recent firmware (v2.7.X - v3.1.5) and Orca-FlashForge updates.
- This direct LAN approach bypasses FlashCloud, significantly reducing latency.

> 💡 *Note: Some API-exposed features (e.g., door sensor functionality) may not yet be implemented in the printer hardware itself.*

## ✨ Features ✨

| Feature                                  | Description                                               |
| :--------------------------------------- | :-------------------------------------------------------- |
| 📡 **Auto Discovery**                     | Automatically discover printers on your Local Network (LAN mode required)             |
| 📄 **File Upload**                      | GCode/3MF, print after upload, auto-level option        |
| ⏯️ **Job Control**                      | Pause, resume, or stop current print jobs               |
| 🌡️ **Temperature Control**              | Adjust print bed & nozzle temperatures    |           |
| 🌬️ **Fan Control**                        | Manage fans and filtration       |
| ↕️ **Z-Axis Offset Adjustment**           | Adjust Z-axis offset                                      |
| ⏩ **Speed Override**                     | Modify print speed percentage                             |
| 💡 **LED Control**                        | Turn printer LEDs on or off                               |
| 📂 **Job Retrieval**                    | Access recent jobs (last 10), full local file list      |
| 🖼️ **Thumbnail Retrieval**                | Get thumbnails for local files                            |
| ℹ️ **Detailed Printer Information**                | S/N, lifetime filament usage, runtime, etc.             |
| 📊 **Current Job Details**              | File name, layer, filament usage (mm/g), time remaining |
| ⚙️ **Raw Command Execution**            | Send G-code/M-code commands directly to printer  |

  <a href="https://star-history.com/#GhostTypes/ff-5mp-api&Date">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date&theme=dark" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date" />
 </picture>
</a>
