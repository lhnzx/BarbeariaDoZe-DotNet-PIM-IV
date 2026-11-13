-- =====================================================================
-- Barbearia do Ze - PIM IV
-- Script completo: PIM III (tabelas, dados e view) + PIM IV (dados de apoio,
-- trigger de conflito de horarios e stored procedure de relatorio).
-- Executar uma unica vez, em um SQL Server / LocalDB limpo.
-- =====================================================================

create database Barbearia_do_Ze;
GO

Use Barbearia_do_Ze;
GO

create table Usuario( 
   ID_Usuario INT PRIMARY KEY IDENTITY(1,1) NOT NULL, 
   Nome VARCHAR(100) NOT NULL, 
   Telefone VARCHAR(11) NOT NULL, 
   Email VARCHAR(255) NOT NULL, 
   Tipo VARCHAR(15) NOT NULL, 
   CHECK (Tipo IN ('Admin', 'Cliente', 'Barbeiro')) 
); 

create table Servico( 
   ID_Servico INT PRIMARY KEY IDENTITY(1,1) NOT NULL, 
   Nome VARCHAR(50) NOT NULL, 
   Descricao VARCHAR(100) NOT NULL, 
   Preco DECIMAL(7,2) NOT NULL, 
   TempoEstimado TIME 
); 

create table Barbeiro( 
   ID_Barbeiro INT PRIMARY KEY NOT NULL, 
   DataAdmissao DATE NOT NULL, 
   Foto VARCHAR(255), 
   Avaliacao DECIMAL(2,1) NOT NULL, 
   CHECK (Avaliacao >= 0 AND Avaliacao <= 5), 
   constraint fk_barbeiro foreign key (ID_Barbeiro) 
   references Usuario(ID_Usuario) ON DELETE CASCADE 
); 

create table HorarioTrabalho ( 
   ID_Horario INT PRIMARY KEY IDENTITY(1,1) NOT NULL, 
   ID_Barbeiro INT, 
   DiaSemana VARCHAR(15) NOT NULL, 
   CHECK (DiaSemana IN ( 
       'Segunda-Feira', 'Terça-Feira', 'Quarta-Feira', 
       'Quinta-Feira', 'Sexta-Feira', 'Sábado', 'Domingo' 
   )), 
   HoraInicio TIME NOT NULL, 
   HoraFim TIME NOT NULL, 
   constraint fk_horario_barbeiro foreign key(ID_Barbeiro) 
   references Barbeiro(ID_Barbeiro) ON DELETE SET NULL 
); 

create table Agendamento ( 
   ID INT PRIMARY KEY IDENTITY(1,1) NOT NULL, 
   DataHoraInicio DATETIME NOT NULL, 
   DataHoraFim DATETIME NOT NULL, 
   Preco DECIMAL(7,2), 
   Status VARCHAR(15) NOT NULL, 
   CHECK (Status IN ('Confirmado', 'Concluido', 'Cancelado')), 
   ID_Usuario INT NOT NULL, 
   ID_Barbeiro INT, 
   ID_Servico INT NOT NULL, 
   constraint fk_agendamento_cliente foreign key(ID_Usuario) 
   references Usuario(ID_Usuario), 
   constraint fk_agendamento_barbeiro foreign key(ID_Barbeiro) 
   references Barbeiro(ID_Barbeiro) ON DELETE SET NULL, 
   constraint fk_agendamento_servico foreign key(ID_Servico) 
   references Servico(ID_Servico) 
); 

insert into Usuario(Nome, Telefone, Email, Tipo) 
values 
('Vinicius Martins Hallal', '16992006570', 'Vinicius.Hallal@gmail.com', 'Cliente'), 
('José Ferreira da Silva', '16992622983', 'José.Ferreira@gmail.com', 'Barbeiro'); 

insert into Servico(Nome, Descricao, Preco, TempoEstimado) 
values('Corte Masculino', 'Corte moderno e acabamento profissional', 40.00, '00:45:00'); 

insert into Barbeiro(ID_Barbeiro, DataAdmissao, Foto, Avaliacao) 
values(2, '2026-01-01', 'Fotos Barbeiros/José Ferreira.jpg', 5); 

