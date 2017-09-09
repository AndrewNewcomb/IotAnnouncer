import json
import urllib
import requests
import random
import RPi.GPIO as GPIO
import time
import inspect
import sys

from pprint import pprint
from os.path import expanduser, isdir, isfile, join, dirname
from os import getenv, remove, listdir
from SimpleCV import Image, Camera # is python 2
from pygame import mixer

"""
An ultrasonic sensor detects someone moving into range
The web cam takes a photo
Cognitive Services Face API is used to identify the person
A tune specific to that person is played

Command line parameters:
    Optional path to the folder containing the music files.
    If not set defaults to a child folder called 'Tunes'.

Expects a config file called ".env" in the json format
 {
   "cognitive_services": {
     "subscription_key":"???your subscription key to cogitive services???",
     "region":"???the azure region, for example westeurope???",
     "person_group_id":"???identifies the face api person group???"
   }
 }

"""

#-----------------------------------
# face detection settings
min_confidence = 0.6

#-----------------------------------
# ultrasonic sensor settings for HC-SR04
trigger_pin = 18
echo_pin = 23

cm_boundary_arm = 250 
cm_boundary_fire = 175
cm_boundary_too_close = 70

cm_boundary_arm = 250
cm_boundary_fire = 30
cm_boundary_too_close = 10

#-----------------------------------
def get_path(extend_path_with):
    return join(dirname(inspect.stack()[0][1]), extend_path_with)

def get_config():
    with open(get_path('.env')) as json_file:
        return json.load(json_file)

config = get_config()

#-----------------------------------
# ultrasonic sensor trigger

def init_GPIO():
    #GPIO.cleanup()
    GPIO.setmode(GPIO.BCM)
    GPIO.setup(trigger_pin, GPIO.OUT)
    GPIO.setup(echo_pin, GPIO.IN)

def get_distance():
    def send_trigger_pulse():
        GPIO.output(trigger_pin, True)
        time.sleep(0.0001)
        GPIO.output(trigger_pin, False)

    def wait_for_echo(value, timeout):
        count = timeout
        while GPIO.input(echo_pin) != value and count > 0:
            count = count - 1
        
    send_trigger_pulse()
    wait_for_echo(True, 10000)
    start = time.time()
    wait_for_echo(False, 10000)
    finish = time.time()
    pulse_len = finish - start
    distance_cm = pulse_len / 0.000058
    distance_in = distance_cm / 2.54
    return (distance_cm, distance_in)

#-----------------------------------
# face and identity detection

face_uri_base = 'https://' + config['cognitive_services']['region'] + '.api.cognitive.microsoft.com'

def get_group_members():
    headers = {
        'Ocp-Apim-Subscription-Key': config['cognitive_services']['subscription_key'],
    }

    response = requests.request('GET', face_uri_base + '/face/v1.0/persongroups/' + config['cognitive_services']['person_group_id'] +'/persons', json=None, data=None, headers=headers, params=None)

    if response.status_code != 200:
        raise ValueError(
            'Request to Azure returned an error %s, the response is:\n%s'
            % (response.status_code, response.text)
        )

    responsejson = response.json()
    
    person_lookup = {}

    for person in responsejson:
        person_name = person['name']
        person_id = person['personId']
        person_lookup[person_id] = person_name

    pprint(person_lookup)
    return person_lookup


def detect_faces(img_file):

    headers = {
        'Content-Type': 'application/octet-stream',
        'Ocp-Apim-Subscription-Key': config['cognitive_services']['subscription_key'],
    }

    params = {
        'returnFaceId': 'true',
        'returnFaceLandmarks': 'false',
        'returnFaceAttributes': 'age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise',
    }

    img = open(expanduser(img_file), 'rb')
    response = requests.request('POST', face_uri_base + '/face/v1.0/detect', json=None, data=img, headers=headers, params=params)


    if response.status_code != 200:
        raise ValueError(
            'Request to Azure returned an error %s, the response is:\n%s'
            % (response.status_code, response.text)
        )

    responsejson = response.json()    
    return responsejson


