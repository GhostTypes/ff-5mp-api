# API Changes & Documentation
- Previously, all communication was done over TCP, with no authentication process. All requests now require the printer's serial number, and a "check code".
- The port for communication has been changed back to 8898 (as of firmware 3.1.3)
- While all newer versions of Orca-FlashForge use the new API, the TCP API is still fully functional (as of firmware 3.1.3)

## Generic response structure
Response
```
{'code': 0, 'message': 'Success'}
```

## Response Code Translation
```
FlashNetwork.h L303-310
#define FNET_OK 0
#define FNET_ERROR -1
#define FNET_ABORTED_BY_CALLBACK 1
#define FNET_DIVICE_IS_BUSY 2
#define FNET_VERIFY_LAN_DEV_FAILED 1001 // invalid serialNumber/checkCode
#define FNET_UNAUTHORIZED 2001          // invalid accessToken/clientAccessToken
#define FNET_INVALID_VALIDATION 2002    // invalid userName/password/SMSCode
#define FNET_DEVICE_HAS_BEEN_BOUND 2003
```


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

## /product
- Lists supported features (circulation control, light control, etc)
```
{
    "code": 0,
    "message": "Success",
    "product": {
        "chamberTempCtrlState": 0,
        "externalFanCtrlState": 1,
        "internalFanCtrlState": 1,
        "lightCtrlState": 1,
        "nozzleTempCtrlState": 1,
        "platformTempCtrlState": 1
    }
}
```


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

Clearing cancelled/completed job state (FW 3.13.3+)
```
"args": {
    "action": "setClearPlatform"
    },
"cmd": "stateCtrl_cmd"
```

Temperature Control
- Example needed, unable to log in wireshark or find reference in FlashForge source code.
```
platformTemp,
rightTemp,
leftTemp,
chamberTemp
```

Other known commands
```
streamCtrl_cmd - printer responds but doesn't do anything
clearFan_cmd

Assuming these are for FlashCloud
deviceUnregister_cmd
userUnregister_cmd
device_cmd
```

## /uploadGcode
- Uploading a .gcode/3mf file (FW 3.13.3+), needs an updated example.
```
POST /uploadGcode HTTP/1.1
Host: 192.168.0.204:8898
Accept: */*
serialNumber:omitted
checkCode:omitted
fileSize:912373
printNow:true
levelingBeforePrint:true
flowCalibration:false
useMatlStation:false
gcodeToolCnt:0
materialMappings:W10=
Content-Length: 912594
Content-Type: multipart/form-data; boundary=------------------------v3GcLTGebpPzgLGBOgAQKJ
```

## /printGcode
- Starting a print from the printer's storage (FW 3.13.3+)
```
POST /printGcode HTTP/1.1
Host: 192.168.0.204:8898
Accept: */*
Content-Type:application/json
Content-Length: 195

{
    "checkCode": "omitted",
    "fileName": "3DBenchy.3mf",
    "flowCalibration": false,
    "gcodeToolCnt": 0,
    "levelingBeforePrint": false,
    "materialMappings": [
    ],
    "serialNumber": "omitted",
    "useMatlStation": false
}
HTTP/1.1 200 OK
Connection: close
Content-Length: 31
Content-Type: appliation/json

{"code":0,"message":"Success"}
```

## /gcodeThumb
- Request a thumbnail for a local file, returns base64 encoded
```
Example needed

checkCode,
serialNumber,
fileName
```

## /getThum
- Get the thumbnail for the current job
```
No json response, just a file
```

## /gcodeList
- Returns 10 most recent local files
```
Example needed
```


