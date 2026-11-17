# Clubera

<img src="https://www.clubera.dev/logo.png" align="right" width="200" alt="Texto Alternativo">

> Sistema para reserva de quadras esportivas online.


O **Clubera** é uma plataforma web que centraliza o gerenciamento de clubes, quadras, horários e reservas esportivas. O projeto é composto por:

- **Frontend:** interface web em **Angular 21** com **Tailwind CSS** e **daisyUI**.
- **API:** API REST em **.NET 10** com **Entity Framework Core** e **SQL Server**.

> Documentação do [FrontEnd](Web/UI-PI-Clubes/README.md) | Documentação do [BackEnd](Back/API-PI-Clubes/README.md)

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

| Papel | Nome |
|---|---|
| Desenvolvedor | Felipe Adrian Lourenço Barbisan |
| Desenvolvedor | Felipe Gabriel Galati de Oliveira |
| Orientador | Gustavo Henrique del Vechio |

Fatec Taquaritinga — 2026

> ⚠️ O sistema está **em desenvolvimento**. Algumas funcionalidades podem mudar ou ainda não estar completas.

---

## Funcionalidades

### Para jogadores

- **Busca de clubes e quadras:** listagens paginadas com filtro por nome, cidade e modalidade esportiva. A cidade é detectada automaticamente por geolocalização (com fallback silencioso caso a permissão seja negada).
- **Página inicial personalizada:** clubes em destaque e clubes perto de você, com carrossel de fotos.
- **Detalhes do clube:** galeria de imagens, endereço completo, mapa incorporado, modalidades e quadras disponíveis.
- **Reservas:** escolha da quadra, da data (próximos 7 dias) e do horário, com confirmação em modal. A API aplica regras de antecedência para criação e cancelamento.
- **Disponibilidade em tempo real:** os horários são atualizados na tela via **SignalR** (agrupado por clube), sem recarregar a página.
- **Minhas reservas:** listagem com busca, filtro por status, paginação e cancelamento.
- **Perfil:** edição de nome e telefone, troca de avatar com pré-visualização e validação de tamanho (até 5 MB).

### Para administradores de clubes

- **Planos e assinatura:** acesso por assinatura, com escolha do plano, pagamento via **Mercado Pago (Checkout Pro)** com redirecionamento ao checkout, validação de assinatura do webhook, acompanhamento da confirmação do pagamento, histórico de pagamentos e cancelamento da assinatura.
- **Gestão de clubes:** cadastro de clubes com upload de fotos e preenchimento automático de endereço pelo CEP (**ViaCEP**).
- **Painel do clube:**
  - **Visão geral:** quadras, reservas do dia, jogadores e reservas recentes.
  - **Quadras:** CRUD completo com tipo, superfície, cobertura, preço e imagens. Cada quadra pode ser vinculada a um ou mais esportes.
  - **Horários:** criação individual ou em massa (por dias da semana e duração do slot).
  - **Reservas:** listagem com busca, filtros, paginação, dados do jogador, contato por WhatsApp e cancelamento.
  - **Configurações:** edição do clube, até 5 fotos, com reordenação por *drag and drop*.

### Contas e autenticação

- Cadastro com e-mail e senha ou com **Google**.
- Verificação de e-mail (com reenvio do link) e redefinição de senha.
- Cadastro em duas etapas, em conformidade com a **LGPD**: conclusão de perfil na segunda etapa (telefone, CPF e data de nascimento), com validação de CPF.
- CPF armazenado criptografado (**AES-256-GCM**) com hash **HMAC-SHA256** para detecção de duplicidade.
- Sessão via cookies (`withCredentials`), com restauração automática ao abrir o app.
- Tema **claro/escuro** salvo no navegador.

### Outros recursos

