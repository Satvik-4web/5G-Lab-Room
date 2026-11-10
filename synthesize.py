import asyncio
import hashlib
import os
import re
import sys
import edge_tts

# Audio cache folder inside the project
CACHE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "audio_cache")
os.makedirs(CACHE_DIR, exist_ok=True)

DEFAULT_VOICE = "en-US-AriaNeural"  # Microsoft Azure Neural Voice - Aria (Expressive, Human, Warm)

def clean_text_for_speech(raw: str) -> str:
    # Clean up formatting artifacts, brackets, tags
    t = re.sub(r'<[^>]+>', '', raw)
    t = re.sub(r'[\*#`_~]', '', t)
    t = re.sub(r'\[V\]', 'the V key', t)
    t = re.sub(r'\[Esc\]', 'Escape', t)
    t = re.sub(r'\[\d\]', '', t)
    t = re.sub(r'\s+', ' ', t).strip()
    return t

async def synthesize(text: str, output_path: str, voice: str = DEFAULT_VOICE):
    cleaned = clean_text_for_speech(text)
    if not cleaned:
        sys.exit(1)

    # Check cache by MD5 hash
    text_hash = hashlib.md5(f"{voice}:{cleaned}".encode("utf-8")).hexdigest()
    cached_file = os.path.join(CACHE_DIR, f"{text_hash}.mp3")

    if os.path.exists(cached_file) and os.path.getsize(cached_file) > 1000:
        # Copy from cache to target output
        with open(cached_file, "rb") as src, open(output_path, "wb") as dst:
            dst.write(src.read())
        print(f"CACHE_HIT:{output_path}")
        return

    # Synthesize via edge-tts with natural speech rate and pitch
    communicate = edge_tts.Communicate(cleaned, voice, rate="+0%", pitch="+0Hz")
    await communicate.save(cached_file)

    # Copy to target
    with open(cached_file, "rb") as src, open(output_path, "wb") as dst:
        dst.write(src.read())
    print(f"SUCCESS:{output_path}")

def main():
    if len(sys.argv) < 3:
        print("Usage: synthesize.py <text> <output_path> [voice]")
        sys.exit(1)

    text = sys.argv[1]
    output_path = sys.argv[2]
    voice = sys.argv[3] if len(sys.argv) > 3 else DEFAULT_VOICE

    try:
        asyncio.run(synthesize(text, output_path, voice))
    except Exception as e:
        print(f"ERROR:{e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
