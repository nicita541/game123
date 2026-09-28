"""Check dimensions, source integrity, local links and release text limits."""
from pathlib import Path
from PIL import Image
from html.parser import HTMLParser
from urllib.parse import urlparse,unquote
import json,hashlib,datetime

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT.parents[1]
checks=[]
def check(condition,message):
    if not condition: raise AssertionError(message)
    checks.append(message)

for size in (216,512,1024):
    path=ROOT/f'icons/app-icon-{size}.png'
    with Image.open(path) as im:
        check(im.size==(size,size) and im.mode=='RGB',f'{path.name}: square RGB PNG')
    check(path.stat().st_size<2_000_000,f'{path.name}: below 2 MB (prepared size, not a claim about store limits)')
cards=sorted((ROOT/'screenshots').glob('*.jpg'))
check(len(cards)==4,'Four plain screenshots')
for path in cards:
    with Image.open(path) as im:
        check(im.size==(1080,2340) and im.mode=='RGB',f'{path.name}: 1080x2340 RGB')
    check(path.stat().st_size<3_000_000,f'{path.name}: below 3 MB')
provenance=json.loads((ROOT/'source/provenance.json').read_text(encoding='utf-8'))
for row in provenance:
    orig=ROOT/'screenshots_original'/Path(row['file']).with_suffix('.png').name
    source=PROJECT/row['source']
    check(hashlib.sha256(orig.read_bytes()).hexdigest()==row['source_sha256']==hashlib.sha256(source.read_bytes()).hexdigest(),f'{orig.name}: identical to recorded original')
    with Image.open(orig) as im: check(im.size==(1080,2340),f'{orig.name}: original 1080x2340')

listing={p.stem:p.read_text(encoding='utf-8').strip() for p in (ROOT/'listing').glob('*.txt')}
check(listing['title']=='Криптограммы: Шифры совы','Final title is consistent')
check(len(listing['short-description'])<=80,'Short description at most 80 characters')
fields=json.loads((ROOT/'listing/store-fields.json').read_text(encoding='utf-8'))
check(fields['supportEmail']=='prostosite42@gmail.com' and fields['releaseCountries']==['RU'],'Owner email and Russia consistent')
check(fields['releaseApk'] is None and fields['privacyPolicyUrl'] is None and fields['ageRating'] is None,'Unfinished release fields are explicitly unset')
settings=(PROJECT/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8')
check('productName: "'+fields['title']+'"' in settings,'Unity Product Name matches store')
check('companyName: "'+fields['developerDisplayName']+'"' in settings,'Unity Company Name matches developer')

class Links(HTMLParser):
    def __init__(self): super().__init__(); self.targets=[]
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('src','href') and value: self.targets.append(value)
for path in ROOT.rglob('*.html'):
    parser=Links(); parser.feed(path.read_text(encoding='utf-8'))
    for url in parser.targets:
        parts=urlparse(url)
        if parts.scheme or not parts.path: continue
        check((path.parent/unquote(parts.path)).is_file(),f'{path.relative_to(ROOT)}: local link {url}')

files=[]
for p in sorted(ROOT.rglob('*')):
    if not p.is_file() or p.name in ('FILE_MANIFEST.json','VERIFICATION.json') or '__pycache__' in p.parts: continue
    check(p.suffix.lower() not in ('.apk','.aab','.jks','.keystore'),f'{p.relative_to(ROOT)}: no application binary or signing key')
    files.append({'file':p.relative_to(ROOT).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(ROOT/'FILE_MANIFEST.json').write_text(json.dumps(files,ensure_ascii=False,indent=2),encoding='utf-8')
result={'checkedAt':datetime.datetime.now(datetime.timezone.utc).isoformat(),'checksPassed':len(checks),'textCharacters':{k:len(v) for k,v in listing.items()},'checks':checks,'limits':'This validates prepared materials only. No Huawei console acceptance, release APK, real device, legal clearance or SDK network behavior was tested.'}
(ROOT/'VERIFICATION.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'PASS: {len(checks)} checks; {len(files)} files recorded. Short description: {len(listing["short-description"])} characters.')
