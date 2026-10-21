using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FiveGLab.VoiceAgent.Editor
{
    public static class KnowledgeAssetGenerator
    {
        [MenuItem("5G Lab/Generate Knowledge & Config Assets")]
        public static void GenerateAssets()
        {
            string assetDir = "Assets/Resources/VoiceAgent";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(assetDir))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "VoiceAgent");
            }

            // 1. Generate LabKnowledge.asset
            string knowledgePath = $"{assetDir}/LabKnowledge.asset";
            LabKnowledgeBase knowledge = AssetDatabase.LoadAssetAtPath<LabKnowledgeBase>(knowledgePath);
            if (knowledge == null)
            {
                knowledge = ScriptableObject.CreateInstance<LabKnowledgeBase>();
                AssetDatabase.CreateAsset(knowledge, knowledgePath);
            }

            knowledge.systemStory = "User Equipment (phones, headsets) connects wirelessly to the gNodeB base station using 5G NR beams. The gNodeB routes signals through high-speed fiber and optical multiplexing to the MEC-Core, where Baseband Units (BBU) and edge servers process compute-heavy workloads in under one millisecond. The RF Spectrum Analyzer continuously monitors and diagnoses radio emissions to eliminate interference and ensure flawless signal integrity.";

            knowledge.equipmentList = new List<EquipmentData>
            {
                new EquipmentData
                {
                    id = EquipmentId.gnodeb,
                    displayName = "gNodeB (5G Base Station / Massive MIMO Antenna)",
                    uiTitle = "gNodeB (BASE STATION)",
                    oneLiner = "Physical radio access node connecting user devices to the 5G core with massive MIMO and active beamforming.",
                    role = "The 5G Radio Access Network (RAN) node.",
                    howItWorks = "Massive MIMO utilizes a large array of antenna elements transmitting and receiving simultaneously to serve multiple users on the same time-frequency resources via spatial multiplexing.\n\nActive beamforming adjusts signal phases and amplitudes to concentrate radio energy into tight, directed beams aimed directly at target user equipment rather than broadcasting in all directions.\n\nThis dramatically improves Signal-to-Noise Ratio (SNR), conserves RF power, increases cell capacity, and mitigates interference, which is particularly essential for overcoming the high path loss of mmWave frequencies.",
                    keySpecs = new []
                    {
                        "Interface: 5G NR (New Radio)",
                        "Bands: Sub-6 GHz and mmWave",
                        "Antenna: Massive MIMO multi-element array",
                        "Technology: Active Beamforming",
                        "Status: ONLINE - Transmitting"
                    },
                    analogies = new []
                    {
                        "Like a focused flashlight with an adjustable lens rather than a bare lightbulb radiating in all directions."
                    },
                    commonQuestions = new []
                    {
                        new QuestionAnswerPair { question = "What is beamforming?", answer = "Beamforming coordinates multiple antenna elements so their waves combine constructively toward your device, delivering faster speeds and less interference." },
                        new QuestionAnswerPair { question = "What does gNodeB mean?", answer = "It stands for next-generation Node B, which is the standard telecom industry term for a 5G cellular base station." }
                    },
                    relatedEquipment = new [] { EquipmentId.mec, EquipmentId.spectrum },
                    quizQuestions = new []
                    {
                        new QuizQuestion { question = "What does active beamforming do to radio signals?", acceptedKeywords = new [] { "focus", "direct", "target", "narrow", "beam", "aim", "concentrate" }, explanation = "Beamforming focuses radio energy into narrow directed beams aimed specifically at active user devices." },
                        new QuizQuestion { question = "What does the 'g' in gNodeB stand for?", acceptedKeywords = new [] { "next generation", "next-gen", "generation", "next gen", "new" }, explanation = "The 'g' represents next-Generation Node B, the 3GPP designation for 5G base stations." }
                    }
                },
                new EquipmentData
                {
                    id = EquipmentId.mec,
                    displayName = "Mobile Edge Computing (MEC-Core) Server Rack",
                    uiTitle = "MOBILE EDGE COMPUTING (MEC-CORE)",
                    oneLiner = "Local edge data center with BBU and optical multiplexing delivering ultra-low latency compute under 1 millisecond.",
                    role = "Local edge processing and baseband aggregation.",
                    howItWorks = "By placing compute, GPU/CPU resources, and the Baseband Unit (BBU) right at the cell tower or local facility, network packets no longer need to travel hundreds of miles across public backhaul to distant cloud data centers.\n\nThe onboard optical multiplexer consolidates multiple high-capacity optical carrier streams onto fiber links.\n\nThis guarantees round-trip latencies below 1 ms, unlocking mission-critical Ultra-Reliable Low-Latency Communication (URLLC) applications such as remote surgery, autonomous driving, AR/VR streaming, and smart factory robotics.",
                    keySpecs = new []
                    {
                        "Compute: High-Performance GPU/CPU blade servers",
                        "Processing: Baseband Unit (BBU) signal processing",
                        "Interconnect: 100 Gbps Optical Multiplexer (MUX)",
                        "Latency: Ultra-Low (< 1 ms)",
                        "Status: ONLINE - Processing"
                    },
                    analogies = new []
                    {
                        "Like having a full kitchen inside your neighborhood restaurant instead of shipping every meal from a central kitchen across the state."
                    },
                    commonQuestions = new []
                    {
                        new QuestionAnswerPair { question = "Why is latency under 1 millisecond important?", answer = "Sub-millisecond latency enables real-time feedback loops required for safety-critical robotics, automated vehicles, and responsive VR interactions." },
                        new QuestionAnswerPair { question = "What is inside this rack?", answer = "It houses high-density baseband compute processors, edge servers, and optical multiplexing hardware for fiber transmission." }
                    },
                    relatedEquipment = new [] { EquipmentId.gnodeb, EquipmentId.spectrum },
                    quizQuestions = new []
                    {
                        new QuizQuestion { question = "Why do we place MEC servers at the edge of the network?", acceptedKeywords = new [] { "latency", "closer", "speed", "real-time", "bandwidth", "local", "fast" }, explanation = "Placing servers at the edge cuts latency to under 1 millisecond and saves backhaul network bandwidth." },
                        new QuizQuestion { question = "Name one real-world use case that requires sub-millisecond latency.", acceptedKeywords = new [] { "autonomous", "driving", "surgery", "robotics", "vr", "ar", "gaming", "car", "vehicle" }, explanation = "Use cases like autonomous driving, remote surgery, and industrial robotics demand instantaneous response times." }
                    }
                },
                new EquipmentData
                {
                    id = EquipmentId.spectrum,
                    displayName = "RF Spectrum Analyzer",
                    uiTitle = "SPECTRUM ANALYZER",
                    oneLiner = "Precision test bench diagnostic tool displaying signal power versus frequency from 2 Hz to 85 GHz.",
                    role = "Radio Frequency diagnostic and compliance verification instrument.",
                    howItWorks = "The spectrum analyzer plots radio frequency amplitude on the vertical Y-axis against frequency on the horizontal X-axis.\n\nIt allows RF engineers to verify that the gNodeB operates strictly within its designated frequency channel, measures occupied channel bandwidth, detects unwanted harmonic distortions, and locates rogue interference.\n\nIts Resolution Bandwidth (RBW) filter adjusts between 1 Hz and 10 MHz: narrower RBW provides razor-sharp spectral resolution and lowers the noise floor at the expense of sweep time, while wider RBW provides rapid sweep updates.",
                    keySpecs = new []
                    {
                        "Frequency Range: 2 Hz to 85 GHz",
                        "Bands Covered: Sub-6 GHz and mmWave 5G bands",
                        "Resolution Bandwidth: 1 Hz to 10 MHz",
                        "Status: ONLINE"
                    },
                    analogies = new []
                    {
                        "Like a musical graphic equalizer that shows every instrument's pitch and volume in real-time."
                    },
                    commonQuestions = new []
                    {
                        new QuestionAnswerPair { question = "What does this device measure?", answer = "It measures signal power across different frequencies to verify clean transmission and detect rogue interference." },
                        new QuestionAnswerPair { question = "What is Resolution Bandwidth (RBW)?", answer = "RBW is the bandpass filter size that determines how clearly the instrument can distinguish closely spaced frequencies." }
                    },
                    relatedEquipment = new [] { EquipmentId.gnodeb, EquipmentId.mec },
                    quizQuestions = new []
                    {
                        new QuizQuestion { question = "What two variables does a spectrum analyzer plot on its screen?", acceptedKeywords = new [] { "amplitude", "power", "frequency", "strength", "dbm", "hz", "magnitude" }, explanation = "A spectrum analyzer plots signal amplitude (power) against frequency." },
                        new QuizQuestion { question = "Which two main 5G frequency regions does this instrument cover?", acceptedKeywords = new [] { "sub-6", "mmwave", "sub 6", "millimeter", "mm-wave" }, explanation = "It covers both Sub-6 GHz and mmWave 5G spectrum bands up to 85 GHz." }
                    }
                }
            };

            knowledge.glossary = new List<GlossaryEntry>
            {
                new GlossaryEntry { term = "5G NR", definition = "5G New Radio, the global 3GPP air interface standard for fifth-generation mobile communications." },
                new GlossaryEntry { term = "UE", definition = "User Equipment, any device like a smartphone, AR headset, or IoT sensor connected to the mobile network." },
                new GlossaryEntry { term = "gNodeB", definition = "Next-Generation Node B, the base station providing cellular radio coverage in 5G NR." },
                new GlossaryEntry { term = "RAN", definition = "Radio Access Network, the radio infrastructure connecting user devices to the core network." },
                new GlossaryEntry { term = "5G Core", definition = "The heart of the 5G system handling authentication, session routing, mobility, and policy management." },
                new GlossaryEntry { term = "eMBB", definition = "Enhanced Mobile Broadband, offering multi-gigabit data throughput for high-bandwidth user experiences." },
                new GlossaryEntry { term = "URLLC", definition = "Ultra-Reliable Low-Latency Communication, delivering guaranteed sub-millisecond latencies for mission-critical tasks." },
                new GlossaryEntry { term = "mMTC", definition = "Massive Machine-Type Communications, supporting millions of connected low-power IoT sensors per square kilometer." },
                new GlossaryEntry { term = "Network Slicing", definition = "Virtual multiplexing that divides a single physical network into isolated virtual pipes tailored for specific applications." },
                new GlossaryEntry { term = "mmWave", definition = "Millimeter Wave frequencies (24 GHz to 100 GHz), providing super-wide bandwidth at shorter propagation distances." },
                new GlossaryEntry { term = "Sub-6", definition = "Mid-band and low-band frequencies below 6 GHz, offering balanced wide-area coverage and high speeds." },
                new GlossaryEntry { term = "Massive MIMO", definition = "Multiple-Input Multiple-Output using dozens of antenna elements to serve many users simultaneously." },
                new GlossaryEntry { term = "Beamforming", definition = "Signal processing technique concentrating radio waves into directed beams pointed toward specific users." },
                new GlossaryEntry { term = "Latency", definition = "The round-trip time required for a packet of data to travel from sender to receiver and back." },
                new GlossaryEntry { term = "Throughput", definition = "The actual volume of data successfully transmitted over a network channel in a given time interval." },
                new GlossaryEntry { term = "Fronthaul", definition = "The high-bandwidth fiber link connecting radio antenna heads (RRUs) to the centralized baseband pool (BBU)." },
                new GlossaryEntry { term = "Backhaul", definition = "The network connection linking the local base station or edge facility back to the central core network." },
                new GlossaryEntry { term = "BBU", definition = "Baseband Unit, digital hardware that processes baseband telecom signals before radio transmission." },
                new GlossaryEntry { term = "Edge Computing", definition = "Executing cloud compute workloads locally near data sources to minimize latency and bandwidth consumption." },
                new GlossaryEntry { term = "SNR", definition = "Signal-to-Noise Ratio, the ratio of desired signal power to background electromagnetic noise power." },
                new GlossaryEntry { term = "RBW", definition = "Resolution Bandwidth, the frequency bandwidth of the analysis filter in a spectrum analyzer." },
                new GlossaryEntry { term = "Interference", definition = "Unwanted electromagnetic energy disrupting or degrading desired radio frequency communications." }
            };

            EditorUtility.SetDirty(knowledge);

            // 2. Generate AgentConfig.asset
            string configPath = $"{assetDir}/AgentConfig.asset";
            AgentConfig config = AssetDatabase.LoadAssetAtPath<AgentConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AgentConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoiceAgent] Generated LabKnowledge and AgentConfig assets in " + assetDir);
        }
    }
}
