# (WIP) FlashForge 5m Pro API
With the release of Orca-FlashForge, and new firmware, previous API's for the 5m no longer work.<br>
This API is created from reverse engineering the communication between Orca-FlashForge and the printer in lan-only mode.<br>
WAN mode goes through FlashCloud first, resulting in latency

# API Changes
- Previously, all communication was done over TCP, with no authentication process. All requests now require the printer's serial number, and a "check code".
- Additionally, the port for communication has been changed to 8898, from 8899

## Generic request structure
- The check code can be obtained from the printer's UI, in network mode settings. <br>
```
printer_ip:8898/endpoint
payload = {
    "serialNumber": "SERIAL_NUMBER",
    "checkCode": "LAN_MODE_CODE"
}
```

## Generic extended request structure
```
{
    "serialNumber": "SERIAL_NUMBER",
    "checkCode": "LAN_MODE_CODE",
    "payload": {} 
}
```

# Known endpoints


## /detail
- Follows generic request structure<br>

Response
```
{
    "code": 0,
    "detail": {
        "autoShutdown": "open",
        "autoShutdownTime": 30,
        "cameraStreamUrl": "http://192.168.0.205:8080/?action=stream",
        "chamberFanSpeed": 100,
        "chamberTargetTemp": 0,
        "chamberTemp": 0,
        "coolingFanSpeed": 100,
        "cumulativeFilament": 1582.89501953125,
        "cumulativePrintTime": 8810,
        "currentPrintSpeed": 500,
        "doorStatus": "close",
        "errorCode": "",
        "estimatedLeftLen": 0,
        "estimatedLeftWeight": 0.0,
        "estimatedRightLen": 3137.3896484375,
        "estimatedRightWeight": 28.1100006103516,
        "estimatedTime": 3664.89832305908,
        "externalFanStatus": "open",
        "fillAmount": 35,
        "firmwareVersion": "2.7.2",
        "flashRegisterCode": "REDACTED",
        "internalFanStatus": "close",
        "ipAddr": "192.168.0.205",
        "leftFilamentType": "",
        "leftTargetTemp": 0,
        "leftTemp": 0,
        "lightStatus": "open",
        "location": "Group A",
        "macAddr": "REDACTED",
        "measure": "220X220X220",
        "name": "Adventurer 5M Pro",
        "nozzleCnt": 1,
        "nozzleModel": "0.4mm",
        "nozzleStyle": 0,
        "pid": 36,
        "platTargetTemp": 50.0,
        "platTemp": 49.9799995422363,
        "polarRegisterCode": "REDACTED",
        "printDuration": 1160,
        "printFileName": "Lego Flower Stem Straight.3mf",
        "printFileThumbUrl": "http://192.168.0.205:8898/getThum",
        "printLayer": 75,
        "printProgress": 0.256462097167969,
        "printSpeedAdjust": 100,
        "remainingDiskSpace": 5.29228973388672,
        "rightFilamentType": "PLA",
        "rightTargetTemp": 220.0,
        "rightTemp": 220.149993896484,
        "status": "printing",
        "targetPrintLayer": 552,
        "tvoc": 23,
        "zAxisCompensation": 0.0
    },
    "message": "Success"
}
```
## /control
- Follows generic extended request structure

Response
```
{'code': 0, 'message': 'Success'}
```

LED Control
```
        "cmd": "lightControl_cmd",
        "args": {
            "status": "open" (open = on, close = off)
        }
```

Z Axis Compensation, Speed, Chamber Fan Speed, and Cooling Fan Speed
```
        "cmd":"printerCtl_cmd",
        "args":{
            "zAxisCompensation": 0E0,
            "speed": 100,
            "chamberFan": 100,
            "coolingFan": 100
        }
```

Internal/External Filtration
- Status of these can be checked via /detail endpoint; internalFanStatus & externalFanStatus


```
        "cmd": "circulateCtl_cmd",
        "args": {
            "internal": "close", (open = on, close = off)
            "external": "close"
        }
```

## /uploadGcode
- More info needed

Example flow
```
POST /uploadGcode HTTP/1.1
Host: 192.168.0.205:8898
Accept: */*
serialNumber:REDACTED
checkCode:REDACTED
fileSize:4922515
printNow:true
levelingBeforePrint:true
Content-Length: 4922755
Content-Type: multipart/form-data; boundary=------------------------DHD3lr8XwXBuyC8G3dWjK7
Expect: 100-continue

HTTP/1.1 100 Continue

--------------------------DHD3lr8XwXBuyC8G3dWjK7
Content-Disposition: form-data; name="gcodeFile"; filename="Lego Rounded Flower Stem x3.3mf"
Content-Type: application/octet-stream
```
