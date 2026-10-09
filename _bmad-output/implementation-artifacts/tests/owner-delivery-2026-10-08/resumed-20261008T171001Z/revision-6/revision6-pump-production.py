from pathlib import Path
p=Path('/home/administrator/projects/hexalith/conversations/src/Hexalith.Conversations.Server/Agents/ConversationDeletionDeliveryPump.cs')
s=p.read_text()
s=s.replace('    private TimeProvider OperationClock => timeProvider ?? TimeProvider.System;','    private TimeProvider OperationClock => timeProvider ?? TimeProvider.System;')
s=s.replace('    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();','    {\n        TimeProvider clock = OperationClock;\n        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), clock, cancellationToken, clock.GetTimestamp());\n        try\n        {\n            deadline.ThrowIfCancellationRequested();',2)
s=s.replace('StillAuthorizedAsync(admitted,','StillAuthorizedAsync(deadline, admitted,').replace('StillAuthorizedAsync(actor,','StillAuthorizedAsync(deadline, actor,')
s=s.replace('AcknowledgeAsync(admitted,','AcknowledgeAsync(deadline, admitted,').replace('QuarantineAsync(admitted,','QuarantineAsync(deadline, admitted,').replace('QuarantineAsync(actor,','QuarantineAsync(deadline, actor,')
s=s.replace('StillAuthorizedAsync(ConversationAgentAuthorization','StillAuthorizedAsync(AuthoritativeStreamReadDeadline deadline, ConversationAgentAuthorization')
s=s.replace('AcknowledgeAsync(ConversationAgentAuthorization','AcknowledgeAsync(AuthoritativeStreamReadDeadline deadline, ConversationAgentAuthorization')
s=s.replace('QuarantineAsync(ConversationAgentAuthorization','QuarantineAsync(AuthoritativeStreamReadDeadline deadline, ConversationAgentAuthorization')
# Replace only independent dependency invocations, retaining the pump continuation on its original await path.
for method,receiver in [('AuthorizeAsync','worker'),('AuthorizeDeliveryAsync','worker'),('CurrentTargetAsync','receiver'),('LookupAsync','receiver'),('SubmitAsync','receiver'),('GetConversationDeletionSourceAsync','conversations'),('RecordConversationDeletionDeliveryAsync','conversations')]:
    needle='await '+receiver+'.'+method+'('
    pos=0
    while True:
        at=s.find(needle,pos)
        if at<0: break
        start=at+len('await ')
        opening=s.find('(',start)
        depth=1;i=opening+1
        in_string=False
        while depth:
            c=s[i]
            if c=='"' and (i==0 or s[i-1]!='\\'): in_string=not in_string
            if not in_string:
                if c=='(': depth+=1
                elif c==')': depth-=1
            i+=1
        call=s[start:i]
        # The outer dependency token is always the final argument.
        for token in ['cancellationToken','token']:
            if call.endswith(', '+token+')'): call=call[:-len(token)-1]+'providerToken)';break
        end=i
        while end<len(s) and s[end].isspace(): end+=1
        if s.startswith('.WaitAsync(',end):
            end=s.find(')',end)+1
        if method=='RecordConversationDeletionDeliveryAsync':
            wrapped='await deadline.ReadAsync(async providerToken => { await '+call+'.ConfigureAwait(false); return true; })'
        else:
            wrapped='await deadline.ReadAsync(providerToken => '+call+')'
        s=s[:at]+wrapped+s[end:]
        pos=at+len(wrapped)
s=s.replace('cancellationToken.ThrowIfCancellationRequested();','deadline.ThrowIfCancellationRequested();').replace('token.ThrowIfCancellationRequested();','deadline.ThrowIfCancellationRequested();')
# Cancellation always reports the original caller token; expiration has the existing safe unavailable form.
for typ in ['ConversationAgentsOutcome','SourcePublicationDeliveryStatus']:
    old='        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException or JsonException)\n        { deadline.ThrowIfCancellationRequested(); return '+typ+'.Unavailable; }\n        catch (Exception) { deadline.ThrowIfCancellationRequested(); throw; }'
    new='        catch (OperationCanceledException) { cancellationToken.ThrowIfCancellationRequested(); return '+typ+'.Unavailable; }\n        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ArgumentException or JsonException)\n        { cancellationToken.ThrowIfCancellationRequested(); return '+typ+'.Unavailable; }\n        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }'
    assert old in s,typ
    s=s.replace(old,new,1)
# Each successful bounded provider read already checks the budget. Check it again at every terminal release,
# including the final local authority/source correlation decision.
lines=s.splitlines()
in_guarded=False;out=[]
for line in lines:
    if '        try'==line: in_guarded=True
    if line.startswith('        catch'): in_guarded=False
    private=any('private async Task<' in x for x in out[-1:]) # terminal private helpers are handled below
    if in_guarded and 'return ' in line:
        line=line.replace('return ','deadline.ThrowIfCancellationRequested(); return ',1)
    out.append(line)
s='\n'.join(out)+'\n'
s=s.replace('        return persisted.Outcome ==','        deadline.ThrowIfCancellationRequested();\n        return persisted.Outcome ==')
p.write_bytes(s.replace('\n','\r\n').encode())
p=Path('/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj')
s=p.read_text();s=s.replace('    <InternalsVisibleTo Include="Hexalith.EventStore.Server" />','    <InternalsVisibleTo Include="Hexalith.EventStore.Server" />\n    <InternalsVisibleTo Include="Hexalith.Conversations.Server" />');p.write_bytes(s.replace('\n','\r\n').encode())
