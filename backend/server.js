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
  max: 120, // 120 requests per minute
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Rate limit exceeded, please wait a moment.' }
});
app.use(limiter);

// Optional Shared Secret Middleware
const sharedSecret = process.env.AGENT_SHARED_SECRET;
if (sharedSecret) {
  app.use((req, res, next) => {
    const authHeader = req.headers['x-agent-secret'];
    if (!authHeader || authHeader !== sharedSecret) {
      return res.status(401).json({ error: 'Unauthorized: Invalid or missing X-Agent-Secret' });
    }
    next();
  });
}

// Clients
const anthropic = process.env.ANTHROPIC_API_KEY ? new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY }) : null;
const openai = process.env.OPENAI_API_KEY ? new OpenAI({ apiKey: process.env.OPENAI_API_KEY }) : null;

// Health Check
app.get('/health', (req, res) => {
  res.json({
    status: 'online',
    anthropicConfigured: !!anthropic,
    openaiConfigured: !!openai,
    timestamp: new Date().toISOString()
  });
});

// 1. STT (Speech-to-Text via Whisper)
app.post('/stt', upload.single('audio'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'Missing audio file in request.' });
    }
    if (!openai) {
      return res.status(503).json({ error: 'OpenAI API key not configured on backend.' });
    }

    const audioFile = await toFile(req.file.buffer, 'recording.wav', { type: 'audio/wav' });
    const transcription = await openai.audio.transcriptions.create({
      file: audioFile,
      model: 'whisper-1',
      language: 'en'
    });

    res.json({ text: transcription.text });
  } catch (error) {
    console.error('[STT Error]', error.message);
    res.status(500).json({ error: error.message || 'Speech-to-text transcription failed.' });
  }
});

// 2. Chat / Reasoning (via Anthropic Claude Messages API)
app.post('/chat', async (req, res) => {
  try {
    const { messages, system, tools, maxTokens } = req.body;
    if (!anthropic) {
      return res.status(503).json({ error: 'Anthropic API key not configured on backend.' });
    }

    // Format tools for Claude
    const claudeTools = (tools || []).map(t => {
      let input_schema = { type: 'object', properties: {} };
      try {
        if (typeof t.parametersJson === 'string' && t.parametersJson.length > 0) {
          input_schema = JSON.parse(t.parametersJson);
        }
      } catch (e) { }
      return {
        name: t.name,
        description: t.description,
        input_schema: input_schema
      };
    });

    // Format messages for Claude
    const formattedMessages = (messages || []).map(m => {
      if (m.role === 'tool') {
        return {
          role: 'user',
          content: [
            {
              type: 'tool_result',
              tool_use_id: m.toolCallId || 'call_default',
              content: m.content
            }
          ]
        };
      }
      return {
        role: m.role === 'assistant' ? 'assistant' : 'user',
        content: m.content
      };
    });

    // Ensure first message is user
    if (formattedMessages.length === 0 || formattedMessages[0].role !== 'user') {
      formattedMessages.unshift({ role: 'user', content: 'Hello Aria.' });
    }

    const requestPayload = {
      model: process.env.CLAUDE_MODEL || 'claude-3-5-haiku-20241022',
      max_tokens: maxTokens || 250,
      system: system || '',
      messages: formattedMessages
    };

    if (claudeTools.length > 0) {
      requestPayload.tools = claudeTools;
    }

    const response = await anthropic.messages.create(requestPayload);

    let textContent = '';
    const toolCalls = [];

    for (const block of response.content) {
      if (block.type === 'text') {
        textContent += block.text;
      } else if (block.type === 'tool_use') {
        toolCalls.push({
          id: block.id,
          name: block.name,
          inputJson: JSON.stringify(block.input)
        });
      }
    }

    res.json({
      text: textContent,
      toolCalls: toolCalls
    });
  } catch (error) {
    console.error('[Chat Error]', error.message);
    res.status(500).json({ error: error.message || 'LLM generation failed.' });
  }
});

// 3. TTS (Text-to-Speech via OpenAI)
app.post('/tts', async (req, res) => {
  try {
    const { text, voice } = req.body;
    if (!text || text.trim().length === 0) {
      return res.status(400).json({ error: 'Text field is required for TTS.' });
    }
    if (!openai) {
      return res.status(503).json({ error: 'OpenAI API key not configured on backend.' });
    }

    const selectedVoice = voice || process.env.TTS_VOICE || 'alloy';
    const mp3Response = await openai.audio.speech.create({
      model: process.env.TTS_MODEL || 'tts-1',
      voice: selectedVoice,
      input: text,
      response_format: 'wav'
    });

    const buffer = Buffer.from(await mp3Response.arrayBuffer());
    res.set({
      'Content-Type': 'audio/wav',
      'Content-Length': buffer.length
    });
    res.send(buffer);
  } catch (error) {
    console.error('[TTS Error]', error.message);
    res.status(500).json({ error: error.message || 'TTS generation failed.' });
  }
});

app.listen(port, () => {
  console.log(`[5G Lab Voice Proxy] Server running on port ${port}`);
});
