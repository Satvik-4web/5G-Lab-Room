import asyncio
import os
import sys

# Ensure D:/5G-Lab-Room is in path
sys.path.insert(0, "D:/5G-Lab-Room")
from synthesize import synthesize

module2_lines = [
    # Line 0: Overview & Transition from Module 1
    ("usecases_line_0.wav", 
     "Awesome job on Module 1! Now let's slide right over to Module 2 to explore our 5G Use Cases banner. You see, 5G isn't just about faster downloads on our smartphones; the telecom world actually split all human and device communication into three distinct superpowers: eMBB, mMTC, and uRLLC. I'll highlight each tier with our laser frame as we go, so you can follow right along with me. Let's start at the top!"),

    # Line 1: eMBB - Enhanced Mobile Broadband
    ("usecases_line_1.wav",
     "Up first in electric cyan is eMBB, or Enhanced Mobile Broadband. Think of this as the ultimate superhighway for raw speed and bandwidth. We're talking up to 20 Gigabits per second! Notice the apps highlighted here: it powers lag-free 8K streaming, immersive virtual and augmented reality worlds, and fiber-speed fixed wireless for our homes—even if you're cruising on a high-speed train at 500 kilometers an hour."),

    # Line 2: mMTC - Massive Machine Type Communication
    ("usecases_line_2.wav",
     "Next, look at the middle green tier: mMTC, which stands for Massive Machine-Type Communication. If eMBB is a superhighway for fast cars, mMTC is a giant stadium parking lot. It isn't about peak speed; it's engineered to connect up to one million smart devices in a single square kilometer! Imagine city water meters, smart agriculture sensors, and wearable fitness trackers that can quietly run on a single tiny coin battery for more than ten years."),

    # Line 3: uRLLC - Ultra-Reliable Low Latency Communication
    ("usecases_line_3.wav",
     "And down at the bottom in coral red is 5G's most critical superpower: uRLLC, or Ultra-Reliable Low Latency Communication. This is the life-saver. It slashes network delay to under one millisecond with ninety-nine point nine-nine-nine percent reliability! Look at the robotic surgery and autonomous vehicle icons highlighted here: when a surgeon operates on a patient from across the world, or self-driving cars coordinate split-second emergency braking, every microsecond counts."),

    # Line 4: Module 2 Quiz Question
    ("usecases_line_4.wav",
     "Let's do a quick knowledge check for Module 2! If a surgeon is performing a delicate remote robotic surgery from another city, which of the three 5G pillars makes that life-saving precision possible? Hold down the V key and let me know!"),

    # Line 5: Module 2 Quiz Feedback & Transition to Module 3
    ("usecases_line_5.wav",
     "Spot on! uRLLC provides the sub-millisecond reaction time and rock-solid reliability critical for robotic surgery. Now that we understand both the network architecture and the use cases, let's step over to Module 3 to inspect our physical 5G base station in 3D!")
]

async def run():
    voice = "en-US-AvaMultilingualNeural"
    cache_dir = "D:/5G-Lab-Room/audio_cache"
    os.makedirs(cache_dir, exist_ok=True)
    
    for filename, text in module2_lines:
        out_wav = os.path.join(cache_dir, filename)
        await synthesize(text, out_wav, voice=voice)
        print(f"Pre-cached {filename} ({len(text)} chars) -> {out_wav}")

if __name__ == "__main__":
    asyncio.run(run())