def identify_face(face_id, max_faces=1):

    headers = {
        'Content-Type': 'application/json',
        'Ocp-Apim-Subscription-Key': config['cognitive_services']['subscription_key'],
    }

    body = {
        'faceIds': [face_id],
        'personGroupId': config['cognitive_services']['person_group_id'],
        'maxNumOfCandidatesReturned ': max_faces,
        'confidenceThreshold':0.5
    }

    response = requests.request('POST', face_uri_base + '/face/v1.0/identify', json=body, data=None, headers=headers, params=None)

    if response.status_code != 200:
        raise ValueError(
            'Request to Azure returned an error %s, the response is:\n%s'
            % (response.status_code, response.text)
        )

    responsejson = response.json()
    return responsejson


def identify(img_file):
    # First detect faces getting FaceIds
    faces_result = detect_faces(img_file)

    if len(faces_result) < 1:
        print('No faces found')
        return {"status":"fail", "img":img_file}

    # Identify person from the first FaceId
    firstface=faces_result[0]
    face_id = firstface["faceId"]

    candidates_result = identify_face(face_id)
    
    if len(candidates_result) < 1:
        print('No candidates found')
        return {"status":"fail", "img":img_file}

    candidates = candidates_result[0]["candidates"]

    if len(candidates) < 1:
        print('No candidates found')
        return {"status":"fail", "img":img_file}

    candidate = candidates[0]

    return {"status":"ok", "img":img_file, "candidate":candidate}

def capture_image(camera):
    img = camera.getImage()
    return img

#-----------------------------------

def get_state(old_state, cm):
    if old_state == "NotSet":
        if cm > cm_boundary_arm:
            new_state = "NotSet"
        elif cm > cm_boundary_fire:
            new_state = "Arm"
        else:
            new_state = "NotSet"
    
    if old_state == "Arm":
        if cm > cm_boundary_arm:
            new_state = "NotSet"
        elif cm > cm_boundary_fire:
            new_state = "Arm"
        elif cm > cm_boundary_too_close:
            new_state = "Fire"
        else:
            new_state = "NotSet"
            
    if old_state == "Fire":
        new_state = "NotSet"

    print("cm=%f\t state was %s\t new state %s" % (cm, old_state, new_state))
    return new_state


def announce(person_name):
    music_root_path = sys.argv[1] if len(sys.argv) > 1 else get_path('Tunes')
    music_directory = join(music_root_path, person_name)

    if not isdir(music_directory):        
        print("Tunes directory not found " + music_directory)
        return

    music_files = [f for f in listdir(music_directory) if isfile(join(music_directory, f))]

    if len(music_files) == 0:
        print("No tunes found in " + music_directory)
        return

    music_file = join(music_directory, random.choice(music_files))

    mixer.init()
    mixer.music.load(music_file)
    mixer.music.play()
    #mixer.music.stop()
    return

def announce_candidate(candidate):
    person_name = person_lookup[candidate["personId"]]
    confidence = candidate["confidence"]

    if confidence > min_confidence:
        print("Detected %s. Confidence is %f" % (person_name, confidence))
        announce(person_name)
    else:
        print("Might be %s. Confidence was only %f" % (person_name, confidence))

    return


def trigger(camera, person_lookup):
    path='/tmp'

    if not isdir(expanduser(path)):
        raise ValueError('Directory to store photos not found %s' % (path))        
        
    img = capture_image(camera)
    full_file_name = img.save(path=path, temp=True, verbose=True)
    
    identify_result = identify(full_file_name)

    if isfile(full_file_name):
        remove(full_file_name)

    if identify_result["status"] == "ok":
        announce_candidate(identify_result["candidate"])

    return    


def main_loop(old_state):
    cm, inches = get_distance()
    new_state = get_state(old_state, cm)
    if new_state == "Fire":
        trigger(camera, person_lookup)
        time.sleep(2)
    elif new_state == "Arm":
        time.sleep(0.8)
    else:
        time.sleep(1)

    return new_state

#-----------------------------------
try:
    camera = Camera()
    init_GPIO()

    person_lookup = get_group_members()
    
    state = "NotSet"
    while True:
        state = main_loop(state)

finally:
    print("cleaning up")
    GPIO.cleanup()
    del camera

#-----------------------------------
