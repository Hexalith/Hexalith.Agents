from pathlib import Path
import json,re,shlex
S=Path('/home/administrator/.codex/sessions/2026/10/08/rollout-2026-10-08T22-00-23-01a11d1a-47b5-72c2-8f83-badb7d4ac2cc.jsonl')
events=[json.loads(x) for x in S.open()]
calls={};outputs={}
for d in events:
 b=d.get('payload',{})
 if d.get('type')!='response_item':continue
 if b.get('type')=='custom_tool_call' and b.get('name')=='exec':calls[b['call_id']]=dict(timestamp=d['timestamp'],input=b.get('input',''))
 if b.get('type')=='custom_tool_call_output':outputs.setdefault(b['call_id'],[]).append(d)
def chunks(value):
 if isinstance(value,dict):
  if 'chunk_id' in value:yield value
  else:
   for v in value.values():yield from chunks(v)
 elif isinstance(value,list):
  for v in value:yield from chunks(v)
def returned(call_id):
 result=[]
 for d in outputs.get(call_id,[]):
  for item in d['payload'].get('output',[]):
   if not isinstance(item,dict) or item.get('type')!='input_text':continue
   try:obj=json.loads(item['text'])
   except (ValueError,KeyError):continue
   for c in chunks(obj):result.append(dict(c,toolOutputTimestamp=d['timestamp'],toolOutputCallId=call_id))
 return result
commands=[];pending={};ambiguous=[]
def actions_in(source):
 for m in re.finditer(r'tools\.(exec_command|write_stdin)\(\{',source):
  start=m.end()-1;depth=0;quote=None;escape=False
  for i in range(start,len(source)):
   ch=source[i]
   if quote:
    if escape:escape=False
    elif ch=='\\':escape=True
    elif ch==quote:quote=None
   elif ch in ['"',"'",'`']:quote=ch
   elif ch=='{':depth+=1
   elif ch=='}':
    depth-=1
    if depth==0:
     yield m[1],source[start+1:i]
     break
for cid,call in calls.items():
 actions=[]
 for kind,body in actions_in(call['input']):
  if kind=='exec_command':
   cm=re.search(r'"?cmd"?\s*:\s*("(?:\\.|[^"\\])*")',body)
   if not cm:continue
   wd=re.search(r'"?workdir"?\s*:\s*("(?:\\.|[^"\\])*")',body)
   command=json.loads(cm[1]);matches=re.findall(r'(?:^|\n)\s*(dotnet [^\n]* > /tmp/revision5-[^\n]+\.log 2>&1)',command)
   a={'kind':kind,'command':command,'selected':[]}
   for exact in matches:
    argv,log=exact.rsplit(' > ',1);c={'timestamp':call['timestamp'],'toolCallId':cid,'workingDirectory':json.loads(wd[1]) if wd else '/home/administrator/projects/hexalith/agents','shellCommand':command,'dotnetCommand':exact,'argv':shlex.split(argv),'originalLog':log.removesuffix(' 2>&1')};commands.append(c);a['selected'].append(c)
  else:
   sm=re.search(r'"?session_id"?\s*:\s*(\d+)',body)
   if not sm:continue
   a={'kind':kind,'sessionId':int(sm[1])}
  actions.append(a)
 vals=returned(cid)
 if len(actions)!=len(vals):
  if cid=='call_4d91ddd445374c4f9c1d9f67194fb9e9':
   assert len(actions)==3 and len(vals)==2 and actions[-1]['kind']=='write_stdin' and actions[-1]['sessionId']==65756
   assert vals[0]['chunk_id']=='cbc8a9' and vals[0]['exit_code']==1
   actions=actions[:2]
  else:
   if any(a.get('selected') or a.get('sessionId') in pending for a in actions):ambiguous.append({'call':cid,'timestamp':call['timestamp'],'actions':[(a['kind'],[c['originalLog'] for c in a.get('selected',[])],a.get('sessionId')) for a in actions],'chunks':vals})
   continue
 for a,o in zip(actions,vals):
  tracked=a.get('selected',[]) if a['kind']=='exec_command' else pending.get(a['sessionId'],[])
  if not tracked:continue
  if 'session_id' in o:
   pending[o['session_id']]=tracked
   for c in tracked:c['processSessionId']=o['session_id']
  if 'exit_code' in o:
   for c in tracked:c.update({'completionExitCode':o['exit_code'],'completionToolOutputTimestamp':o['toolOutputTimestamp'],'completionToolCallId':o['toolOutputCallId'],'completionToolChunkId':o['chunk_id'],'completionToolWallTimeSeconds':o['wall_time_seconds']})
missing=[c['originalLog'] for c in commands if 'completionExitCode' not in c]
Path('/tmp/revision5-command-ledger.json').write_text(json.dumps({'commands':commands,'ambiguous':ambiguous,'missingActualCompletion':missing},indent=2)+'\n')
print(json.dumps({'commands':len(commands),'missingActualCompletion':missing,'ambiguous':[(a['call'],a['actions'],[(c['chunk_id'],c.get('session_id'),c.get('exit_code')) for c in a['chunks']]) for a in ambiguous]},indent=2))
