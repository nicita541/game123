"""Create portable local HTML previews from the prepared text files."""
from pathlib import Path
import html, re

ROOT=Path(__file__).resolve().parents[1]
STYLE='''*{box-sizing:border-box}body{margin:0;background:#fbf4e5;color:#233c32;font:17px/1.65 Segoe UI,Arial,sans-serif}main{max-width:1120px;margin:48px auto;padding:0 24px 80px}h1,h2,h3{font-family:Georgia,serif;line-height:1.2}h1{font-size:46px;max-width:850px}h2{font-size:29px;margin-top:48px}h3{font-size:23px}a{color:#246853}a:hover{color:#aa7529}header{background:#173e35;color:#fbf4e5;padding:22px 28px;letter-spacing:2px}header a{color:#fbf4e5}p{max-width:900px}pre{background:#fffaf0;border:1px solid #d7c7a8;padding:20px;white-space:pre-wrap;overflow-wrap:anywhere}code{font-size:14px}table{border-collapse:collapse;width:100%;background:#fffbf3}td,th{text-align:left;padding:12px;border:1px solid #d9ceb9}th{background:#ebe5d5}.notice{padding:20px 24px;background:#f1e2bf;border-left:4px solid #ad803c;border-radius:3px;margin:24px 0}.gallery{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}.gallery img{width:100%;border-radius:8px;box-shadow:0 10px 25px #173e3520}.hero{width:100%;border-radius:12px;margin:24px 0}.icon{width:144px;border-radius:20px;float:right;margin:0 0 24px 20px}.links{display:flex;gap:12px;flex-wrap:wrap;margin:24px 0}.links a{display:block;padding:10px 18px;background:#173e35;color:#fff8e7;border-radius:6px;text-decoration:none}.copy{width:100%;font:16px/1.6 Segoe UI,sans-serif;min-height:95px;resize:vertical;padding:18px;background:#fffbf3;border:1px solid #c9bb9e;border-radius:8px;color:#233c32}button{margin:8px 0 20px;padding:10px 20px;background:#173e35;color:#fff8e7;border:0;border-radius:5px;cursor:pointer}.muted{color:#63766d;font-size:14px}li{margin:8px 0}@media(max-width:800px){.gallery{grid-template-columns:repeat(2,1fr)}h1{font-size:34px}.icon{width:90px}main{margin:24px auto}table{font-size:14px}}@media print{header,.links,button{display:none}main{max-width:none;margin:0}h1{font-size:30px}h2{break-after:avoid}pre,table{font-size:11px}body{font-size:12px}.notice{background:white}}'''

def inline(s):
    s=html.escape(s)
    s=re.sub(r'`([^`]+)`',r'<code>\1</code>',s)
    s=re.sub(r'\*\*([^*]+)\*\*',r'<strong>\1</strong>',s)
    s=re.sub(r'\[([^\]]+)\]\((https?://[^)]+)\)',r'<a href="\2">\1</a>',s)
    return s

def md(text):
    lines=text.splitlines(); result=[]; i=0
    while i<len(lines):
        line=lines[i]
        if not line.strip(): i+=1; continue
        if line.startswith('```'):
            code=[]; i+=1
            while i<len(lines) and not lines[i].startswith('```'):
                code.append(lines[i]); i+=1
            result.append('<pre><code>'+html.escape('\n'.join(code))+'</code></pre>'); i+=1; continue
        m=re.match(r'^(#{1,6}) (.*)',line)
        if m:
            level=len(m[1]); result.append(f'<h{level}>{inline(m[2])}</h{level}>'); i+=1; continue
        if line.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].startswith('|'):
                cells=[c.strip() for c in lines[i].strip('|').split('|')]
                if not all(re.fullmatch(r'[:\- ]+',c) for c in cells): rows.append(cells)
                i+=1
            result.append('<table>'+''.join('<tr>'+''.join(f'<{"th" if n==0 else "td"}>{inline(c)}</{"th" if n==0 else "td"}>' for c in row)+'</tr>' for n,row in enumerate(rows))+'</table>'); continue
        if re.match(r'^(?:- |\d+\. )',line):
            ordered=bool(re.match(r'^\d+\. ',line)); tag='ol' if ordered else 'ul'; items=[]
            while i<len(lines) and re.match(r'^(?:- |\d+\. )',lines[i]):
                value=re.sub(r'^(?:- |\d+\. )','',lines[i]); value=value.replace('[x]','✓').replace('[ ]','☐')
                items.append('<li>'+inline(value)+'</li>'); i+=1
            result.append(f'<{tag}>'+''.join(items)+f'</{tag}>'); continue
        para=[line]; i+=1
        while i<len(lines) and lines[i].strip() and not re.match(r'^(?:#|\||```|- |\d+\. )',lines[i]):
            para.append(lines[i]); i+=1
        result.append('<p>'+inline(' '.join(para))+'</p>')
    return '\n'.join(result)

