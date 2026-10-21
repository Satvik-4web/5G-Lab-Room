require('dotenv').config();
const express = require('express');
const cors = require('cors');
const multer = require('multer');
const rateLimit = require('express-rate-limit');
const Anthropic = require('@anthropic-ai/sdk');
const OpenAI = require('openai');
const { toFile } = require('openai');

const app = express();
const port = process.env.PORT || 3000;
const upload = multer({ storage: multer.memoryStorage(), limits: { fileSize: 15 * 1024 * 1024 } });

// Security & Rate Limiting
app.use(cors());
app.use(express.json({ limit: '5mb' }));

const limiter = rateLimit({
  windowMs: 60 * 1000,
  max: 120,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Rate limit exceeded, please wait a moment.' }
});
app.use(limiter);

// Clients
const anthropic = process.env.ANTHROPIC_API_KEY ? new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY }) : null;
const openai = process.env.OPENAI_API_KEY ? new OpenAI({ apiKey: process.env.OPENAI_API_KEY }) : null;

// Free Groq Client (OpenAI-compatible)
const groqApiKey = process.env.GROQ_API_KEY;
const groq = groqApiKey ? new OpenAI({
  apiKey: groqApiKey,
  baseURL: 'https://api.groq.com/openai/v1'
}) : null;

// Health Check
app.get('/health', (req, res) => {
  res.json({
    status: 'online',
    freeGroqEnabled: !!groq,
    anthropicEnabled: !!anthropic,
    openaiEnabled: !!openai,
    freeTtsEnabled: true,
    freePollinationsEnabled: true,
    timestamp: new Date().toISOString()
  });
});

// Helper: Free Google Neural TTS (100% Free, Zero Key)
async function fetchGoogleTTS(text) {
  const clean = text.replace(/[\r\n\t]+/g, ' ').substring(0, 180);
  const url = `https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl=en&q=${encodeURIComponent(clean)}`;
  
  const response = await fetch(url, {
    headers: { 'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)' }
  });
  
  if (!response.ok) {
    throw new Error(`Google TTS error: ${response.statusText}`);
  }
  return Buffer.from(await response.arrayBuffer());
}

// Helper: Free Pollinations AI LLM (100% Free, Zero Key)
async function callPollinationsLLM(messages, system) {
  const combined = [];
  if (system) combined.push({ role: 'system', content: system });
  for (const m of messages) {
    combined.push({ role: m.role === 'assistant' ? 'assistant' : 'user', content: m.content });
  }

  const response = await fetch('https://text.pollinations.ai/', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      messages: combined,
      model: 'openai',
      jsonMode: false
    })
  });

  if (!response.ok) {
    throw new Error(`Pollinations error: ${response.statusText}`);
  }
  return await response.text();
}

// 1. STT (Speech-to-Text)
app.post('/stt', upload.single('audio'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'Missing audio file in request.' });
    }

    // A. Use Free Groq Whisper if configured
    if (groq) {
      const audioFile = await toFile(req.file.buffer, 'recording.wav', { type: 'audio/wav' });
      const transcription = await groq.audio.transcriptions.create({
        file: audioFile,
        model: 'whisper-large-v3',
        language: 'en'
      });
      return res.json({ text: transcription.text });
    }

    // B. Use OpenAI Whisper if configured
    if (openai) {
      const audioFile = await toFile(req.file.buffer, 'recording.wav', { type: 'audio/wav' });
      const transcription = await openai.audio.transcriptions.create({
        file: audioFile,
        model: 'whisper-1',
        language: 'en'
      });
      return res.json({ text: transcription.text });
    }

    // C. Hint to user to add free Groq key
    res.json({
      text: "To enable free speech recognition, add your free Groq API key in backend/.env!"
    });
  } catch (error) {
    console.error('[STT Error]', error.message);
    res.status(500).json({ error: error.message || 'Speech-to-text failed.' });
  }
});

// 2. Chat / Reasoning
app.post('/chat', async (req, res) => {
  try {
    const { messages, system, tools, maxTokens } = req.body;

    // A. Use Free Groq LLaMA 3.3 if configured
    if (groq) {
      const formatted = (messages || []).map(m => ({
        role: m.role === 'assistant' ? 'assistant' : 'user',
        content: m.content
      }));
      if (system) formatted.unshift({ role: 'system', content: system });

      const completion = await groq.chat.completions.create({
        model: 'llama-3.3-70b-versatile',
        messages: formatted,
        max_tokens: maxTokens || 200
      });

      const reply = completion.choices[0]?.message?.content || '';
      return res.json({ text: reply, toolCalls: [] });
    }

    // B. Use Anthropic Claude if configured
    if (anthropic) {
      const formattedMessages = (messages || []).map(m => ({
        role: m.role === 'assistant' ? 'assistant' : 'user',
        content: m.content
      }));
      if (formattedMessages.length === 0 || formattedMessages[0].role !== 'user') {
        formattedMessages.unshift({ role: 'user', content: 'Hello Aria.' });
      }

      const response = await anthropic.messages.create({
        model: process.env.CLAUDE_MODEL || 'claude-3-5-haiku-20241022',
        max_tokens: maxTokens || 200,
        system: system || '',
        messages: formattedMessages
      });

      let textContent = '';
      for (const block of response.content) {
        if (block.type === 'text') textContent += block.text;
      }
      return res.json({ text: textContent, toolCalls: [] });
    }

    // C. Use 100% Free Keyless Pollinations AI
    const reply = await callPollinationsLLM(messages || [], system || '');
    res.json({ text: reply, toolCalls: [] });
  } catch (error) {
    console.error('[Chat Error]', error.message);
    res.status(500).json({ error: error.message || 'LLM generation failed.' });
  }
});

// 3. TTS (Text-to-Speech)
app.post('/tts', async (req, res) => {
  try {
    const { text, voice } = req.body;
    if (!text || text.trim().length === 0) {
      return res.status(400).json({ error: 'Text field is required for TTS.' });
    }

    // A. Use OpenAI TTS if configured
    if (openai) {
      const mp3Response = await openai.audio.speech.create({
        model: process.env.TTS_MODEL || 'tts-1',
        voice: voice || process.env.TTS_VOICE || 'alloy',
        input: text,
        response_format: 'wav'
      });
      const buffer = Buffer.from(await mp3Response.arrayBuffer());
      res.set({ 'Content-Type': 'audio/wav', 'Content-Length': buffer.length });
      return res.send(buffer);
    }

    // B. 100% Free Keyless Google Neural Voice
    const audioBuffer = await fetchGoogleTTS(text);
    res.set({
      'Content-Type': 'audio/mpeg',
      'Content-Length': audioBuffer.length
    });
    res.send(audioBuffer);
  } catch (error) {
    console.error('[TTS Error]', error.message);
    res.status(500).json({ error: error.message || 'TTS generation failed.' });
  }
});

app.listen(port, () => {
  console.log(`[5G Lab Voice Proxy] Running on port ${port} with FREE AI support!`);
});