- **Imagens:** upload com geração de três variantes (Thumb, Medium e Full) usando SkiaSharp. Armazenamento local ou em Azure Blob Storage.
- **Configuração dinâmica da API:** parâmetros e segredos (JWT, SMTP, Mercado Pago etc.) ficam na tabela `AppSettings`, com os segredos criptografados, e podem ser alterados por um endpoint de backoffice, sem novo deploy.
- **Limpeza automática:** serviços em segundo plano para rotinas de manutenção (ex.: limpeza de reservas antigas).

---

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | Angular 21, TypeScript (strict) |
| Estilo | Tailwind CSS + daisyUI |
| Reatividade | Signals e RxJS |
| Tempo real | SignalR (`@microsoft/signalr` no cliente) |
| Máscaras de input | `ngx-mask` |
| Calendário | `cally` |
| Login social | Google Identity Services |
| API | .NET 10, ASP.NET Core |
| Persistência | Entity Framework Core, SQL Server |
| Pagamentos | Mercado Pago |
| Imagens | SkiaSharp |
| Integrações externas | ViaCEP (endereço por CEP), Nominatim/OpenStreetMap (geocodificação reversa), Google Maps (mapa incorporado) |
| Infra | Docker, Docker Compose, GitHub Actions, GHCR |

Fontes do frontend: **Roboto** (texto) e **Space Grotesk** (destaques).

---

## Arquitetura

### Frontend

Aplicação **Angular standalone** (sem NgModules), organizada por responsabilidade:

```
src/app/
├── core/          # Serviços globais, guards, interceptors e modelos de autenticação
├── features/      # Funcionalidades, uma pasta por área
│   ├── admin/     #   Painel do administrador (clubes, quadras, horários, reservas, planos, pagamento)
│   ├── auth/      #   Login, cadastro, verificação de e-mail, recuperação de senha, perfil
│   ├── clubs/     #   Listagem e detalhes de clubes e quadras (visão do jogador)
│   ├── home-page/ #   Página inicial
│   └── user/      #   Perfil e reservas do jogador
├── layout/        # MainLayout (navbar + footer) e AdminLayout (menu lateral)
└── shared/        # Componentes, diretivas, validadores e resolvers reutilizáveis
```

**Padrões adotados**

- **Signals** para estado local e estado dos serviços (`signal`, `computed`, `asReadonly`).
- **`ChangeDetectionStrategy.OnPush`** nos componentes.
- **`inject()`** no lugar de injeção por construtor.
- **Lazy loading** das rotas com `loadComponent`.
- **Reactive Forms** com validações e mensagens acessíveis (`aria-invalid`, `aria-describedby`, `role="alert"`).
- **Control flow nativo** (`@if`, `@for`, `@switch`) nos templates.
- **`NgOptimizedImage`** para as imagens.
- Serviços por domínio, com `providedIn: 'root'`, expondo estado de `loading` e `error`.
- Acessibilidade seguindo as diretrizes **WCAG AA**.

**Rotas e proteção**

| Guard | Função |
|---|---|
| `guestGuard` | Impede usuários logados de acessar login e cadastro. |
| `authGuard` | Exige sessão ativa. |
| `adminGuard` | Restringe o painel administrativo a usuários com papel de administrador. |
| `completeProfileGuard` | Direciona quem ainda não concluiu o cadastro. |
| `selectRoleGuard` | Redireciona administradores à área de gestão. |

Um **interceptor HTTP** envia as credenciais (cookies) em todas as requisições à API e as desativa nas chamadas ao ViaCEP.

### API

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

---

## Rodando localmente

### Pré-requisitos

- **Node.js** (versão compatível com o Angular 21) e **npm**
- **Angular CLI**: `npm install -g @angular/cli`
- **Docker** e **Docker Compose** (para a API)

### 1. API (com Docker)

A API é publicada como imagem no **GitHub Container Registry (GHCR)**.

**Pré-requisitos adicionais**

