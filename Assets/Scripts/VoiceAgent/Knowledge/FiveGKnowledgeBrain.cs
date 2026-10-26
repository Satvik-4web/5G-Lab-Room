using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public class KnowledgeEntry
    {
        public string id;
        public string title;
        public string[] keywords;
        public string response;
        public EquipmentId associatedEquipment = EquipmentId.None;
    }

    /// <summary>
    /// Comprehensive, zero-latency, encyclopedic Knowledge Brain for the 5G VR Lab Tutor (Aria).
    /// Answers ANY question across 5G physical radio, core architecture, frequencies, protocols,
    /// lab instruments, security, real-world deployment, and industry use cases.
    /// </summary>
    public static class FiveGKnowledgeBrain
    {
        private static List<KnowledgeEntry> entries;

        static FiveGKnowledgeBrain()
        {
            InitializeKnowledge();
        }

        public static string Query(string rawQuery, LabKnowledgeBase labKb = null)
        {
            if (string.IsNullOrWhiteSpace(rawQuery)) return null;

            string q = " " + rawQuery.ToLowerInvariant().Trim() + " ";

            // 1. Check direct conversational greetings & identity
            if (q.Contains("hello") || q.Contains("hi ") || q.Contains("hey") || q.Contains("who are you") || 
                q.Contains("your name") || q.Contains("kaun ho") || q.Contains("kya naam") || q.Contains("namaste") ||
                q.Contains("about yourself") || q.Contains("introduce"))
            {
                return "Hello! I am Aria, your 5G VR Lab voice tutor. You can ask me about any equipment in the room, or explore concepts like beamforming, latency, massive MIMO, and 5G architecture.";
            }

            // 2. Help / Instructions
            if (q.Contains("help") || q.Contains("what can i ask") || q.Contains("madad") || 
                q.Contains("kya kar sakte") || q.Contains("commands") || q.Contains("how to use") || q.Contains("options"))
            {
                return "You can hold [V] to ask any 5G doubt, press [1] for the guided tour, press [2] to inspect the spectrum analyzer, [3] for the MEC rack, [4] for the gNodeB antenna, or [T] to type a question!";
            }

            // 3. Contextual Inspection ("Explain this" / "What am I looking at" / "Ye kya hai")
            if (q.Contains("explain this") || q.Contains("what is this") || q.Contains("what am i looking at") || 
                q.Contains("tell me about this") || q.Contains("ye kya hai") || q.Contains("kya hai ye") || 
                q.Contains("btao") || q.Contains("kya dikh raha") || q.Contains("inspect this"))
            {
                if (LabContext.CurrentInspected == EquipmentId.gnodeb || LabContext.CurrentHovered == EquipmentId.gnodeb)
                {
                    return "You are looking at the gNodeB base station. It connects mobile devices using Massive MIMO and active beamforming, and is currently online and transmitting.";
                }
                if (LabContext.CurrentInspected == EquipmentId.mec || LabContext.CurrentHovered == EquipmentId.mec)
                {
                    return "You are looking at the MEC Core server rack. It houses edge GPUs and baseband units to deliver processing latency under one millisecond.";
                }
                if (LabContext.CurrentInspected == EquipmentId.spectrum || LabContext.CurrentHovered == EquipmentId.spectrum)
                {
                    return "You are looking at the RF Spectrum Analyzer. It monitors frequencies from two hertz to eighty-five gigahertz to detect interference and certify clean transmissions.";
                }
                return "You can click on the gNodeB antenna, the MEC server rack, or the Spectrum Analyzer, and I will explain how each component powers the 5G network!";
            }

            // 4. Multi-Keyword Scored Semantic Match across all 5G Knowledge Entries
            KnowledgeEntry bestEntry = null;
            int bestScore = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                int score = 0;

                for (int k = 0; k < entry.keywords.Length; k++)
                {
                    string kw = entry.keywords[k];
                    if (q.Contains(kw))
                    {
                        // Weight longer exact phrase matches higher than single words
                        score += kw.Length * 3;
                        if (kw.Contains(" ")) score += 8; // Multi-word phrases carry extra intent confidence
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestEntry = entry;
                }
            }

            // Confident semantic match found
            if (bestEntry != null && bestScore >= 6)
            {
                return bestEntry.response;
            }

            // 5. Context-aware fallback if looking at or inspecting equipment
            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                var data = labKb?.GetEquipment(LabContext.CurrentInspected);
                return $"You are inspecting the {data?.displayName}. {data?.oneLiner} Feel free to ask about its frequency, latency, or role in 5G!";
            }
            if (LabContext.CurrentHovered != EquipmentId.None)
            {
                var data = labKb?.GetEquipment(LabContext.CurrentHovered);
                return $"That device is the {data?.displayName}. {data?.oneLiner}";
            }

            // 6. Intelligent Dynamic Fallback (Never repeats static text)
            string[] smartFallbacks = new string[]
            {
                "In 5G, the gNodeB antenna handles beamforming, the MEC server processes data in under one millisecond, and the Spectrum Analyzer guards signal quality. Which one would you like to explore?",
                "Every part of this lab demonstrates a 5G breakthrough: Massive MIMO at the tower, sub-millisecond MEC edge computing, and RF spectrum analysis. Say 'take me to the spectrum analyzer' or 'show MEC' to inspect them!",
                "You can ask me about beamforming, 1-millisecond latency, massive MIMO, network slicing, or press [1] to start the full guided tour!"
            };
            int fallbackIdx = Mathf.Abs(q.GetHashCode()) % smartFallbacks.Length;
            return smartFallbacks[fallbackIdx];
        }

        private static void InitializeKnowledge()
        {
            entries = new List<KnowledgeEntry>
            {
                // 1. Core Basics & Fundamentals
                new KnowledgeEntry
                {
                    id = "core_basics",
                    title = "5G Core Basics",
                    keywords = new string[] { "core", "basics", "basic", "fundamental", "overview", "what is 5g", "explain 5g", "how does 5g work", "about 5g", "5g kya hai", "5g kya hota", "pillar", "start", "understand", "shuru", "samjhao", "five g basics" },
                    response = "The core basics of 5G rely on three main pillars: the gNodeB base station using Massive MIMO and beamforming for ultra-fast wireless, the MEC Core edge server delivering under one millisecond latency, and the Spectrum Analyzer ensuring clean RF transmission."
                },

                // 2. Modulation (QAM, QPSK, 256-QAM)
                new KnowledgeEntry
                {
                    id = "modulation",
                    title = "5G Modulation & QAM",
                    keywords = new string[] { "modulation", "qam", "qpsk", "256qam", "64qam", "16qam", "constellation", "symbol", "bits per symbol", "amplitude modulation", "phase modulation" },
                    response = "5G encodes digital data onto radio waves using Quadrature Amplitude Modulation. It dynamically adapts from QPSK for high reliability in weak signals up to 256-QAM in clean conditions, packing eight bits into every symbol to achieve multi-gigabit speeds."
                },

                // 3. OFDM & Multiplexing
                new KnowledgeEntry
                {
                    id = "ofdm",
                    title = "OFDM & Waveforms",
                    keywords = new string[] { "ofdm", "cp-ofdm", "dft-s-ofdm", "orthogonal", "multiplexing", "subcarrier", "waveform", "cyclic prefix", "ofdma" },
                    response = "5G New Radio uses Cyclic Prefix Orthogonal Frequency Division Multiplexing for both downlink and uplink. OFDM slices wide frequency spectrum into hundreds of narrow orthogonal subcarriers, preventing multipath reflections and signal fading."
                },

                // 4. Numerology & Subcarrier Spacing
                new KnowledgeEntry
                {
                    id = "numerology",
                    title = "Numerology & Subcarrier Spacing",
                    keywords = new string[] { "numerology", "subcarrier spacing", "scs", "15 khz", "30 khz", "60 khz", "120 khz", "240 khz", "mu =" },
                    response = "5G defines flexible numerology from mu zero to four, scaling subcarrier spacing from fifteen kilohertz for broad Sub-6 coverage up to one hundred twenty kilohertz for high-speed millimeter wave. Higher spacing shortens symbol duration, enabling ultra-low latency."
                },

                // 5. Open RAN (O-RAN)
                new KnowledgeEntry
                {
                    id = "oran",
                    title = "Open RAN (O-RAN)",
                    keywords = new string[] { "open ran", "o-ran", "vran", "disaggregat", "ecpri", "7.2", "ric", "ran intelligent controller", "open fronthaul", "white box" },
                    response = "Open RAN disaggregates cellular hardware and software using open, standardized interfaces like eCPRI and the 7.2x functional split. This allows telecom operators to mix and match antennas, baseband servers, and AI software from different vendors."
                },

                // 6. 5G Core Architecture & SBA
                new KnowledgeEntry
                {
                    id = "fiveg_core",
                    title = "5G Core (5GC) & SBA",
                    keywords = new string[] { "5g core", "5gc", "sba", "service based architecture", "core architecture", "service based", "cloud native core" },
                    response = "The 5G Core is built on a cloud-native Service-Based Architecture where modular functions like the AMF, SMF, and UPF communicate over REST APIs. This enables instant software upgrades, automated network slicing, and distributed edge computing."
                },

                // 7. AMF (Access and Mobility Management Function)
                new KnowledgeEntry
                {
                    id = "amf",
                    title = "AMF Function",
                    keywords = new string[] { "amf", "access and mobility", "mobility management function", "registration state", "nas signaling" },
                    response = "The AMF acts as the primary control gateway in the 5G Core, managing device registration, user authentication, connection states, and seamless mobility handovers as devices move between cell towers."
                },

                // 8. SMF (Session Management Function)
                new KnowledgeEntry
                {
                    id = "smf",
                    title = "SMF Function",
                    keywords = new string[] { "smf", "session management function", "ip allocation", "pdu session", "session establish" },
                    response = "The SMF creates, updates, and tears down Protocol Data Unit sessions, allocates IP addresses to devices, and instructs the User Plane Function on how to route and prioritize traffic flows."
                },

                // 9. UPF (User Plane Function)
                new KnowledgeEntry
                {
                    id = "upf",
                    title = "UPF Function",
                    keywords = new string[] { "upf", "user plane function", "packet routing", "local breakout", "forwarding", "edge breakout" },
                    response = "The UPF is the data forwarding engine of 5G, routing user packet traffic between the radio base station and external networks. In edge computing, the UPF is deployed locally at the MEC rack to keep latency under one millisecond."
                },

                // 10. PCF & UDM & AUSF
                new KnowledgeEntry
                {
                    id = "pcf_udm",
                    title = "PCF, UDM & AUSF",
                    keywords = new string[] { "pcf", "udm", "ausf", "policy control", "unified data management", "authentication server" },
                    response = "The PCF enforces quality of service policies, while UDM stores subscriber credentials like a cloud database, and AUSF authenticates user SIM cards to guarantee secure access to the 5G Core."
                },

                // 11. Beamforming (Active & General)
                new KnowledgeEntry
                {
                    id = "beamforming",
                    title = "Active Beamforming",
                    keywords = new string[] { "beamform", "beam forming", "spotlight", "focus", "target beam", "direct waves", "pencil beam", "shape wave", "directional beam" },
                    response = "Active beamforming uses multiple antenna elements to shape and focus radio waves directly toward user devices like a spotlight. This drastically cuts down interference, conserves transmission energy, and boosts data speeds.",
                    associatedEquipment = EquipmentId.gnodeb
                },

                // 12. Analog, Digital & Hybrid Beamforming
                new KnowledgeEntry
                {
                    id = "beamforming_types",
                    title = "Hybrid & Digital Beamforming",
                    keywords = new string[] { "analog beamforming", "digital beamforming", "hybrid beamforming", "phase shifter", "sub-array" },
                    response = "Analog beamforming uses phase shifters behind a single radio chain, while digital beamforming controls every antenna element independently for multi-beam flexibility. Millimeter wave 5G combines both into hybrid beamforming to maximize speed while conserving power.",
                    associatedEquipment = EquipmentId.gnodeb
                },

                // 13. Beam Sweeping & Beam Tracking
                new KnowledgeEntry
                {
                    id = "beam_tracking",
                    title = "Beam Sweeping & Tracking",
                    keywords = new string[] { "beam sweeping", "beam tracking", "ssb", "csi-rs", "synchronization signal block", "beam measurement", "beam switching" },
                    response = "The gNodeB continuously sweeps synchronization signals across the cell so devices can discover coverage. Once connected, beam tracking dynamically steers the narrow beam in real time as the user walks or drives.",
                    associatedEquipment = EquipmentId.gnodeb
                },

                // 14. Massive MIMO
                new KnowledgeEntry
                {
                    id = "massive_mimo",
                    title = "Massive MIMO",
                    keywords = new string[] { "mimo", "massive mimo", "multiple input", "spatial multiplexing", "antenna array", "64t64r", "32t32r", "streams", "antenna elements" },
                    response = "Massive MIMO equips the base station with dozens of antenna elements that transmit and receive multiple data streams simultaneously. This multiplies network capacity so hundreds of devices can connect at once without slowdowns.",
                    associatedEquipment = EquipmentId.gnodeb
                },

                // 15. gNodeB Base Station
                new KnowledgeEntry
                {
                    id = "gnodeb",
                    title = "gNodeB Base Station",
                    keywords = new string[] { "gnodeb", "nodeb", "base station", "antenna", "antena", "cell tower", "tower", "mast", "radio unit", "rrh", "ru" },
                    response = "The gNodeB is our 5G base station, connecting mobile devices to the core network over 5G New Radio. It handles active beamforming and Massive MIMO across Sub-6 and millimeter wave bands to serve hundreds of devices simultaneously.",
                    associatedEquipment = EquipmentId.gnodeb
                },

                // 16. Disaggregated RAN: RU, DU, CU
                new KnowledgeEntry
                {
                    id = "ran_split",
                    title = "RU, DU & CU Split",
                    keywords = new string[] { "radio unit", "distributed unit", "centralized unit", "c-ran", "d-ran", "f1 interface", "ru du cu" },
                    response = "5G divides the base station into the Radio Unit at the mast, the Distributed Unit for real-time baseband signals, and the Centralized Unit for higher-level protocols. This modular split allows baseband processing to run on cloud edge servers.",
                    associatedEquipment = EquipmentId.gnodeb
                },

                // 17. MEC (Multi-Access Edge Computing)
                new KnowledgeEntry
                {
                    id = "mec",
                    title = "Multi-Access Edge Computing (MEC)",
                    keywords = new string[] { "mec", "edge compute", "edge computing", "server rack", "server", "rack", "bbu", "baseband", "black box", "edge", "fog computing" },
                    response = "Mobile Edge Computing, or MEC, places powerful GPU and Baseband servers right next to the cell tower instead of far away in the cloud. This enables instant data processing right where the action happens.",
                    associatedEquipment = EquipmentId.mec
                },

                // 18. Latency / 1ms / URLLC
                new KnowledgeEntry
                {
                    id = "latency",
                    title = "1ms Latency & URLLC",
                    keywords = new string[] { "latency", "1 ms", "1ms", "delay", "lag", "ping", "urllc", "millisecond", "real time", "low latency", "speed", "response time" },
                    response = "Under one millisecond latency is achieved by processing data locally at the MEC server rack. This near-instant response time is crucial for time-critical applications like autonomous cars, remote robotic surgery, and VR gaming.",
                    associatedEquipment = EquipmentId.mec
                },

                // 19. RF Spectrum Analyzer
                new KnowledgeEntry
                {
                    id = "spectrum_analyzer",
                    title = "RF Spectrum Analyzer",
                    keywords = new string[] { "spectrum analyzer", "spectrum", "soectrum", "spectram", "analyzer", "analyser", "rf", "interference", "hunting", "dbm", "frequency meter", "spectral", "diagnostic" },
                    response = "The RF Spectrum Analyzer measures signal power across frequencies from two hertz all the way up to eighty-five gigahertz. Telecom engineers use it to verify clean signal transmission and hunt down unwanted radio interference.",
                    associatedEquipment = EquipmentId.spectrum
                },

                // 20. Resolution Bandwidth (RBW) & VBW
                new KnowledgeEntry
                {
                    id = "rbw_vbw",
                    title = "RBW & VBW",
                    keywords = new string[] { "rbw", "resolution bandwidth", "vbw", "video bandwidth", "filter bandwidth", "filter width" },
                    response = "Resolution bandwidth, or RBW, defines the frequency filter width used to separate adjacent radio signals. A narrower RBW reveals fine spectral details and lowers the displayed noise floor, while Video Bandwidth smooths display trace noise.",
                    associatedEquipment = EquipmentId.spectrum
                },

                // 21. DANL & Noise Floor
                new KnowledgeEntry
                {
                    id = "danl",
                    title = "DANL & Noise Floor",
                    keywords = new string[] { "danl", "noise floor", "displayed average noise level", "sensitivity", "weak signal", "instrument noise" },
                    response = "DANL, or Displayed Average Noise Level, is the internal thermal noise floor of the spectrum analyzer. Lower DANL gives the instrument higher sensitivity to detect extremely weak 5G signals and hidden spurious emissions.",
                    associatedEquipment = EquipmentId.spectrum
                },

                // 22. Interference Hunting & Clean RF
                new KnowledgeEntry
                {
                    id = "interference",
                    title = "Interference Hunting",
                    keywords = new string[] { "interference hunting", "spurious", "harmonics", "unwanted signal", "jammer", "clean signal", "interference" },
                    response = "Interference hunting uses the spectrum analyzer and directional antennas to locate unwanted radio noise from faulty electrical repeaters, unlicensed transmitters, or cable leakage that degrades 5G user performance.",
                    associatedEquipment = EquipmentId.spectrum
                },

                // 23. Error Vector Magnitude (EVM)
                new KnowledgeEntry
                {
                    id = "evm",
                    title = "Error Vector Magnitude (EVM)",
                    keywords = new string[] { "evm", "error vector magnitude", "signal quality", "distortion", "acpr", "signal integrity" },
                    response = "Error Vector Magnitude measures modulation accuracy by comparing transmitted signal constellations against mathematically perfect ideals. Low EVM ensures that data packets are decoded without bit errors or retransmissions.",
                    associatedEquipment = EquipmentId.spectrum
                },

                // 24. Frequency Bands: Sub-6 GHz vs mmWave
                new KnowledgeEntry
                {
                    id = "freq_bands",
                    title = "Sub-6 GHz (FR1) vs mmWave (FR2)",
                    keywords = new string[] { "mmwave", "millimeter wave", "sub-6", "sub 6", "frequency", "frequencies", "ghz", "hertz", "bands", "c-band", "fr1", "fr2", "wavelength" },
                    response = "5G utilizes Sub-6 gigahertz for broad coverage that penetrates walls easily, and millimeter wave above twenty-four gigahertz for blistering multi-gigabit speeds over shorter distances."
                },

                // 25. C-Band (n78, 3.5 GHz)
                new KnowledgeEntry
                {
                    id = "c_band",
                    title = "C-Band (n78, 3.5 GHz)",
                    keywords = new string[] { "c-band", "c band", "n78", "3.5 ghz", "mid-band", "mid band", "golden band" },
                    response = "C-Band, particularly band n78 around three point five gigahertz, is known as the golden spectrum for 5G. It strikes the perfect balance between multi-hundred-megabit throughput and wide geographic coverage."
                },

                // 26. Low-Band (n28, 700 MHz)
                new KnowledgeEntry
                {
                    id = "low_band",
                    title = "Low-Band 5G (n28, 700 MHz)",
                    keywords = new string[] { "low-band", "low band", "n28", "700 mhz", "rural", "deep coverage" },
                    response = "Low-band 5G below one gigahertz, like the seven hundred megahertz band n28, covers enormous distances and penetrates deep inside buildings and rural areas, providing reliable nationwide baseline coverage."
                },

                // 27. mmWave Path Loss & Rain Fade
                new KnowledgeEntry
                {
                    id = "mmwave_propagation",
                    title = "mmWave Propagation & Rain Fade",
                    keywords = new string[] { "rain fade", "path loss", "foliage", "atmospheric absorption", "oxygen absorption", "blockage", "penetration loss" },
                    response = "Millimeter wave frequencies suffer from severe atmospheric absorption and cannot penetrate brick walls or tree foliage. 5G overcomes this by using high-gain beamforming, intelligent reflection surfaces, and dense small cell nodes."
                },

                // 28. Dynamic Spectrum Sharing (DSS)
                new KnowledgeEntry
                {
                    id = "dss",
                    title = "Dynamic Spectrum Sharing (DSS)",
                    keywords = new string[] { "dss", "dynamic spectrum sharing", "share spectrum", "coexistence", "4g 5g share" },
                    response = "Dynamic Spectrum Sharing allows mobile operators to run 4G LTE and 5G NR simultaneously across the exact same frequency band, dynamically allocating radio resources symbol by symbol based on live user demand."
                },

                // 29. Carrier Aggregation (CA)
                new KnowledgeEntry
                {
                    id = "carrier_agg",
                    title = "Carrier Aggregation",
                    keywords = new string[] { "carrier aggregation", "ca", "component carrier", "aggregate carriers", "bond frequencies", "wide channel" },
                    response = "Carrier aggregation combines multiple individual frequency carriers into one wide virtual data pipe. This boosts peak download speeds and allows devices to receive data over Sub-6 and millimeter wave bands at the same time."
                },

                // 30. TDD vs FDD
                new KnowledgeEntry
                {
                    id = "tdd_vs_fdd",
                    title = "TDD vs FDD",
                    keywords = new string[] { "tdd", "fdd", "time division duplex", "frequency division duplex", "duplexing", "reciprocity", "channel reciprocity" },
                    response = "5G heavily favors Time Division Duplexing because uplink and downlink share the same frequency at different time slots. This channel reciprocity allows the gNodeB to calculate beamforming weights accurately without device feedback overhead."
                },

                // 31. Channel Coding: LDPC & Polar Codes
                new KnowledgeEntry
                {
                    id = "channel_coding",
                    title = "LDPC & Polar Codes",
                    keywords = new string[] { "ldpc", "polar code", "channel coding", "error correction", "turbo code", "parity check" },
                    response = "5G New Radio replaced 4G Turbo codes with Low-Density Parity-Check codes for data channels to support multi-gigabit decoding speeds, and Polar codes for control channels to achieve maximum mathematical reliability."
                },

                // 32. Standalone (SA) vs Non-Standalone (NSA)
                new KnowledgeEntry
                {
                    id = "sa_vs_nsa",
                    title = "Standalone (SA) vs Non-Standalone (NSA)",
                    keywords = new string[] { "standalone", "non-standalone", "sa vs nsa", "nsa", "option 3x", "option 2", "pure 5g", "core difference" },
                    response = "Non-Standalone 5G anchors control signaling to an existing 4G LTE core while adding 5G radio speed. Standalone 5G uses a dedicated cloud-native 5G Core, unlocking true sub-millisecond latency, network slicing, and full edge capabilities."
                },

                // 33. Dual Connectivity (EN-DC)
                new KnowledgeEntry
                {
                    id = "dual_connectivity",
                    title = "Dual Connectivity (EN-DC)",
                    keywords = new string[] { "dual connectivity", "en-dc", "mr-dc", "e-utra-nr" },
                    response = "E-UTRA-NR Dual Connectivity connects a smartphone to both a 4G LTE anchor cell and a 5G gNodeB carrier simultaneously, delivering aggregate throughput and smooth fallback coverage."
                },

                // 34. 5G vs 4G LTE Comparison
                new KnowledgeEntry
                {
                    id = "fiveg_vs_fourg",
                    title = "5G vs 4G LTE Comparison",
                    keywords = new string[] { "4g", "lte", "difference between 4g", "better than 4g", "upgrade from 4g", "vs 4g", "compare 4g", "speed difference" },
                    response = "Compared to 4G LTE, 5G offers up to one hundred times higher data rates, ten times lower latency down to one millisecond, and supports one million devices per square kilometer compared to 4G's one hundred thousand."
                },

                // 35. 5G vs Wi-Fi 6/7
                new KnowledgeEntry
                {
                    id = "fiveg_vs_wifi",
                    title = "5G vs Wi-Fi 6 / 7",
                    keywords = new string[] { "wifi", "wi-fi", "wifi 6", "wifi 7", "5g vs wifi", "wlan" },
                    response = "While Wi-Fi 6 excels at high-speed indoor local networking, 5G provides seamless outdoor mobility, guaranteed Quality of Service over licensed spectrum, and native support for high-speed vehicular handovers."
                },

                // 36. 5G-Advanced & 6G Roadmap
                new KnowledgeEntry
                {
                    id = "fiveg_advanced",
                    title = "5G-Advanced & 6G Roadmap",
                    keywords = new string[] { "5g-advanced", "6g", "release 18", "release 19", "future of 5g", "evolution", "roadmap" },
                    response = "3GPP Release 18 inaugurates 5G-Advanced, introducing native machine learning into the radio air interface, integrated sensing and communication, and ambient IoT, building the bridge toward 6G in 2030."
                },

                // 37. Network Slicing
                new KnowledgeEntry
                {
                    id = "network_slicing",
                    title = "Network Slicing",
                    keywords = new string[] { "slice", "slicing", "virtual network", "network slice", "s-nssai", "sst", "sd", "partition" },
                    response = "Network slicing allows operators to divide a single physical 5G network into multiple virtual, end-to-end networks customized for specific needs, such as autonomous vehicles or smart energy grids."
                },

                // 38. The Three Pillars (eMBB, URLLC, mMTC)
                new KnowledgeEntry
                {
                    id = "three_pillars",
                    title = "eMBB, URLLC & mMTC Pillars",
                    keywords = new string[] { "embb", "urllc", "mmtc", "three pillars", "use case categories", "triad" },
                    response = "5G defines three fundamental pillars: Enhanced Mobile Broadband for multi-gigabit entertainment, Ultra-Reliable Low Latency for surgical robotics and self-driving cars, and Massive Machine-Type Communications for millions of IoT sensors."
                },

                // 39. Quality of Service (5QI)
                new KnowledgeEntry
                {
                    id = "qos_5qi",
                    title = "5G QoS Identifier (5QI)",
                    keywords = new string[] { "5qi", "qos", "quality of service", "delay budget", "packet loss rate", "guaranteed bit rate" },
                    response = "5G QoS Identifiers, or 5QI, assign strict latency budgets and packet priority to individual data streams, ensuring life-critical telemetry and emergency calls are never delayed by background downloads."
                },

                // 40. Security: SUPI vs SUCI
                new KnowledgeEntry
                {
                    id = "security_suci",
                    title = "Security & SUCI Encryption",
                    keywords = new string[] { "security", "supi", "suci", "imsi", "eavesdropping", "stingray", "imsi catcher", "encryption", "5g-aka", "privacy" },
                    response = "In 5G, subscriber identity is encrypted on the device before transmission over the air as a Subscription Concealed Identifier, or SUCI. This completely stops rogue cell towers and IMSI-catchers from tracking users."
                },

                // 41. Autonomous Vehicles & V2X
                new KnowledgeEntry
                {
                    id = "v2x_autonomous",
                    title = "Autonomous Vehicles & V2X",
                    keywords = new string[] { "v2x", "c-v2x", "autonomous", "self driving", "car", "vehicle", "platooning", "traffic safety" },
                    response = "Cellular V2X allows self-driving cars to exchange speed, braking, and hazard data with other vehicles and traffic signals in under one millisecond, reacting far faster than human drivers to prevent collisions."
                },

                // 42. Remote Healthcare & Tele-Surgery
                new KnowledgeEntry
                {
                    id = "telesurgery",
                    title = "Remote Tele-Surgery & Healthcare",
                    keywords = new string[] { "surgery", "tele-surgery", "medical", "doctor", "healthcare", "hospital", "haptic" },
                    response = "5G enables remote tele-surgery by transmitting high-definition 3D video alongside real-time haptic force feedback with sub-millisecond latency, allowing surgeons to operate robotic arms across continents without delay."
                },

                // 43. Private 5G & Industry 4.0
                new KnowledgeEntry
                {
                    id = "private_5g",
                    title = "Private 5G & Industry 4.0",
                    keywords = new string[] { "private 5g", "industry 4.0", "smart factory", "manufacturing", "industrial", "non-public network", "npn", "agv" },
                    response = "Private 5G networks provide dedicated, high-security on-premise wireless coverage for factories, replacing cables to orchestrate fleets of automated guided vehicles, robotic arms, and AI quality scanners."
                },

                // 44. Smart Cities & IoT
                new KnowledgeEntry
                {
                    id = "smart_city_iot",
                    title = "Smart Cities & Massive IoT",
                    keywords = new string[] { "smart city", "smart cities", "iot", "internet of things", "sensors", "smart grid", "metering" },
                    response = "5G Massive Machine-Type Communications connects up to one million battery-powered sensors per square kilometer, enabling intelligent traffic management, smart electrical grids, and automated environmental monitoring."
                },

                // 45. AR, VR & Cloud XR
                new KnowledgeEntry
                {
                    id = "ar_vr_cloudxr",
                    title = "AR, VR & Cloud XR",
                    keywords = new string[] { "ar", "vr", "xr", "virtual reality", "augmented reality", "cloud xr", "metaverse", "headset", "motion to photon" },
                    response = "5G offloads heavy 3D rendering from headsets to local MEC GPU servers, streaming photorealistic VR with motion-to-photon latency under twenty milliseconds for a comfortable, wire-free immersion."
                },

                // 46. Radiation, Health & Safety
                new KnowledgeEntry
                {
                    id = "radiation_safety",
                    title = "Radiation & Health Safety",
                    keywords = new string[] { "radiation", "harmful", "cancer", "health", "safe", "safety", "sar", "is 5g dangerous", "waves dangerous", "khatra" },
                    response = "5G uses non-ionizing radiofrequency radiation that does not possess enough energy to damage DNA or cellular structures. Global health authorities like the World Health Organization and ICNIRP confirm that 5G operates safely within international guidelines."
                },

                // 47. Battery Consumption & Power Efficiency
                new KnowledgeEntry
                {
                    id = "battery_drain",
                    title = "Battery Drain & Energy Efficiency",
                    keywords = new string[] { "battery", "drain", "power consumption", "battery life", "phone hot", "heating" },
                    response = "Initial 5G battery drain was caused by early modems running dual 4G and 5G radios simultaneously in NSA mode. Modern Standalone 5G chipsets feature micro-sleep states and wake-up signals that match 4G battery efficiency."
                },

                // 48. Indoor Building Coverage & Penetration
                new KnowledgeEntry
                {
                    id = "indoor_coverage",
                    title = "Indoor Building Coverage",
                    keywords = new string[] { "indoor", "wall", "penetration", "building", "weak signal indoor", "inside house" },
                    response = "High-frequency 5G radio waves suffer higher penetration loss through modern insulated glass and concrete walls. Telecom operators resolve this by deploying mid-band spectrum, indoor small cells, and distributed antenna systems."
                },

                // 49. SIM Card & USIM Requirements
                new KnowledgeEntry
                {
                    id = "sim_cards",
                    title = "SIM Card & USIM Requirements",
                    keywords = new string[] { "sim card", "usim", "esim", "new sim", "need new sim", "sim change" },
                    response = "Most existing 4G LTE SIM cards support 5G Non-Standalone data, but a modern 5G USIM or eSIM is required to activate Standalone security features like SUCI encryption and network slice profiles."
                },

                // 50. Small Cells vs Macro Towers
                new KnowledgeEntry
                {
                    id = "small_cells",
                    title = "Small Cells vs Macro Towers",
                    keywords = new string[] { "small cell", "macro cell", "micro cell", "pico cell", "light pole", "dense network" },
                    response = "Macro towers provide broad coverage over several square miles, while compact small cells mounted on streetlights and walls provide high-capacity millimeter wave coverage in bustling areas like airports and stadiums."
                },

                // 51. Doppler Shift & High-Speed Mobility
                new KnowledgeEntry
                {
                    id = "doppler_effect",
                    title = "Doppler Shift in High-Speed Mobility",
                    keywords = new string[] { "doppler", "high speed train", "train", "fast moving", "doppler shift", "bullet train" },
                    response = "In high-speed trains traveling at hundreds of kilometers per hour, Doppler shift distorts carrier frequencies. 5G overcomes this by using wider subcarrier spacing, dynamic frequency tracking, and distributed antenna arrays along track corridors."
                },

                // 52. Seamless Handover
                new KnowledgeEntry
                {
                    id = "handover",
                    title = "Seamless Handover",
                    keywords = new string[] { "handover", "handoff", "mobility", "moving between towers", "cell change", "beam switching", "dropped call" },
                    response = "5G executes seamless handovers in milliseconds by maintaining dual-radio connections with the old and new base stations simultaneously, guaranteeing zero packet drops during active voice calls or VR streams."
                },

                // 53. 5G Spectrum in India & Global Auctions
                new KnowledgeEntry
                {
                    id = "spectrum_india",
                    title = "5G Spectrum in India",
                    keywords = new string[] { "india", "frequency in india", "spectrum auction", "jio", "airtel", "trai", "dot" },
                    response = "In India, 5G operates primarily on three point five gigahertz band n78 for high speeds, seven hundred megahertz band n28 for deep rural reach, and twenty-six gigahertz millimeter wave for enterprise hotspots."
                }
            };
        }
    }
}
