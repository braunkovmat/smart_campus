# Заглушка шлюза для демонстрации клиента (повторяет примеры из openapi.yaml)
import json
from http.server import BaseHTTPRequestHandler, HTTPServer
ITEMS=[{"id":101,"name":"Худи с логотипом кампуса","category":"clothes","price":2490.0,"stock":35},
       {"id":102,"name":"Блокнот А5 «Кампус»","category":"stationery","price":350.0,"stock":120},
       {"id":103,"name":"Кружка керамическая","category":"souvenirs","price":490.0,"stock":58}]
class H(BaseHTTPRequestHandler):
    def _send(s,code,obj):
        b=json.dumps(obj,ensure_ascii=False).encode('utf8')
        s.send_response(code); s.send_header('Content-Type','application/json'); s.send_header('Content-Length',str(len(b))); s.end_headers(); s.wfile.write(b)
    def read_body(s):
        if s.headers.get('Transfer-Encoding','').lower()=='chunked':
            data=b''
            while True:
                size=int(s.rfile.readline().strip(),16)
                if size==0: s.rfile.readline(); break
                data+=s.rfile.read(size); s.rfile.readline()
            return data
        return s.rfile.read(int(s.headers.get('Content-Length',0)))
    def do_GET(s):
        if s.path.startswith('/api/v1/catalog/items'): s._send(200,{"items":ITEMS,"total":len(ITEMS)})
        else: s._send(404,{"code":"NOT_FOUND","message":"Нет такого ресурса"})
    def do_POST(s):
        d=json.loads(s.read_body() or b'{}')
        if s.path!='/api/v1/orders/checkout': return s._send(404,{"code":"NOT_FOUND","message":"Нет такого ресурса"})
        total=0
        for l in d.get('lines',[]):
            it=next((i for i in ITEMS if i['id']==l['itemId']),None)
            if not it: return s._send(400,{"code":"BAD_ITEM","message":"Товар не найден"})
            if l['quantity']>it['stock']: return s._send(409,{"code":"OUT_OF_STOCK","message":"Недостаточно товара на складе"})
            total+=it['price']*l['quantity']
        s._send(201,{"orderId":"ORD-2026-000451","status":"created","total":total,"createdAt":"2026-10-30T10:15:00Z"})
    def log_message(*a): pass
HTTPServer(('127.0.0.1',5080),H).serve_forever()