insert into HorarioTrabalho(ID_Barbeiro, DiaSemana, HoraInicio, HoraFim) 
values 
(2, 'Segunda-Feira', '08:00:00', '18:00:00'), 
(2, 'Terça-Feira', '08:00:00', '18:00:00'); 

insert into Agendamento(ID_Usuario, ID_Barbeiro, ID_Servico, DataHoraInicio, DataHoraFim, Status, Preco) 
values 
(1, 2, 1, '2026-01-01 10:00:00', '2026-01-01 10:45:00', 'Concluido', 
(SELECT Preco FROM Servico WHERE ID_Servico = 1));

GO
CREATE VIEW Agendamento_Total AS
SELECT
    S.Nome          AS Servico,
    A.Preco         AS Valor,
    A.DataHoraInicio AS Inicio,
    A.DataHoraFim    AS Fim,
    U.Nome          AS Barbeiro
FROM Agendamento A
JOIN Servico S  ON A.ID_Servico  = S.ID_Servico
JOIN Barbeiro B ON A.ID_Barbeiro = B.ID_Barbeiro
JOIN Usuario U  ON B.ID_Barbeiro = U.ID_Usuario;
GO

-- =====================================================================
-- Barbearia do Zé - PIM IV (4º semestre)
-- A base do PIM III já está incluída acima neste arquivo completo.
-- Conteúdo: dados de apoio, TRIGGER e STORED PROCEDURE.
-- =====================================================================
USE Barbearia_do_Ze;
GO

-- ---------------------------------------------------------------------
-- Dados de apoio (serviços e barbeiros exibidos no site do PIM III)
-- ---------------------------------------------------------------------
INSERT INTO Servico (Nome, Descricao, Preco, TempoEstimado) VALUES
(N'Barba',        N'Modelagem completa com hidratação',           30.00, '00:35:00'),
(N'Combo',        N'Corte e barba com desconto',                  60.00, '01:20:00'),
(N'Luzes',        N'Visual moderno com estilo e personalidade',   80.00, '01:05:00'),
(N'Sobrancelha',  N'Design e alinhamento profissional',           20.00, '00:25:00'),
(N'Bigode',       N'Acabamento detalhado e alinhamento perfeito', 20.00, '00:20:00');
GO

INSERT INTO Usuario (Nome, Telefone, Email, Tipo) VALUES
(N'Rodrigo Alves', '16990000001', 'rodrigo@barbeariadoze.com', 'Barbeiro'),
(N'Mateus Lima',   '16990000002', 'mateus@barbeariadoze.com',  'Barbeiro');

INSERT INTO Barbeiro (ID_Barbeiro, DataAdmissao, Foto, Avaliacao)
SELECT ID_Usuario, '2026-02-01', NULL, 4.8 FROM Usuario WHERE Email = 'rodrigo@barbeariadoze.com'
UNION ALL
SELECT ID_Usuario, '2026-03-01', NULL, 4.7 FROM Usuario WHERE Email = 'mateus@barbeariadoze.com';

-- Horário de trabalho: segunda a sábado, 08:00 às 18:00 (todos os barbeiros)
INSERT INTO HorarioTrabalho (ID_Barbeiro, DiaSemana, HoraInicio, HoraFim)
SELECT B.ID_Barbeiro, D.Dia, '08:00:00', '18:00:00'
FROM Barbeiro B
CROSS JOIN (VALUES (N'Segunda-Feira'), (N'Terça-Feira'), (N'Quarta-Feira'),
                   (N'Quinta-Feira'), (N'Sexta-Feira'), (N'Sábado')) AS D(Dia)
WHERE NOT EXISTS (SELECT 1 FROM HorarioTrabalho H
                  WHERE H.ID_Barbeiro = B.ID_Barbeiro AND H.DiaSemana = D.Dia);
GO

-- ---------------------------------------------------------------------
-- TRIGGER: impede dois agendamentos ativos sobrepostos para o mesmo barbeiro.
-- Garante a regra de negócio também no banco, mesmo que a aplicação falhe.
-- ---------------------------------------------------------------------
CREATE TRIGGER trg_Agendamento_ValidaConflito
ON Agendamento
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Agendamento a
          ON a.ID_Barbeiro = i.ID_Barbeiro
         AND a.ID <> i.ID
         AND a.Status <> 'Cancelado'
         AND i.DataHoraInicio < a.DataHoraFim
         AND i.DataHoraFim   > a.DataHoraInicio
        WHERE i.Status <> 'Cancelado'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'Conflito de horário: o barbeiro já possui agendamento neste período.', 1;
    END