- Um **SQL Server** rodando na rede Docker externa `homelab-network`, com o container chamado `sqlserver-clubes` (é o host usado na connection string)
- Um arquivo `.env` configurado, incluindo a `SETTINGS_MASTER_KEY` (veja [Configurando o `.env` da API](#configurando-o-env-da-api))

**Passo a passo**

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

Depois de conferir, você pode remover do `.env` e do `docker-compose.api.yml` as variáveis que já estão na tabela, mantendo só o bootstrap (veja [Como a configuração da API funciona](#como-a-configuração-da-api-funciona)).

**Comandos úteis**

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

**Usando outra imagem**

Por padrão o compose usa `ghcr.io/felipebarbisan766/api-clubes:latest`. Para usar outra tag ou imagem, defina `API_IMAGE` no `.env`:

```env
API_IMAGE=ghcr.io/felipebarbisan766/api-clubes:1.0.0
```

**Desenvolvendo a API com build local**

O `docker-compose.yml` da raiz da API sobe o SQL Server e builda a API a partir do código-fonte. Crie um `.env` ao lado dele (sem commitar) com `SETTINGS_MASTER_KEY`, `EMAIL_SMTP_USERNAME` e `EMAIL_SMTP_PASSWORD`, e rode `docker compose up -d --build`. As migrations e o seed também rodam automaticamente nesse modo. A rede `clubes-network` desse compose é externa e precisa existir antes (`docker network create clubes-network`).

**Deploy automático**

O compose `docker-compose.api.yml` é o que o **GitHub Actions** recria a cada deploy. Se o `.env` do servidor é gerado pelo workflow, lembre-se de incluir `SETTINGS_MASTER_KEY` entre os secrets do repositório.

### 2. Frontend

```bash
# 1. Clone o repositório
git clone <url-do-repositorio>
cd <pasta-do-projeto>

# 2. Instale as dependências
npm install

# 3. Inicie o servidor de desenvolvimento
ng serve
```

A aplicação ficará disponível em `http://localhost:4200` e espera a API em `http://localhost:5000`.

**Comandos úteis**

```bash
# Servidor de desenvolvimento
ng serve

# Build de produção (saída em dist/)
ng build

# Testes unitários
ng test
```

---

## Configuração

### Ambiente do frontend

Os ambientes ficam em `src/environments/`:

| Arquivo | Uso | `apiUrl` |
|---|---|---|
| `environment.development.ts` | Desenvolvimento (`ng serve`) | `http://localhost:5000/api` |
| `environment.ts` | Produção (`ng build`) | `https://api.clubera.dev/api` |

```ts
export const environment = {
  production: false,
  name: 'development',
  apiUrl: 'http://localhost:5000/api',
  googleClientId: 'SEU_CLIENT_ID.apps.googleusercontent.com',
};
```

| Variável | Descrição |
|---|---|
| `apiUrl` | URL base da API. O hub do SignalR (`/hubs/court-availability`) também é derivado dela. |
| `googleClientId` | Client ID do Google OAuth, usado no login e no cadastro com Google. Deve ser o mesmo configurado na API (`GOOGLE_CLIENT_ID` no `.env`, ou `Google:ClientId` na tabela `AppSettings`). |

### Como a configuração da API funciona

A configuração da API vem de duas origens, e **a tabela `AppSettings` tem prioridade máxima**. O `.env`/compose serve de base e de fallback.

Ordem de prioridade (o último vence):

```
appsettings.json  <  appsettings.{Ambiente}.json  <  variáveis de ambiente (.env/compose)  <  tabela AppSettings
```

| Grupo | Onde fica | O que é |
|---|---|---|
| **Bootstrap** | Sempre no `.env` | `ASPNETCORE_ENVIRONMENT`, `API_IMAGE`, `MSSQL_SA_PASSWORD` (e a connection string montada com ela), `SETTINGS_MASTER_KEY`, `CPF_ENCRYPTION_KEY`, `CPF_HMAC_KEY` |
| **Configuração comum** | Tabela `AppSettings`, texto puro | URLs, `Booking:*`, `Share:*`, `Jwt:Expiration*`, `Jwt:Issuer/Audience`, `Storage:Provider`, `Email:SenderName`, `ReserveCleanup:RetentionMonths` etc. |
| **Segredos** | Tabela `AppSettings`, **criptografados** (AES-256-GCM) | `Jwt:Key`, `Jwt:EmailVerificationKey`, `Jwt:ResetPasswordKey`, `Email:SmtpPassword`, `AzureStorage:ConnectionString`, `MercadoPago:AccessToken`, `MercadoPago:WebhookSecret` |

Por que o bootstrap fica fora do banco:

- A connection string e a senha do SQL são necessárias para chegar ao banco.
- `SETTINGS_MASTER_KEY` é a chave que decifra os segredos da tabela. Guardá-la no mesmo banco anularia a criptografia.
- `CPF_ENCRYPTION_KEY` e `CPF_HMAC_KEY` protegem dados já gravados. Uma troca (ou uma edição errada numa tabela) tornaria todos os CPFs ilegíveis.

### Configurando o `.env` da API

Crie um arquivo `.env` na mesma pasta do `docker-compose.api.yml`. **Nunca commite esse arquivo** — adicione `.env` ao `.gitignore`.

**Modelo**

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

**Descrição das variáveis**

| Variável | Descrição |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Ambiente da aplicação. Padrão: `Production`. |
| `API_IMAGE` | Imagem Docker da API (opcional). |
| `MSSQL_SA_PASSWORD` | Senha do usuário `sa` do SQL Server. Precisa atender à política de complexidade do SQL Server. |
| `SETTINGS_MASTER_KEY` | Chave (Base64, 32 bytes) que criptografa e decifra os segredos da tabela `AppSettings`. **Obrigatória**: sem ela a API não sobe. |
| `CPF_ENCRYPTION_KEY` | Chave de criptografia do CPF (AES-256-GCM). |
| `CPF_HMAC_KEY` | Chave do HMAC-SHA256 usado para detectar CPFs duplicados. |
| `API_BASE_URL` | URL pública da **API**. Origem de `MercadoPago:WebhookUrl` (`/api/payment/webhook`) e de `LocalStorage:BaseUrl` (URL dos arquivos enviados). |
| `PUBLIC_BASE_URL` | URL pública do **frontend**. Origem de `Email:BaseUrl` (links dos e-mails) e de `MercadoPago:BackUrls:*` (`/payment/success`, `/failure`, `/pending`). |
| `JWT_KEY` | Chave de assinatura dos tokens JWT. |
| `JWT_EMAIL_VERIFICATION_KEY` | Chave dos tokens de verificação de e-mail. |
| `JWT_RESET_PASSWORD_KEY` | Chave dos tokens de redefinição de senha. |
| `EMAIL_SENDER` | E-mail remetente. |
| `EMAIL_SMTP_USERNAME` | Usuário do SMTP (o servidor configurado é `smtp.gmail.com:587`). |
| `EMAIL_SMTP_PASSWORD` | Senha do SMTP. No Gmail, use uma **senha de app**, não a senha da conta. |
| `AZURE_STORAGE_CONNECTION_STRING` | Connection string do Azure Blob Storage. Só é necessária se `Storage:Provider` for `Azure`. |
| `GOOGLE_CLIENT_ID` | Client ID do Google OAuth, para login com Google. |
| `MERCADOPAGO_ACCESS_TOKEN` | Access token da aplicação no Mercado Pago. |
| `MERCADOPAGO_WEBHOOK_SECRET` | Segredo para validar a assinatura (`x-signature`) dos webhooks. |

**Gerando chaves seguras**

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
> - Se você trocar `SETTINGS_MASTER_KEY`, os segredos já gravados na tabela não podem mais ser decifrados e a API falha ao subir.
>
> Guarde as três chaves com cuidado, em local **separado do backup do banco**.

### Configurações da API no banco (AppSettings)

**Valores padrão**

Estes valores já vêm definidos no `docker-compose.api.yml` e são copiados para a tabela na primeira subida. Depois disso, altere-os na tabela:

| Configuração | Valor padrão |
|---|---|
| `Jwt:Expiration` | `3` |
| `Jwt:ExpirationEmailVerification` | `30` |
| `Jwt:ExpirationResetPassword` | `15` |
| `Jwt:Issuer` / `Jwt:Audience` | `api-pi-clubes` |
| `Storage:Provider` | `Local` |
| `ReserveCleanup:RetentionMonths` | `6` |
| `Email:SenderName` | `Clube PI` |
| `Email:SmtpServer` / `Email:SmtpPort` / `Email:EnableSsl` | `smtp.gmail.com` / `587` / `true` |
| `Booking:BookingCutoffMinutes` | `10` |
| `Booking:CancellationCutoffMinutes` | `10` |
| `Booking:TimeZoneId` | `America/Sao_Paulo` |
| `Share:FrontendBaseUrl` | `https://www.clubera.dev` |
| `Share:ApiPublicBaseUrl` | `https://api.clubera.dev` |
| `Share:AllowedImageHosts:0` | `api.clubera.dev` |

**Alterando valores**

Pelo endpoint de backoffice (recomendado, e **a única forma de alterar segredos**, já que a API é quem criptografa o valor). Exige o usuário root.

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

Cria a chave se ela não existir e atualiza se existir (responde `204`). Com `isSecret: true` o valor é criptografado antes de gravar; com `false`, é gravado em texto puro. Chaves de bootstrap (`ConnectionStrings`, `Security`, `SETTINGS_MASTER_KEY`) são recusadas, e chaves terminadas em `Key`, `Password`, `Secret`, `Token` ou `ConnectionString` exigem `isSecret: true`.

Valores **não secretos** também podem ser alterados direto por SQL:

```sql
UPDATE AppSettings
SET [Value] = 'America/Sao_Paulo', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = 'manual'
WHERE [Key] = 'Booking:TimeZoneId';
```

**Quando a mudança passa a valer**

- A API recarrega a tabela **a cada 1 minuto**. Pelo endpoint (`PUT`), a instância que atendeu a requisição recarrega na hora.
- Valores lidos **uma vez no startup** exigem reiniciar o container. Exemplos: `Jwt:Expiration` (duração do cookie de sessão) e as demais configurações de autenticação. Trocar `Jwt:Key` também invalida os tokens já emitidos.
- Na dúvida, reinicie: `docker compose -f docker-compose.api.yml --env-file .env restart api`.

**⚠️ Valores derivados das URLs**

No `.env`, algumas chaves são **montadas** a partir de `API_BASE_URL` e `PUBLIC_BASE_URL`. Depois do seed, elas ficam na tabela já com o valor final, e **mudar o `.env` não as atualiza mais**. Se trocar o domínio, edite estas chaves na tabela:

| Chave | Formato |
|---|---|
| `MercadoPago:WebhookUrl` | `{API_BASE_URL}/api/payment/webhook` |
| `LocalStorage:BaseUrl` | `{API_BASE_URL}` |
| `Email:BaseUrl` | `{PUBLIC_BASE_URL}` |
| `MercadoPago:BackUrls:Success` | `{PUBLIC_BASE_URL}/payment/success` |
| `MercadoPago:BackUrls:Failure` | `{PUBLIC_BASE_URL}/payment/failure` |
| `MercadoPago:BackUrls:Pending` | `{PUBLIC_BASE_URL}/payment/pending` |
| `Share:FrontendBaseUrl` / `Share:ApiPublicBaseUrl` / `Share:AllowedImageHosts:0` | Domínios do frontend e da API |

Se mudar a URL da API, atualize também o webhook no painel do Mercado Pago.

> Para a lista completa de erros comuns (chave mestra errada, `.env` que "não surte efeito", falha de migration etc.), veja a [documentação do BackEnd](Back/API-PI-Clubes/README.md).

**Migrations automáticas**

Toda vez que a API sobe, ela aplica as migrations pendentes antes de aceitar requisições, com até **10 tentativas** (5 s de intervalo) para esperar o SQL Server. Se todas falharem, o container encerra com erro e o `restart: unless-stopped` o reinicia (veja `docker logs api-clubes`).

- Funciona bem com **uma única instância** da API.
- Migrations que **apagam coluna ou tabela** rodam sozinhas no deploy: faça **backup antes**.
- Novas migrations são criadas no ambiente de desenvolvimento (a imagem de produção não tem o SDK): `dotnet ef migrations add NomeDaMigration`.

**Armazenamento de imagens**

Por padrão (`Storage:Provider = Local`), os arquivos são gravados em `/app/wwwroot/uploads` dentro do container, mapeado para `/DATA/AppData/homelab-api-uploads` no host. Para usar Azure Blob Storage, salve `AzureStorage:ConnectionString` (segredo) pelo endpoint de backoffice, altere `Storage:Provider` para `Azure` e reinicie o container.

### Integração entre frontend e API

- A API precisa liberar **CORS com credenciais** para a origem do frontend (`http://localhost:4200` em desenvolvimento), pois a autenticação usa cookies.
- A URL pública do frontend (`PUBLIC_BASE_URL`, que alimenta `Email:BaseUrl` e `MercadoPago:BackUrls:*` na tabela) deve apontar para o frontend, porque é usada nos links dos e-mails (verificação e redefinição de senha) e nos redirecionamentos do pagamento (`/payment/success`, `/payment/failure`, `/payment/pending`).
- O `googleClientId` do frontend deve ser o mesmo `GOOGLE_CLIENT_ID` da API. No Google Cloud Console, adicione a URL do frontend às **origens JavaScript autorizadas** do Client ID.

---

## Boas práticas de segurança

- Nunca versione o `.env` nem exponha as chaves em logs ou issues. Isso vale também para senhas de app em arquivos de compose versionados: se alguma já foi exposta, revogue e gere outra.
- Use senhas e chaves diferentes por ambiente (desenvolvimento, homologação, produção).
- Em produção, mantenha `ASPNETCORE_ENVIRONMENT=Production` e restrinja `AllowedHosts` ao seu domínio.
- Mantenha `SETTINGS_MASTER_KEY`, `CPF_ENCRYPTION_KEY` e `CPF_HMAC_KEY` com backup **separado** do backup do banco.
- O endpoint `/api/Setting/Backoffice` deve ficar restrito ao root, e o corpo das requisições **não** deve ser logado. Restrinja também a permissão de escrita na tabela `AppSettings`: quem a altera controla a API.
- Faça backup periódico do banco e da pasta de uploads, e um backup do banco antes de migrations destrutivas.
- O frontend não guarda tokens em `localStorage`: a sessão fica em cookies enviados pela API.
- O `localStorage` é usado apenas para a preferência de tema, e o `sessionStorage` para lembrar que o aviso de perfil incompleto foi dispensado.
- Valores sensíveis (chaves, segredos) pertencem somente à API; o `googleClientId` é público por natureza.
- Mantenha o `apiUrl` de produção sempre em **HTTPS**.

---

## Referências

- [Documentação do Angular](https://angular.dev/docs)
- [Tailwind CSS](https://tailwindcss.com/docs)
- [daisyUI](https://daisyui.com/docs/)
- [Documentação do ASP.NET Core](https://learn.microsoft.com/pt-br/aspnet/core/)
- [Documentação técnica do SQL Server](https://learn.microsoft.com/pt-br/sql/sql-server/)
- [SignalR (cliente JavaScript)](https://learn.microsoft.com/pt-br/aspnet/core/signalr/javascript-client)
- [ViaCEP](https://viacep.com.br/)
- [GitHub Copilot](https://docs.github.com/en/copilot)
- [Claude](https://www.anthropic.com/claude)

---

## Licença

Projeto acadêmico desenvolvido na Fatec Taquaritinga.

[Apache 2.0 License](LICENSE), Version 2.0, January 2004, [www.apache.org/licenses/](http://www.apache.org/licenses/).
