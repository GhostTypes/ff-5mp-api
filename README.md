# FlashForge 5M (Pro) API

UI Control software utilizing this API is being developed [here](https://github.com/Parallel-7/FlashForgeUI)

## Important
This API is being ported to TypeScript [here](https://github.com/GhostTypes/ff-5mp-api-ts) and will recieve minimal updates going forward.


This being done to enable cross-platform usage through Node.JS, and will be used in an Electron rewrite of FlashForgeUI


## Overview

With the release of Orca-FlashForge and new firmware updates, previous APIs for the FlashForge 5M series no longer function properly. This API is developed by reverse-engineering the communication between Orca-FlashForge and the printer in LAN-only mode. Using WAN mode routes through FlashCloud, introducing unnecessary latency.

## Compatibility

While many features may work on older FlashForge printers with minimal adjustments, this API specifically targets the **FlashForge 5M and 5M Pro** models.

*Supports firmware: 2.7.X - 3.1.5*


*Note: Some features listed may not yet be implemented in the printer hardware, such as the door sensor functionality.*

## Features

### Replicates Orca-FlashForge & FlashPrint 5 Capabilities

- **Auto-Discovery of Printers on Local Network**
  - Requires LAN mode enabled
- **File Upload**
  - Upload GCode/3MF files
  - Option to print immediately after upload
  - Auto-level setting
- **Job Control**
  - Pause, resume, or stop current print jobs
- **Fan Control**
  - Manage chamber and cooling fans (internal/external filtration)
- **Z-Axis Offset Adjustment**
- **Speed Override**
- **LED Control**
  - Turn printer LEDs on or off
- **Job Retrieval**
  - Access recent jobs (last 10)
  - View full local file list
- **Thumbnail Retrieval**
  - Get thumbnails for local files
- **Printer Information**
  - Retrieve serial number, lifetime filament usage, runtime, etc.
- **Current Job Details**
  - File name, current layer, filament usage (in mm and grams), estimated time remaining, etc.

### Adds New Controls Not Available in FlashForge Software

- **Temperature Control**
  - Adjust print bed and nozzle temperatures
- **Axis Homing**
- **Filament Management**
  - Load and unload filament *(Work in Progress)*
- **Filament Runout Sensor**
  - Enable or disable the sensor
- **Raw Command Execution**
  - Send raw G-code and M-code commands directly to the printer

## To-Do

- Implement robust error handling

  <a href="https://star-history.com/#GhostTypes/ff-5mp-api&Date">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date&theme=dark" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date" />
 </picture>
</a>
