#!/usr/bin/env python3
"""Requer SQL Server descartável e permissões de criar/excluir apenas o banco de teste."""
import pathlib,uuid
from test_integracao import sql
root=pathlib.Path(__file__).resolve().parents[1]
nome='PIM_Teste_'+uuid.uuid4().hex[:10]
base=(root/'Database/Script.sql').read_text(encoding='utf-8-sig').split('-- PIM IV: atualização atômica')[0]
try:
 assert sql(base.replace('Barbearia_do_Ze',nome))['ok']
 assert sql(f'USE {nome}; INSERT INTO Usuario(Nome,Telefone,Email,Tipo) SELECT Nome,Telefone,Email,Tipo FROM Usuario WHERE ID_Usuario=1;')['ok']
 resultado=sql((root/'Database/Atualizar_PIM_IV.sql').read_text().replace('Barbearia_do_Ze',nome))
 assert resultado.get('numero')==50010
 resultado=sql(f"USE {nome}; SELECT COL_LENGTH('Usuario','SenhaHash') AS CampoSeguranca,COUNT(*) AS Total FROM Usuario;")
 row=resultado['resultados'][0][0]
 assert row['CampoSeguranca'] is None and row['Total']==5
 print('Aprovado: migração bloqueada, esquema inalterado e registros preservados.')
finally:
 sql(f'USE master; IF DB_ID(\'{nome}\') IS NOT NULL DROP DATABASE {nome};')
