# Meta Quest build

Open this project with Unity **6000.4.8f1**. In Unity Hub, add **Android Build Support**, including **Android SDK & NDK Tools** and **OpenJDK**, to that same editor installation. Sign in and activate the Unity editor license if prompted.

In the editor, stop Play mode, then use **5G Lab > Build Meta Quest APK**. This prepares Android OpenXR with Meta Quest Support and the Oculus Touch profile, switches to Android, and writes `Builds/5GLabRoom-Quest.apk`. The included scene is `ComputerLab`.

Enable developer mode on the headset and install the APK with `adb install -r Builds/5GLabRoom-Quest.apk`. Test the headset build in the lab, particularly the module viewpoints, controller inputs, microphone permission, and frame rate.

Controls: **A** advances the tour; **X** starts it; **right grip** starts/stops speech; **right trigger** inspects equipment under the headset gaze outside the tour; **left stick** moves and **right stick** snap turns outside the tour. The headset tour jumps between stations while retaining physical eye height.

The bundled voice configuration uses mock services and local narration. A live AI backend cannot be reached from a Quest using `http://localhost:3000`; configure a headset reachable backend address and service settings if live conversations are required.