def page(title,body):
    return '<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+html.escape(title)+'</title><style>'+STYLE+'</style></head><body><header>ШИФРЫ СОВЫ · МАТЕРИАЛЫ ДЛЯ APPGALLERY</header><main>'+body+'</main></body></html>'

docs=['START_HERE','INSTRUCTION_RU','RELEASE_CHECKLIST','TECHNICAL_AUDIT','ASSETS','SOURCES']
for name in docs:
    body='<a href="preview/index.html">← К обзору материалов</a>'+md((ROOT/f'{name}.md').read_text(encoding='utf-8'))
    (ROOT/f'{name}.html').write_text(page(name,body),encoding='utf-8')
privacy=md((ROOT/'privacy/PRIVACY_POLICY_DRAFT_RU.md').read_text(encoding='utf-8'))
(ROOT/'privacy/privacy-policy-draft.html').write_text(page('Черновик политики конфиденциальности',privacy),encoding='utf-8')

gallery=''.join(f'<a href="../screenshots/{f.name}"><img src="../screenshots/{f.name}" alt="{html.escape(f.stem)}"></a>' for f in sorted((ROOT/'screenshots').glob('*.jpg')))
copyblocks=[]
for name,label,height in [('title','Название',80),('short-description','Краткое описание',95),('full-description','Полное описание',420),('whats-new','Что нового',120),('reviewer-notes','Примечания для модератора',230)]:
    text=(ROOT/f'listing/{name}.txt').read_text(encoding='utf-8').strip()
    copyblocks.append(f'<h3>{label}</h3><p class="muted">{len(text)} символов</p><textarea class="copy" id="{name}" style="height:{height}px" readonly>{html.escape(text)}</textarea><button data-copy="{name}">Копировать</button>')
body='''<img class="icon" src="../icons/app-icon-512.png" alt="Иконка: сова с книгой"><p class="muted">РОССИЯ · ANDROID · РУССКИЙ ЯЗЫК</p><h1>Криптограммы:<br>Шифры совы</h1><p>Разработчик: <strong>Никита Цурбан</strong><br>Поддержка: <a href="mailto:prostosite42@gmail.com">prostosite42@gmail.com</a></p><div class="links"><a href="../INSTRUCTION_RU.html">Как опубликовать</a><a href="../RELEASE_CHECKLIST.html">Проверка перед отправкой</a><a href="../privacy/privacy-policy-draft.html">Черновик политики</a></div><div class="notice"><strong>Материалы готовы, выпуск ещё не отправлен.</strong> До публикации нужны релизная подпись, рабочие рекламные ID, завершённая публичная политика и проверка окончательного APK на Huawei. Старый APK 0.1.5 не подходит как готовый релиз.</div><h2>Скриншоты игры</h2><p>1080 × 2340 · JPEG · четыре полных экрана игры без рекламных подписей и рамок. Нажмите на снимок, чтобы открыть полный размер.</p><div class="gallery">'''+gallery+'''</div><p>Исходники PNG находятся в <code>screenshots_original/</code>. Перед отправкой сравните их с финальной сборкой.</p><h2>Иконки</h2><div class="links"><a href="../icons/app-icon-216.png">216 × 216</a><a href="../icons/app-icon-512.png">512 × 512</a><a href="../icons/app-icon-1024.png">1024 × 1024</a></div><h2>Дополнительный баннер</h2><a href="../promo/banner-1920x1080.jpg"><img class="hero" src="../promo/banner-1920x1080.jpg" alt="Шифры совы: промобаннер"></a><p class="muted">Для продвижения. Не заменяет скриншоты игры и не заявлен обязательным ресурсом AppGallery.</p><h2>Тексты для копирования</h2>'''+''.join(copyblocks)+'''<h2>Все документы</h2><ul><li><a href="../START_HERE.html">Состав комплекта и оставшиеся действия</a></li><li><a href="../TECHNICAL_AUDIT.html">Проверенное состояние проекта</a></li><li><a href="../ASSETS.html">Размеры и происхождение картинок</a></li><li><a href="../SOURCES.html">Официальные источники</a></li></ul><p class="muted">Подготовлено 28 сентября 2026 года. Это локальный комплект, а не опубликованная карточка магазина.</p><script>document.querySelectorAll('[data-copy]').forEach(b=>b.onclick=async()=>{const t=document.getElementById(b.dataset.copy);t.focus();t.select();try{await navigator.clipboard.writeText(t.value);b.textContent='Скопировано'}catch(e){b.textContent='Текст выделен — нажмите Ctrl+C'}setTimeout(()=>b.textContent='Копировать',3000)});</script>'''
(ROOT/'preview/index.html').write_text(page('Криптограммы: Шифры совы — комплект AppGallery',body),encoding='utf-8')
print('Created offline overview, HTML instructions, checklist and privacy draft.')
