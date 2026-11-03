#!/usr/bin/env python3
"""
Fetch CC0 assets from Poly Haven into godot/assets/polyhaven/<id>/ and record each one in
godot/assets/polyhaven/LICENSES.json. Poly Haven assets are CC0 (public domain): free for
commercial use, no attribution required. We record the source anyway for the asset log.

Usage: fetch_polyhaven.py [--res 1k] model:<id> ... texture:<id> ...
"""
import json
import os
import sys
import urllib.request

ROOT = os.path.join(os.path.dirname(__file__), '..', 'assets', 'polyhaven')
API = 'https://api.polyhaven.com'
HEADERS = {'User-Agent': 'PanaceaAssetFetch/1.0'}
TEXTURE_MAPS = {'Diffuse': 'albedo', 'nor_gl': 'normal', 'Rough': 'roughness', 'AO': 'ao'}


def get_json(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS)) as r:
        return json.load(r)


def download(url, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if os.path.exists(path):
        return
    with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS)) as r, open(path, 'wb') as f:
        f.write(r.read())


def fetch_model(asset_id, res):
    files = get_json(f'{API}/files/{asset_id}')['gltf'][res]['gltf']
    out = os.path.join(ROOT, asset_id)
    download(files['url'], os.path.join(out, f'{asset_id}.gltf'))
    for rel, info in files.get('include', {}).items():
        download(info['url'], os.path.join(out, rel))


def fetch_texture(asset_id, res):
    files = get_json(f'{API}/files/{asset_id}')
    out = os.path.join(ROOT, asset_id)
    for key, name in TEXTURE_MAPS.items():
        if key in files and res in files[key]:
            download(files[key][res]['jpg']['url'], os.path.join(out, f'{name}.jpg'))


def main(argv):
    res = '1k'
    if argv[:1] == ['--res']:
        res, argv = argv[1], argv[2:]
    log_path = os.path.join(ROOT, 'LICENSES.json')
    os.makedirs(ROOT, exist_ok=True)
    log = json.load(open(log_path)) if os.path.exists(log_path) else {}
    for spec in argv:
        kind, asset_id = spec.split(':', 1)
        info = get_json(f'{API}/info/{asset_id}')
        (fetch_model if kind == 'model' else fetch_texture)(asset_id, res)
        log[asset_id] = {
            'kind': kind,
            'name': info.get('name'),
            'authors': sorted(info.get('authors', {}).keys()),
            'source': f'https://polyhaven.com/a/{asset_id}',
            'license': 'CC0 1.0',
            'resolution': res,
        }
        print('fetched', kind, asset_id)
    with open(log_path, 'w') as f:
        json.dump(dict(sorted(log.items())), f, indent=2)
        f.write('\n')


if __name__ == '__main__':
    main(sys.argv[1:])
