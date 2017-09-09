# IotAnnouncer
Playing with a raspberry pi. A project that announces someone's entry into the room by playing their signature tune.

## Phase 1
First attempt is all the code in one script
- Detects movement with an ultrasonic sensor.
- When in range takes a photo with the web cam.
- Uploads the photo to Microsoft Azure Cognitive Services Face Api and tries to identify the person in the photo against some known faces.
- If a match is found it plays a tune for that person.

## Future phases?
Want to separate the triggering so that it could be handled by a separate pi. If on the same network with Zero MQ. If not on the same network then a cloud based messaging service.
