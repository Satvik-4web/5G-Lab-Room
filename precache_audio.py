import asyncio
import os
import sys
from synthesize import synthesize

lines = [
    "Hey there! Welcome to our 5G Virtual Reality Lab. I'm Aria, your interactive lab mentor. To help you truly visualize how next-generation mobile networks work, I've projected our university's CUPS architecture banner right here in front of us as a holographic smart display. Throughout this module, I'll be using dynamic laser outlines and target markers so you can clearly trace how signaling and data split across modern 5G networks. Let's begin with Module 1!",
    "Look over at the left side, where our cyan laser outline is pulsing. These are our User Equipments and heterogeneous Access Networks. You see, 5G isn't just about faster smartphones; it unifies smart city IoT sensors, connected autonomous cars, and high-speed workstations. Notice how they connect through diverse radios: 5G New Radio, legacy 4G LTE, and even untrusted home Wi-Fi via our secure N3IWF gateway. The graphics highlight this in cyan because this is the access frontier where all user traffic enters our network.",
    "Now, follow the golden laser up to the top box: our 5G Control Plane. I've highlighted this in gold to represent the central intelligence of 5G. Standardized in 3GPP Release 15 as a cloud-native Service-Based Architecture, microservice functions like the AMF for device mobility, the SMF for session control, and the UDM for authentication communicate over lightweight REST APIs. Crucially, the control plane only processes signaling—it never carries the heavy user payload data.",
    "Down below, look at the emerald green laser framing our 5G User Plane. Here is the true breakthrough of CUPS—Control and User Plane Separation. While the SMF centrally manages session policies from afar over the N4 interface, lightweight User Plane Functions, or UPFs, are distributed all the way out to the cell edge. This enables local breakout into a Local Data Network, cutting round-trip latency to under one millisecond! That's why real-time robotics and autonomous driving are finally possible.",
    "Quick check on Module 1! In this CUPS architecture, which edge network function handles user data forwarding and enables that sub-millisecond local breakout? Hold down the V key and tell me!",
    "Great job! Now that we have the architectural blueprint in mind, let's step over to Module 2 to inspect our physical 3D gNodeB base station!"
]

async def run():
    for idx, text in enumerate(lines):
        out_file = f"D:/5G-Lab-Room/audio_cache/mod1_line_{idx}.mp3"
        await synthesize(text, out_file)
        print(f"Pre-cached line {idx}")

if __name__ == "__main__":
    asyncio.run(run())
