import math
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 3840, 2560
img = Image.new("RGBA", (W, H), (255, 255, 255, 255)) # Clean academic pure white
draw = ImageDraw.Draw(img)

# Windows System Fonts
font_bold = "C:/Windows/Fonts/segoeuib.ttf"
font_regular = "C:/Windows/Fonts/segoeui.ttf"
font_mono = "C:/Windows/Fonts/consola.ttf"

def get_font(size, bold=True, mono=False):
    path = font_mono if mono else (font_bold if bold else font_regular)
    return ImageFont.truetype(path, size)

# High-Visibility Presentation Color Palette
col_bg = (255, 255, 255, 255)
col_navy_header = (16, 28, 56, 255)       # Rich deep Thapar navy
col_navy_accent = (26, 54, 93, 255)       # Secondary navy
col_crimson = (140, 20, 45, 255)          # Thapar crimson
col_gold = (218, 165, 32, 255)            # Gold accent
col_text_dark = (10, 15, 30, 255)         # Ultra-black slate for maximum contrast
col_text_sub = (51, 65, 85, 255)          # Crisp slate subtext
col_card_bg = (248, 250, 252, 255)        # Clean crisp card fill
col_card_border = (15, 23, 42, 255)       # Solid black card border
col_shadow = (203, 213, 225, 200)         # Soft drop shadow
col_line_solid = (15, 23, 42, 255)        # Bold solid black connectors
col_line_dashed = (51, 65, 85, 255)       # High-visibility dashed lines

# Outer Border
draw.rectangle([(16, 16), (W - 16, H - 16)], outline=(148, 163, 184, 255), width=5)
draw.rectangle([(24, 24), (W - 24, H - 24)], outline=(226, 232, 240, 255), width=3)

# ==============================================================================
# 1. TOP HEADER BAR: Thapar University Crests & Department Title
# ==============================================================================
header_y0, header_y1 = 45, 240
draw.rectangle([(40, header_y0), (W - 40, header_y1)], fill=col_navy_header)

def draw_thapar_crest(cx, cy):
    r = 75
    draw.ellipse([(cx - r, cy - r), (cx + r, cy + r)], fill=col_crimson, outline=col_gold, width=5)
    draw.ellipse([(cx - r + 9, cy - r + 9), (cx + r - 9, cy + r - 9)], outline=col_gold, width=2)
    f_ti = get_font(56, bold=True)
    draw.text((cx - 26, cy - 38), "ti", font=f_ti, fill=col_gold)
    f_sub = get_font(14, bold=True)
    draw.text((cx - 40, cy + 26), "ESTD. 1956", font=f_sub, fill=(255, 255, 255, 240))

