#!/usr/bin/env python3
"""Regressão em ambiente descartável. Não executar contra banco de produção."""
import concurrent.futures,datetime,email,html,json,os,pathlib,queue,re,secrets,socketserver,subprocess,threading,urllib.error,urllib.parse,urllib.request,uuid
from zoneinfo import ZoneInfo
ROOT=pathlib.Path(__file__).resolve().parents[1]
API=os.getenv('PIM_TEST_API','http://127.0.0.1:5114/api');WEB=os.getenv('PIM_TEST_WEB','http://127.0.0.1:5299')
RESULT=[];MAIL=queue.Queue()
def check(name,ok,detail=''):
 RESULT.append({'cenario':name,'passou':bool(ok),'resultado':detail});print(('OK ' if ok else 'FALHOU ')+name+' '+detail,flush=True)
 if not ok:raise AssertionError(name)
def request(method,path,body=None,token=None):
 h={'Content-Type':'application/json'}
 if token:h['Authorization']='Bearer '+token
 r=urllib.request.Request(API+path,headers=h,data=None if body is None else json.dumps(body).encode(),method=method)
 try:resp=urllib.request.urlopen(r,timeout=35)
 except urllib.error.HTTPError as e:resp=e
 raw=resp.read().decode()
 try:data=json.loads(raw)
 except ValueError:data=raw
 return resp.status,data

def sql(text):
 env=os.environ.copy();r=subprocess.run([os.getenv('DOTNET_EXE','dotnet'),str(ROOT/'tests/SqlRunner/bin/Debug/net10.0/SqlRunner.dll')],input=text,env=env,text=True,capture_output=True,timeout=100)
 try:return json.loads(r.stdout)
 except ValueError:raise RuntimeError('SqlRunner indisponível: compile o utilitário e configure PIM_TEST_CONNECTION.')
class SMTP(socketserver.StreamRequestHandler):
 def handle(self):
  def send(t):self.wfile.write((t+'\r\n').encode());self.wfile.flush()
  send('220 localhost test sink')
  while line:=self.rfile.readline():
   cmd=line.decode(errors='replace').strip().upper()
   if cmd.startswith(('EHLO','HELO')):send('250-localhost');send('250 SIZE 10485760')
   elif cmd=='DATA':
    send('354 send message');data=b''
    while (l:=self.rfile.readline()) not in [b'.\r\n',b'.\n',b''] :data+=l
    MAIL.put(data);send('250 accepted locally')
   elif cmd=='QUIT':send('221 bye');break
   else:send('250 OK')
class Server(socketserver.ThreadingTCPServer):allow_reuse_address=True;daemon_threads=True

def post_form(path,values):
 import http.cookiejar
 jar=http.cookiejar.CookieJar();opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
 page=opener.open(WEB+path,timeout=30).read().decode()
 from html.parser import HTMLParser
 class Inputs(HTMLParser):
  def __init__(self):super().__init__();self.csrf=None
  def handle_starttag(self,tag,attrs):
   a=dict(attrs)
   if tag=='input' and a.get('name')=='__RequestVerificationToken':self.csrf=a['value']
 p=Inputs();p.feed(page);values['__RequestVerificationToken']=p.csrf
 resp=opener.open(urllib.request.Request(WEB+path,data=urllib.parse.urlencode(values).encode()),timeout=30)
 return resp.read().decode()

