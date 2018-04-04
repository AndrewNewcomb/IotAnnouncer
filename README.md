# IotAnnouncer
Playing with a raspberry pi. A project that announces someone's entry into the room by playing their signature tune.

## Phase 1
First attempt is all the code in one script
- Detects movement with an ultrasonic sensor.
- When in range takes a photo with the web cam.
- Uploads the photo to Microsoft Azure Cognitive Services Face Api and tries to identify the person in the photo against some known faces.
- If a match is found it plays a tune for that person.

## Phase 2 - WIP
Host the main logic in the cloud.
- using Azure Functions. First step is working in that it identifies people. I haven't done any work on changing the pi code to use the new url, or on the best way for the pi (or other device) to subscribe to the result.