draw_thapar_crest(135, (header_y0 + header_y1) // 2)
draw_thapar_crest(W - 135, (header_y0 + header_y1) // 2)

# Department Title
f_dept = get_font(46, bold=True)
dept_text = "DEPARTMENT OF ELECTRONICS AND COMMUNICATION ENGINEERING"
t_bbox = f_dept.getbbox(dept_text)
draw.text(((W - (t_bbox[2] - t_bbox[0])) // 2, header_y0 + 36), dept_text, font=f_dept, fill=(255, 255, 255, 255))

f_inst = get_font(26, bold=False)
inst_text = "THAPAR INSTITUTE OF ENGINEERING & TECHNOLOGY (DEEMED TO BE UNIVERSITY), PATIALA"
i_bbox = f_inst.getbbox(inst_text)
draw.text(((W - (i_bbox[2] - i_bbox[0])) // 2, header_y0 + 104), inst_text, font=f_inst, fill=(226, 232, 240, 255))

# ==============================================================================
# 2. MAIN POSTER TITLE: 5G Architecture based on CUPS
# ==============================================================================
title_y = 280
f_title = get_font(64, bold=True)
title_text = "5G Architecture based on CUPS"
m_bbox = f_title.getbbox(title_text)
t_w = m_bbox[2] - m_bbox[0]
t_x = (W - t_w) // 2
draw.text((t_x, title_y), title_text, font=f_title, fill=col_text_dark)

# Bold Underline
draw.line([(t_x - 40, title_y + 80), (t_x + t_w + 40, title_y + 80)], fill=col_text_dark, width=6)

f_subtitle = get_font(26, bold=False)
sub_text = "Control and User Plane Separation | 3GPP Rel-15 Service-Based Architecture"
s_bbox = f_subtitle.getbbox(sub_text)
draw.text(((W - (s_bbox[2] - s_bbox[0])) // 2, title_y + 98), sub_text, font=f_subtitle, fill=col_text_sub)

# ==============================================================================
# High-Visibility Line & Card Drawing Helpers (8px - 10px strokes)
# ==============================================================================
def draw_dashed_line(p1, p2, fill=col_line_dashed, width=6, dash_len=24, gap_len=14):
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

def draw_dashed_rect(box, outline=col_line_dashed, width=8, dash_len=28, gap_len=16):
    x0, y0, x1, y1 = box
    draw_dashed_line((x0, y0), (x1, y0), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)
    draw_dashed_line((x1, y0), (x1, y1), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)
    draw_dashed_line((x1, y1), (x0, y1), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)
    draw_dashed_line((x0, y1), (x0, y0), fill=outline, width=width, dash_len=dash_len, gap_len=gap_len)

def draw_arrow(p1, p2, fill=col_line_solid, width=7, head_len=22):
    draw.line([p1, p2], fill=fill, width=width)
    x1, y1 = p1
    x2, y2 = p2
    dx = x2 - x1
    dy = y2 - y1
    dist = math.hypot(dx, dy)
    if dist == 0: return
    ux, uy = dx / dist, dy / dist
    px, py = -uy, ux
    pt_tip = (x2, y2)
    pt_left = (x2 - ux * head_len + px * (head_len * 0.55), y2 - uy * head_len + py * (head_len * 0.55))
    pt_right = (x2 - ux * head_len - px * (head_len * 0.55), y2 - uy * head_len - py * (head_len * 0.55))
    draw.polygon([pt_tip, pt_left, pt_right], fill=fill)

def draw_bidirectional_arrow(p1, p2, fill=col_line_solid, width=6, head_len=18):
    draw.line([p1, p2], fill=fill, width=width)
    draw_arrow(p2, p1, fill=fill, width=width, head_len=head_len)
    draw_arrow(p1, p2, fill=fill, width=width, head_len=head_len)

def draw_unit_box(box, text, subtext=None, fill=col_card_bg, outline=col_card_border, width=6):
    x0, y0, x1, y1 = box
    # Solid clean drop shadow
    draw.rectangle([(x0 + 8, y0 + 8), (x1 + 8, y1 + 8)], fill=col_shadow)
    draw.rectangle([x0, y0, x1, y1], fill=fill, outline=outline, width=width)
    
    cx = (x0 + x1) // 2
    f_main = get_font(44 if len(text) <= 5 else 36, bold=True)
    bbox = f_main.getbbox(text)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    
    if subtext:
        cy = (y0 + y1) // 2 - 14
        draw.text((cx - tw // 2, cy - th // 2), text, font=f_main, fill=col_text_dark)
        f_s = get_font(21, bold=False)
        s_bbox = f_s.getbbox(subtext)
        draw.text((cx - (s_bbox[2] - s_bbox[0]) // 2, cy + th // 2 + 10), subtext, font=f_s, fill=col_text_sub)
    else:
        cy = (y0 + y1) // 2
        draw.text((cx - tw // 2, cy - th // 2), text, font=f_main, fill=col_text_dark)

# ==============================================================================
# 3. COLUMN 1: USER EQUIPMENT (UE)
# ==============================================================================
ue_x = 240
f_col = get_font(54, bold=True)
draw.text((ue_x - 30, 430), "UE", font=f_col, fill=col_text_dark)

def draw_radio_arches(cx, cy, radius_list=[28, 48, 68], color=col_text_dark):
    for r in radius_list:
        draw.arc([(cx - r, cy - r), (cx + r, cy + r)], start=210, end=330, fill=color, width=6)

# 1. IoT Gateway (y = 660)
y_iot = 660
draw_radio_arches(ue_x + 10, y_iot - 35, [25, 44, 62])
draw.rounded_rectangle([(ue_x - 70, y_iot), (ue_x + 90, y_iot + 44)], radius=8, fill=(241, 245, 249), outline=col_text_dark, width=5)
draw.ellipse([(ue_x - 50, y_iot + 16), (ue_x - 38, y_iot + 28)], fill=(16, 185, 129))
draw.ellipse([(ue_x - 22, y_iot + 16), (ue_x - 10, y_iot + 28)], fill=col_text_dark)
draw.ellipse([(ue_x + 6, y_iot + 16), (ue_x + 18, y_iot + 28)], fill=col_text_dark)

# 2. Connected Car (y = 1090)
y_car = 1080
draw_radio_arches(ue_x + 10, y_car - 45, [25, 44, 62])
draw.polygon([(ue_x - 85, y_car + 20), (ue_x - 60, y_car - 8), (ue_x - 18, y_car - 30), (ue_x + 50, y_car - 30), (ue_x + 85, y_car - 8), (ue_x + 105, y_car + 10), (ue_x + 105, y_car + 25), (ue_x - 85, y_car + 25)], fill=(241, 245, 249), outline=col_text_dark)
draw.line([(ue_x - 85, y_car + 25), (ue_x + 105, y_car + 25)], fill=col_text_dark, width=6)
draw.polygon([(ue_x - 50, y_car - 2), (ue_x - 14, y_car - 20), (ue_x + 12, y_car - 20), (ue_x + 12, y_car - 2)], fill=col_navy_header)
draw.polygon([(ue_x + 22, y_car - 2), (ue_x + 22, y_car - 20), (ue_x + 46, y_car - 20), (ue_x + 72, y_car - 2)], fill=col_navy_header)
draw.ellipse([(ue_x - 58, y_car + 10), (ue_x - 24, y_car + 44)], fill=(30, 41, 59), outline=col_text_dark, width=5)
draw.ellipse([(ue_x + 46, y_car + 10), (ue_x + 80, y_car + 44)], fill=(30, 41, 59), outline=col_text_dark, width=5)

# 3. Smartphone (y = 1520)
y_phone = 1520
draw.rounded_rectangle([(ue_x - 42, y_phone - 65), (ue_x + 46, y_phone + 75)], radius=14, fill=(241, 245, 249), outline=col_text_dark, width=6)
draw.rectangle([(ue_x - 32, y_phone - 50), (ue_x + 36, y_phone + 54)], fill=(255, 255, 255), outline=(203, 213, 225), width=3)
draw.ellipse([(ue_x - 3, y_phone - 58), (ue_x + 7, y_phone - 52)], fill=col_text_dark)
draw_radio_arches(ue_x + 2, y_phone + 5, [12, 22, 32])

# 4. Workstation / PC (y = 1960)
y_pc = 1960
draw.rectangle([(ue_x - 70, y_pc - 50), (ue_x + 38, y_pc + 30)], fill=(255, 255, 255), outline=col_text_dark, width=6)
draw.rectangle([(ue_x - 22, y_pc + 30), (ue_x - 10, y_pc + 48)], fill=col_text_dark)
draw.rectangle([(ue_x - 38, y_pc + 48), (ue_x + 6, y_pc + 56)], fill=col_text_dark)
draw.rectangle([(ue_x + 52, y_pc - 60), (ue_x + 94, y_pc + 56)], fill=(241, 245, 249), outline=col_text_dark, width=5)
draw.line([(ue_x + 60, y_pc - 42), (ue_x + 86, y_pc - 42)], fill=col_text_dark, width=4)
draw.ellipse([(ue_x + 68, y_pc - 24), (ue_x + 78, y_pc - 14)], fill=(16, 185, 129))

# ==============================================================================
# 4. COLUMN 2: ACCESS NETWORK
# ==============================================================================
an_x0, an_x1 = 780, 1280
draw.text((an_x0 + 35, 410), "Access\nNetwork", font=f_col, fill=col_text_dark)

def draw_cell_tower(cx, cy, is_5g=True):
    draw.polygon([(cx, cy - 75), (cx - 40, cy + 65), (cx + 40, cy + 65)], outline=col_text_dark, fill=None, width=5)
    draw.line([(cx - 18, cy - 18), (cx + 18, cy - 18)], fill=col_text_dark, width=3)
    draw.line([(cx - 30, cy + 24), (cx + 30, cy + 24)], fill=col_text_dark, width=3)
    draw.line([(cx - 40, cy + 65), (cx + 40, cy + 65)], fill=col_text_dark, width=5)
    draw.line([(cx - 30, cy + 24), (cx + 18, cy - 18)], fill=col_text_dark, width=3)
    draw.line([(cx + 30, cy + 24), (cx - 18, cy - 18)], fill=col_text_dark, width=3)
    draw.line([(cx, cy - 75), (cx, cy - 100)], fill=col_text_dark, width=6)
    draw.ellipse([(cx - 9, cy - 108), (cx + 9, cy - 90)], fill=col_crimson if is_5g else col_navy_accent)
    draw_radio_arches(cx, cy - 100, [22, 38, 54], color=col_crimson if is_5g else col_navy_accent)

# 1. 5G BS
box_5g_y = (530, 790)
draw_cell_tower(an_x0 - 95, (box_5g_y[0] + box_5g_y[1]) // 2, is_5g=True)
draw_unit_box((an_x0, box_5g_y[0], an_x1, box_5g_y[1]), "5G BS", "3GPP New Radio (NR gNodeB)")

# 2. 4G BS
box_4g_y = (970, 1230)
draw_cell_tower(an_x0 - 95, (box_4g_y[0] + box_4g_y[1]) // 2, is_5g=False)
draw_unit_box((an_x0, box_4g_y[0], an_x1, box_4g_y[1]), "4G BS", "Legacy LTE (eNodeB Anchor)")

# 3. Wi-Fi Access
box_wifi_y = (1410, 1670)
rx, ry = an_x0 - 95, (box_wifi_y[0] + box_wifi_y[1]) // 2
draw.rounded_rectangle([(rx - 60, ry - 14), (rx + 50, ry + 28)], radius=8, fill=(241, 245, 249), outline=col_text_dark, width=5)
draw.line([(rx - 38, ry - 14), (rx - 38, ry - 52)], fill=col_text_dark, width=5)
draw.line([(rx + 28, ry - 14), (rx + 28, ry - 52)], fill=col_text_dark, width=5)
draw_radio_arches(rx - 5, ry - 35, [18, 34, 50], color=col_navy_accent)
draw_unit_box((an_x0, box_wifi_y[0], an_x1, box_wifi_y[1]), "Wi-Fi", "Untrusted Non-3GPP (N3IWF)")

# 4. Fixed Wireline
box_fixed_y = (1850, 2110)
fx, fy = an_x0 - 95, (box_fixed_y[0] + box_fixed_y[1]) // 2
draw.rectangle([(fx - 70, fy - 18), (fx + 60, fy + 28)], fill=(241, 245, 249), outline=col_text_dark, width=5)
for p in range(6):
    draw.rectangle([(fx - 60 + p*20, fy - 6), (fx - 46 + p*20, fy + 8)], fill=col_navy_header)
draw_unit_box((an_x0, box_fixed_y[0], an_x1, box_fixed_y[1]), "Fixed", "Wireline Broadband (W-5GAN)")

# Connectors: UE to Access
draw_dashed_line((ue_x + 95, y_iot + 15), (an_x0 - 155, (box_5g_y[0] + box_5g_y[1]) // 2), width=5)
draw_dashed_line((ue_x + 105, y_car), (an_x0 - 155, (box_4g_y[0] + box_4g_y[1]) // 2), width=5)
draw.line([(ue_x + 55, y_phone), (an_x0 - 165, (box_wifi_y[0] + box_wifi_y[1]) // 2)], fill=col_line_solid, width=5)
draw.line([(ue_x + 95, y_pc), (an_x0 - 175, (box_fixed_y[0] + box_fixed_y[1]) // 2)], fill=col_line_solid, width=5)

# ==============================================================================
# 5. CONTAINER 1: 5G CONTROL PLANE (SBA)
# ==============================================================================
cp_box = (1450, 440, 3680, 1260)
draw_dashed_rect(cp_box, outline=col_text_dark, width=7, dash_len=30, gap_len=18)

f_plane_hdr = get_font(52, bold=True)
draw.text((cp_box[2] - 440, cp_box[1] + 35), "5G Control\nPlane", font=f_plane_hdr, fill=col_text_dark)

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
draw_dashed_line(((nssf_box[0] + nssf_box[2]) // 2, nssf_box[3]), amf_top_center, width=5)
draw_dashed_line(((ausf_box[0] + ausf_box[2]) // 2, ausf_box[3]), amf_top_center, width=5)
draw_dashed_line(((udm_box[0] + udm_box[2]) // 2, udm_box[3]), amf_top_center, width=5)

# Service Bus between AMF, SMF, PCF, AF
y_bus = (amf_box[1] + amf_box[3]) // 2
draw.line([(amf_box[2], y_bus), (smf_box[0], y_bus)], fill=col_line_solid, width=6)
draw.line([(smf_box[2], y_bus), (pcf_box[0], y_bus)], fill=col_line_solid, width=6)
draw.line([(pcf_box[2], y_bus), (af_box[0], y_bus)], fill=col_line_solid, width=6)

# Control signaling from Access Network into AMF (N2 Interface)
draw_dashed_line((an_x1, (box_5g_y[0] + box_5g_y[1]) // 2), (amf_box[0], y_bus - 35), width=5)
draw_dashed_line((an_x1, (box_4g_y[0] + box_4g_y[1]) // 2), (amf_box[0], y_bus + 35), width=5)

# ==============================================================================
# 6. CONTAINER 2: 5G USER PLANE (CUPS)
# ==============================================================================
up_box = (1700, 1370, 3140, 2220)
draw_dashed_rect(up_box, outline=col_text_dark, width=7, dash_len=30, gap_len=18)

draw.text((up_box[0] + 620, up_box[3] - 75), "5G User Plane", font=f_plane_hdr, fill=col_text_dark)

def draw_server_rack(cx, cy):
    draw.rectangle([(cx - 32, cy - 28), (cx + 32, cy + 28)], fill=(30, 41, 59), outline=col_text_dark, width=3)
    for row in range(3):
        ry = cy - 18 + row * 16
        draw.rectangle([(cx - 26, ry), (cx + 26, ry + 10)], fill=(15, 23, 42))
        draw.ellipse([(cx + 14, ry + 2), (cx + 20, ry + 8)], fill=(16, 185, 129))
        draw.ellipse([(cx + 6, ry + 2), (cx + 12, ry + 8)], fill=(56, 189, 248))

# 3 Distributed UPF Units
upf1_box = (1750, 1430, 2180, 1650) # Top Edge UPF
upf2_box = (1750, 1750, 2180, 1970) # Bottom Edge UPF (Local Breakout)
upf3_box = (2440, 1540, 2920, 1840) # Central Anchor UPF

draw_unit_box(upf1_box, "UPF", "Edge UPF (eMBB)")
draw_server_rack(upf1_box[2] - 50, (upf1_box[1] + upf1_box[3]) // 2)

draw_unit_box(upf2_box, "UPF", "Edge UPF (URLLC Breakout)")
draw_server_rack(upf2_box[2] - 50, (upf2_box[1] + upf2_box[3]) // 2)

draw_unit_box(upf3_box, "UPF", "Intermediate & Central Core UPF")
draw_server_rack(upf3_box[2] - 55, (upf3_box[1] + upf3_box[3]) // 2)

# User Plane Internal Routing
draw.line([(upf1_box[2], (upf1_box[1] + upf1_box[3]) // 2), (upf3_box[0], (upf3_box[1] + upf3_box[3]) // 2 - 30)], fill=col_line_solid, width=7)
draw.line([(upf2_box[2], (upf2_box[1] + upf2_box[3]) // 2), (upf3_box[0], (upf3_box[1] + upf3_box[3]) // 2 + 30)], fill=col_line_solid, width=7)

# User Data payload lines from Access Network converging to UPFs (N3 Interface)
ran_converge_pt = (up_box[0], 1700)
draw.line([(an_x1, (box_5g_y[0] + box_5g_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=6)
draw.line([(an_x1, (box_4g_y[0] + box_4g_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=6)
draw.line([(an_x1, (box_wifi_y[0] + box_wifi_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=6)
draw.line([(an_x1, (box_fixed_y[0] + box_fixed_y[1]) // 2), ran_converge_pt], fill=col_line_solid, width=6)

# Line into UPF inputs
draw.line([ran_converge_pt, (upf1_box[0], (upf1_box[1] + upf1_box[3]) // 2)], fill=col_line_solid, width=6)
draw.line([ran_converge_pt, (upf2_box[0], (upf2_box[1] + upf2_box[3]) // 2)], fill=col_line_solid, width=6)

# Interface Label (R)AN-UPF
f_badge = get_font(26, bold=True)
draw_bidirectional_arrow((1300, 2260), (1680, 2260), fill=col_text_dark, width=6)
draw.text((1360, 2285), "(R)AN-UPF (N3 User Data)", font=f_badge, fill=col_text_dark)

# ==============================================================================
# 7. DATA NETWORKS: LOCAL DN & CENTRAL DN
# ==============================================================================
def draw_database_cloud(cx, cy, label, subtext):
    cyl_w = 100
    cyl_h = 28
    for tier in range(3):
        ty = cy - 28 + tier * 28
        draw.ellipse([(cx - cyl_w//2, ty), (cx + cyl_w//2, ty + cyl_h)], fill=(241, 245, 249), outline=col_text_dark, width=5)
        draw.line([(cx - cyl_w//2, ty + cyl_h//2), (cx - cyl_w//2, ty + cyl_h + 12)], fill=col_text_dark, width=5)
        draw.line([(cx + cyl_w//2, ty + cyl_h//2), (cx + cyl_w//2, ty + cyl_h + 12)], fill=col_text_dark, width=5)
        draw.arc([(cx - cyl_w//2, ty + 12), (cx + cyl_w//2, ty + cyl_h + 12)], start=0, end=180, fill=col_text_dark, width=5)
    
    cloud_x = cx + 65
    cloud_y = cy + 28
    draw.ellipse([(cloud_x - 35, cloud_y - 24), (cloud_x + 35, cloud_y + 24)], fill=(255, 255, 255), outline=col_text_dark, width=5)
    draw.ellipse([(cloud_x - 12, cloud_y - 42), (cloud_x + 30, cloud_y - 4)], fill=(255, 255, 255), outline=col_text_dark, width=5)
    draw.rectangle([(cloud_x - 26, cloud_y - 10), (cloud_x + 26, cloud_y + 22)], fill=(255, 255, 255))
    
    f_lbl = get_font(38, bold=True)
    draw.text((cx + 120, cy - 18), label, font=f_lbl, fill=col_text_dark)
    f_sub = get_font(22, bold=False)
    draw.text((cx + 120, cy + 28), subtext, font=f_sub, fill=col_text_sub)

# 1. Local DN
local_dn_cx, local_dn_cy = 1965, 2380
draw_database_cloud(local_dn_cx, local_dn_cy, "Local DN", "MEC Edge Servers (<1ms Latency)")
draw.line([(upf2_box[0] + (upf2_box[2] - upf2_box[0])//2, upf2_box[3]), (local_dn_cx, local_dn_cy - 45)], fill=col_line_solid, width=7)
draw.line([(local_dn_cx + 90, 2060), (local_dn_cx + 90, 2220)], fill=col_text_dark, width=4)
draw.text((local_dn_cx + 105, 2125), "UPF-DN (N6)", font=get_font(24, bold=True), fill=col_text_dark)

# 2. Central Data Network (DN)
central_dn_cx, central_dn_cy = 3380, 1690
draw_database_cloud(central_dn_cx, central_dn_cy, "DN", "Internet / IMS / Operator Services")
draw.line([(upf3_box[2], (upf3_box[1] + upf3_box[3]) // 2), (central_dn_cx - 75, central_dn_cy)], fill=col_line_solid, width=7)
draw_bidirectional_arrow((3160, 2260), (3420, 2260), fill=col_text_dark, width=6)
draw.text((3230, 2285), "UPF-DN", font=f_badge, fill=col_text_dark)

# ==============================================================================
# 8. CUPS SEPARATION CONDUIT: N4 Interface (SMF to User Plane)
# ==============================================================================
smf_bottom_center = ((smf_box[0] + smf_box[2]) // 2, smf_box[3])
up_top_center = (smf_bottom_center[0], up_box[1])

draw_dashed_line(smf_bottom_center, up_top_center, fill=col_crimson, width=8, dash_len=20, gap_len=12)
draw_arrow(smf_bottom_center, (up_top_center[0], up_top_center[1] + 30), fill=col_crimson, width=7, head_len=26)

f_n4 = get_font(26, bold=True)
n4_text = "N4 Interface: Packet Forwarding Control Protocol (PFCP)"
n4_bbox = f_n4.getbbox(n4_text)
n4_w = n4_bbox[2] - n4_bbox[0]
n4_box_x = smf_bottom_center[0] - n4_w // 2 - 25
draw.rounded_rectangle([(n4_box_x, 1270), (n4_box_x + n4_w + 50, 1340)], radius=10, fill=(255, 255, 255), outline=col_crimson, width=5)
draw.text((n4_box_x + 25, 1285), n4_text, font=f_n4, fill=col_crimson)

# ==============================================================================
# SAVE 4K DIGITAL MASTER
# ==============================================================================
out_path = "D:/5G-Lab-Room/Assets/equipments/cups_banner_clean.png"
img.save(out_path, "PNG", quality=100)
print(f"High-Visibility Academic 4K Banner generated at: {out_path}")
