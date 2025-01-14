# TCP API Documentation
## Supports FW 3.1.3 and *all* prior versions.
*All other (older) wifi-enabled FlashForge printers use this API*

## Login/Logout
- Must send when starting/stopping control over the TCP API
```
~M601 S1
CMD M601 Received.
Control Success V2.1.
ok

~M602
CMD M602 Received.
Control Release.
ok
```

## Endstop Info
```
~M115
CMD M115 Received.
Machine Type: Flashforge Adventurer 5M Pro
Machine Name: Adventurer 5M Pro
Firmware: v3.1.3
SN: omitted
X: 220 Y: 220 Z: 220
Tool Count: 1
Mac Address:88:A9:A7:97:B2:BF 
ok
```

## Location Info
```
~M114
CMD M114 Received.
X:-8.262 Y:-33.980 Z:28.772 A:18631.348 B:0
ok
```

## Endstop Info (Current Job Info)
```
~M119
CMD M119 Received.
Endstop: X-max: 110 Y-max: 110 Z-min: 0
MachineStatus: BUILDING_FROM_SD
MoveMode: MOVING
Status: S:1 L:0 J:0 F:0
LED: 1
CurrentFile: 3x2 Rugged Drawer Outer x2.3mf
ok
```


## Current Job Progress
```
~M27
CMD M27 Received.
SD printing byte 18/100
Layer: 117/523
ok
```


## Current Temp Info
```
~M105
CMD M105 Received.
T0:220.3/220.0 T1:0.0/0.0 B:50.0/50.0
ok
```

## LED Control
```
~M146 r255 g255 b255 F0 (Led ON)
~M146 r0 g0 b0 F0 (Led OFF)
```

## Job Control
```
~M26 (Stop current job)
~M23 0:/user/%%filename%%\r (Start printing %%filename%%)
```

## File Control
```
~M28 %%size%% 0:/user/%%filename%%\r (Start file transfer)
~M29\r (Save file)
```

## Unsure/Untested
```
~M240 (Take picture with webcam, save to local storage?)
~M405 (Filament Runout Sensor OFF)
~M406 (Filament Runout Sensor ON)
```

