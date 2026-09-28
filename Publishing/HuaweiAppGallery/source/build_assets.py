"""Rebuild the store artwork from existing game art (Pillow required).

No generated gameplay or UI: each screenshot is kept complete and proportional.
Run: python Publishing/HuaweiAppGallery/source/build_assets.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import shutil, json, hashlib

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parents[1]
for folder in ('icons', 'screenshots', 'screenshots_original', 'promo', 'preview'):
    (OUT / folder).mkdir(parents=True, exist_ok=True)
FONT = Path('C:/Windows/Fonts')
GREEN = '#173E35'
CREAM = '#FBF4E5'
GOLD = '#B88B42'
INK = '#233C32'

def font(size, serif=False):
    return ImageFont.truetype(str(FONT / ('georgiab.ttf' if serif else 'segoeui.ttf')), size)

def center(draw, text, y, size, fill=INK, serif=False, width=1080):
    f = font(size, serif)
    box = draw.textbbox((0, 0), text, font=f)
    assert box[2] - box[0] < width - 64, text
    draw.text(((width - (box[2]-box[0]))/2, y), text, fill=fill, font=f)

owl = Image.open(ROOT / 'Assets/Art/owl_mascot.png').convert('RGBA')

# Square opaque store icon; same original character, no AI redrawing.
icon = Image.new('RGB', (1024, 1024), GREEN)
d = ImageDraw.Draw(icon)
d.ellipse((84, 48, 940, 940), fill='#E0B967')
d.ellipse((110, 74, 914, 914), fill='#F4DCA0')
mascot = owl.copy()
mascot.thumbnail((910, 920), Image.Resampling.LANCZOS)
icon.paste(mascot, ((1024-mascot.width)//2, 52), mascot)
for size in (216, 512, 1024):
    icon.resize((size, size), Image.Resampling.LANCZOS).save(OUT / f'icons/app-icon-{size}.png', optimize=True)

slides = [
    ('01_gameplay', 'Review/ContentCatalogReview/Gameplay_ThreeHearts.png', ['Раскройте', 'тайну букв'], 'Находите буквы. Разгадывайте фразы.'),
    ('02_collections', 'Review/ContentCatalogReview/Collections.png', ['Собирайте', 'любимые цитаты'], 'Авторы, темы и книги в вашей коллекции.'),
    ('03_text_types', 'Review/VarietyReview/TextKinds.png', ['Каждый текст —', 'новая загадка'], 'Цитаты, истории, пословицы и загадки.'),
    ('05_progress', 'Review/FeedbackPass/Statistics.png', ['Замечайте', 'свой прогресс'], 'Решения, точность и игровая активность.'),
]
provenance = []
for index, (name, source, lines, subtitle) in enumerate(slides, 1):
    src = ROOT / source
    shutil.copy2(src, OUT / f'screenshots_original/{name}.png')
    shot = Image.open(src).convert('RGB')
    # Store screenshots are the full real game screen, without marketing decoration.
    shot.save(OUT / f'screenshots/{name}.jpg', quality=95, subsampling=0, optimize=True)
    provenance.append({'file':f'screenshots/{name}.jpg','source':source,'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'size':list(shot.size), 'method':'full unmodified game screenshot; JPEG encoding only'})

# Optional social/promotion artwork; these are not claimed to be required store fields.
banner = Image.new('RGB', (1920,1080), GREEN)
d = ImageDraw.Draw(banner)
d.rectangle((72,82,78,994),fill=GOLD)
d.text((124,130),'К Р И П Т О Г Р А М М Ы',font=font(36),fill='#E7C889')
d.text((118,248),'Шифры',font=font(136,True),fill=CREAM)
d.text((118,400),'совы',font=font(136,True),fill=CREAM)
d.text((126,635),'Буква за буквой —',font=font(48),fill=CREAM)
d.text((126,706),'к маленьким открытиям.',font=font(48),fill=CREAM)
d.text((126,914),'Цитаты • Истории • Загадки',font=font(36),fill='#E7C889')
large=owl.copy(); large.thumbnail((860,920),Image.Resampling.LANCZOS)
banner.paste(large,(1030,100),large)
banner.save(OUT/'promo/banner-1920x1080.jpg',quality=95,subsampling=0,optimize=True)

# A compact contact sheet for the owner to review the complete set.
contact = Image.new('RGB',(1440,780),'#FFFFFF')
for i, (name,*_) in enumerate(slides):
    card=Image.open(OUT/f'screenshots/{name}.jpg'); card.thumbnail((350,760),Image.Resampling.LANCZOS)
    contact.paste(card,(10+i*360,10))
contact.save(OUT/'preview/contact-sheet.jpg',quality=94)
(OUT/'source/provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2),encoding='utf-8')
print('Created: 3 icons, 4 plain screenshots, original screenshots, 1 banner, contact sheet.')
