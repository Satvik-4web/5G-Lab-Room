# 5G Lab Voice Proxy Backend

Secure, provider-agnostic backend proxy for **Aria** (the voice-driven AI tutor in 5G Lab Room VR).

## Features
- **Security:** Hides your OpenAI and Anthropic API keys from client builds and reverse engineering.
- **Speech-to-Text (STT):** `/stt` endpoint powered by OpenAI Whisper (`whisper-1`).
- **LLM Reasoning & Scene Tools:** `/chat` endpoint using Anthropic Claude (`claude-3-5-haiku` / `claude-3-5-sonnet`) with full tool-calling support.
- **Text-to-Speech (TTS):** `/tts` streaming WAV endpoint powered by OpenAI TTS (`tts-1`).
- **Rate-Limiting & Auth:** Built-in rate limiting (120 req/min) and optional `X-Agent-Secret` shared token header.

---

## Local Setup

1. Install Node.js (v18+ recommended):
   ```bash
   cd backend
   npm install
   ```

2. Create your `.env` file from `.env.example`:
   ```bash
   cp .env.example .env
   ```

3. Fill in your API keys in `.env`:
   ```env
   ANTHROPIC_API_KEY=sk-ant-api03-...
   OPENAI_API_KEY=sk-proj-...
   PORT=3000
   ```

4. Start the server:
   ```bash
   npm start
   ```

---

## Deployment (1-Click Hosting)

### Option A: Render (Recommended, Free tier available)
1. Fork or push this repository to GitHub.
2. Go to [Render.com](https://render.com) and click **New + > Web Service**.
3. Select your repository.
4. Set **Root Directory** to `backend`.
5. Set **Build Command** to `npm install`.
6. Set **Start Command** to `node server.js`.
7. In the **Environment Variables** section, add:
   - `ANTHROPIC_API_KEY`
   - `OPENAI_API_KEY`
   - `CLAUDE_MODEL` = `claude-3-5-haiku-20241022`
8. Copy the generated service URL (e.g. `https://5g-lab-proxy.onrender.com`) and paste it into `AgentConfig.asset` in Unity!

### Option B: Railway
1. Connect your repo on [Railway.app](https://railway.app).
2. Set root directory to `backend`.
3. Add `ANTHROPIC_API_KEY` and `OPENAI_API_KEY` under Variables.
4. Deploy and copy the public domain to Unity's `AgentConfig.asset`.