END;
GO

-- ---------------------------------------------------------------------
-- STORED PROCEDURE: relatório por serviço em um período.
-- Executada pela camada de Repositório (Dapper) e usada no painel e na API.
-- ---------------------------------------------------------------------
CREATE PROCEDURE sp_RelatorioServicos
    @DataInicio DATE,
    @DataFim    DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        S.Nome                                                         AS Servico,
        COUNT(A.ID)                                                    AS TotalAgendamentos,
        SUM(CASE WHEN A.Status = 'Concluido' THEN 1 ELSE 0 END)        AS Concluidos,
        SUM(CASE WHEN A.Status = 'Cancelado' THEN 1 ELSE 0 END)        AS Cancelados,
        ISNULL(SUM(CASE WHEN A.Status = 'Concluido' THEN A.Preco END), 0) AS Receita,
        CAST(CASE WHEN COUNT(A.ID) = 0 THEN 0
                  ELSE 100.0 * SUM(CASE WHEN A.Status = 'Cancelado' THEN 1 ELSE 0 END) / COUNT(A.ID)
             END AS DECIMAL(5,2))                                      AS TaxaCancelamento
    FROM Servico S
    LEFT JOIN Agendamento A
           ON A.ID_Servico = S.ID_Servico
          AND CAST(A.DataHoraInicio AS DATE) BETWEEN @DataInicio AND @DataFim
    GROUP BY S.Nome
    ORDER BY COUNT(A.ID) DESC, S.Nome;
END;
GO


-- PIM IV: atualização atômica, sem exclusão dos dados existentes.
-- Faça backup antes de aplicar em banco com dados reais.
USE Barbearia_do_Ze;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

-- Falhas são informadas antes de qualquer alteração: não há deduplicação nem exclusão automática.
IF EXISTS (SELECT LOWER(LTRIM(RTRIM(Email))) FROM Usuario GROUP BY LOWER(LTRIM(RTRIM(Email))) HAVING COUNT(*) > 1)
    THROW 50010, 'Existem e-mails duplicados. Revise-os antes de aplicar a atualização.', 1;
IF EXISTS (SELECT 1 FROM Servico WHERE Preco <= 0 OR (TempoEstimado IS NOT NULL AND TempoEstimado <= '00:00:00'))
    THROW 50011, 'Existem serviços com preço ou duração inválidos.', 1;
IF EXISTS (SELECT 1 FROM Agendamento WHERE DataHoraFim <= DataHoraInicio OR Preco < 0 OR Preco IS NULL)
    THROW 50012, 'Existem agendamentos com intervalo ou preço inválido.', 1;
IF EXISTS (SELECT 1 FROM HorarioTrabalho WHERE HoraFim <= HoraInicio)
    THROW 50013, 'Existem expedientes com intervalo inválido.', 1;
IF COL_LENGTH('Usuario','SenhaHash') IS NULL ALTER TABLE Usuario ADD SenhaHash VARCHAR(512) NULL;
IF COL_LENGTH('Usuario','SecurityStamp') IS NULL ALTER TABLE Usuario ADD SecurityStamp VARCHAR(32) NULL;
IF COL_LENGTH('Usuario','RecuperacaoHash') IS NULL ALTER TABLE Usuario ADD RecuperacaoHash VARCHAR(64) NULL;
IF COL_LENGTH('Usuario','RecuperacaoExpiraEmUtc') IS NULL ALTER TABLE Usuario ADD RecuperacaoExpiraEmUtc DATETIME2 NULL;
IF COL_LENGTH('Usuario','EmailNormalizado') IS NULL ALTER TABLE Usuario ADD EmailNormalizado AS LOWER(LTRIM(RTRIM(Email))) PERSISTED;