def main():
 admin_email=os.environ['PIM_TEST_ADMIN_EMAIL'];admin_password=os.environ['PIM_TEST_ADMIN_PASSWORD']
 tag=uuid.uuid4().hex[:10];password='SenhaAuditoria!'+secrets.token_hex(5)
 users=[{'nome':'Cliente Auditoria '+s,'telefone':'11999999999','email':f'auditoria.{s.lower()}.{tag}@example.invalid','senha':password} for s in ['A','B']]
 st,services=request('GET','/servicos');check('Catálogo público',st==200 and len(services)==6,str(st))
 st,barbeiros=request('GET','/barbeiros');check('Profissionais públicos',st==200 and len(barbeiros)==3,str(st))
 for method,path,body,name in [('GET','/agendamentos?email=qualquer@example.invalid',None,'Consulta anônima'),('POST','/agendamentos',{'servicoId':1,'barbeiroId':2,'inicio':'2026-12-01T08:00:00'},'Reserva anônima'),('PUT','/agendamentos/1/status',{'status':'Cancelado'},'Cancelamento anônimo'),('GET','/relatorios/servicos?inicio=2026-01-01&fim=2026-12-31',None,'Relatório anônimo')]:
  st,_=request(method,path,body);check(name,st==401,str(st))
 for u in users:
  st,_=request('POST','/conta/cadastro',u);check('Cadastro '+u['nome'][-1],st==201,str(st))
 st,_=request('POST','/conta/cadastro',{**users[0],'email':users[0]['email'].upper()});check('E-mail duplicado normalizado',st==400,str(st))
 st,_=request('POST','/conta/cadastro',{**users[0],'email':f'fraca.{tag}@example.invalid','senha':'123'});check('Senha fraca rejeitada',st==400,str(st))
 st,_=request('POST','/conta/login',{'email':users[0]['email'],'senha':'SenhaIncorreta!'});check('Senha incorreta',st==401,str(st))
 tokens=[]
 for u in users:
  st,j=request('POST','/conta/login',{'email':u['email'],'senha':password});check('Login '+u['nome'][-1],st==200,str(st));tokens.append(j['token'])
 st,j=request('POST','/conta/login',{'email':admin_email,'senha':admin_password});check('Login administrador',st==200,str(st));admin=j['token']
 st,_=request('GET','/agendamentos',token=tokens[0]+'x');check('Assinatura adulterada',st==401,str(st))
 st,_=request('GET','/relatorios/servicos?inicio=2026-01-01&fim=2026-12-31',token=tokens[0]);check('Cliente não lê relatório',st==403,str(st))
 dia=datetime.datetime.now(ZoneInfo('America/Sao_Paulo')).date()+datetime.timedelta(days=7)
 while dia.weekday()==6:dia+=datetime.timedelta(days=1)
 horarios_path=f'/agendamentos/horarios-disponiveis?data={dia}&servicoId=1&barbeiroId=2'
 st,horarios=request('GET',horarios_path);check('Disponibilidade',st==200 and len(horarios)>5,str(st))
 payload={'servicoId':1,'barbeiroId':2,'inicio':horarios[0]}
 st,reserva=request('POST','/agendamentos',payload,tokens[0]);check('Reserva autenticada',st==201,str(st));id1=reserva['id']
 st,_=request('POST','/agendamentos',payload,tokens[0]);check('Sobreposição rejeitada',st==400,str(st))
 st,j=request('GET','/agendamentos?email='+users[0]['email'],token=tokens[1]);check('E-mail alheio não expõe reservas',st==200 and j==[],str(st))
 st,_=request('PUT',f'/agendamentos/{id1}/status',{'status':'Cancelado'},tokens[1]);check('Conta B não cancela conta A',st==403,str(st))
 st,_=request('PUT',f'/agendamentos/{id1}/status',{'status':'Concluido'},tokens[0]);check('Cliente não conclui atendimento',st==403,str(st))
 st,_=request('PUT',f'/agendamentos/{id1}/status',{'status':'Concluido'},admin);check('Admin conclui atendimento',st==204,str(st))
 st,_=request('PUT',f'/agendamentos/{id1}/status',{'status':'Cancelado'},tokens[0]);check('Atendimento encerrado é imutável',st==400,str(st))
 payload['inicio']=horarios[4];payload['email']=users[1]['email']
 st,reserva=request('POST','/agendamentos',payload,tokens[0]);check('Identidade enviada não altera titularidade',st==201,str(st));id2=reserva['id']
 st,j=request('GET','/agendamentos',token=tokens[0]);check('Reserva pertence à conta do token',st==200 and id2 in [a['id'] for a in j],str(st))
 st,_=request('PUT',f'/agendamentos/{id2}/status',{'status':'Cancelado'},tokens[0]);check('Cliente cancela própria reserva',st==204,str(st))
 payload.pop('email');payload['inicio']=horarios[8]
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:codes=list(pool.map(lambda t:request('POST','/agendamentos',payload,t)[0],tokens))
 check('Disputa simultânea tem uma reserva aceita',sorted(codes)==[201,400],str(sorted(codes)))
 st,j=request('GET','/relatorios/servicos?inicio=2026-01-01&fim=2026-12-31',token=admin);check('Relatório do administrador',st==200 and len(j)==6,str(st))
 st,_=request('POST','/agendamentos',{'servicoId':1,'barbeiroId':2,'inicio':str(dia)+'T23:00:00'},tokens[0]);check('Fora do expediente rejeitado',st==400,str(st))
 for name,q,err in [
 ('Banco rejeita e-mail duplicado',"INSERT INTO Usuario(Nome,Telefone,Email,Tipo) SELECT Nome,Telefone,UPPER(Email),Tipo FROM Usuario WHERE ID_Usuario=1;",[2601,2627]),
 ('Banco rejeita preço negativo',"INSERT INTO Agendamento VALUES ('20500101 10:00','20500101 10:45',-1,'Confirmado',1,2,1);",[547]),
 ('Banco rejeita intervalo invertido',"INSERT INTO Agendamento VALUES ('20500101 11:00','20500101 10:45',40,'Confirmado',1,2,1);",[547]),
 ('Banco rejeita expediente invertido',"INSERT INTO HorarioTrabalho VALUES(2,'Domingo','18:00','08:00');",[547]),
 ('Trigger rejeita sobreposição direta',"INSERT INTO Agendamento VALUES ('20260101 10:20','20260101 11:05',40,'Confirmado',1,2,1);",[50001]),
 ('Trigger rejeita conflito no mesmo lote',"INSERT INTO Agendamento VALUES ('20500102 10:00','20500102 11:00',40,'Confirmado',1,2,1),('20500102 10:30','20500102 11:30',40,'Confirmado',1,2,1);",[50001])]:
  j=sql(q);check(name,not j['ok'] and j['numero'] in err,str(j.get('numero')))
 j=sql("BEGIN TRAN; INSERT INTO Servico VALUES('Corte Masculino','Teste homonimo',50,'00:30'); EXEC sp_RelatorioServicos '20260101','20260101'; ROLLBACK;")
 check('Procedure separa serviços homônimos',j['ok'] and len([r for r in j['resultados'][0] if r['Servico']=='Corte Masculino'])==2)
 j=sql((ROOT/'Database/Atualizar_PIM_IV.sql').read_text());check('Migração pode ser reaplicada',j['ok'])
 # SMTP é um sumidouro local. Nenhuma mensagem é entregue a pessoas.
 with Server(('127.0.0.1',2525),SMTP) as smtp:
  thread=threading.Thread(target=smtp.serve_forever,daemon=True);thread.start()
  st,_=request('POST','/conta/recuperacao',{'email':users[0]['email']});check('Solicitar recuperação',st==200,str(st))
  msg=email.message_from_bytes(MAIL.get(timeout=15));body=msg.get_payload(decode=True).decode(msg.get_content_charset() or 'utf-8')
  token=re.search(r'token=([A-Fa-f0-9]{64})',body).group(1);new_password='NovaSenha!'+secrets.token_hex(8)
  page=post_form('/Conta/Redefinir?token='+token,{'Token':token,'Senha':new_password,'ConfirmarSenha':new_password})
  check('Link redefine senha', 'Senha atualizada' in html.unescape(page))
  page=post_form('/Conta/Redefinir?token='+token,{'Token':token,'Senha':password,'ConfirmarSenha':password})
  check('Link de recuperação tem uso único','já utilizado' in html.unescape(page))
  st,_=request('GET','/agendamentos',token=tokens[0]);check('Redefinição revoga token anterior',st==401,str(st))
  st,_=request('POST','/conta/login',{'email':users[0]['email'],'senha':password});check('Senha anterior não autentica',st==401,str(st))
  st,j=request('POST','/conta/login',{'email':users[0]['email'],'senha':new_password});check('Senha nova autentica',st==200,str(st))
  st,_=request('POST','/conta/recuperacao',{'email':users[0]['email']});check('Novo link de recuperação',st==200,str(st))
  msg=email.message_from_bytes(MAIL.get(timeout=15));body=msg.get_payload(decode=True).decode(msg.get_content_charset() or 'utf-8');expired_token=re.search(r'token=([A-Fa-f0-9]{64})',body).group(1)
  sql("UPDATE Usuario SET RecuperacaoExpiraEmUtc=DATEADD(MINUTE,-1,SYSUTCDATETIME()) WHERE Email='"+users[0]['email']+"';")
  page=post_form('/Conta/Redefinir?token='+expired_token,{'Token':expired_token,'Senha':password,'ConfirmarSenha':password});check('Link expirado rejeitado','expirado' in html.unescape(page));smtp.shutdown()
 st,_=request('POST','/conta/login',{'email':users[0]['email'],'senha':new_password});check('Limite de tentativas é aplicado',st==429,str(st))
 fixture={'dia':str(dia),'a':{**users[0],'senha':new_password},'b':users[1],'admin_email':admin_email,'admin_password':admin_password}
 if os.getenv('PIM_TEST_FIXTURE_PATH'):pathlib.Path(os.environ['PIM_TEST_FIXTURE_PATH']).write_text(json.dumps(fixture))

if __name__=='__main__':
 out=pathlib.Path(os.getenv('PIM_TEST_REPORT','relatorio-integracao.json'))
 try:main()
 finally:out.write_text(json.dumps(RESULT,indent=2,ensure_ascii=False))
 print(f'{len(RESULT)} verificações aprovadas.')
