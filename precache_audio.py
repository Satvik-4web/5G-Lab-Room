import asyncio
import os
import sys
from synthesize import synthesize

lines = [
    # Module 1 - Line 0: Introduction to the Lab & 5G CUPS Banner
    "Hey there! Welcome to our 5G Lab. I'm Aria, and I'll be walking you through the lab today. Right in front of us on the wall is our university's official 5G Architecture poster. It lays out the foundation of modern 5G—called CUPS, or Control and User Plane Separation. Basically, older cellular networks bundled everything into one big bottleneck. In 5G, we completely separated the brain of the network from the muscle that moves data. Let's break it down step-by-step!",

    # Module 1 - Line 1: User Equipment (UE)
    "First, let's look at the left column under UE, or User Equipment. 5G isn't just for phones—it connects IoT drones at the top, autonomous self-driving cars, handheld smartphones, and enterprise workstations. Notice how every single device starts its journey right here.",

    # Module 1 - Line 2: Access Network
    "Right next to the devices is the Access Network column. They connect through 5G New Radio base stations, existing 4G LTE towers, and even home Wi-Fi and wired fiber. Everything plugs into the network through one of these radio links.",

    # Module 1 - Line 3: 5G Control Plane (SBA)
    "Now, look up at the top box highlighted in gold. That's the 5G Control Plane. Think of this as the brain. You've got the AMF tracking your device as you move around, the SMF setting up your data sessions, and the UDM verifying your SIM. But here's the golden rule of CUPS: this entire top box only handles signaling instructions. It never touches your actual videos, downloads, or user data packets.",

    # Module 1 - Line 4: 5G User Plane & CUPS
    "Down below, in green, is the 5G User Plane. This is the muscle that carries all your data. Instead of routing traffic all the way to a distant cloud server, 5G distributes these lightweight UPFs right to the edge of the network. See that Local DN icon at the bottom? User data can break out right here to a local server in under one millisecond! That's the secret to zero-lag gaming, real-time robotics, and safe autonomous driving.",

    # Module 1 - Line 5: Module 1 Quiz
    "Alright, quick check to see if that makes sense! On this poster, which edge function is doing the heavy lifting of forwarding user data so we get that sub-millisecond local speed? Hold down the V key and let me know!",

    # Module 2 - Line 6: Transition to gNodeB
    "Spot on! The UPF forwards all the heavy data at the edge. Now that we have the architectural blueprint in mind, let's step over to Module 2 to inspect our physical 5G base station in 3D!",

    # Module 2 - Line 7: gNodeB & Beamforming
    "This is our gNodeB, connecting mobile devices over 5G New Radio. Notice the Massive MIMO antenna array on top—it packs 64 transmit and 64 receive elements. Instead of broadcasting radio signals everywhere like older towers, it uses beamforming to focus radio waves into tight, steerable beams aimed straight at active users. That eliminates interference and gives you blazing fast speeds.",

    # Module 2 - Line 8: Module 2 Quiz
    "Quick check on Module 2: What does active beamforming actually do to 5G radio signals? Hold down the V key and tell me!",

    # Module 3 - Line 9: Transition to MEC
    "Awesome! Beamforming focuses radio energy directly at active users. Next up, let's step to Module 3: our Mobile Edge Computing core server rack!",

    # Module 3 - Line 10: MEC Deep Dive
    "Here's our MEC server rack. Remember the UPF and Local Data Network we saw on the banner? This rack is where they actually live! By running high-performance compute and user plane functions right here on-site, we cut out long internet round-trips. That's how we get sub-millisecond response times for real-time AI and robotics.",

    # Module 3 - Line 11: Module 3 Quiz
    "Quick check on Module 3: Why do we put MEC servers right at the edge of the network instead of the cloud? Hold down the V key to answer!",

    # Module 4 - Line 12: Transition to Spectrum Analyzer
    "Exactly—putting compute at the edge kills latency. Finally, let's head over to Module 4 to inspect our precision RF Spectrum Analyzer!",

    # Module 4 - Line 13: Spectrum Analyzer Diagnostics
    "This is our RF Spectrum Analyzer. It's the ultimate lab diagnostic tool. It captures high-frequency radio waves across Sub-6 and millimeter wave bands, plotting signal amplitude on the vertical axis against frequency on the horizontal axis. That lets us verify signal purity, check bandwidth, and hunt down any rogue interference.",

    # Module 4 - Line 14: Module 4 Quiz
    "Final check on Module 4: What two variables does the spectrum analyzer display on its screen? Hold down the V key and let me know!",

    # Graduation - Line 15: Graduation & Wrap-Up
    "Fantastic work! You've successfully finished all four modules in our 5G Lab! From the CUPS architecture banner on the wall to the gNodeB, the MEC edge servers, and the Spectrum Analyzer. Whenever you want to explore more or test your knowledge, just hold V and ask me anything!"
]

async def run():
    voice = "en-US-AvaMultilingualNeural"
    for idx, text in enumerate(lines):
        out_wav = f"D:/5G-Lab-Room/audio_cache/mod_line_{idx}.wav"
        await synthesize(text, out_wav, voice=voice)
        print(f"Pre-cached line {idx} -> {out_wav}")

if __name__ == "__main__":
    asyncio.run(run())
