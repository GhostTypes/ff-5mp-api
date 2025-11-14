<div align="center">
  <h1>FlashForge 5M (Pro) API</h1>
</div>

<div align="center">
  <p>
    ⚠️ <strong>Important Update:</strong> A cross-platform TypeScript rewrite is now available 
    <a href="https://github.com/GhostTypes/ff-5mp-api-ts">here</a>, offering more features, bug fixes, and support for additional printers. 
    This repository will receive minimal updates.
  </p>
</div>

<br>

<div align="center">
  <p>
    This API enables robust local network control for <strong>FlashForge 5M & 5M Pro</strong> printers.  
    It was built by reverse-engineering the LAN communication used by Orca-FlashForge, restoring functionality 
    that previous APIs lost after firmware updates (v2.7.X – v3.1.5).  
    By communicating directly over LAN, it avoids FlashCloud entirely — resulting in significantly lower latency.
    <br><br>
    💡 <em>Some API-exposed features, such as door-sensor data, may not yet be implemented in the printer hardware.</em>
  </p>
</div>


<div align="center">
  <h2>Features</h2>
</div>

<div align="center">
<table>
  <tr>
    <th>Feature</th>
    <th>Description</th>
  </tr>
  <tr>
    <td>Auto Discovery</td>
    <td>Automatically discover printers on your Local Network (LAN mode required)</td>
  </tr>
  <tr>
    <td>File Upload</td>
    <td>Upload GCode/3MF, auto-level option, print after upload</td>
  </tr>
  <tr>
    <td>Job Control</td>
    <td>Pause, resume, or stop current print jobs</td>
  </tr>
  <tr>
    <td>Temperature Control</td>
    <td>Adjust print bed & nozzle temperatures</td>
  </tr>
  <tr>
    <td>Fan Control</td>
    <td>Manage fans and filtration</td>
  </tr>
  <tr>
    <td>Z-Axis Offset Adjustment</td>
    <td>Adjust Z-axis offset</td>
  </tr>
  <tr>
    <td>Speed Override</td>
    <td>Modify print speed percentage</td>
  </tr>
  <tr>
    <td>LED Control</td>
    <td>Toggle printer LEDs on or off</td>
  </tr>
  <tr>
    <td>Job Retrieval</td>
    <td>Access recent jobs (last 10) and full local file list</td>
  </tr>
  <tr>
    <td>Thumbnail Retrieval</td>
    <td>Retrieve thumbnail previews for local files</td>
  </tr>
  <tr>
    <td>Detailed Printer Information</td>
    <td>Read S/N, lifetime filament usage, runtime, etc.</td>
  </tr>
  <tr>
    <td>Current Job Details</td>
    <td>Track filename, layer, filament usage (mm/g), time remaining</td>
  </tr>
  <tr>
    <td>Raw Command Execution</td>
    <td>Send G-code/M-code commands directly to the printer</td>
  </tr>
</table>
</div>

<br>

<div align="center">
  <a href="https://star-history.com/#GhostTypes/ff-5mp-api&Date">
    <picture>
      <source media="(prefers-color-scheme: dark)"
        srcset="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date&theme=dark" />
      <source media="(prefers-color-scheme: light)"
        srcset="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date" />
      <img alt="Star History Chart"
        src="https://api.star-history.com/svg?repos=GhostTypes/ff-5mp-api&type=Date" />
    </picture>
  </a>
</div>
