# Aria — Voice-Driven AI Tutor for 5G Lab Room VR / Simulator

Aria is an interactive, voice-driven AI tutor agent embedded directly inside the **5G Lab Room VR Simulator** (Unity 6, URP, XR Interaction Toolkit 3.x, OpenXR). She answers learner questions in real-time, teaches complex telecom concepts, provides automated scene control (inspection zoom and highlights), and conducts an interactive Guided Tour with spoken check quizzes.

---

## 1. Quick Start (Instant In-Editor Testing)

1. Open **Unity Hub** and open the project at `D:\5G-Lab-Room`.
2. In the bottom **Project** tab, open `Assets/Scenes/ComputerLab.unity`.
3. In the top Unity menu bar, click:
   **`5G Lab > Setup Complete Voice Agent (Aria)`**
   *(This generates the knowledge base, configures the `VoiceAgent_Aria` GameObject, creates `VoiceAgent.prefab`, and saves the scene).*
4. Press **Play** in Unity:
   - **Hold `[V]` on your keyboard** and speak into your microphone (e.g. *"What is beamforming?"* or *"What am I looking at?"*).
   - Release `[V]` to let Aria transcribe, think, and reply with 3D audio and live HUD subtitles!
   - Say: *"Start the tour"* to trigger the 3-device guided walkthrough with interactive spoken quiz checks.

> **Note on Zero-Cost / Offline Mode:** By default, `useMockServices` is enabled in `AgentConfig.asset`. The complete voice loop (transcription, intelligent contextual answers, speech playback, subtitles, inspection zooms, and tour) works 100% offline without any API keys or network connection!

---

## 2. Connecting the Cloud AI Backend Proxy

To connect real **Claude 3.5**, **OpenAI Whisper**, and **OpenAI TTS**:

### Step 1: Start the Backend Proxy
1. Open a terminal in the `backend/` folder:
   ```bash
   cd backend
   npm install
   ```
2. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```
3. Fill in your API keys in `backend/.env`:
   ```env
   ANTHROPIC_API_KEY=sk-ant-api03-...
   OPENAI_API_KEY=sk-proj-...
   PORT=3000
   ```
4. Start the server:
   ```bash
   npm start
   ```

### Step 2: Configure Unity
1. In Unity's Project tab, select:
   `Assets/Resources/VoiceAgent/AgentConfig.asset`
2. In the Inspector:
   - Uncheck **Use Mock Services**.
   - Ensure **Proxy Base Url** is set to `http://localhost:3000` (or your deployed cloud URL like `https://your-service.onrender.com`).
3. Press Play — Aria will now stream live Claude intelligence and Whisper transcription!

---

## 3. Controls & Interaction Summary

| Platform | Action | Control |
|---|---|---|
| **Desktop** | Push-to-Talk (Mic) | Hold **`V`** |
| **Desktop** | Inspect Equipment | **Left Click** on equipment (Click again to zoom out) |
| **Desktop** | Look / Move | Mouse / WASD (XR Device Simulator) |
| **Meta Quest (VR)** | Push-to-Talk (Mic) | Right Controller **Grip** or **Primary Button** |
| **Meta Quest (VR)** | Inspect Equipment | Point Ray Interactor and press **Trigger** |

---

## 4. Scene Tools & Voice Commands

Aria understands natural language and can execute scene actions through LLM tool calling:

- **Inspection Zoom:**
  - *"Take me to the spectrum analyzer"*
  - *"Zoom in on the MEC rack"*
  - *"Step back out"* / *"Exit inspection"*
- **Equipment Highlighting:**
  - *"Where is the gNodeB base station?"* (pulses the physical antenna in soft blue)
- **Guided Tour:**
  - *"Can you give me a tour of the lab?"*
  - *"Start the 5G tour"*
- **Glossary Lookups:**
  - *"What is URLLC?"*
  - *"Define beamforming"*

---

## 5. Building for Meta Quest Standalone (APK)

1. Connect your Meta Quest headset via USB-C with Developer Mode enabled.
2. In the top Unity menu, click:
   **`Antigravity > Build Meta Quest APK`**
3. Choose a destination folder (e.g. your Desktop).
4. The builder script automatically enforces:
   - Android Architecture: **ARM64**
   - Graphics APIs: **Vulkan** & **OpenGLES3**
   - Texture Compression: **ASTC**
   - Minimum Android SDK: **API Level 29**
   - Permissions: `RECORD_AUDIO`, `INTERNET`, `MODIFY_AUDIO_SETTINGS`
5. Sideload the resulting `.apk` to your headset using **SideQuest** or **Meta Quest Developer Hub (MQDH)**.

---

## 6. How to Add New Equipment to Knowledge Base

1. Select `Assets/Resources/VoiceAgent/LabKnowledge.asset` in the Unity Project window.
2. In the Inspector under **Equipment List**, click **+**:
   - Assign an `id`, `displayName`, and `uiTitle`.
   - Provide `oneLiner`, `role`, and `howItWorks` paragraphs.
   - Add technical bullet specs and analogies.
   - Add quiz questions with accepted keyword lists.
3. Add a corresponding `BoxCollider`, `XRSimpleInteractable`, and `InteractableLabel` to your 3D GameObject in the scene.
4. Aria will automatically include the new equipment in her live context and answers!

---

## 7. Troubleshooting

- **Microphone Permission Denied:**
  - On Quest: Go to Quest Settings -> Apps -> 5G Lab VR -> Permissions -> Enable Microphone.
  - In Editor: Ensure Windows microphone privacy settings allow desktop apps to access the microphone.
- **No Sound from Aria:**
  - Verify your system audio output is unmuted.
  - In `AgentSpeaker` on `VoiceAgent_Aria`, check that `AudioSource` volume is set to 1.0.
- **Latency / Response Time:**
  - Use `claude-3-5-haiku-20241022` in `backend/.env` for sub-second LLM reasoning.
  - Sentence-by-sentence streaming automatically starts playback on the first sentence while subsequent sentences synthesize in parallel.
