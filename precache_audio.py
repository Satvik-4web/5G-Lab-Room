import asyncio
import os
import sys
from synthesize import synthesize

lines = [
    "Hey there! Welcome to our 5G Virtual Reality Lab. I'm Aria, your interactive lab mentor. On the wall in front of us is our university's official 5G Architecture poster, based on Control and User Plane Separation, or CUPS. Throughout this module, I'll be using laser outlines and target markers so you can clearly trace how signaling and data split across next-generation networks. Let's begin with Module 1!",
    "Look at the left side, where our cyan laser outline is pulsing. Here are the User Equipments and our heterogeneous Access Networks. You see, 5G isn't just for phones; it connects smart city IoT sensors, autonomous connected vehicles, and high-speed enterprise workstations. Notice how they connect through diverse radios: 5G New Radio, legacy 4G LTE, and even untrusted home Wi-Fi via our secure N3IWF gateway.",
    "Now, follow the golden laser up to the top box: the 5G Control Plane. I've highlighted this in gold to represent the brain of the network. Standardized in 3GPP Release 15 as a cloud-native Service-Based Architecture, microservice functions like the AMF for device mobility, the SMF for session control, and the UDM for authentication communicate over lightweight REST APIs. Crucially, the control plane only handles signaling—it never carries the heavy user payload data.",
    "Down below, look at the emerald green laser framing the 5G User Plane. Here is the true breakthrough of CUPS—Control and User Plane Separation. While the SMF centrally manages session policies from above over the crimson N4 interface, lightweight User Plane Functions, or UPFs, are distributed all the way out to the cell edge. This enables local breakout into a Local Data Network, cutting latency to under one millisecond! That's why real-time robotics and autonomous driving are finally possible.",
    "Quick check on Module 1! In this CUPS architecture, which edge network function handles user data forwarding and enables that sub-millisecond local breakout? Hold down the V key and tell me!",
    "Great job! Now that we have the architectural blueprint in mind, let's step over to Module 2 to inspect our physical 3D gNodeB base station!",
    "This is our gNodeB, connecting mobile UEs over 5G New Radio. It features Massive MIMO with 64 transmit and 64 receive elements. Using active beamforming, it shapes radio waves into narrow, high-gain beams directed straight at active users, drastically eliminating interference.",
    "Check on Module 2: What does active beamforming do to 5G radio signals? Hold the V key and tell me!",
    "Next, let's advance to Module 3: the Mobile Edge Computing core server rack!",
    "This is the MEC server rack. As we saw on the CUPS banner, placing baseband units and a localized UPF right here at the edge delivers processing latency under one millisecond. This ultra-reliable low latency is mandatory for tele-surgery, robotics, and self-driving cars.",
    "Check on Module 3: Why do we place MEC servers at the edge of the network? Hold the V key to answer!",
    "Finally, let's step over to Module 4: our RF Spectrum Analyzer!",
    "This is the high-frequency RF Spectrum Analyzer. It captures signals across Sub-6 GHz and mmWave bands, visualizing spectral power density, constellation diagrams, and EVM to verify that our transmissions comply with 3GPP standards.",
    "Final check on Module 4: What critical metric does the spectrum analyzer measure to verify transmission quality? Hold the V key and answer!",
    "Congratulations! You have completed all four modules of our 5G VR Laboratory! From the CUPS architecture banner on the wall to the gNodeB, the MEC edge servers, and the Spectrum Analyzer. Feel free to ask me anything about 5G anytime."
]

async def run():
    for idx, text in enumerate(lines):
        out_wav = f"D:/5G-Lab-Room/audio_cache/mod_line_{idx}.wav"
        await synthesize(text, out_wav)
        print(f"Pre-cached line {idx} -> {out_wav}")

if __name__ == "__main__":
    asyncio.run(run())
