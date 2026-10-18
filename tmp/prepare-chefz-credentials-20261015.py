import json,base64,secrets,hmac,hashlib,os
from pathlib import Path
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
private=Path('artifacts/chefz-import-20261015');private.mkdir(parents=True,exist_ok=True)
keyfile=private/'encryption-key.txt'
if not keyfile.exists():keyfile.write_text(base64.b64encode(secrets.token_bytes(64)).decode(),encoding='utf-8')
master=base64.b64decode(keyfile.read_text().strip());assert len(master)>=32
cfg=json.loads(Path('artifacts/chefz-import-20261015/current-appsettings.json').read_text(encoding='utf-8-sig'))
assert not cfg.get('SensitiveData',{}).get('EncryptionKey') or cfg['SensitiveData']['EncryptionKey']==base64.b64encode(master).decode(),'Different key found: do not replace'
cfg.setdefault('SensitiveData',{})['EncryptionKey']=base64.b64encode(master).decode()
(private/'appsettings.proposed.json').write_text(json.dumps(cfg,ensure_ascii=False,indent=2),encoding='utf-8')
planpath=Path('tmp/chefz-plan-20261015.json');plan=json.loads(planpath.read_text(encoding='utf-8'))
source=json.loads(Path('artifacts/chefz-import-20261015/source.json').read_text(encoding='utf-8'))
rows={x['row']:x['cells'] for x in source['sheets'][0]['rows']}
aes=AESGCM(hmac.new(master,b'LogisticsERP.PlatformCredential.v1',hashlib.sha256).digest())
encrypted=[]
for p in plan['passwordRows']:
 password=str(rows[p['row']]['I']).strip().encode('utf-8');nonce=secrets.token_bytes(12);result=aes.encrypt(nonce,password,None)
 assert aes.decrypt(nonce,result,None)==password
 encrypted.append({'accountId':p['accountId'],'id':str(__import__('uuid').uuid4()),'ciphertext':base64.b64encode(result[:-16]).decode(),
 'nonce':base64.b64encode(nonce).decode(),'tag':base64.b64encode(result[-16:]).decode()})
plan['credentials']=encrypted
planpath.write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared encrypted credential versions:',len(encrypted),'; verified AES-GCM round trips. No passwords or key printed.')
