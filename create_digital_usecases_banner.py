import math
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 3840, 2160
img = Image.new("RGBA", (W, H), (255, 255, 255, 255))
draw = ImageDraw.Draw(img)

# Windows System Fonts
font_bold = "C:/Windows/Fonts/segoeuib.ttf"
font_regular = "C:/Windows/Fonts/segoeui.ttf"
font_mono = "C:/Windows/Fonts/consola.ttf"

def get_font(size, bold=True, mono=False):
    path = font_mono if mono else (font_bold if bold else font_regular)
    try:
        return ImageFont.truetype(path, size)
    except:
        return ImageFont.load_default()

# High-Visibility Academic & Cybernetic Color Palette
col_bg = (255, 255, 255, 255)
col_navy_header = (16, 28, 56, 255)        # Deep Navy Header
col_navy_accent = (26, 54, 93, 255)        # Secondary Navy
col_crimson = (140, 20, 45, 255)           # University Crimson
col_gold = (218, 165, 32, 255)             # Gold Accent
col_text_dark = (15, 23, 42, 255)          # Deep Charcoal Text
col_text_sub = (71, 85, 105, 255)          # Slate Subtitle Text
col_card_bg = (255, 255, 255, 255)         # Card pure white
col_shadow = (210, 215, 225, 180)          # Soft Drop Shadow

# 3 Pillars Color Themes
# Tier 1: eMBB (Cyan / Teal)
col_embb_main = (0, 160, 175, 255)
col_embb_bg = (235, 248, 250, 255)
col_embb_border = (64, 185, 200, 255)

# Tier 2: mMTC (Emerald / Green)
col_mmtc_main = (34, 145, 75, 255)
col_mmtc_bg = (238, 250, 240, 255)
col_mmtc_border = (60, 180, 100, 255)

# Tier 3: uRLLC (Coral / Crimson)
col_urllc_main = (220, 50, 40, 255)
col_urllc_bg = (254, 242, 241, 255)
col_urllc_border = (235, 85, 75, 255)

# Outer Double Border
draw.rectangle([(16, 16), (W - 16, H - 16)], outline=(148, 163, 184, 255), width=5)
draw.rectangle([(24, 24), (W - 24, H - 24)], outline=(226, 232, 240, 255), width=3)

# ==============================================================================
# 1. TOP HEADER BAR: University Crests & Department Title
# ==============================================================================
header_y0, header_y1 = 40, 195
draw.rectangle([(40, header_y0), (W - 40, header_y1)], fill=col_navy_header)

def draw_thapar_crest(cx, cy):
    r = 65
    draw.ellipse([(cx - r, cy - r), (cx + r, cy + r)], fill=col_crimson, outline=col_gold, width=4)
    draw.ellipse([(cx - r + 8, cy - r + 8), (cx + r - 8, cy + r - 8)], outline=col_gold, width=2)
    f_ti = get_font(50, bold=True)
    draw.text((cx - 24, cy - 34), "ti", font=f_ti, fill=col_gold)
    f_sub = get_font(12, bold=True)
    draw.text((cx - 34, cy + 22), "ESTD. 1956", font=f_sub, fill=(255, 255, 255, 240))

