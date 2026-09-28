"""Download free (name-your-price) itch.io packs: python itchdl.py user/slug ..."""
import re, sys, json, urllib.parse
import requests

UA = {'User-Agent': 'Mozilla/5.0'}
for spec in sys.argv[1:]:
    user, slug = spec.split('/')
    base = 'https://%s.itch.io/%s' % (user, slug)
    s = requests.Session()
    s.headers.update(UA)
    page = s.get(base + '/purchase').text
    token = re.search(r'csrf_token" value="([^"]+)"', page).group(1)
    url = s.post(base + '/download_url', data={'csrf_token': token}).json()['url']
    key = urllib.parse.unquote(url.rstrip('/').split('/download/')[1])
    dl = s.get(url).text
    token = re.search(r'csrf_token" value="([^"]+)"', dl)
    token = token.group(1) if token else re.search(r'name="csrf_token" content="([^"]+)"', dl).group(1)
    for uid in sorted(set(re.findall(r'data-upload_id="(\d+)"', dl))):
        r = s.post(base + '/file/%s' % uid, params={'source': 'view_game', 'as_props': '1', 'after_download_lightbox': 'true'}, data={'csrf_token': token}).json()
        print(r) if 'url' not in r else None
        f = s.get(r['url'])
        name = re.findall(r'filename="?([^";]+)"?', f.headers.get('content-disposition', '')) or [slug + '_' + uid + '.zip']
        out = slug + '__' + name[0]
        open(out, 'wb').write(f.content)
        print(spec, '->', out, len(f.content))
