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
