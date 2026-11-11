import math
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 3840, 2560
img = Image.new("RGBA", (W, H), (255, 255, 255, 255)) # Crisp academic pure white
draw = ImageDraw.Draw(img)

# Windows System Fonts
font_bold = "C:/Windows/Fonts/segoeuib.ttf"
font_regular = "C:/Windows/Fonts/segoeui.ttf"
font_mono = "C:/Windows/Fonts/consola.ttf"

def get_font(size, bold=True, mono=False):
    path = font_mono if mono else (font_bold if bold else font_regular)
    return ImageFont.truetype(path, size)

# Color Palette: Academic Engineering Poster
col_bg = (255, 255, 255, 255)
col_navy_header = (18, 32, 64, 255)       # Deep Thapar navy
col_navy_accent = (26, 54, 93, 255)       # Secondary navy
col_crimson = (120, 18, 40, 255)          # Thapar crimson
col_gold = (218, 165, 32, 255)            # Gold accent
col_text_dark = (15, 23, 42, 255)         # High-contrast slate black
col_text_sub = (71, 85, 105, 255)         # Muted slate
col_box_bg = (248, 250, 252, 255)         # Crisp card white/slate
col_box_border = (15, 23, 42, 255)        # Clean black 2px border
col_box_alt_bg = (241, 245, 249, 255)
col_line_solid = (15, 23, 42, 255)        # Solid black lines
col_line_dashed = (71, 85, 105, 255)      # Muted slate dashed lines
col_server_blue = (30, 64, 175, 255)      # UPF Server blue

# Outer Border
draw.rectangle([(16, 16), (W - 16, H - 16)], outline=(203, 213, 225, 255), width=3)
draw.rectangle([(22, 22), (W - 22, H - 22)], outline=(226, 232, 240, 255), width=2)

# ==============================================================================
# 1. TOP HEADER BAR: Thapar University Crests & Department Title
# ==============================================================================
header_y0, header_y1 = 40, 220
draw.rectangle([(40, header_y0), (W - 40, header_y1)], fill=col_navy_header)

def draw_thapar_crest(cx, cy):
    # Crimson roundel with gold border
    r = 68
    draw.ellipse([(cx - r, cy - r), (cx + r, cy + r)], fill=col_crimson, outline=col_gold, width=4)
    # Inner gold ring
    draw.ellipse([(cx - r + 8, cy - r + 8), (cx + r - 8, cy + r - 8)], outline=col_gold, width=2)
    # Monogram "ti"
    f_ti = get_font(52, bold=True)
    draw.text((cx - 24, cy - 36), "ti", font=f_ti, fill=col_gold)
    f_sub = get_font(13, bold=True)
    draw.text((cx - 36, cy + 24), "ESTD. 1956", font=f_sub, fill=(255, 255, 255, 230))