EXEC(N'UPDATE Usuario SET SecurityStamp = REPLACE(CONVERT(VARCHAR(36),NEWID()),''-'','''') WHERE SecurityStamp IS NULL;
ALTER TABLE Usuario ALTER COLUMN SecurityStamp VARCHAR(32) NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID(''Usuario'') AND name=''DF_Usuario_SecurityStamp'')
    ALTER TABLE Usuario ADD CONSTRAINT DF_Usuario_SecurityStamp DEFAULT REPLACE(CONVERT(VARCHAR(36),NEWID()),''-'','''') FOR SecurityStamp;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''Usuario'') AND name=''UX_Usuario_EmailNormalizado'')
    CREATE UNIQUE INDEX UX_Usuario_EmailNormalizado ON Usuario(EmailNormalizado);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=''CK_Servico_PrecoDuracao'')
    ALTER TABLE Servico WITH CHECK ADD CONSTRAINT CK_Servico_PrecoDuracao CHECK (Preco > 0 AND (TempoEstimado IS NULL OR TempoEstimado > ''00:00:00''));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=''CK_Agendamento_IntervaloPreco'')
    ALTER TABLE Agendamento WITH CHECK ADD CONSTRAINT CK_Agendamento_IntervaloPreco CHECK (DataHoraFim > DataHoraInicio AND Preco IS NOT NULL AND Preco >= 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=''CK_HorarioTrabalho_Intervalo'')
    ALTER TABLE HorarioTrabalho WITH CHECK ADD CONSTRAINT CK_HorarioTrabalho_Intervalo CHECK (HoraFim > HoraInicio);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''Agendamento'') AND name=''IX_Agendamento_Conflito'')
    CREATE INDEX IX_Agendamento_Conflito ON Agendamento(ID_Barbeiro, DataHoraInicio) INCLUDE(DataHoraFim, Status);
');
EXEC(N'CREATE OR ALTER TRIGGER trg_Agendamento_ValidaConflito ON Agendamento
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    -- Intervalos semiabertos: um atendimento pode começar exatamente no fim de outro.
    -- Bloqueios de atualização/intervalo protegem contra concorrência; a aplicação trata a rejeição.
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN Agendamento a WITH (UPDLOCK, HOLDLOCK)
          ON a.ID_Barbeiro = i.ID_Barbeiro AND a.ID <> i.ID
         AND a.Status <> ''Cancelado''
         AND i.DataHoraInicio < a.DataHoraFim AND i.DataHoraFim > a.DataHoraInicio
        WHERE i.Status <> ''Cancelado''
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, ''Conflito de horário: o barbeiro já possui agendamento neste período.'', 1;
    END
END;');
EXEC(N'CREATE OR ALTER PROCEDURE sp_RelatorioServicos @DataInicio DATE, @DataFim DATE AS
BEGIN
    SET NOCOUNT ON;
    IF @DataInicio IS NULL OR @DataFim IS NULL OR @DataFim < @DataInicio OR @DataFim = ''99991231''
        THROW 50014, ''Informe um período válido para o relatório.'', 1;
    SELECT S.Nome AS Servico, COUNT(A.ID) AS TotalAgendamentos,
        SUM(CASE WHEN A.Status=''Concluido'' THEN 1 ELSE 0 END) AS Concluidos,
        SUM(CASE WHEN A.Status=''Cancelado'' THEN 1 ELSE 0 END) AS Cancelados,
        ISNULL(SUM(CASE WHEN A.Status=''Concluido'' THEN A.Preco END),0) AS Receita,
        CAST(CASE WHEN COUNT(A.ID)=0 THEN 0 ELSE
             100.0*SUM(CASE WHEN A.Status=''Cancelado'' THEN 1 ELSE 0 END)/COUNT(A.ID)
             END AS DECIMAL(5,2)) AS TaxaCancelamento
    FROM Servico S LEFT JOIN Agendamento A
      ON A.ID_Servico=S.ID_Servico
     AND A.DataHoraInicio >= @DataInicio AND A.DataHoraInicio < DATEADD(DAY,1,@DataFim)
    -- Serviços homônimos continuam separados pelo identificador.
    GROUP BY S.ID_Servico,S.Nome ORDER BY COUNT(A.ID) DESC,S.Nome,S.ID_Servico;
END;');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
