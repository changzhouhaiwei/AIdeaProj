import pathlib,struct,json,csv,sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
# Reuse the layout decoder without executing the pool extraction loop.
scope={'__file__':str(ROOT/'Tools/extract.py')}
exec((ROOT/'Tools/extract.py').read_text(encoding='utf8').split('rows=[];')[0],scope)
b=(ROOT/'Evidence/config_blocks/10.bin').read_bytes();p=0
def read(fmt):
 global p
 v=struct.unpack_from('<'+fmt,b,p);p+=struct.calcsize('<'+fmt);return v[0] if len(v)==1 else v
def string():
 global p
 n=read('i')
 if n==-1:return None
 if n==0:return ''
 if n<0:
  length=~n;chars=read('i');s=b[p:p+length].decode('utf8');p+=length;return s
 s=b[p:p+n*2].decode('utf-16le');p+=n*2;return s
rules=[];fixed=[]
for _ in range(read('i')):
 key=string();assert read('B')==14
 r={'dictionaryKey':key,'id':string()}
 r.update(zip(['groupId','groupPriority','priority','type','intParam1','intParam2'],read('iiiiii')))
 r['stringParam1']=string();r['stringParam2']=string()
 r.update(zip(['floatParam1','target','operator'],read('fii')))
 r['stringValue1']=string();r['stringValue2']=string();rules.append(r)
 if r['type']==1 and r['target']==1 and r['operator']==1 and ':' in (r['stringValue1'] or ''):
  raw=r['stringValue1'];fixed.append({'key':'FIXED/'+r['id']+'/'+str(r['intParam1']),'ruleId':r['id'],'progressLevel':r['intParam1'],'group':'FIXED','pool':r['id'],'entryId':r['intParam1'],'difficulty':0,'difficultyKnown':False,'raw':raw,**scope['parse'](raw)})
assert p==len(b),(p,len(b))
(ROOT/'match_rules.json').write_text(json.dumps(rules,ensure_ascii=False,indent=2),encoding='utf8')
out=ROOT/'UnityProject/Assets/StreamingAssets/Levels';(out/'FIXED').mkdir(exist_ok=True)
(out/'FIXED/fixed.json').write_text(json.dumps({'entries':fixed},ensure_ascii=False,indent=2),encoding='utf8')
index=json.loads((out/'index.json').read_text(encoding='utf8'));index['entries']=[r for r in index['entries'] if r['group']!='FIXED']
for e in fixed:index['entries'].append({**{k:e[k] for k in ['key','group','pool','entryId','difficulty']},'file':'FIXED/fixed.json'})
(out/'index.json').write_text(json.dumps(index),encoding='utf8')
with (ROOT/'fixed_level_index.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=['key','ruleId','progressLevel','capacity','boardCount','groupsTotal']);w.writeheader();w.writerows({k:e[k] for k in w.fieldnames} for e in fixed)
with (ROOT/'Evidence/fixed_layouts.tsv').open('w',encoding='utf8') as f:
 for e in fixed:f.write(e['key']+'\t'+e['raw']+'\n')
print('rules',len(rules),'fixed layouts',len(fixed),'levels',[x['progressLevel'] for x in fixed])