draw_thapar_crest(135, (header_y0 + header_y1) // 2)
draw_thapar_crest(W - 135, (header_y0 + header_y1) // 2)

f_dept = get_font(42, bold=True)
dept_text = "DEPARTMENT OF ELECTRONICS AND COMMUNICATION ENGINEERING"
t_bbox = f_dept.getbbox(dept_text)
draw.text(((W - (t_bbox[2] - t_bbox[0])) // 2, header_y0 + 26), dept_text, font=f_dept, fill=(255, 255, 255, 255))

f_inst = get_font(24, bold=False)
inst_text = "THAPAR INSTITUTE OF ENGINEERING & TECHNOLOGY (DEEMED TO BE UNIVERSITY), PATIALA"
i_bbox = f_inst.getbbox(inst_text)
draw.text(((W - (i_bbox[2] - i_bbox[0])) // 2, header_y0 + 90), inst_text, font=f_inst, fill=(226, 232, 240, 255))

# ==============================================================================
# 2. MAIN TITLE: Summary of 5G Use Cases
# ==============================================================================
title_y = 220
f_title = get_font(54, bold=True)
title_text = "Summary of 5G Use Cases"
m_bbox = f_title.getbbox(title_text)
t_w = m_bbox[2] - m_bbox[0]
t_x = (W - t_w) // 2
draw.text((t_x, title_y), title_text, font=f_title, fill=col_text_dark)

# Accent line under title
draw.line([(t_x - 30, title_y + 68), (t_x + t_w + 30, title_y + 68)], fill=col_navy_accent, width=5)

f_subtitle = get_font(22, bold=False)
sub_text = "ITU-R IMT-2020 & 3GPP Standardized Pillars: eMBB | mMTC | uRLLC"
s_bbox = f_subtitle.getbbox(sub_text)
draw.text(((W - (s_bbox[2] - s_bbox[0])) // 2, title_y + 78), sub_text, font=f_subtitle, fill=col_text_sub)

# ==============================================================================
# 3. COLUMN CLASSIFICATION HEADERS (H2H, H2M, M2M)
# ==============================================================================
col_header_y = 330
left_col_w = 460
spec_col_w = 720
apps_area_x0 = 40 + left_col_w + 30
apps_area_x1 = W - 40 - spec_col_w - 30
total_app_w = apps_area_x1 - apps_area_x0
h_col_w = total_app_w / 3.0

def draw_col_badge(cx, y, title, subtitle):
    w, h = 420, 60
    x0, y0 = cx - w // 2, y
    draw.rounded_rectangle([(x0, y0), (x0 + w, y0 + h)], radius=12, fill=(241, 245, 249, 255), outline=(203, 213, 225, 255), width=2)
    f_t = get_font(22, bold=True)
    tb = f_t.getbbox(title)
    draw.text((cx - (tb[2] - tb[0]) // 2, y0 + 8), title, font=f_t, fill=col_navy_header)
    f_s = get_font(15, bold=False)
    sb = f_s.getbbox(subtitle)
    draw.text((cx - (sb[2] - sb[0]) // 2, y0 + 34), subtitle, font=f_s, fill=col_text_sub)

draw_col_badge(int(apps_area_x0 + h_col_w * 0.5), col_header_y, "Human to Human (H2H)", "Immersive Telepresence & Social VR")
draw_col_badge(int(apps_area_x0 + h_col_w * 1.5), col_header_y, "Human to Machine (H2M)", "Remote Robotics & Teleoperation")
draw_col_badge(int(apps_area_x0 + h_col_w * 2.5), col_header_y, "Machine to Machine (M2M)", "Autonomous Systems & Dense IoT")

# Category Label Header
f_cat_hdr = get_font(22, bold=True)
draw.text((60, col_header_y + 16), "SERVICE CLASS", font=f_cat_hdr, fill=col_navy_header)
# Spec Label Header
draw.text((W - 40 - spec_col_w + 30, col_header_y + 16), "KEY PERFORMANCE SPECS", font=f_cat_hdr, fill=col_navy_header)

# ==============================================================================
# Helper to Draw Application Cards with Icons
# ==============================================================================
def draw_app_card(x0, y0, w, h, title, subtitle, icon_type, theme_col):
    # Drop shadow
    draw.rounded_rectangle([(x0 + 4, y0 + 4), (x0 + w + 4, y0 + h + 4)], radius=16, fill=col_shadow)
    # Card surface
    draw.rounded_rectangle([(x0, y0), (x0 + w, y0 + h)], radius=16, fill=col_card_bg, outline=(226, 232, 240, 255), width=2)
    # Left accent indicator
    draw.rounded_rectangle([(x0, y0), (x0 + 10, y0 + h)], radius=4, fill=theme_col)
    
    # Icon circle
    ic_cx = x0 + 55
    ic_cy = y0 + h // 2
    r = 28
    draw.ellipse([(ic_cx - r, ic_cy - r), (ic_cx + r, ic_cy + r)], fill=(theme_col[0], theme_col[1], theme_col[2], 35))
    
    # Draw simple vector icon based on type
    draw_vector_icon(ic_cx, ic_cy, icon_type, theme_col)
    
    # Text
    tx = x0 + 100
    f_t = get_font(20, bold=True)
    draw.text((tx, y0 + 16), title, font=f_t, fill=col_text_dark)
    f_d = get_font(15, bold=False)
    draw.text((tx, y0 + 46), subtitle, font=f_d, fill=col_text_sub)

def draw_vector_icon(cx, cy, itype, col):
    if itype == "vr":
        # VR Headset
        draw.rounded_rectangle([(cx - 16, cy - 10), (cx + 16, cy + 10)], radius=4, outline=col, width=3)
        draw.line([(cx - 16, cy), (cx - 24, cy)], fill=col, width=2)
        draw.line([(cx + 16, cy), (cx + 24, cy)], fill=col, width=2)
        draw.ellipse([(cx - 7, cy - 4), (cx - 1, cy + 4)], fill=col)
        draw.ellipse([(cx + 1, cy - 4), (cx + 7, cy + 4)], fill=col)
    elif itype == "video":
        # Video Camera
        draw.rounded_rectangle([(cx - 14, cy - 10), (cx + 6, cy + 10)], radius=3, outline=col, width=3)
        draw.polygon([(cx + 8, cy - 8), (cx + 18, cy - 2), (cx + 18, cy + 2), (cx + 8, cy + 8)], fill=col)
    elif itype == "wireless":
        # Antenna / Tower
        draw.line([(cx, cy + 14), (cx, cy - 8)], fill=col, width=3)
        draw.line([(cx - 10, cy + 14), (cx, cy + 2)], fill=col, width=2)
        draw.line([(cx + 10, cy + 14), (cx, cy + 2)], fill=col, width=2)
        draw.arc([(cx - 14, cy - 18), (cx + 14, cy)], 200, 340, fill=col, width=2)
        draw.arc([(cx - 20, cy - 24), (cx + 20, cy + 6)], 200, 340, fill=col, width=2)
    elif itype == "cctv":
        # Security Eye / Camera
        draw.ellipse([(cx - 14, cy - 10), (cx + 14, cy + 10)], outline=col, width=3)
        draw.ellipse([(cx - 5, cy - 5), (cx + 5, cy + 5)], fill=col)
    elif itype == "uhd":
        # 4K Screen
        draw.rectangle([(cx - 15, cy - 11), (cx + 15, cy + 7)], outline=col, width=3)
        draw.line([(cx - 6, cy + 7), (cx - 8, cy + 13)], fill=col, width=2)
        draw.line([(cx + 6, cy + 7), (cx + 8, cy + 13)], fill=col, width=2)
        draw.line([(cx - 12, cy + 13), (cx + 12, cy + 13)], fill=col, width=2)
    elif itype == "cloud":
        # Cloud computing
        draw.ellipse([(cx - 14, cy - 6), (cx + 2, cy + 8)], fill=col)
        draw.ellipse([(cx - 6, cy - 12), (cx + 12, cy + 6)], fill=col)
        draw.ellipse([(cx + 4, cy - 4), (cx + 16, cy + 8)], fill=col)
        draw.rectangle([(cx - 10, cy), (cx + 12, cy + 8)], fill=col)
    elif itype == "wearable":
        # Smartwatch
        draw.rectangle([(cx - 8, cy - 16), (cx + 8, cy + 16)], fill=col)
        draw.rounded_rectangle([(cx - 13, cy - 11), (cx + 13, cy + 11)], radius=4, fill=(255, 255, 255), outline=col, width=3)
        draw.ellipse([(cx - 4, cy - 4), (cx + 4, cy + 4)], fill=col)
    elif itype == "mesh":
        # Social / Sensor Mesh
        draw.ellipse([(cx - 12, cy - 8), (cx - 4, cy)], fill=col)
        draw.ellipse([(cx + 4, cy - 8), (cx + 12, cy)], fill=col)
        draw.ellipse([(cx - 4, cy + 4), (cx + 4, cy + 12)], fill=col)
        draw.line([(cx - 8, cy - 4), (cx + 8, cy - 4)], fill=col, width=2)
        draw.line([(cx - 8, cy - 4), (cx, cy + 8)], fill=col, width=2)
        draw.line([(cx + 8, cy - 4), (cx, cy + 8)], fill=col, width=2)
    elif itype == "smarthome":
        # Smart House
        draw.polygon([(cx, cy - 14), (cx - 14, cy - 2), (cx + 14, cy - 2)], fill=col)
        draw.rectangle([(cx - 11, cy - 2), (cx + 11, cy + 12)], outline=col, width=2)
        draw.rectangle([(cx - 3, cy + 3), (cx + 3, cy + 12)], fill=col)
    elif itype == "health":
        # Heart / Cross
        draw.rectangle([(cx - 4, cy - 12), (cx + 4, cy + 12)], fill=col)
        draw.rectangle([(cx - 12, cy - 4), (cx + 12, cy + 4)], fill=col)
    elif itype == "v2i":
        # Car to infrastructure
        draw.rounded_rectangle([(cx - 12, cy - 4), (cx + 12, cy + 8)], radius=3, outline=col, width=2)
        draw.ellipse([(cx - 9, cy + 7), (cx - 4, cy + 12)], fill=col)
        draw.ellipse([(cx + 4, cy + 7), (cx + 9, cy + 12)], fill=col)
        draw.arc([(cx - 12, cy - 16), (cx + 12, cy - 2)], 210, 330, fill=col, width=2)
    elif itype == "industrial":
        # Gear / Factory
        draw.ellipse([(cx - 10, cy - 10), (cx + 10, cy + 10)], outline=col, width=4)
        draw.ellipse([(cx - 4, cy - 4), (cx + 4, cy + 4)], fill=col)
        for ang in [0, 45, 90, 135]:
            rad = math.radians(ang)
            dx = math.cos(rad) * 13
            dy = math.sin(rad) * 13
            draw.line([(cx - dx, cy - dy), (cx + dx, cy + dy)], fill=col, width=3)
    elif itype == "safety":
        # Shield
        draw.polygon([(cx, cy + 14), (cx - 12, cy + 4), (cx - 12, cy - 12), (cx + 12, cy - 12), (cx + 12, cy + 4)], outline=col, width=3)
        draw.line([(cx, cy - 8), (cx, cy + 8)], fill=col, width=2)
    elif itype == "surgery":
        # Robotic Arm / Scalpel
        draw.line([(cx - 12, cy - 10), (cx, cy + 4)], fill=col, width=4)
        draw.line([(cx, cy + 4), (cx + 10, cy + 2)], fill=col, width=3)
        draw.line([(cx + 10, cy + 2), (cx + 14, cy + 12)], fill=col, width=3)
        draw.ellipse([(cx - 1, cy + 3), (cx + 3, cy + 7)], fill=col)
    elif itype == "v2p":
        # Vehicle to Pedestrian
        draw.ellipse([(cx - 8, cy - 10), (cx - 2, cy - 4)], fill=col)
        draw.line([(cx - 5, cy - 4), (cx - 5, cy + 10)], fill=col, width=2)
        draw.rounded_rectangle([(cx + 2, cy - 2), (cx + 16, cy + 8)], radius=2, outline=col, width=2)
    elif itype == "v2v":
        # Vehicle to Vehicle
        draw.rounded_rectangle([(cx - 15, cy - 2), (cx - 3, cy + 8)], radius=2, outline=col, width=2)
        draw.rounded_rectangle([(cx + 3, cy - 2), (cx + 15, cy + 8)], radius=2, outline=col, width=2)
        draw.line([(cx - 1, cy + 2), (cx + 1, cy + 2)], fill=col, width=2)
    else:
        # Default star/circle
        draw.ellipse([(cx - 8, cy - 8), (cx + 8, cy + 8)], fill=col)

# ==============================================================================
# Helper to Draw a Spec Summary Box
# ==============================================================================
def draw_spec_box(x0, y0, w, h, title, specs, theme_col):
    # Drop shadow
    draw.rounded_rectangle([(x0 + 4, y0 + 4), (x0 + w + 4, y0 + h + 4)], radius=18, fill=col_shadow)
    # Box surface
    draw.rounded_rectangle([(x0, y0), (x0 + w, y0 + h)], radius=18, fill=col_card_bg, outline=theme_col, width=3)
    
    # Header bar
    draw.rounded_rectangle([(x0, y0), (x0 + w, y0 + 56)], radius=18, fill=theme_col)
    draw.rectangle([(x0, y0 + 36), (x0 + w, y0 + 56)], fill=theme_col) # square bottom corners
    
    f_st = get_font(22, bold=True)
    draw.text((x0 + 24, y0 + 14), title, font=f_st, fill=(255, 255, 255, 255))
    
    # Specs rows
    cur_y = y0 + 74
    f_spec = get_font(18, bold=True)
    f_val = get_font(18, bold=False)
    for idx, (label, val) in enumerate(specs):
        bullet_y = cur_y + 10
        draw.ellipse([(x0 + 26, bullet_y - 4), (x0 + 34, bullet_y + 4)], fill=theme_col)
        draw.text((x0 + 46, cur_y), f"{idx + 1}. {label}:", font=f_spec, fill=col_text_dark)
        draw.text((x0 + 46, cur_y + 26), f"    {val}", font=f_val, fill=theme_col)
        cur_y += 56

# ==============================================================================
# 4. ROW 1: eMBB (Extreme / Enhanced Mobile Broadband)
# ==============================================================================
row1_y0, row1_y1 = 410, 940
# Outer container
draw.rounded_rectangle([(40, row1_y0), (W - 40, row1_y1)], radius=20, fill=col_embb_bg, outline=col_embb_border, width=3)

# Left Category Badge
draw.rounded_rectangle([(55, row1_y0 + 20), (55 + left_col_w, row1_y1 - 20)], radius=16, fill=col_card_bg, outline=col_embb_main, width=3)
draw.rounded_rectangle([(55, row1_y0 + 20), (55 + 16, row1_y1 - 20)], radius=8, fill=col_embb_main)
f_b1 = get_font(32, bold=True)
draw.text((95, row1_y0 + 70), "Extreme Mobile", font=f_b1, fill=col_embb_main)
draw.text((95, row1_y0 + 115), "Broadband", font=f_b1, fill=col_embb_main)
f_b2 = get_font(42, bold=True)
draw.text((95, row1_y0 + 180), "(eMBB)", font=f_b2, fill=col_navy_header)
f_bd = get_font(17, bold=False)
embb_desc = "Extreme data rates, massive capacity,\nand wide-area high-speed mobility\nfor human-centric multimedia."
draw.text((95, row1_y0 + 270), embb_desc, font=f_bd, fill=col_text_sub)

# Application Cards Grid (2 rows x 3 cols in middle)
app_w = int((apps_area_x1 - apps_area_x0 - 40) / 3)
app_h = 105
row1_r1 = row1_y0 + 35
row1_r2 = row1_y0 + 165
row1_r3 = row1_y0 + 295

draw_app_card(apps_area_x0, row1_r1, app_w, app_h, "Virtual & Augmented Reality", "360° 8K Immersive Gaming & Telepresence", "vr", col_embb_main)
draw_app_card(apps_area_x0 + app_w + 20, row1_r1, app_w, app_h, "Video Calling & Meetings", "Ultra-HD Holographic Telepresence", "video", col_embb_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row1_r1, app_w, app_h, "Fixed Wireless Access (FWA)", "Fiber-Speed Gigabit Home Broadband", "wireless", col_embb_main)

draw_app_card(apps_area_x0, row1_r2, app_w, app_h, "Video Surveillance & Analytics", "Real-Time 4K Smart City Monitoring", "cctv", col_embb_main)
draw_app_card(apps_area_x0 + app_w + 20, row1_r2, app_w, app_h, "Ultra HD Video Streaming", "8K Broadcast & On-Demand Media", "uhd", col_embb_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row1_r2, app_w, app_h, "Mobile Cloud Computing", "Zero-Lag GPU Cloud Gaming & Workstations", "cloud", col_embb_main)

draw_app_card(apps_area_x0, row1_r3, app_w * 2 + 20, app_h, "High-Speed Rail & Aviation Mobility", "Seamless Gigabit connectivity at velocities up to 500 km/h", "wireless", col_embb_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row1_r3, app_w, app_h, "Broadband Hotspots", "High-Density Stadiums & Venues", "mesh", col_embb_main)

# Right Spec Card (eMBB)
specs_embb = [
    ("Peak Data Rate", "20 Gbps Downlink / 10 Gbps Uplink"),
    ("User Experienced Rate", "100 Mbps Guaranteed Everywhere"),
    ("User Plane Latency", "4 ms End-to-End Over-the-Air"),
    ("Mobility Velocity", "Up to 500 km/h (High-Speed Trains)"),
    ("Spectrum Efficiency", "3x improvement over 4G LTE-A")
]
draw_spec_box(W - 40 - spec_col_w, row1_y0 + 20, spec_col_w - 15, row1_y1 - row1_y0 - 40, "ENHANCED MBB SPECIFICATIONS", specs_embb, col_embb_main)

# ==============================================================================
# 5. ROW 2: mMTC (Massive Scale Communication / Machine Type Communication)
# ==============================================================================
row2_y0, row2_y1 = 960, 1490
draw.rounded_rectangle([(40, row2_y0), (W - 40, row2_y1)], radius=20, fill=col_mmtc_bg, outline=col_mmtc_border, width=3)

# Left Category Badge
draw.rounded_rectangle([(55, row2_y0 + 20), (55 + left_col_w, row2_y1 - 20)], radius=16, fill=col_card_bg, outline=col_mmtc_main, width=3)
draw.rounded_rectangle([(55, row2_y0 + 20), (55 + 16, row2_y1 - 20)], radius=8, fill=col_mmtc_main)
draw.text((95, row2_y0 + 70), "Massive Scale", font=f_b1, fill=col_mmtc_main)
draw.text((95, row2_y0 + 115), "Communication", font=f_b1, fill=col_mmtc_main)
draw.text((95, row2_y0 + 180), "(mMTC)", font=f_b2, fill=col_navy_header)
mmtc_desc = "Hyper-dense connectivity for billions\nof low-cost, ultra-long battery life\nIoT and telemetry endpoints."
draw.text((95, row2_y0 + 270), mmtc_desc, font=f_bd, fill=col_text_sub)

# Application Cards Grid
row2_r1 = row2_y0 + 35
row2_r2 = row2_y0 + 165
row2_r3 = row2_y0 + 295

draw_app_card(apps_area_x0, row2_r1, app_w, app_h, "Smart Wearables", "Fitness Trackers, Medical Biosensors & Watches", "wearable", col_mmtc_main)
draw_app_card(apps_area_x0 + app_w + 20, row2_r1, app_w, app_h, "Sensor Social Mesh", "Distributed Ambient Weather & Pollution Nodes", "mesh", col_mmtc_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row2_r1, app_w, app_h, "Smart Home & Smart Cities", "Intelligent Water, Gas & Power Grid Management", "smarthome", col_mmtc_main)

draw_app_card(apps_area_x0, row2_r2, app_w, app_h, "Health care Telemetry", "Continuous Remote Patient & Cardiac Monitoring", "health", col_mmtc_main)
draw_app_card(apps_area_x0 + app_w + 20, row2_r2, app_w, app_h, "Vehicle to Infrastructure", "Connected Traffic Signals & Roadside Sensors", "v2i", col_mmtc_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row2_r2, app_w, app_h, "Industrial Automation", "Factory Logistics, Pallet Tracking & Asset Tags", "industrial", col_mmtc_main)

draw_app_card(apps_area_x0, row2_r3, app_w * 2 + 20, app_h, "Smart Agriculture & Precision Farming", "Automated soil moisture probes, drone surveys & smart irrigation", "smarthome", col_mmtc_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row2_r3, app_w, app_h, "Smart Logistics", "Global Fleet & Cold-Chain Tracking", "mesh", col_mmtc_main)

# Right Spec Card (mMTC)
specs_mmtc = [
    ("Connection Density", "1,000,000 Devices / km²"),
    ("Battery Lifetime", "10+ Years on Single Coin-Cell"),
    ("Coverage Enhancement", "+20 dB Beyond Standard Cellular"),
    ("Device Hardware Cost", "Ultra-Low-Cost Microcontroller Modules"),
    ("Network Slicing", "Independent Dedicated IoT Slice")
]
draw_spec_box(W - 40 - spec_col_w, row2_y0 + 20, spec_col_w - 15, row2_y1 - row2_y0 - 40, "MASSIVE MTC SPECIFICATIONS", specs_mmtc, col_mmtc_main)

# ==============================================================================
# 6. ROW 3: uRLLC (Ultra-Reliable Low Latency Communication)
# ==============================================================================
row3_y0, row3_y1 = 1510, 2040
draw.rounded_rectangle([(40, row3_y0), (W - 40, row3_y1)], radius=20, fill=col_urllc_bg, outline=col_urllc_border, width=3)

# Left Category Badge
draw.rounded_rectangle([(55, row3_y0 + 20), (55 + left_col_w, row3_y1 - 20)], radius=16, fill=col_card_bg, outline=col_urllc_main, width=3)
draw.rounded_rectangle([(55, row3_y0 + 20), (55 + 16, row3_y1 - 20)], radius=8, fill=col_urllc_main)
draw.text((95, row3_y0 + 70), "Ultra-Reliable", font=f_b1, fill=col_urllc_main)
draw.text((95, row3_y0 + 115), "Low Latency", font=f_b1, fill=col_urllc_main)
draw.text((95, row3_y0 + 180), "(uRLLC)", font=f_b2, fill=col_navy_header)
urllc_desc = "Mission-critical sub-millisecond reflexes\nwith carrier-grade 99.9999% availability\nfor life-critical systems."
draw.text((95, row3_y0 + 270), urllc_desc, font=f_bd, fill=col_text_sub)

# Application Cards Grid
row3_r1 = row3_y0 + 35
row3_r2 = row3_y0 + 165
row3_r3 = row3_y0 + 295

draw_app_card(apps_area_x0, row3_r1, app_w, app_h, "Public Safety & Defense", "Mission-Critical First Responder Comms", "safety", col_urllc_main)
draw_app_card(apps_area_x0 + app_w + 20, row3_r1, app_w, app_h, "Remote Robotic Surgery", "Sub-Millisecond Haptic Tele-Operation", "surgery", col_urllc_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row3_r1, app_w, app_h, "Vehicle to Pedestrian (V2P)", "Active Real-Time Collision Avoidance", "v2p", col_urllc_main)

draw_app_card(apps_area_x0, row3_r2, app_w, app_h, "Vehicle to Vehicle (V2V)", "Autonomous Highway Platooning & Braking", "v2v", col_urllc_main)
draw_app_card(apps_area_x0 + app_w + 20, row3_r2, app_w, app_h, "Precision Factory Robotics", "Closed-Loop Real-Time Motion Control", "industrial", col_urllc_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row3_r2, app_w, app_h, "Smart Electrical Grid", "Microsecond Substation Fault Isolation", "safety", col_urllc_main)

draw_app_card(apps_area_x0, row3_r3, app_w * 2 + 20, app_h, "Autonomous Drone Swarms & Air Traffic", "Ultra-safe BVLOS (Beyond Visual Line of Sight) avionics", "v2i", col_urllc_main)
draw_app_card(apps_area_x0 + (app_w + 20) * 2, row3_r3, app_w, app_h, "Tactile Internet", "Zero-Lag Touch & Physical Simulation", "surgery", col_urllc_main)

# Right Spec Card (uRLLC)
specs_urllc = [
    ("User Plane Latency", "< 1 ms Radio Latency (0.5 ms Uplink)"),
    ("Service Reliability", "99.9999% (Five-Nines Carrier Grade)"),
    ("Security & Resilience", "Quantum-Resistant Encryption & Anti-Jamming"),
    ("Jitter Variation", "< 1 microsecond Deterministic Delivery"),
    ("Packet Error Rate", "< 10^-6 at 1 ms latency budget")
]
draw_spec_box(W - 40 - spec_col_w, row3_y0 + 20, spec_col_w - 15, row3_y1 - row3_y0 - 40, "URLLC SPECIFICATIONS", specs_urllc, col_urllc_main)

# ==============================================================================
# 7. FOOTER NOTE
# ==============================================================================
f_foot = get_font(18, bold=False)
foot_text = "Thapar Institute of Engineering & Technology | 5G Experimental Research & Training Facility | 3GPP Release 15/16/17 Architecture"
fb = f_foot.getbbox(foot_text)
draw.text(((W - (fb[2] - fb[0])) // 2, H - 54), foot_text, font=f_foot, fill=col_text_sub)

# Save Master Output
out_path_clean = "D:/5G-Lab-Room/Assets/equipments/usecases_banner_clean.png"
out_path_p2 = "D:/5G-Lab-Room/Assets/equipments/2..png"

# Convert RGBA to RGB for standard texture
rgb_img = img.convert("RGB")
rgb_img.save(out_path_clean, "PNG", quality=95)
rgb_img.save(out_path_p2, "PNG", quality=95)
print(f"Successfully generated 4K Use Cases banner ({W}x{H}) at {out_path_clean} and replaced {out_path_p2}")
