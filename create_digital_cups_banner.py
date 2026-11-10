import math
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 4096, 2048
img = Image.new("RGBA", (W, H), (11, 17, 32, 255)) # Deep midnight navy
draw = ImageDraw.Draw(img)

# Fonts
font_path = "C:/Windows/Fonts/segoeuib.ttf"
font_regular = "C:/Windows/Fonts/segoeui.ttf"
font_mono = "C:/Windows/Fonts/consola.ttf"

def get_font(size, bold=True, mono=False):
    path = font_mono if mono else (font_path if bold else font_regular)
    return ImageFont.truetype(path, size)

# 1. Subtle High-Tech Background Grid
grid_col = (20, 32, 54, 180)
for x in range(0, W, 80):
    draw.line([(x, 0), (x, H)], fill=grid_col, width=1)
for y in range(0, H, 80):
    draw.line([(0, y), (W, y)], fill=grid_col, width=1)

# Outer Border Glow
draw.rectangle([(20, 20), (W - 20, H - 20)], outline=(30, 58, 95, 255), width=4)
draw.rectangle([(26, 26), (W - 26, H - 26)], outline=(0, 229, 255, 120), width=2)

# Helper: Rounded Glass Card
def draw_card(box, fill=(15, 23, 42, 230), outline=(51, 65, 85, 255), width=2, r=16):
    draw.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)

# Helper: Pill Badge
def draw_badge(x, y, text, col_bg, col_fg, font_size=28):
    f = get_font(font_size, bold=True)
    bbox = f.getbbox(text)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    pad_x, pad_y = 18, 8
    draw.rounded_rectangle([(x, y), (x + tw + pad_x*2, y + th + pad_y*2)], radius=8, fill=col_bg)
    draw.text((x + pad_x, y + pad_y - 2), text, font=f, fill=col_fg)
    return x + tw + pad_x*2

# ==============================================================================
# HEADER: Institution Crest, Department Title & Main Architecture Header
# ==============================================================================
draw_card([(40, 40), (W - 40, 260)], fill=(13, 20, 38, 250), outline=(0, 229, 255, 140), width=3, r=20)
# Top accent glow
draw.line([(60, 42), (W - 60, 42)], fill=(0, 229, 255, 255), width=4)

# Crest Emblem (Left & Right)
def draw_crest(cx, cy):
    # Burgundy shield with gold rim
    r = 75
    draw.ellipse([(cx - r, cy - r), (cx + r, cy + r)], fill=(128, 0, 32, 255), outline=(255, 215, 0, 255), width=5)
    f_crest = get_font(46, bold=True)
    draw.text((cx - 26, cy - 32), "ti", font=f_crest, fill=(255, 215, 0, 255))
    f_sub = get_font(16, bold=True)
    draw.text((cx - 40, cy + 22), "ESTD. 1956", font=f_sub, fill=(240, 240, 240, 220))

draw_crest(130, 150)
draw_crest(W - 130, 150)

