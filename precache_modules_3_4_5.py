import asyncio
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from synthesize import synthesize

audio_items = [
    # Module 3: Spectrometer (RF Spectrum Analyzer)
    ('mod3_spectrum_0.wav',
     'Awesome job on the use cases! Now, let us step over to Module 3 to check out our lab spectrometer, specifically this RF Spectrum Analyzer right here on the workbench. In wireless communication, radio waves are completely invisible, but this instrument is basically our superpower. It captures raw 5G airwaves and translates them into live visual graphs on screen so we can see every single signal.'),

    ('mod3_spectrum_1.wav',
     'Take a look at the display grid. The horizontal axis plots frequency, spanning from low-band Sub-6 gigahertz all the way up to high-band 28 gigahertz millimeter wave. The vertical axis shows signal amplitude, or power. Whenever a 5G transmitter fires, you will see a clean, sharp peak rise right above the background noise floor. That lets our engineers instantly spot signal distortion, verify transmission bandwidth, and hunt down any rogue interference.'),

    ('mod3_spectrum_2.wav',
     'Let us do a quick knowledge check for Module 3! On this spectrum analyzer, what two key variables are plotted on the screen to show how our 5G signal is performing? Hold down the V key and let me know!'),

    ('mod3_spectrum_3.wav',
     'Spot on! The spectrum analyzer plots frequency on the horizontal axis and signal amplitude or power on the vertical axis. Now that we know how to measure RF signals, let us step over to Module 4 to inspect the hardware actually transmitting those waves: our 5G Antenna!'),

    # Module 4: Antenna (Massive MIMO on Tripod)
    ('mod4_antenna_0.wav',
     'Welcome to Module 4! Take a look at this tripod setup right in front of us. This is our 5G Antenna unit. In older 4G cell towers, antennas were passive and blasted radio waves in every direction like an unfocused lightbulb, which wasted huge amounts of energy. 5G completely revolutionized this with an active phased array technology called Massive MIMO.'),

    ('mod4_antenna_1.wav',
     'Notice the antenna array mounted up top. Packed behind that casing are 64 transmit and 64 receive elements. Instead of broadcasting broad signals, the antenna dynamically shifts the phase of each element to synthesize tight, steerable radio beams. It literally tracks active users, beaming high-speed gigabit data straight to your phone while you are walking, and creating zero interference for everyone else!'),

    ('mod4_antenna_2.wav',
     'Quick check for Module 4: What advanced technique does this 5G antenna use to steer focused, high-power radio beams directly at active users? Hold down the V key and tell me!'),

    ('mod4_antenna_3.wav',
     'Exactly right! Active beamforming focuses radio energy directly at target devices. Now that we have seen how the antenna transmits over the air, let us head over to Module 5 to inspect the massive computing brain running the whole show: the gNodeB base station!'),

    # Module 5: gNodeB (Base Station & 5G Core Rack)
    ('mod5_gnodeb_0.wav',
     'And here we are at Module 5! Take a look at this large illuminated server cabinet. This is the heart of the gNodeB and our 5G processing system. gNodeB stands for next-generation Node B, and it is the official 3GPP standard name for modern 5G base stations. While the antenna handles the wireless radio waves, this rack houses the massive digital processing units that control the entire network.'),

    ('mod5_gnodeb_1.wav',
     'Inside this rack, the gNodeB is split into the Distributed Unit and Centralized Unit. It handles microsecond-level packet scheduling, quality of service, security encryption, and manages seamless handovers between cell towers. Mounted right alongside it is our Mobile Edge Computing server, hosting the UPF so data breaks out locally without long trips to distant clouds. Together, they deliver the ultra-reliable, high-throughput promise of 5G!'),

    ('mod5_gnodeb_2.wav',
     'Final knowledge check for Module 5! What is the official 3GPP name for the next-generation 5G base station that manages radio resources and connects users to the core network? Hold down the V key and give it a shot!'),

    ('mod5_gnodeb_3.wav',
     'Bingo! gNodeB, or next-generation Node B, is the true engine of 5G connectivity! Incredible job: you have now successfully completed all five modules in our virtual 5G lab, from the CUPS architecture and Use Cases banners to the Spectrometer, the Massive MIMO Antenna, and the gNodeB base station. Feel free to explore the lab freely, click any piece of equipment, or hold V to ask me anything anytime!')
]

async def run():
    voice = 'en-US-AvaMultilingualNeural'
    cache_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'audio_cache')
    os.makedirs(cache_dir, exist_ok=True)
    for filename, text in audio_items:
        out_wav = os.path.join(cache_dir, filename)
        if os.path.exists(out_wav) and os.path.getsize(out_wav) > 1000:
            print(f'Already cached: {filename} ({os.path.getsize(out_wav)} bytes)')
            continue
        await synthesize(text, out_wav, voice=voice)
        print(f'Done: {filename} ({os.path.getsize(out_wav)} bytes)')

if __name__ == '__main__':
    asyncio.run(run())
