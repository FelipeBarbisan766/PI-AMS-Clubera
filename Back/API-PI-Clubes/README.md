# Clubera — API

> Sistema para reserva de quadras esportivas online.

API REST do **Clubera**, uma plataforma web que centraliza o gerenciamento de clubes, quadras, horários e reservas esportivas. Desenvolvida em **.NET 10** com **Entity Framework Core** e **SQL Server**, e consumida por um frontend em **Angular**.

---

## Sobre o projeto

### História

O Clubera nasceu como projeto acadêmico da **Fatec Taquaritinga**. A motivação veio de um problema comum: o gerenciamento de reservas de quadras esportivas, quando feito de forma manual (planilhas, mensagens, ligações), gera conflitos de horário, perda de informação e muito retrabalho para quem administra o espaço.

A proposta é um sistema web que centraliza o controle de quadras, horários e reservas, tornando o processo mais organizado, eficiente e prático tanto para quem administra quanto para quem joga.

### Objetivo

Desenvolver uma aplicação que facilite o gerenciamento e a reserva de quadras esportivas, reduzindo conflitos de agenda e facilitando o controle das informações.

### Como o projeto é construído

O desenvolvimento envolve levantamento de requisitos, modelagem, implementação e testes das funcionalidades. Ferramentas de inteligência artificial (Claude e GitHub Copilot) são usadas como apoio na implementação e na resolução de dúvidas técnicas.

### Equipe

|Papel|Nome|
|---|---|
|Desenvolvedor|Felipe Adrian Lourenço Barbisan|
|Desenvolvedor|Felipe Gabriel Galati de Oliveira|
|Orientador|Gustavo Henrique del Vechio|

Fatec Taquaritinga — 2026

> ⚠️ O sistema está **em desenvolvimento**. Algumas funcionalidades podem mudar ou ainda não estar completas.

---

## Funcionalidades

- **Clubes e quadras:** cadastro e busca de clubes, com quadras vinculadas a um ou mais esportes.
- **Horários e reservas:** seleção de horários disponíveis, criação e cancelamento de reservas, com regras de antecedência.
- **Painel administrativo:** consulta das reservas realizadas nos clubes do administrador.
- **Planos e assinaturas:** acesso de administradores por assinatura, com pagamento via **Mercado Pago (Checkout Pro)** e validação de assinatura do webhook.
- **Disponibilidade em tempo real:** atualização de horários via **SignalR**, agrupada por clube.
- **Imagens:** upload com geração de três variantes (Thumb, Medium e Full) usando SkiaSharp. Armazenamento local ou em Azure Blob Storage.
- **Contas e segurança:**
    - Cadastro em duas etapas, em conformidade com a **LGPD**.
    - CPF armazenado criptografado (AES-256-GCM) com hash HMAC-SHA256 para detecção de duplicidade.
    - Login com Google, verificação de e-mail e redefinição de senha.
- **Configuração dinâmica:** parâmetros e segredos (JWT, SMTP, Mercado Pago etc.) ficam na tabela `AppSettings`, com os segredos criptografados, e podem ser alterados por um endpoint de backoffice, sem novo deploy.
- **Limpeza automática:** serviços em segundo plano para rotinas de manutenção (ex.: limpeza de reservas antigas).

---

##  Arquitetura

Arquitetura em camadas, no estilo "Clean-ish":

```
Controller → Service → Repository → Mapper → DTO → Validator → EF Configuration
```

- Controllers sem `try/catch`: um `ExceptionMiddleware` global converte exceções de domínio em respostas padronizadas (`statusCode`, `errorCode`, `message`, `errors`, `traceId`, `timestamp`).
- Validações com **FluentValidation**, incluindo validadores para CPF, CEP, telefone e UF.
- Configurações de entidades via `IEntityTypeConfiguration<T>`.
- Rotinas em background com `BackgroundService` + `PeriodicTimer`.
- Configuração lida de um `ConfigurationProvider` próprio que consulta a tabela `AppSettings` (segredos cifrados com AES-256-GCM), com prioridade sobre o `.env`.
- Migrations do EF aplicadas automaticamente na subida da API.

## Stack