# University & Department Titles
f_dept = get_font(42, bold=True)
dept_text = "DEPARTMENT OF ELECTRONICS AND COMMUNICATION ENGINEERING"
t_bbox = f_dept.getbbox(dept_text)
draw.text(((W - (t_bbox[2] - t_bbox[0])) // 2, 60), dept_text, font=f_dept, fill=(241, 245, 249, 255))

f_uni = get_font(26, bold=False)
uni_text = "THAPAR INSTITUTE OF ENGINEERING & TECHNOLOGY (DEEMED TO BE UNIVERSITY), PATIALA"
u_bbox = f_uni.getbbox(uni_text)
draw.text(((W - (u_bbox[2] - u_bbox[0])) // 2, 115), uni_text, font=f_uni, fill=(148, 163, 184, 255))

# Main Banner Title
f_title = get_font(54, bold=True)
title_text = "5G Architecture based on CUPS"
m_bbox = f_title.getbbox(title_text)
draw.text(((W - (m_bbox[2] - m_bbox[0])) // 2, 160), title_text, font=f_title, fill=(0, 229, 255, 255))

f_subtitle = get_font(22, bold=False, mono=True)
sub_text = "Control and User Plane Separation | 3GPP TS 23.214 / TS 23.501 Rel-15 Service-Based Architecture"
s_bbox = f_subtitle.getbbox(sub_text)
draw.text(((W - (s_bbox[2] - s_bbox[0])) // 2, 222), sub_text, font=f_subtitle, fill=(203, 213, 225, 220))

# ==============================================================================
# ZONE 1: USER EQUIPMENT (UE) — Column on the Left
# ==============================================================================
ue_x0, ue_x1 = 50, 780
draw_card([(ue_x0, 290), (ue_x1, H - 50)], fill=(13, 21, 38, 240), outline=(0, 229, 255, 180), width=3, r=16)

# Header
draw_badge(ue_x0 + 25, 310, "ZONE 1: USER EQUIPMENT (UE)", (0, 229, 255, 40), (0, 229, 255, 255), 24)
f_h = get_font(34, bold=True)
draw.text((ue_x0 + 25, 360), "User Equipment Stack", font=f_h, fill=(248, 250, 252, 255))

# 4 UE Cards
ue_items = [
    ("IoT Devices & Sensors", "Smart Grid Meters, Industrial IoT, eMTC, NB-IoT\nMassive Machine Type Comms (mMTC)", (0, 229, 255)),
    ("Connected Vehicles", "C-V2X Telematics, Autonomous Driving, Platooning\nUltra-Reliable Low-Latency Comms (URLLC <1ms)", (56, 189, 248)),
    ("5G Smartphones", "Sub-6 GHz & mmWave New Radio (NR)\nEnhanced Mobile Broadband (eMBB >1 Gbps)", (14, 165, 233)),
    ("Fixed Wireline & Workstations", "Enterprise LAN, FTTH Broadband, Local Servers\nDeterministic Low-Jitter IP Endpoints", (99, 102, 241))
]

ue_y = 425
for title, desc, col in ue_items:
    draw_card([(ue_x0 + 20, ue_y), (ue_x1 - 20, ue_y + 350)], fill=(18, 28, 48, 255), outline=col, width=2, r=12)
    # Icon Placeholder box
    draw.rounded_rectangle([(ue_x0 + 35, ue_y + 20), (ue_x0 + 115, ue_y + 100)], radius=8, fill=(col[0]//4, col[1]//4, col[2]//4, 255), outline=col, width=2)
    f_ico = get_font(38, bold=True)
    draw.text((ue_x0 + 55, ue_y + 35), "⚡" if "IoT" in title else ("🚗" if "Vehicles" in title else ("📱" if "Phone" in title else "💻")), font=f_ico, fill=col)

    f_it = get_font(28, bold=True)
    draw.text((ue_x0 + 135, ue_y + 28), title, font=f_it, fill=(255, 255, 255, 255))
    
    f_desc = get_font(20, bold=False)
    draw.text((ue_x0 + 135, ue_y + 75), desc, font=f_desc, fill=(148, 163, 184, 255))
    ue_y += 375

# ==============================================================================
# ZONE 2: ACCESS NETWORK (RAN) — Second Column
# ==============================================================================
an_x0, an_x1 = 820, 1620
draw_card([(an_x0, 290), (an_x1, H - 50)], fill=(13, 21, 38, 240), outline=(0, 229, 255, 180), width=3, r=16)

draw_badge(an_x0 + 25, 310, "ACCESS NETWORK (RAN)", (0, 229, 255, 40), (0, 229, 255, 255), 24)
draw.text((an_x0 + 25, 360), "Heterogeneous Radios", font=f_h, fill=(248, 250, 252, 255))

an_items = [
    ("5G BS (gNodeB)", "3GPP 5G New Radio (NR) Base Station\nMassive MIMO (64T64R), Active Beamforming\nDual Connectivity & Carrier Aggregation", (0, 229, 255)),
    ("4G BS (eNodeB)", "Legacy LTE Advanced Radio Access\nAnchor Carrier for Non-Standalone (NSA Option 3x)\nInter-RAT Mobility with 5G NR", (56, 189, 248)),
    ("Wi-Fi Access (Untrusted)", "Untrusted Non-3GPP WLAN (Wi-Fi 6/6E)\nSecured via N3IWF (IPsec Tunnel & NWu Interface)\nSeamless 3GPP Core Convergence", (14, 165, 233)),
    ("Fixed Wireline (W-5GAN)", "Wireline 5G Access Network Gateway\nBroadband Forum (BBF) Fiber & Cable\nConverged User Plane to 5G UPF", (99, 102, 241))
]

an_y = 425
for title, desc, col in an_items:
    draw_card([(an_x0 + 20, an_y), (an_x1 - 20, an_y + 350)], fill=(18, 28, 48, 255), outline=col, width=2, r=12)
    # Icon
    draw.rounded_rectangle([(an_x0 + 35, an_y + 20), (an_x0 + 115, an_y + 100)], radius=8, fill=(col[0]//4, col[1]//4, col[2]//4, 255), outline=col, width=2)
    draw.text((an_x0 + 55, an_y + 35), "📡" if "BS" in title else ("📶" if "Wi-Fi" in title else "🔌"), font=f_ico, fill=col)

    draw.text((an_x0 + 135, an_y + 28), title, font=f_it, fill=(255, 255, 255, 255))
    draw.text((an_x0 + 135, an_y + 75), desc, font=f_desc, fill=(148, 163, 184, 255))
    an_y += 375

# Inter-connect arrows: UE to Access
for i in range(4):
    y_pos = 425 + i * 375 + 160
    draw.line([(ue_x1 - 18, y_pos), (an_x0 + 18, y_pos)], fill=(0, 229, 255, 200), width=3)
    draw.polygon([(an_x0 + 18, y_pos), (an_x0 + 8, y_pos - 6), (an_x0 + 8, y_pos + 6)], fill=(0, 229, 255, 255))

# ==============================================================================
# ZONE 3: 5G CONTROL PLANE (SBA) — Top Right Big Block
# ==============================================================================
cp_x0, cp_x1 = 1660, W - 50
cp_y0, cp_y1 = 290, 1100
draw_card([(cp_x0, cp_y0), (cp_x1, cp_y1)], fill=(20, 24, 38, 245), outline=(255, 213, 79, 200), width=3, r=16)

draw_badge(cp_x0 + 25, cp_y0 + 20, "ZONE 2: 5G CONTROL PLANE (SBA)", (255, 213, 79, 40), (255, 213, 79, 255), 24)
draw.text((cp_x0 + 25, cp_y0 + 65), "Service-Based Architecture (SBA)", font=f_h, fill=(255, 236, 179, 255))

f_subcp = get_font(20, bold=False, mono=True)
draw.text((cp_x0 + 560, cp_y0 + 75), "Cloud-Native RESTful Microservices via HTTP/2 JSON APIs", font=f_subcp, fill=(255, 213, 79, 200))

# Horizontal SBA Bus Line
bus_y = cp_y0 + 150
draw.line([(cp_x0 + 50, bus_y), (cp_x1 - 50, bus_y)], fill=(255, 213, 79, 255), width=6)
draw.text((cp_x1 - 280, bus_y - 30), "Service-Based Bus", font=get_font(20, bold=True, mono=True), fill=(255, 213, 79, 255))

# Microservices: Top Row
top_nfs = [
    ("NSSF", "Network Slice Selection\nAllocates network slices for eMBB/URLLC/mMTC", "Nnssf"),
    ("AUSF", "Authentication Server\n5G-AKA & EAP-AKA' subscriber security", "Nausf"),
    ("UDM", "Unified Data Management\nStores subscriber profile, SIM credentials & keys", "Nudm"),
    ("NRF", "NF Repository Function\nDynamic service discovery & registration", "Nnrf")
]

card_w = (cp_x1 - cp_x0 - 100) // 4
for i, (name, role, iface) in enumerate(top_nfs):
    nx = cp_x0 + 40 + i * (card_w + 8)
    ny = cp_y0 + 190
    draw_card([(nx, ny), (nx + card_w, ny + 240)], fill=(30, 36, 56, 255), outline=(255, 213, 79, 180), width=2, r=10)
    # Bus drop link
    draw.line([(nx + card_w//2, bus_y), (nx + card_w//2, ny)], fill=(255, 213, 79, 220), width=3)
    draw.text((nx + 15, ny + 15), name, font=get_font(32, bold=True), fill=(255, 213, 79, 255))
    draw.text((nx + card_w - 75, ny + 20), iface, font=get_font(18, bold=True, mono=True), fill=(255, 236, 179, 200))
    draw.text((nx + 15, ny + 70), role, font=get_font(18, bold=False), fill=(226, 232, 240, 240))

# Microservices: Bottom Row
bot_nfs = [
    ("AMF", "Access & Mobility Management\nRegistration, connection, mobility, N2 to RAN", "Namf"),
    ("SMF", "Session Management Function\nIP allocation, QoS, governs UPF via N4 (PFCP)", "Nsmf"),
    ("PCF", "Policy Control Function\nUnified policy rules, dynamic QoS & charging", "Npcf"),
    ("AF", "Application Function\nInfluence traffic routing & edge low latency", "Naf")
]

for i, (name, role, iface) in enumerate(bot_nfs):
    nx = cp_x0 + 40 + i * (card_w + 8)
    ny = cp_y0 + 460
    # Highlight AMF & SMF as key CUPS controllers
    is_key = name in ["AMF", "SMF"]
    outline_col = (255, 179, 0) if is_key else (255, 213, 79, 180)
    draw_card([(nx, ny), (nx + card_w, ny + 250)], fill=(36, 44, 68, 255) if is_key else (30, 36, 56, 255), outline=outline_col, width=3 if is_key else 2, r=10)
    # Bus link
    draw.line([(nx + card_w//2, bus_y), (nx + card_w//2, ny)], fill=(255, 213, 79, 220), width=3)
    draw.text((nx + 15, ny + 15), name, font=get_font(34, bold=True), fill=(255, 213, 79, 255))
    draw.text((nx + card_w - 75, ny + 20), iface, font=get_font(18, bold=True, mono=True), fill=(255, 236, 179, 200))
    draw.text((nx + 15, ny + 72), role, font=get_font(18, bold=False), fill=(241, 245, 249, 255))

# N2 Signaling link from AMF to RAN
amf_x = cp_x0 + 40 + card_w // 2
draw.line([(an_x1 - 18, 550), (cp_x0, 550), (amf_x, cp_y0 + 460)], fill=(255, 213, 79, 180), width=3)
draw.text((cp_x0 - 150, 520), "N2 (Signaling)", font=get_font(20, bold=True, mono=True), fill=(255, 213, 79, 255))

# ==============================================================================
# ZONE 4: 5G USER PLANE (CUPS) — Bottom Right Big Block
# ==============================================================================
up_y0, up_y1 = 1140, H - 50
draw_card([(cp_x0, up_y0), (cp_x1, up_y1)], fill=(12, 28, 28, 245), outline=(0, 230, 118, 200), width=3, r=16)

draw_badge(cp_x0 + 25, up_y0 + 20, "ZONE 3: 5G USER PLANE (CUPS)", (0, 230, 118, 40), (0, 230, 118, 255), 24)
draw.text((cp_x0 + 25, up_y0 + 65), "Decoupled High-Speed Data Forwarding (UPF)", font=f_h, fill=(232, 245, 233, 255))
draw.text((cp_x0 + 720, up_y0 + 75), "Control & User Plane Separation enables Local Edge Breakout (<1ms Latency)", font=f_subcp, fill=(0, 230, 118, 220))

# 1. Edge UPF (Local Breakout)
upf1_box = [(cp_x0 + 60, up_y0 + 140), (cp_x0 + 680, up_y0 + 480)]
draw_card(upf1_box, fill=(18, 48, 42, 255), outline=(0, 230, 118, 255), width=3, r=12)
draw.text((upf1_box[0][0] + 25, upf1_box[0][1] + 20), "EDGE UPF (Local Breakout)", font=get_font(32, bold=True), fill=(0, 230, 118, 255))
draw.text((upf1_box[0][0] + 25, upf1_box[0][1] + 75), 
    "• Collocated at Cell Site / MEC Edge Server Rack\n"
    "• Ultra-Reliable Low-Latency Processing (<1 ms)\n"
    "• Direct packet routing to on-premise AI/Cache\n"
    "• Eliminates core network transport backhaul", 
    font=get_font(20, bold=False), fill=(226, 232, 240, 255))

# 2. Local Data Network (Local DN / MEC)
ldn_box = [(cp_x0 + 60, up_y0 + 540), (cp_x0 + 680, up_y0 + 780)]
draw_card(ldn_box, fill=(18, 38, 48, 255), outline=(56, 189, 248, 255), width=2, r=12)
draw.text((ldn_box[0][0] + 25, ldn_box[0][1] + 20), "LOCAL DATA NETWORK (Local DN)", font=get_font(28, bold=True), fill=(56, 189, 248, 255))
draw.text((ldn_box[0][0] + 25, ldn_box[0][1] + 70), 
    "• Mobile Edge Computing (MEC) Server Cluster\n"
    "• Real-time video analytics, AR rendering & robotics\n"
    "• Private campus networks & enterprise intranet", 
    font=get_font(20, bold=False), fill=(203, 213, 225, 240))

# Edge UPF to Local DN arrow
draw.line([(cp_x0 + 370, upf1_box[1][1]), (cp_x0 + 370, ldn_box[0][1])], fill=(0, 230, 118, 255), width=5)
draw.polygon([(cp_x0 + 370, ldn_box[0][1]), (cp_x0 + 360, ldn_box[0][1] - 12), (cp_x0 + 380, ldn_box[0][1] - 12)], fill=(0, 230, 118, 255))
draw.text((cp_x0 + 385, (upf1_box[1][1] + ldn_box[0][1]) // 2 - 12), "N6 (Local)", font=get_font(20, bold=True, mono=True), fill=(0, 230, 118, 255))

# 3. Central UPF (Core Network)
upf2_box = [(cp_x0 + 950, up_y0 + 140), (cp_x0 + 1550, up_y0 + 480)]
draw_card(upf2_box, fill=(18, 48, 42, 255), outline=(0, 230, 118, 255), width=3, r=12)
draw.text((upf2_box[0][0] + 25, upf2_box[0][1] + 20), "CENTRAL UPF (Regional Core)", font=get_font(32, bold=True), fill=(0, 230, 118, 255))
draw.text((upf2_box[0][0] + 25, upf2_box[0][1] + 75), 
    "• High-throughput aggregate data forwarding\n"
    "• Deep packet inspection (DPI) & charging\n"
    "• QoS flow enforcement & rate shaping\n"
    "• Regional operator backbone gateway", 
    font=get_font(20, bold=False), fill=(226, 232, 240, 255))

# 4. Central Data Network (Internet / Cloud)
cdn_box = [(cp_x0 + 1720, up_y0 + 140), (cp_x1 - 60, up_y0 + 480)]
draw_card(cdn_box, fill=(24, 32, 54, 255), outline=(129, 140, 248, 255), width=2, r=12)
draw.text((cdn_box[0][0] + 25, cdn_box[0][1] + 20), "DATA NETWORK (DN)", font=get_font(28, bold=True), fill=(129, 140, 248, 255))
draw.text((cdn_box[0][0] + 25, cdn_box[0][1] + 70), 
    "• Public Internet\n"
    "• Cloud Service Providers\n"
    "• Operator IMS & VoNR Voice\n"
    "• Third-party Content Delivery", 
    font=get_font(20, bold=False), fill=(203, 213, 225, 240))

# Central UPF to Data Network arrow
draw.line([(upf2_box[1][0], up_y0 + 310), (cdn_box[0][0], up_y0 + 310)], fill=(0, 230, 118, 255), width=5)
draw.polygon([(cdn_box[0][0], up_y0 + 310), (cdn_box[0][0] - 14, up_y0 + 300), (cdn_box[0][0] - 14, up_y0 + 320)], fill=(0, 230, 118, 255))
draw.text((upf2_box[1][0] + 40, up_y0 + 270), "N6 (WAN)", font=get_font(22, bold=True, mono=True), fill=(0, 230, 118, 255))

# Inter-UPF link (N9 interface)
draw.line([(upf1_box[1][0], up_y0 + 310), (upf2_box[0][0], up_y0 + 310)], fill=(0, 230, 118, 255), width=5)
draw.polygon([(upf2_box[0][0], up_y0 + 310), (upf2_box[0][0] - 14, up_y0 + 300), (upf2_box[0][0] - 14, up_y0 + 320)], fill=(0, 230, 118, 255))
draw.text((upf1_box[1][0] + 80, up_y0 + 270), "N9 (Inter-UPF)", font=get_font(22, bold=True, mono=True), fill=(0, 230, 118, 255))

# RAN to UPF Data Path (N3 Interface)
ran_out_y = 650
draw.line([(an_x1, ran_out_y), (cp_x0 - 40, ran_out_y), (cp_x0 - 40, up_y0 + 310), (upf1_box[0][0], up_y0 + 310)], fill=(0, 230, 118, 255), width=5)
draw.polygon([(upf1_box[0][0], up_y0 + 310), (upf1_box[0][0] - 14, up_y0 + 300), (upf1_box[0][0] - 14, up_y0 + 320)], fill=(0, 230, 118, 255))
draw.text((an_x1 + 30, ran_out_y - 30), "N3 / GTP-U (User Data Payload)", font=get_font(22, bold=True, mono=True), fill=(0, 230, 118, 255))

# ==============================================================================
# THE CUPS DECOUPLING CONDUIT: N4 Interface (SMF to UPF)
# ==============================================================================
# SMF is at nx = cp_x0 + 40 + 1 * (card_w + 8) + card_w // 2
smf_cx = cp_x0 + 40 + card_w + 8 + card_w // 2
smf_bottom_y = cp_y0 + 460 + 250
draw.line([(smf_cx, smf_bottom_y), (smf_cx, up_y0 + 140)], fill=(255, 110, 64, 255), width=6)
# Arrow pointing to UPF
draw.polygon([(smf_cx, up_y0 + 140), (smf_cx - 14, up_y0 + 120), (smf_cx + 14, up_y0 + 120)], fill=(255, 110, 64, 255))

# N4 Interface Badge
n4_text = " N4 Interface: Packet Forwarding Control Protocol (PFCP) "
f_n4 = get_font(22, bold=True, mono=True)
n4_bbox = f_n4.getbbox(n4_text)
n4_w = n4_bbox[2] - n4_bbox[0]
draw_card([(smf_cx - n4_w//2 - 10, (smf_bottom_y + up_y0 + 140)//2 - 24), (smf_cx + n4_w//2 + 10, (smf_bottom_y + up_y0 + 140)//2 + 24)],
          fill=(36, 18, 12, 255), outline=(255, 110, 64, 255), width=2, r=8)
draw.text((smf_cx - n4_w//2, (smf_bottom_y + up_y0 + 140)//2 - 14), n4_text, font=f_n4, fill=(255, 171, 145, 255))

# ==============================================================================
# FOOTER CALLOUT BADGE
# ==============================================================================
foot_text = "CUPS (Control and User Plane Separation): 3GPP Rel-15 allows control signaling and user payload forwarding to scale completely independently."
f_foot = get_font(22, bold=True)
draw.text((cp_x0 + 40, up_y1 - 45), foot_text, font=f_foot, fill=(148, 163, 184, 255))

# Save output
out_png = "D:/5G-Lab-Room/Assets/equipments/cups_banner_clean.png"
img.save(out_png, "PNG", quality=100)
print(f"Generated 4K digital master: {out_png}, size={os.path.getsize(out_png)} bytes")
