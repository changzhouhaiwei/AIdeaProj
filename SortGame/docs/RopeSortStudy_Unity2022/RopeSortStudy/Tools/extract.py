import sys, pathlib, struct, json, csv, hashlib, collections
sys.path.insert(0, 'D:/Tool/work/mahjong_deps')
import brotli
ROOT=pathlib.Path(__file__).resolve().parents[1]
SRC=pathlib.Path(sys.argv[1]) if len(sys.argv)>1 else ROOT/'Evidence/SourceData'
OUT=ROOT/'UnityProject/Assets/StreamingAssets/Levels'
OUT.mkdir(parents=True,exist_ok=True)
B62='0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
def column(s):
    p=s.split('.'); a=p[0].split(','); lock={'kind':'none','count':0,'color':-1}
    for attr in a[1:]:
        v=list(map(B62.index,attr))
        lock={'kind':'collect_count' if v[0]==1 else 'collect_type','count':v[-1],'color':v[1] if v[0]==2 else -1}
    beads=[{'color':B62.index(c),'hidden':False} for c in (p[1] if len(p)>1 else '')]
    for mask in p[2:]:
        v=list(map(B62.index,mask.split(','))); bits=sum(x<<(5*i) for i,x in enumerate(v[1:])); beads[v[0]]['hidden']=bool(bits&1)
    return {'targetColor':B62.index(a[0]) if a[0] else -1,'beads':beads,'lock':lock}
def parse(raw):
    head,body=raw.split(':',1); cap,n=map(int,head.split(';')); cols=list(map(column,body.split(';')))
    return {'capacity':cap,'boardCount':n,'groupsTotal':sum(c['targetColor']>=0 for c in cols),'initial':cols[:n],'incoming':cols[n:]}
rows=[]; pools=[]; summary={}; seen=set(); raw_lines=[]
for group in ['gp101','gp201','mf1']:
    data=next(SRC.glob('*experiment_'+group+'_level_data_bytes*')).read_bytes()
    idx=list(csv.DictReader((SRC/(group+'_data_offsets.csv')).open(encoding='utf-8-sig')))
    cat={r['Id']:r for r in csv.DictReader((SRC/(group+'_catalog.csv')).open(encoding='utf-8-sig'))}
    count=0
    for pool in idx:
        start,size=int(pool['Value1']),int(pool['Value2']); blob=brotli.decompress(data[start:start+size]); h=struct.unpack_from('<i',blob)[0]; assert h%16==0
        entries=[]
        for pos in range(4,4+h,16):
            entry,diff,off,length=struct.unpack_from('<ifii',blob,pos); raw=blob[4+h+off:4+h+off+length].decode(); config=parse(raw)
            key=f'{group.upper()}/{pool["Id"]}/{entry}'; assert key not in seen; seen.add(key)
            item={'key':key,'group':group.upper(),'pool':pool['Id'],'entryId':entry,'difficulty':diff,'raw':raw,**config}; entries.append(item); raw_lines.append(key+'\t'+raw)
            cols=config['initial']+config['incoming']
            rows.append({'key':key,'group':group.upper(),'pool':pool['Id'],'entryId':entry,'difficulty':diff,'capacity':config['capacity'],'boardCount':config['boardCount'],'groupsTotal':config['groupsTotal'],'incomingCount':len(config['incoming']),'hiddenCount':sum(b['hidden'] for c in cols for b in c['beads']),'lockCount':sum(c['lock']['kind']!='none' for c in cols),'file':group.upper()+'/'+pool['Id']+'.json'})
        folder=OUT/group.upper(); folder.mkdir(exist_ok=True)
        (folder/(pool['Id']+'.json')).write_text(json.dumps({'entries':entries},ensure_ascii=False,indent=2),encoding='utf8')
        pools.append({'group':group.upper(),'pool':pool['Id'],'count':len(entries),'minDifficulty':float(cat[pool['Id']]['Value1']),'maxDifficulty':float(cat[pool['Id']]['Value2']),'offset':start,'compressedLength':size})
        count+=len(entries)
    summary[group.upper()]={'pools':len(idx),'layouts':count}
for name,rs in [('level_index',rows),('pool_index',pools)]:
    with (ROOT/(name+'.csv')).open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rs[0]));w.writeheader();w.writerows(rs)
(OUT/'index.json').write_text(json.dumps({'entries':rows},ensure_ascii=False),encoding='utf8')
(ROOT/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf8')
(ROOT/'Evidence/all_layouts.tsv').write_text('\n'.join(raw_lines)+'\n',encoding='utf8')
print(json.dumps(summary)); print('TOTAL',len(rows))