|Camada|Tecnologia|
|---|---|
|API|.NET 10, ASP.NET Core|
|Persistência|Entity Framework Core, SQL Server|
|Frontend|Angular|
|Pagamentos|Mercado Pago|
|Tempo real|SignalR|
|Imagens|SkiaSharp|
|Infra|Docker, Docker Compose, GitHub Actions, GHCR|

---

## Rodando com Docker

A API é publicada como imagem no **GitHub Container Registry (GHCR)**.

### Pré-requisitos

- Docker e Docker Compose instalados
- Um **SQL Server** rodando na rede Docker externa `homelab-network`, com o container chamado `sqlserver-clubes` (é o host usado na connection string)
- Um arquivo `.env` configurado (veja a próxima seção), incluindo a `SETTINGS_MASTER_KEY`

### Passo a passo

**1. Crie a rede externa (apenas na primeira vez):**

```bash
docker network create homelab-network
```

**2. Suba o banco de dados** (compose do SQL Server do projeto, na mesma rede):

```bash
docker compose -f docker-compose.db.yml --env-file .env up -d
```

**3. Crie a pasta de uploads no servidor (apenas na primeira vez):**

```bash
mkdir -p /DATA/AppData/homelab-api-uploads
```

> Ela fica **fora** da pasta de trabalho do runner do GitHub Actions de propósito: arquivos enviados por usuários não podem ser apagados por um `git clean` a cada deploy.

**4. Baixe a imagem e suba a API:**

```bash
docker compose -f docker-compose.api.yml --env-file .env pull
docker compose -f docker-compose.api.yml --env-file .env up -d
```

A API ficará disponível em `http://localhost:5000` (porta `5000` do host → `8080` do container).

**5. O que acontece na primeira subida:**

1. A API aplica as **migrations** pendentes (e cria o banco `ClubesDB` se ele não existir).
2. Copia para a tabela `AppSettings` os valores do `.env` que ainda não existem lá (segredos entram criptografados; as linhas criadas ficam com `UpdatedBy = 'seed'`).
3. Passa a carregar a configuração a partir da tabela.

Confira o resultado:

```sql
SELECT [Key], IsSecret, UpdatedAt, UpdatedBy FROM AppSettings ORDER BY [Key];
```

