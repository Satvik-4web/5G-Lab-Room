import speech_recognition as sr
import sys

r = sr.Recognizer()
try:
    with sr.AudioFile(sys.argv[1]) as source:
        audio = r.record(source)
    print(r.recognize_google(audio))
except Exception as e:
    print('')