draw_thapar_crest(130, (header_y0 + header_y1) // 2)
draw_thapar_crest(W - 130, (header_y0 + header_y1) // 2)

# Department Title
f_dept = get_font(42, bold=True)
dept_text = "DEPARTMENT OF ELECTRONICS AND COMMUNICATION ENGINEERING"
t_bbox = f_dept.getbbox(dept_text)
draw.text(((W - (t_bbox[2] - t_bbox[0])) // 2, header_y0 + 35), dept_text, font=f_dept, fill=(255, 255, 255, 255))

f_inst = get_font(25, bold=False)
inst_text = "THAPAR INSTITUTE OF ENGINEERING & TECHNOLOGY (DEEMED TO BE UNIVERSITY), PATIALA"
i_bbox = f_inst.getbbox(inst_text)
draw.text(((W - (i_bbox[2] - i_bbox[0])) // 2, header_y0 + 98), inst_text, font=f_inst, fill=(226, 232, 240, 240))

# ==============================================================================
# 2. MAIN POSTER TITLE: 5G Architecture based on CUPS
# ==============================================================================
title_y = 265
f_title = get_font(56, bold=True)
title_text = "5G Architecture based on CUPS"
m_bbox = f_title.getbbox(title_text)
t_w = m_bbox[2] - m_bbox[0]
t_x = (W - t_w) // 2
draw.text((t_x, title_y), title_text, font=f_title, fill=col_text_dark)

# Underline
draw.line([(t_x - 30, title_y + 70), (t_x + t_w + 30, title_y + 70)], fill=col_text_dark, width=4)

f_subtitle = get_font(23, bold=False)
sub_text = "Control and User Plane Separation | 3GPP Rel-15 Service-Based Architecture"
s_bbox = f_subtitle.getbbox(sub_text)
draw.text(((W - (s_bbox[2] - s_bbox[0])) // 2, title_y + 85), sub_text, font=f_subtitle, fill=col_text_sub)

# ==============================================================================
# Helper Drawing Functions: Dashed Lines, Boxes, and Arrows
# ==============================================================================
def draw_dashed_line(p1, p2, fill=col_line_dashed, width=2, dash_len=14, gap_len=10):
    x1, y1 = p1
    x2, y2 = p2
    dx = x2 - x1
    dy = y2 - y1
    dist = math.hypot(dx, dy)
    if dist == 0: return
    ux = dx / dist
    uy = dy / dist
    
    cur = 0
    while cur < dist:
        seg_end = min(cur + dash_len, dist)
        draw.line([(x1 + ux * cur, y1 + uy * cur), (x1 + ux * seg_end, y1 + uy * seg_end)], fill=fill, width=width)
        cur += dash_len + gap_len

def draw_dashed_rect(box, fill=None, outline=col_line_dashed, width=3, dash_len=16, gap_len=10):
    x0, y0, x1, y1 = box
    draw_dashed_line((x0, y0), (x1, y0), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)
    draw_dashed_line((x1, y0), (x1, y1), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)
    draw_dashed_line((x1, y1), (x0, y1), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)
    draw_dashed_line((x0, y1), (x0, y0), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)

def draw_arrow(p1, p2, fill=col_line_solid, width=3, head_len=16):
    draw.line([p1, p2], fill=fill, width=width)
    x1, y1 = p1
    x2, y2 = p2
    dx = x2 - x1
    dy = y2 - y1
    dist = math.hypot(dx, dy)
    if dist == 0: return
    ux, uy = dx / dist, dy / dist
    # Perpendicular
    px, py = -uy, ux
    pt_tip = (x2, y2)
    pt_left = (x2 - ux * head_len + px * (head_len * 0.5), y2 - uy * head_len + py * (head_len * 0.5))
    pt_right = (x2 - ux * head_len - px * (head_len * 0.5), y2 - uy * head_len - py * (head_len * 0.5))
    draw.polygon([pt_tip, pt_left, pt_right], fill=fill)

def draw_bidirectional_arrow(p1, p2, fill=col_line_solid, width=3, head_len=14):
    draw.line([p1, p2], fill=fill, width=width)
    draw_arrow(p2, p1, fill=fill, width=width, head_len=head_len)
    draw_arrow(p1, p2, fill=fill, width=width, head_len=head_len)

# Helper: Functional Unit Box with crisp shadow
def draw_unit_box(box, text, subtext=None, fill=col_box_bg, outline=col_box_border, width=3):
    x0, y0, x1, y1 = box
    # Crisp drop shadow
    draw.rectangle([(x0 + 4, y0 + 4), (x1 + 4, y1 + 4)], fill=(226, 232, 240, 180))
    draw.rectangle([box[0], box[1], box[2], box[3]], fill=fill, outline=outline, width=width)
    
    # Text centered
    cx = (x0 + x1) // 2
    f_main = get_font(38 if len(text) <= 5 else 32, bold=True)
    bbox = f_main.getbbox(text)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    
    if subtext:
        cy = (y0 + y1) // 2 - 12
        draw.text((cx - tw // 2, cy - th // 2), text, font=f_main, fill=col_text_dark)
        f_s = get_font(18, bold=False)
        s_bbox = f_s.getbbox(subtext)
        draw.text((cx - (s_bbox[2] - s_bbox[0]) // 2, cy + th // 2 + 8), subtext, font=f_s, fill=col_text_sub)
    else:
        cy = (y0 + y1) // 2
        draw.text((cx - tw // 2, cy - th // 2), text, font=f_main, fill=col_text_dark)

# ==============================================================================
# 3. COLUMN 1: USER EQUIPMENT (UE)
# ==============================================================================
ue_x = 240
f_col = get_font(48, bold=True)
draw.text((ue_x - 30, 440), "UE", font=f_col, fill=col_text_dark)

# Device drawing functions
def draw_radio_arches(cx, cy, radius_list=[25, 42, 58], color=col_text_dark):
    for r in radius_list:
        draw.arc([(cx - r, cy - r), (cx + r, cy + r)], start=210, end=330, fill=color, width=4)

# 1. IoT Gateway (y = 660)
y_iot = 650
draw_radio_arches(ue_x + 10, y_iot - 30, [22, 38, 52])
# Device box
draw.rounded_rectangle([(ue_x - 65, y_iot), (ue_x + 85, y_iot + 38)], radius=6, fill=(241, 245, 249), outline=col_text_dark, width=3)
draw.ellipse([(ue_x - 45, y_iot + 14), (ue_x - 35, y_iot + 24)], fill=(16, 185, 129))
draw.ellipse([(ue_x - 20, y_iot + 14), (ue_x - 10, y_iot + 24)], fill=col_text_dark)
draw.ellipse([(ue_x + 5, y_iot + 14), (ue_x + 15, y_iot + 24)], fill=col_text_dark)

# 2. Connected Car (y = 1080)
y_car = 1070
draw_radio_arches(ue_x + 10, y_car - 40, [22, 38, 52])
# Car body
draw.polygon([(ue_x - 75, y_car + 20), (ue_x - 55, y_car - 5), (ue_x - 15, y_car - 25), (ue_x + 45, y_car - 25), (ue_x + 75, y_car - 5), (ue_x + 95, y_car + 10), (ue_x + 95, y_car + 25), (ue_x - 75, y_car + 25)], fill=(241, 245, 249), outline=col_text_dark)
draw.line([(ue_x - 75, y_car + 25), (ue_x + 95, y_car + 25)], fill=col_text_dark, width=4)
# Windows
draw.polygon([(ue_x - 45, y_car - 2), (ue_x - 12, y_car - 18), (ue_x + 10, y_car - 18), (ue_x + 10, y_car - 2)], fill=col_navy_header)
draw.polygon([(ue_x + 18, y_car - 2), (ue_x + 18, y_car - 18), (ue_x + 40, y_car - 18), (ue_x + 65, y_car - 2)], fill=col_navy_header)
# Wheels
draw.ellipse([(ue_x - 50, y_car + 12), (ue_x - 20, y_car + 42)], fill=(30, 41, 59), outline=col_text_dark, width=3)
draw.ellipse([(ue_x + 40, y_car + 12), (ue_x + 70, y_car + 42)], fill=(30, 41, 59), outline=col_text_dark, width=3)

# 3. Smartphone (y = 1520)
y_phone = 1500
draw.rounded_rectangle([(ue_x - 38, y_phone - 55), (ue_x + 42, y_phone + 65)], radius=12, fill=(241, 245, 249), outline=col_text_dark, width=4)
# Screen
draw.rectangle([(ue_x - 30, y_phone - 42), (ue_x + 34, y_phone + 48)], fill=(255, 255, 255), outline=(203, 213, 225), width=2)
# Home indicator / speaker
draw.ellipse([(ue_x - 2, y_phone - 48), (ue_x + 6, y_phone - 44)], fill=col_text_dark)
draw_radio_arches(ue_x + 2, y_phone + 5, [10, 18, 26])

# 4. Workstation / PC (y = 1960)
y_pc = 1940
# Monitor
draw.rectangle([(ue_x - 65, y_pc - 45), (ue_x + 35, y_pc + 25)], fill=(255, 255, 255), outline=col_text_dark, width=4)
# Screen stand
draw.rectangle([(ue_x - 20, y_pc + 25), (ue_x - 10, y_pc + 42)], fill=col_text_dark)
draw.rectangle([(ue_x - 35, y_pc + 42), (ue_x + 5, y_pc + 48)], fill=col_text_dark)
# Tower PC
draw.rectangle([(ue_x + 48, y_pc - 55), (ue_x + 85, y_pc + 48)], fill=(241, 245, 249), outline=col_text_dark, width=3)
draw.line([(ue_x + 55, y_pc - 40), (ue_x + 78, y_pc - 40)], fill=col_text_dark, width=3)
draw.ellipse([(ue_x + 63, y_pc - 25), (ue_x + 70, y_pc - 18)], fill=(16, 185, 129))

# ==============================================================================
# 4. COLUMN 2: ACCESS NETWORK
# ==============================================================================
an_x0, an_x1 = 780, 1280
draw.text((an_x0 + 40, 420), "Access\nNetwork", font=f_col, fill=col_text_dark)

# Icons to the left of boxes
def draw_cell_tower(cx, cy, is_5g=True):
    # Triangular lattice tower
    draw.polygon([(cx, cy - 65), (cx - 35, cy + 55), (cx + 35, cy + 55)], outline=col_text_dark, fill=None, width=3)
    # Cross braces
    draw.line([(cx - 15, cy - 15), (cx + 15, cy - 15)], fill=col_text_dark, width=2)
    draw.line([(cx - 25, cy + 20), (cx + 25, cy + 20)], fill=col_text_dark, width=2)
    draw.line([(cx - 35, cy + 55), (cx + 35, cy + 55)], fill=col_text_dark, width=3)
    draw.line([(cx - 25, cy + 20), (cx + 15, cy - 15)], fill=col_text_dark, width=2)
    draw.line([(cx + 25, cy + 20), (cx - 15, cy - 15)], fill=col_text_dark, width=2)
    # Antenna mast & radiation waves
    draw.line([(cx, cy - 65), (cx, cy - 85)], fill=col_text_dark, width=4)
    draw.ellipse([(cx - 7, cy - 92), (cx + 7, cy - 78)], fill=col_crimson if is_5g else col_navy_accent)
    draw_radio_arches(cx, cy - 85, [18, 32, 46], color=col_crimson if is_5g else col_navy_accent)

# 1. 5G BS
box_5g_y = (530, 780)
draw_cell_tower(an_x0 - 90, (box_5g_y[0] + box_5g_y[1]) // 2, is_5g=True)
draw_unit_box((an_x0, box_5g_y[0], an_x1, box_5g_y[1]), "5G BS", "3GPP New Radio (NR gNodeB)")

# 2. 4G BS
box_4g_y = (970, 1220)
draw_cell_tower(an_x0 - 90, (box_4g_y[0] + box_4g_y[1]) // 2, is_5g=False)
draw_unit_box((an_x0, box_4g_y[0], an_x1, box_4g_y[1]), "4G BS", "Legacy LTE (eNodeB Anchor)")

# 3. Wi-Fi Access
box_wifi_y = (1410, 1660)
# Router icon
rx, ry = an_x0 - 90, (box_wifi_y[0] + box_wifi_y[1]) // 2
draw.rounded_rectangle([(rx - 55, ry - 10), (rx + 45, ry + 25)], radius=6, fill=(241, 245, 249), outline=col_text_dark, width=3)
draw.line([(rx - 35, ry - 10), (rx - 35, ry - 45)], fill=col_text_dark, width=3)
draw.line([(rx + 25, ry - 10), (rx + 25, ry - 45)], fill=col_text_dark, width=3)
draw_radio_arches(rx - 5, ry - 30, [15, 28, 42], color=col_navy_accent)
draw_unit_box((an_x0, box_wifi_y[0], an_x1, box_wifi_y[1]), "Wi-Fi", "Untrusted Non-3GPP (N3IWF)")

# 4. Fixed Wireline
box_fixed_y = (1850, 2100)
# Switch icon
fx, fy = an_x0 - 90, (box_fixed_y[0] + box_fixed_y[1]) // 2
draw.rectangle([(fx - 65, fy - 15), (fx + 55, fy + 25)], fill=(241, 245, 249), outline=col_text_dark, width=3)
for p in range(6):
    draw.rectangle([(fx - 55 + p*18, fy - 5), (fx - 43 + p*18, fy + 8)], fill=col_navy_header)
draw_unit_box((an_x0, box_fixed_y[0], an_x1, box_fixed_y[1]), "Fixed", "Wireline Broadband (W-5GAN)")

# Connectors: UE to Access
draw_dashed_line((ue_x + 95, y_iot + 15), (an_x0 - 150, (box_5g_y[0] + box_5g_y[1]) // 2), width=3)
draw_dashed_line((ue_x + 95, y_car), (an_x0 - 150, (box_4g_y[0] + box_4g_y[1]) // 2), width=3)
draw.line([(ue_x + 50, y_phone), (an_x0 - 155, (box_wifi_y[0] + box_wifi_y[1]) // 2)], fill=col_line_solid, width=3)
draw.line([(ue_x + 90, y_pc), (an_x0 - 165, (box_fixed_y[0] + box_fixed_y[1]) // 2)], fill=col_line_solid, width=3)

# ==============================================================================
# 5. CONTAINER 1: 5G CONTROL PLANE (SBA)
# ==============================================================================
cp_box = (1450, 440, 3680, 1260)
draw_dashed_rect(cp_box, outline=col_text_dark, width=3, dash_len=18, gap_len=12)

f_plane_hdr = get_font(44, bold=True)
draw.text((cp_box[2] - 420, cp_box[1] + 35), "5G Control\nPlane", font=f_plane_hdr, fill=col_text_dark)

# Row 1: NSSF, AUSF, UDM
nssf_box = (1500, 520, 1920, 710)
ausf_box = (1960, 520, 2400, 710)
udm_box  = (2440, 520, 2880, 710)
draw_unit_box(nssf_box, "NSSF", "Slice Selection")
draw_unit_box(ausf_box, "AUSF", "Authentication")
draw_unit_box(udm_box, "UDM", "Unified Data Mgmt")

# Row 2: AMF, SMF, PCF, AF
amf_box = (1960, 830, 2340, 1020)
smf_box = (2400, 830, 2780, 1020)
pcf_box = (2840, 830, 3220, 1020)
af_box  = (3280, 830, 3620, 1020)
draw_unit_box(amf_box, "AMF", "Access & Mobility")
draw_unit_box(smf_box, "SMF", "Session Control")
draw_unit_box(pcf_box, "PCF", "Policy Rules")
draw_unit_box(af_box, "AF", "App Function")

# Control Plane Internal Signaling
amf_top_center = ((amf_box[0] + amf_box[2]) // 2, amf_box[1])
draw_dashed_line(((nssf_box[0] + nssf_box[2]) // 2, nssf_box[3]), amf_top_center, width=3)
draw_dashed_line(((ausf_box[0] + ausf_box[2]) // 2, ausf_box[3]), amf_top_center, width=3)
draw_dashed_line(((udm_box[0] + udm_box[2]) // 2, udm_box[3]), amf_top_center, width=3)

# Service Bus between AMF, SMF, PCF, AF
y_bus = (amf_box[1] + amf_box[3]) // 2
draw.line([(amf_box[2], y_bus), (smf_box[0], y_bus)], fill=col_line_solid, width=3)
draw.line([(smf_box[2], y_bus), (pcf_box[0], y_bus)], fill=col_line_solid, width=3)
draw.line([(pcf_box[2], y_bus), (af_box[0], y_bus)], fill=col_line_solid, width=3)

# Control signaling from Access Network into AMF (N2 Interface)
draw_dashed_line((an_x1, (box_5g_y[0] + box_5g_y[1]) // 2), (amf_box[0], y_bus - 35), width=3)
draw_dashed_line((an_x1, (box_4g_y[0] + box_4g_y[1]) // 2), (amf_box[0], y_bus + 35), width=3)

# ==============================================================================
# 6. CONTAINER 2: 5G USER PLANE (CUPS)
# ==============================================================================
up_box = (1700, 1370, 3140, 2220)
draw_dashed_rect(up_box, outline=col_text_dark, width=3, dash_len=18, gap_len=12)

draw.text((up_box[0] + 620, up_box[3] - 75), "5G User Plane", font=f_plane_hdr, fill=col_text_dark)

# Helper: Draw Server Icon on UPF
def draw_server_rack(cx, cy):
    # Sleek rack chassis
    draw.rectangle([(cx - 30, cy - 25), (cx + 30, cy + 25)], fill=(30, 41, 59), outline=col_text_dark, width=2)
    for row in range(3):
        ry = cy - 16 + row * 14
        draw.rectangle([(cx - 24, ry), (cx + 24, ry + 8)], fill=(15, 23, 42))
        draw.ellipse([(cx + 14, ry + 2), (cx + 18, ry + 6)], fill=(16, 185, 129))
        draw.ellipse([(cx + 8, ry + 2), (cx + 12, ry + 6)], fill=(56, 189, 248))

# 3 Distributed UPF Units
upf1_box = (1750, 1430, 2180, 1650) # Top Edge UPF
upf2_box = (1750, 1750, 2180, 1970) # Bottom Edge UPF (Local Breakout)
upf3_box = (2440, 1540, 2920, 1840) # Central Anchor UPF

draw_unit_box(upf1_box, "UPF", "Edge UPF (eMBB)")
draw_server_rack(upf1_box[2] - 45, (upf1_box[1] + upf1_box[3]) // 2)

draw_unit_box(upf2_box, "UPF", "Edge UPF (URLLC Breakout)")
draw_server_rack(upf2_box[2] - 45, (upf2_box[1] + upf2_box[3]) // 2)

draw_unit_box(upf3_box, "UPF", "Intermediate & Central Core UPF")
draw_server_rack(upf3_box[2] - 50, (upf3_box[1] + upf3_box[3]) // 2)

# User Plane Internal Routing
draw.line([(upf1_box[2], (upf1_box[1] + upf1_box[3]) // 2), (upf3_box[0], (upf3_box[1] + upf3_box[3]) // 2 - 30)], fill=col_line_solid, width=4)
draw.line([(upf2_box[2], (upf2_box[1] + upf2_box[3]) // 2), (upf3_box[0], (upf3_box[1] + upf3_box[3]) // 2 + 30)], fill=col_line_solid, width=4)

# User Data payload lines from Access Network converging to UPFs (N3 Interface)
ran_converge_pt = (up_box[0], 1700)
draw.line([(an_x1, (box_5g_y[0] + box_5g_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=3)
draw.line([(an_x1, (box_4g_y[0] + box_4g_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=3)
draw.line([(an_x1, (box_wifi_y[0] + box_wifi_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=3)
draw.line([(an_x1, (box_fixed_y[0] + box_fixed_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=3)

# Line into UPF inputs
draw.line([ran_converge_pt, (upf1_box[0], (upf1_box[1] + upf1_box[3]) // 2)], fill=col_line_solid, width=3)
draw.line([ran_converge_pt, (upf2_box[0], (upf2_box[1] + upf2_box[3]) // 2)], fill=col_line_solid, width=3)

# Interface Label (R)AN-UPF
f_badge = get_font(22, bold=True)
draw_bidirectional_arrow((1300, 2260), (1680, 2260), fill=col_text_dark, width=3)
draw.text((1380, 2280), "(R)AN-UPF (N3 User Data)", font=f_badge, fill=col_text_dark)

# ==============================================================================
# 7. DATA NETWORKS: LOCAL DN & CENTRAL DN
# ==============================================================================
def draw_database_cloud(cx, cy, label, subtext):
    # 3-tier database cylinder
    cyl_w = 90
    cyl_h = 24
    for tier in range(3):
        ty = cy - 25 + tier * 24
        draw.ellipse([(cx - cyl_w//2, ty), (cx + cyl_w//2, ty + cyl_h)], fill=(241, 245, 249), outline=col_text_dark, width=3)
        draw.line([(cx - cyl_w//2, ty + cyl_h//2), (cx - cyl_w//2, ty + cyl_h + 10)], fill=col_text_dark, width=3)
        draw.line([(cx + cyl_w//2, ty + cyl_h//2), (cx + cyl_w//2, ty + cyl_h + 10)], fill=col_text_dark, width=3)
        draw.arc([(cx - cyl_w//2, ty + 10), (cx + cyl_w//2, ty + cyl_h + 10)], start=0, end=180, fill=col_text_dark, width=3)
    # Cloud puff next to it
    cloud_x = cx + 55
    cloud_y = cy + 25
    draw.ellipse([(cloud_x - 30, cloud_y - 20), (cloud_x + 30, cloud_y + 20)], fill=(255, 255, 255), outline=col_text_dark, width=3)
    draw.ellipse([(cloud_x - 10, cloud_y - 35), (cloud_x + 25, cloud_y - 5)], fill=(255, 255, 255), outline=col_text_dark, width=3)
    draw.rectangle([(cloud_x - 22, cloud_y - 8), (cloud_x + 22, cloud_y + 19)], fill=(255, 255, 255))
    
    # Label
    f_lbl = get_font(34, bold=True)
    draw.text((cx + 105, cy - 15), label, font=f_lbl, fill=col_text_dark)
    f_sub = get_font(20, bold=False)
    draw.text((cx + 105, cy + 25), subtext, font=f_sub, fill=col_text_sub)

# 1. Local DN (Local Breakout for sub-1ms latency)
local_dn_cx, local_dn_cy = 1965, 2380
draw_database_cloud(local_dn_cx, local_dn_cy, "Local DN", "MEC Edge Servers (<1ms Latency)")
# Downward connection from Edge UPF2
draw.line([(upf2_box[0] + (upf2_box[2] - upf2_box[0])//2, upf2_box[3]), (local_dn_cx, local_dn_cy - 40)], fill=col_line_solid, width=4)
# Label UPF-DN
draw.line([(local_dn_cx + 90, 2060), (local_dn_cx + 90, 2220)], fill=col_text_dark, width=2)
draw.text((local_dn_cx + 105, 2120), "UPF-DN (N6)", font=get_font(20, bold=True), fill=col_text_dark)

# 2. Central Data Network (DN)
central_dn_cx, central_dn_cy = 3380, 1690
draw_database_cloud(central_dn_cx, central_dn_cy, "DN", "Internet / IMS / Operator Services")
# Connection from Central UPF
draw.line([(upf3_box[2], (upf3_box[1] + upf3_box[3]) // 2), (central_dn_cx - 65, central_dn_cy)], fill=col_line_solid, width=4)
# Label UPF-DN
draw_bidirectional_arrow((3160, 2260), (3420, 2260), fill=col_text_dark, width=3)
draw.text((3230, 2280), "UPF-DN", font=f_badge, fill=col_text_dark)

# ==============================================================================
# 8. CUPS SEPARATION CONDUIT: N4 Interface (SMF to User Plane)
# ==============================================================================
smf_bottom_center = ((smf_box[0] + smf_box[2]) // 2, smf_box[3])
up_top_center = (smf_bottom_center[0], up_box[1])

# Distinct CUPS conduit with arrow
draw_dashed_line(smf_bottom_center, up_top_center, fill=col_crimson, width=5, dash_len=14, gap_len=8)
draw_arrow(smf_bottom_center, (up_top_center[0], up_top_center[1] + 25), fill=col_crimson, width=4, head_len=20)

# High-profile N4 interface badge
f_n4 = get_font(24, bold=True)
n4_text = "N4 Interface: Packet Forwarding Control Protocol (PFCP)"
n4_bbox = f_n4.getbbox(n4_text)
n4_w = n4_bbox[2] - n4_bbox[0]
n4_box_x = smf_bottom_center[0] - n4_w // 2 - 20
draw.rounded_rectangle([(n4_box_x, 1275), (n4_box_x + n4_w + 40, 1335)], radius=8, fill=(255, 255, 255), outline=col_crimson, width=3)
draw.text((n4_box_x + 20, 1290), n4_text, font=f_n4, fill=col_crimson)

# ==============================================================================
# SAVE 4K DIGITAL MASTER
# ==============================================================================
out_path = "D:/5G-Lab-Room/Assets/equipments/cups_banner_clean.png"
img.save(out_path, "PNG", quality=100)
print(f"Pristine Academic 4K 5G Banner generated successfully at: {out_path} ({W}x{H})")