Depois de conferir, você pode remover do `.env` e do `docker-compose.api.yml` as variáveis que já estão na tabela, mantendo só o bootstrap (veja [Como a configuração funciona](#como-a-configuração-funciona)).

### Comandos úteis

```bash
# Ver logs
docker logs -f api-clubes

# Parar a API
docker compose -f docker-compose.api.yml --env-file .env down

# Reiniciar (necessário para valores lidos só no startup, como a duração do cookie de sessão)
docker compose -f docker-compose.api.yml --env-file .env restart api

# Atualizar para a versão mais recente
docker compose -f docker-compose.api.yml --env-file .env pull
docker compose -f docker-compose.api.yml --env-file .env up -d
```

### Usando outra imagem

Por padrão o compose usa `ghcr.io/felipebarbisan766/api-clubes:latest`. Para usar outra tag ou imagem, defina `API_IMAGE` no `.env`:

```env
API_IMAGE=ghcr.io/felipebarbisan766/api-clubes:1.0.0
```

### Deploy automático

O compose `docker-compose.api.yml` é o que o **GitHub Actions** recria a cada deploy. Se o `.env` do servidor é gerado pelo workflow, lembre-se de incluir `SETTINGS_MASTER_KEY` entre os secrets do repositório.

---

## Como a configuração funciona

A configuração da API vem de duas origens, e **a tabela `AppSettings` tem prioridade máxima**. O `.env`/compose serve de base e de fallback.

Ordem de prioridade (o último vence):

```
appsettings.json  <  appsettings.{Ambiente}.json  <  variáveis de ambiente (.env/compose)  <  tabela AppSettings
```

|Grupo|Onde fica|O que é|
|---|---|---|
|**Bootstrap**|Sempre no `.env`|`ASPNETCORE_ENVIRONMENT`, `API_IMAGE`, `MSSQL_SA_PASSWORD` (e a connection string montada com ela), `SETTINGS_MASTER_KEY`, `CPF_ENCRYPTION_KEY`, `CPF_HMAC_KEY`|
|**Configuração comum**|Tabela `AppSettings`, texto puro|URLs, `Booking:*`, `Share:*`, `Jwt:Expiration*`, `Jwt:Issuer/Audience`, `Storage:Provider`, `Email:SenderName`, `ReserveCleanup:RetentionMonths` etc.|
|**Segredos**|Tabela `AppSettings`, **criptografados** (AES-256-GCM)|`Jwt:Key`, `Jwt:EmailVerificationKey`, `Jwt:ResetPasswordKey`, `Email:SmtpPassword`, `AzureStorage:ConnectionString`, `MercadoPago:AccessToken`, `MercadoPago:WebhookSecret`|

Por que o bootstrap fica fora do banco:

- A connection string e a senha do SQL são necessárias para chegar ao banco.
- `SETTINGS_MASTER_KEY` é a chave que decifra os segredos da tabela. Guardá-la no mesmo banco anularia a criptografia.
- `CPF_ENCRYPTION_KEY` e `CPF_HMAC_KEY` protegem dados já gravados. Uma troca (ou uma edição errada numa tabela) tornaria todos os CPFs ilegíveis.

---

## Configurando o `.env`

Crie um arquivo `.env` na mesma pasta do `docker-compose.api.yml`. **Nunca commite esse arquivo** — adicione `.env` ao `.gitignore`.

### Modelo

```env
# --- Bootstrap (sempre no .env) ---
ASPNETCORE_ENVIRONMENT=Production
API_IMAGE=ghcr.io/felipebarbisan766/api-clubes:latest
MSSQL_SA_PASSWORD=TroqueEstaSenha@123
SETTINGS_MASTER_KEY=
CPF_ENCRYPTION_KEY=
CPF_HMAC_KEY=

# --- URLs ---
API_BASE_URL=https://api.seudominio.com
PUBLIC_BASE_URL=https://app.seudominio.com

# --- JWT ---
JWT_KEY=
JWT_EMAIL_VERIFICATION_KEY=
JWT_RESET_PASSWORD_KEY=

# --- E-mail (SMTP) ---
EMAIL_SENDER=seu-email@gmail.com
EMAIL_SMTP_USERNAME=seu-email@gmail.com
EMAIL_SMTP_PASSWORD=

# --- Azure Storage (deixe vazio se usar armazenamento local) ---
AZURE_STORAGE_CONNECTION_STRING=

# --- Google ---
GOOGLE_CLIENT_ID=

# --- Mercado Pago ---
MERCADOPAGO_ACCESS_TOKEN=
MERCADOPAGO_WEBHOOK_SECRET=
```

> Os blocos de URLs, JWT, e-mail, Azure, Google e Mercado Pago são lidos **na primeira subida** para popular a tabela `AppSettings`. Depois disso, o valor da tabela é o que vale.

### Descrição das variáveis

|Variável|Descrição|
|---|---|
|`ASPNETCORE_ENVIRONMENT`|Ambiente da aplicação. Padrão: `Production`.|
|`API_IMAGE`|Imagem Docker da API (opcional).|
|`MSSQL_SA_PASSWORD`|Senha do usuário `sa` do SQL Server. Precisa atender à política de complexidade do SQL Server.|
|`SETTINGS_MASTER_KEY`|Chave (Base64, 32 bytes) que criptografa e decifra os segredos da tabela `AppSettings`. **Obrigatória**: sem ela a API não sobe.|
|`CPF_ENCRYPTION_KEY`|Chave de criptografia do CPF (AES-256-GCM).|
|`CPF_HMAC_KEY`|Chave do HMAC-SHA256 usado para detectar CPFs duplicados.|
|`API_BASE_URL`|URL pública da **API**. Origem de `MercadoPago:WebhookUrl` (`/api/payment/webhook`) e de `LocalStorage:BaseUrl` (URL dos arquivos enviados).|
|`PUBLIC_BASE_URL`|URL pública do **frontend**. Origem de `Email:BaseUrl` (links dos e-mails) e de `MercadoPago:BackUrls:*` (`/payment/success`, `/failure`, `/pending`).|
|`JWT_KEY`|Chave de assinatura dos tokens JWT.|
|`JWT_EMAIL_VERIFICATION_KEY`|Chave dos tokens de verificação de e-mail.|
|`JWT_RESET_PASSWORD_KEY`|Chave dos tokens de redefinição de senha.|
|`EMAIL_SENDER`|E-mail remetente.|
|`EMAIL_SMTP_USERNAME`|Usuário do SMTP (o servidor configurado é `smtp.gmail.com:587`).|
|`EMAIL_SMTP_PASSWORD`|Senha do SMTP. No Gmail, use uma **senha de app**, não a senha da conta.|
|`AZURE_STORAGE_CONNECTION_STRING`|Connection string do Azure Blob Storage. Só é necessária se `Storage:Provider` for `Azure`.|
|`GOOGLE_CLIENT_ID`|Client ID do Google OAuth, para login com Google.|
|`MERCADOPAGO_ACCESS_TOKEN`|Access token da aplicação no Mercado Pago.|
|`MERCADOPAGO_WEBHOOK_SECRET`|Segredo para validar a assinatura (`x-signature`) dos webhooks.|

### Gerando chaves seguras

Use valores longos e aleatórios, diferentes para cada chave:

```bash
openssl rand -base64 48   # chaves JWT
openssl rand -base64 32   # SETTINGS_MASTER_KEY e chaves de CPF (256 bits)
```

> Confirme no código o formato esperado por `CPF_ENCRYPTION_KEY` (tamanho e codificação) antes de gerar a sua.
> A `SETTINGS_MASTER_KEY` precisa ser Base64 de **32 bytes**.
>
> **Atenção:**
> - Se você trocar `CPF_ENCRYPTION_KEY` ou `CPF_HMAC_KEY` depois que já existirem CPFs salvos, os dados antigos deixam de ser legíveis e a detecção de duplicidade quebra.
> - Se você trocar `SETTINGS_MASTER_KEY`, os segredos já gravados na tabela não podem mais ser decifrados e a API falha ao subir (veja [Problemas comuns](#problemas-comuns)).
>
> Guarde as três chaves com cuidado, em local **separado do backup do banco**.

---

## Configurações no banco (AppSettings)

### Tabela

|Coluna|Descrição|
|---|---|
|`Key`|Chave no formato do .NET, com `:` como separador (`Jwt:Key`, `Booking:TimeZoneId`). Única, sem diferenciar maiúsculas/minúsculas. Listas usam índice: `Share:AllowedImageHosts:0`.|
|`Value`|Texto puro ou, se `IsSecret = 1`, Base64 do valor criptografado.|
|`IsSecret`|Indica se o valor é criptografado.|
|`Description`|Descrição livre.|
|`UpdatedAt` / `UpdatedBy`|Auditoria (data UTC e id do usuário, ou `seed`).|

### Valores padrão

Estes valores já vêm definidos no `docker-compose.api.yml` e são copiados para a tabela na primeira subida. Depois disso, altere-os na tabela:

|Configuração|Valor padrão|
|---|---|
|`Jwt:Expiration`|`3`|
|`Jwt:ExpirationEmailVerification`|`30`|
|`Jwt:ExpirationResetPassword`|`15`|
|`Jwt:Issuer` / `Jwt:Audience`|`api-pi-clubes`|
|`Storage:Provider`|`Local`|
|`ReserveCleanup:RetentionMonths`|`6`|
|`Email:SenderName`|`Clube PI`|
|`Email:SmtpServer` / `Email:SmtpPort` / `Email:EnableSsl`|`smtp.gmail.com` / `587` / `true`|
|`Booking:BookingCutoffMinutes`|`10`|
|`Booking:CancellationCutoffMinutes`|`10`|
|`Booking:TimeZoneId`|`America/Sao_Paulo`|
|`Share:FrontendBaseUrl`|`https://www.clubera.dev`|
|`Share:ApiPublicBaseUrl`|`https://api.clubera.dev`|
|`Share:AllowedImageHosts:0`|`api.clubera.dev`|

### Alterando valores

**Pelo endpoint de backoffice** (recomendado, e **a única forma de alterar segredos**, já que a API é quem criptografa o valor). Exige o usuário root.

```http
GET /api/Setting/Backoffice
```

Lista todas as chaves. Os valores de segredos voltam como `********`: pela API ninguém lê um segredo de volta, só sobrescreve.

```http
PUT /api/Setting/Backoffice
Content-Type: application/json

{
  "key": "Email:SmtpPassword",
  "value": "nova-senha-de-app",
  "isSecret": true,
  "description": "Senha SMTP do Gmail"
}
```

Cria a chave se ela não existir e atualiza se existir (responde `204`). Com `isSecret: true` o valor é criptografado antes de gravar; com `false`, é gravado em texto puro.

Regras de validação:

- `key` no formato `Secao:Chave` (letras, números, `_`, `-` e `.`), até 200 caracteres.
- Chaves de bootstrap (`ConnectionStrings`, `Security`, `SETTINGS_MASTER_KEY`) são recusadas: só existem no `.env`.
- Chaves terminadas em `Key`, `Password`, `Secret`, `Token` ou `ConnectionString` exigem `isSecret: true`.
- `value` é obrigatório (até 4000 caracteres) e `description` pode ter até 500.

**Direto por SQL** (somente valores **não secretos**):

```sql
UPDATE AppSettings
SET [Value] = 'America/Sao_Paulo', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = 'manual'
WHERE [Key] = 'Booking:TimeZoneId';
```

### Quando a mudança passa a valer

- A API recarrega a tabela **a cada 1 minuto**. Pelo endpoint (`PUT`), a instância que atendeu a requisição recarrega na hora.
- Valores lidos **a cada uso** (via `IOptionsSnapshot`/`IOptionsMonitor` ou direto de `IConfiguration`) passam a valer sem reiniciar.
- Valores lidos **uma vez no startup** exigem reiniciar o container. Exemplos: `Jwt:Expiration` (duração do cookie de sessão) e as demais configurações de autenticação. Trocar `Jwt:Key` também invalida os tokens já emitidos.
- Na dúvida, reinicie: `docker compose -f docker-compose.api.yml --env-file .env restart api`.

### ⚠️ Valores derivados das URLs

No `.env`, algumas chaves são **montadas** a partir de `API_BASE_URL` e `PUBLIC_BASE_URL`. Depois do seed, elas ficam na tabela já com o valor final, e **mudar o `.env` não as atualiza mais**. Se trocar o domínio, edite estas chaves na tabela:

|Chave|Formato|
|---|---|
|`MercadoPago:WebhookUrl`|`{API_BASE_URL}/api/payment/webhook`|
|`LocalStorage:BaseUrl`|`{API_BASE_URL}`|
|`Email:BaseUrl`|`{PUBLIC_BASE_URL}`|
|`MercadoPago:BackUrls:Success`|`{PUBLIC_BASE_URL}/payment/success`|
|`MercadoPago:BackUrls:Failure`|`{PUBLIC_BASE_URL}/payment/failure`|
|`MercadoPago:BackUrls:Pending`|`{PUBLIC_BASE_URL}/payment/pending`|
|`Share:FrontendBaseUrl` / `Share:ApiPublicBaseUrl` / `Share:AllowedImageHosts:0`|Domínios do frontend e da API|

Se mudar a URL da API, atualize também o webhook no painel do Mercado Pago.

---

## Migrations automáticas

Toda vez que a API sobe, ela aplica as migrations pendentes antes de aceitar requisições:

- Faz até **10 tentativas**, com 5 s de intervalo (o SQL Server pode ainda estar inicializando, já que roda em outro compose).
- Se todas falharem, o container encerra com erro e o `restart: unless-stopped` o reinicia. Veja `docker logs api-clubes`.
- Cria o banco `ClubesDB` automaticamente se ele não existir.

Cuidados:

- Funciona bem com **uma única instância** da API. Com várias réplicas subindo ao mesmo tempo, passa a haver disputa.
- Migrations que **apagam coluna ou tabela** rodam sozinhas no deploy: faça **backup antes**.

### Criando uma migration

As migrations são criadas no ambiente de desenvolvimento (a imagem de produção não tem o SDK):

```bash
dotnet ef migrations add NomeDaMigration --project API_PI_Clubes --startup-project API_PI_Clubes
```

O `AppDbContextFactory` lê a connection string do `appsettings.json` da API. Confirme que o caminho relativo da factory aponta para a pasta correta do projeto.

---

## Armazenamento de imagens

Por padrão (`Storage:Provider = Local`), os arquivos são gravados em `/app/wwwroot/uploads` dentro do container, mapeado para `/DATA/AppData/homelab-api-uploads` no host. Para usar Azure Blob Storage:

1. Salve `AzureStorage:ConnectionString` (segredo) pelo endpoint de backoffice, ou preencha `AZURE_STORAGE_CONNECTION_STRING` no `.env` na primeira subida.
2. Altere `Storage:Provider` para `Azure`.
3. Reinicie o container.

---

## Desenvolvimento local

Use o `docker-compose.yml` da raiz do projeto. Crie um `.env` ao lado dele (sem commitar):

```env
SETTINGS_MASTER_KEY=<openssl rand -base64 32>
EMAIL_SMTP_USERNAME=seu-email@gmail.com
EMAIL_SMTP_PASSWORD=<senha de app do Gmail>
```

```bash
docker compose up -d --build
```

As migrations e o seed também rodam automaticamente na subida local. A rede `clubes-network` do compose de desenvolvimento é externa e precisa existir antes (`docker network create clubes-network`).

---

## Problemas comuns

|Sintoma|Causa e solução|
|---|---|
|A API reinicia em loop com `SETTINGS_MASTER_KEY não configurada`|Variável ausente ou vazia no `.env`. Defina-a e suba de novo.|
|Erro de criptografia (`CryptographicException`) ao subir|`SETTINGS_MASTER_KEY` diferente da usada para gravar os segredos. Restaure a chave original. Se ela foi perdida: `DELETE FROM AppSettings WHERE IsSecret = 1;`, mantenha os segredos no `.env` e suba de novo para o seed recriá-los com a nova chave (ou reenvie pelo `PUT`).|
|Mudei o `.env` e nada mudou|A tabela tem prioridade sobre o `.env`. Altere o valor na tabela ou pelo endpoint.|
|`400` no `PUT` pedindo `isSecret = true`|A chave parece sensível (termina em `Key`, `Password`, `Secret`, `Token` ou `ConnectionString`). Envie `isSecret: true`.|
|`400` no `PUT` dizendo que a chave só pode ser definida no `.env`|É uma chave de bootstrap: edite o `.env` e reinicie.|
|Mudei na tabela e demora a valer|Aguarde até 1 minuto, use o `PUT` ou reinicie o container (obrigatório para valores lidos no startup).|
|`Falha ao migrar (tentativa X/10)` repetido|SQL Server fora do ar, senha errada em `MSSQL_SA_PASSWORD` ou rede `homelab-network` incorreta. Veja `docker logs sqlserver-clubes`.|
|Webhook do Mercado Pago rejeita a assinatura|`MercadoPago:WebhookSecret` diferente do configurado no painel do Mercado Pago (o modo de teste usa segredo próprio).|

---

## Boas práticas de segurança

- Nunca versione o `.env` nem exponha as chaves em logs ou issues. Isso vale também para senhas de app em arquivos de compose versionados: se alguma já foi exposta, revogue e gere outra.
- Use senhas e chaves diferentes por ambiente (desenvolvimento, homologação, produção).
- Em produção, mantenha `ASPNETCORE_ENVIRONMENT=Production` e restrinja `AllowedHosts` ao seu domínio.
- Mantenha `SETTINGS_MASTER_KEY`, `CPF_ENCRYPTION_KEY` e `CPF_HMAC_KEY` com backup **separado** do backup do banco.
- O endpoint `/api/Setting/Backoffice` deve ficar restrito ao root, e o corpo das requisições **não** deve ser logado.
- Restrinja a permissão de escrita na tabela `AppSettings`: quem a altera controla a API.
- Faça backup periódico do banco e da pasta de uploads, e um backup do banco antes de migrations destrutivas.

---

## Referências

- [Documentação do Angular](https://angular.dev/docs)
- [Documentação do ASP.NET Core](https://learn.microsoft.com/pt-br/aspnet/core/)
- [Documentação técnica do SQL Server](https://learn.microsoft.com/pt-br/sql/sql-server/)
- [Claude](https://www.anthropic.com/claude)

---

## Licença

Projeto acadêmico desenvolvido na Fatec Taquaritinga. 

[Apache 2.0 License](LICENSE), Version 2.0, January 2004, [www.apache.org/licenses/](http://www.apache.org/licenses/)
