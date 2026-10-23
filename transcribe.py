import speech_recognition as sr
import sys
import os

if len(sys.argv) < 2 or not os.path.exists(sys.argv[1]):
    print("")
    sys.exit(0)

audio_file = sys.argv[1]
r = sr.Recognizer()

try:
    with sr.AudioFile(audio_file) as source:
        audio = r.record(source)

    # 1. Try Indian English (en-IN)
    try:
        text = r.recognize_google(audio, language="en-IN")
        if text and len(text.strip()) > 0:
            print(text.strip())
            sys.exit(0)
    except Exception:
        pass

    # 2. Try Standard English (en-US)
    try:
        text = r.recognize_google(audio, language="en-US")
        if text and len(text.strip()) > 0:
            print(text.strip())
            sys.exit(0)
    except Exception:
        pass

    # 3. Try Hindi (hi-IN) if user asked in Hindi
    try:
        text = r.recognize_google(audio, language="hi-IN")
        if text and len(text.strip()) > 0:
            print(text.strip())
            sys.exit(0)
    except Exception:
        pass

    print("")
except Exception:
    print("")
