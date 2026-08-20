# Histórico de alterações

Tradução de [CHANGELOG.md](../../CHANGELOG.md). O inglês é a fonte; havendo
divergência, vale o texto inglês.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/), e o
versionamento [Semantic Versioning](https://semver.org/lang/pt-BR/).

## [Não publicado]

### Alterado
- **O ícone da área de notificação mostra em que estado está.** Sem login:
  cinza. Com login e tudo dentro do limite: um visto verde; a partir do limiar
  de aviso, uma interrogação âmbar; do crítico, uma exclamação vermelha. Antes
  isso ficava só por conta da cor. Um sinal por estado: a dezesseis pixels,
  dois já não se distinguem.

### Corrigido
- **O crédito adicional aparecia cem vezes maior e na unidade errada.** A API
  informa `used_credits: 2276`, e não são 2276 créditos e sim 22,76 EUR: um
  valor na menor unidade da moeda. **A moeda vem da conta** — USD, BRL, conforme
  o caso —, assim como o número de casas decimais, porque nem toda moeda tem
  duas. Agora é lido o campo `spend`, que diz o que seus números significam.

## [0.6.1] – 2026-08-20

### Alterado
- Versão de manutenção.

## [0.6.0] – 2026-08-20

### Corrigido
- **Os limites semanais por modelo não apareciam.** Quem tem um limite do Fable
  não o via em lugar nenhum – nem na dica de ferramenta, nem no menu de
  contexto, nem na janela de detalhes – embora o próprio Claude o informe. O
  motivo: o aplicativo lia os campos `seven_day_opus` e `seven_day_sonnet`, que
  trazem o nome do modelo no identificador. Ambos estão vazios agora, e não
  existe um campo `seven_day_fable`.

  A API entrega os mesmos valores também em uma lista `limits`, que nomeia o
  modelo no conteúdo (`scope.model.display_name`). Essa lista passa a ter
  preferência; os campos antigos continuam como reserva. **Qualquer modelo
  futuro aparecerá sozinho**, sem alteração aqui. Detalhes em
  [docs/api-research.md](../api-research.md).

  O ícone na área de notificação também considera esses limites – antes ele
  continuava verde enquanto uma cota de modelo já estava esgotada.

### Adicionado
- **Nove idiomas.** Alemão, inglês, espanhol, francês, italiano, português
  (Brasil e Portugal separadamente), russo e chinês simplificado. Na primeira
  execução o aplicativo segue o idioma do sistema; ele pode ser trocado na
  janela de instalação – onde a escolha vale de imediato e é assumida por
  **ambos** os botões – e depois a qualquer momento nas configurações.

  Junto com o idioma muda também a cultura de números, datas e horas: quem
  coloca a interface em francês não espera ali datas alemãs.

  **O histórico de alterações também está traduzido.** O resumo exibido após uma
  atualização aparece, portanto, no mesmo idioma da interface. O inglês é
  a fonte e está em [CHANGELOG.md](../../CHANGELOG.md); as traduções, entre elas
  a alemã, ficam em [docs/changelog/](.).

  Não se traduzem nomes de produtos e modelos: «Claude UsageChecker», «Claude
  Code» e o nome do modelo vindo da API – «Fable» se chama Fable em qualquer
  idioma.
- **Os limiares de aviso e crítico são configuráveis.** A partir de que uso o
  ícone fica amarelo, e a partir de qual fica vermelho, agora se ajusta nas
  configurações em vez de ficar fixo no código (padrões inalterados: 75 % e
  90 %). Um limiar de aviso acima do crítico é recusado em vez de corrigido em
  silêncio: ele nunca chegaria a valer.
- **Resumo das novidades após uma atualização.** Na primeira execução de uma
  nova versão, o aplicativo mostra o que mudou desde a versão executada
  anteriormente. Versões intermediárias puladas entram junto. A fonte é o
  histórico que acompanha o programa, sem acesso à rede: o resumo fica
  disponível mesmo offline e mostra forçosamente o estado que pertence à versão
  em execução. Na primeiríssima execução ele é omitido.
- **«Sobre o Claude UsageChecker» no menu de contexto.** Mostra o ícone, a
  versão, uma breve descrição e leva à página do projeto. De lá também se chega
  ao histórico completo.

### Alterado
- **O idioma do projeto é o inglês.** Documentação, comentários, identificadores
  e nomes de testes: tudo no repositório, exceto os textos alemães da interface
  e o histórico de commits até aqui. O motivo é simples: é um repositório
  público, e quem o encontrar deveria conseguir lê-lo. A documentação em alemão
  segue em paralelo em [docs/de/](../de/).
- A versão executada por último fica anotada no arquivo de configurações
  (`lastRunVersion`). É a única indicação pela qual o aplicativo consegue
  reconhecer uma atualização: o executável em si não sabe o que rodava antes
  dele.

  As versões mais antigas não conheciam esse campo. Quem atualiza a partir de
  uma delas não tem nada anotado; nesse caso decide a existência do arquivo de
  configurações: ela comprova que o aplicativo já rodou, e são mostradas as
  novidades da versão em curso. Sem esse ramo, justamente a versão que introduz
  o resumo não mostraria nenhum.
- `MonitorOptions` não carrega mais os limiares. O monitor nunca os leu: ele
  busca valores, não os julga. O julgamento acontece em um único lugar, no
  `TrayIconSeverityResolver`, a partir das configurações do usuário. Dois
  lugares para o mesmo dado seriam um convite a mexer depois no botão errado.
- O `PollInterval` calculado não é mais escrito no arquivo de configurações. Lá
  ele nunca era lido; apenas parecia uma segunda indicação sobre o intervalo de
  consulta, capaz de contradizer a primeira.
- **A janela de configurações permanece na tela.** Ela cresce com o próprio
  conteúdo e não é redimensionável; em uma tela baixa, saía pela borda inferior
  levando junto o botão «Salvar». Agora há duas garantias: a linha de botões
  fica ancorada abaixo da área rolável e continua visível por mais baixa que
  seja a tela, e a janela é medida depois de montada e deslocada para cima caso
  ainda ultrapasse. Limitar a altura não bastava: o Avalonia centraliza a janela
  pela altura que ela tem ao abrir, e o conteúdo cresce depois.

### Removido
- **A digitação manual de um token** saiu das configurações. Não podia servir a
  ninguém: o único token possível de colar vem de `claude setup-token`, e falta
  a ele o escopo `user:profile` exigido pelo endpoint. Os tokens que funcionam —
  o da instalação do Claude Code e o do login do próprio aplicativo — não são
  digitados à mão. Um token armazenado por uma versão anterior continua sendo
  lido; some apenas a maneira de acrescentar um. Justificativa em
  [docs/api-research.md](../api-research.md).

### Documentação
- **Modelos para relatos de erro e pedidos de recurso** em
  `.github/ISSUE_TEMPLATE/`, além de um modelo para pull requests e
  [CONTRIBUTING.md](../../CONTRIBUTING.md) – em inglês, para que um relato
  também possa vir de fora da área de língua alemã. Os formulários perguntam a
  versão, o sistema operacional, a assinatura e a origem do token, e alertam
  expressamente contra colar um token.
- As notas sobre a API ([docs/api-research.md](../api-research.md)) registram
  o novo formato de resposta, inclusive os campos que ficam sem uso e por quê.

## [0.5.0] – 2026-08-19

### Alterado
- O destino da instalação agora é
  `%LOCALAPPDATA%\Programs\ClaudeUsageChecker` em vez de
  `%USERPROFILE%\ClaudeUsageChecker`. É o lugar previsto pelo Windows para
  aplicativos sem direitos de administrador: lá também ficam o VS Code e o
  Signal. A raiz do perfil do usuário fica assim livre, onde ninguém espera
  programas ao lado de documentos e downloads.

  **As instalações já existentes não se mudam sozinhas.** Elas continuam
  rodando do local antigo. Para mudar, basta abrir as configurações e salvar:
  com a caixa de inicialização automática marcada, a cópia vai para o novo
  destino. O diretório antigo pode ser excluído depois à mão.

## [0.4.2] – 2026-08-19

### Corrigido
- Quem pulava a instalação na primeira execução e mais tarde apenas marcava
  «Iniciar com o Windows» recebia uma entrada de inicialização automática que
  apontava para a pasta de downloads – sem valor já na primeira limpeza dessa
  pasta. A caixa agora acarreta também a mudança, com aviso prévio sobre o
  caminho de destino e a reinicialização.
- **Desmarcá-la**, ao contrário, deixa o aplicativo onde está. Só a entrada de
  inicialização automática é removida; uma vez instalado, continua instalado.

## [0.4.1] – 2026-08-19

### Corrigido
- As pastas de extração de versões anteriores ficavam no diretório temporário.
  Um arquivo único compactado não consegue carregar suas bibliotecas nativas do
  pacote: o runtime do .NET as extrai para
  `%TEMP%\.net\ClaudeUsageChecker\<identificador>`, e como o identificador
  depende do conteúdo, cada versão ganhava a própria pasta. Cerca de 16 MB por
  atualização, acumulando sem limite. O aplicativo agora as remove sozinho.

### Documentação
- [SECURITY.md](../../SECURITY.md) lista integralmente o que o aplicativo
  armazena e onde, e o que restaria após uma desinstalação.

## [0.4.0] – 2026-08-19

### Adicionado
- **Instalação permanente.** Se o aplicativo roda fora do seu destino, ele
  oferece uma única vez, na primeira execução, copiar-se para
  `%USERPROFILE%\ClaudeUsageChecker`, configurar a inicialização automática e
  reiniciar de lá. O motivo não é apreço pela ordem: a inicialização automática,
  o ícone fixado na área de notificação e a autoatualização dependem todos do
  caminho do executável; se ele estiver na pasta de downloads, os três quebram
  assim que essa pasta for limpa.
- A inicialização automática é ativada junto com a instalação e aponta para o
  caminho de destino, não para o local de partida. Pode ser desligada nas
  configurações.

### Alterado
- A janela de detalhes aparece centralizada na tela e traz uma borda fina na cor
  do ícone em vez da moldura do sistema.

### Adicionado
- Um teste verifica se a borda realmente recebe sua cor. Um `DynamicResource`
  não resolvível ficaria, do contrário, vazio em silêncio.

## [0.3.3] – 2026-08-19

### Alterado
- O arquivo publicado tem o mesmo nome em cada versão:
  `ClaudeUsageChecker.exe` em vez de `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  A autoatualização escreve a nova versão no caminho do arquivo em execução: um
  nome com versão afirmaria depois uma versão errada. E o Windows memoriza a
  fixação na área de notificação por caminho: se o nome não continuasse igual, o
  ícone acabaria de novo na área de estouro após cada atualização.

## [0.3.2] – 2026-08-19

### Corrigido
- Os botões do aviso de atualização saíam da janela. Lado a lado precisavam de
  cerca de 420 pixels, e a janela tem 380 de largura: «Abrir a página da versão»
  só era legível pela metade. Agora ficam um abaixo do outro.

### Adicionado
- Testes que revelam transbordamentos na janela de detalhes. Eles medem a
  colocação real após um ciclo completo de layout e comparam a borda direita de
  cada elemento com a largura da janela. Nem o tamanho desejado dos controles
  nem o da janela servem para isso: o Avalonia limita ambos ao valor informado,
  de modo que um transbordamento nem pode aparecer ali.

## [0.3.1] – 2026-08-19

### Alterado
- A interface escreve tremas como tremas. Antes constava «Auf Aktualisierungen
  pruefen», «Gueltig bis» ou «Der Browser liess sich nicht oeffnen» – essas
  transliterações vinham do desenvolvimento e não tinham nada que fazer na tela.
  36 cadeias afetadas.
- A mensagem sobre a falta de permissão de acesso remete às configurações também
  onde antes exigia um token a ser guardado.

### Adicionado
- Um teste verifica a codificação de caracteres do arquivo-fonte até a
  interface. Um erro de codificação aparece assim na execução dos testes em vez
  de no usuário.

## [0.3.0] – 2026-08-19

A primeira versão capaz de se atualizar sozinha. A partir daqui basta um clique:
o download manual deixa de ser necessário.

### Corrigido
- As versões são exibidas com três componentes. O quarto vem da versão de
  assembly e não diz nada: «A versão 0.2.0.0 está atualizada» só confundia.

### Adicionado
- **Atualização com um clique.** «Instalar agora e reiniciar» baixa a nova
  versão, confere sua soma SHA-256 contra a publicada, substitui o arquivo em
  execução e reinicia. Um aviso que precisa ser resolvido à mão, na prática,
  acaba ficando para depois.
  - Se a soma de verificação não bater ou faltar, nada é instalado nem
    executado.
  - O endereço vem da resposta do GitHub sobre este repositório; endereços sem
    HTTPS são descartados.
  - Somente após um clique explícito, nunca em silêncio em segundo plano.
  - A substituição aproveita que o Windows permite renomear um arquivo em
    execução. Se a colocação falhar, a renomeação é desfeita.

### Alterado
- «Mostrar detalhes» foi retirado do menu de contexto. O clique esquerdo no
  ícone abre a janela de detalhes, e os números estão de qualquer forma nas
  linhas de status acima: a entrada apenas oferecia o mesmo caminho uma segunda
  vez.
- O aviso sobre a falta de permissão de acesso cita primeiro o login próprio.
  Antes constava «Faça login no Claude Code» – um conselho que ninguém podia
  seguir em uma máquina sem Claude Code.

## [0.2.0] – 2026-08-19

Primeira publicação. Arquivo único autônomo para Windows x64, 21 MB, sem
necessidade de runtime do .NET.

### Exibição

- Limite de sessão de 5 horas e limites semanais (total, Opus, Sonnet) de
  `GET /api/oauth/usage`: valores oficiais, não estimativas.
- Dica de ferramenta com o uso, o horário de reinício e o tempo restante. Se o
  reinício cair em outro dia, o dia da semana vem antes; a partir de uma semana,
  a data – um horário isolado seria ambíguo para o limite semanal.
- Menu de contexto com **todos** os limites informados.
- Janela de detalhes com barras de progresso, horários de reinício, créditos
  adicionais (`extra_usage`) e a origem do token efetivamente usada.
- Ícone da área de notificação com código de cores: normal, tenso, crítico.

### Login

- **Login próprio via OAuth com PKCE** (RFC 7636, S256): torna o aplicativo
  independente de uma instalação do Claude Code em execução. A única permissão
  solicitada é `user:profile`; expressamente **não** `user:inference` nem
  `org:create_api_key`.
- Sem servidor web local: o código é colado à mão em vez de recebido por um
  redirecionamento para `localhost`. Nenhuma porta aberta.
- O token próprio é renovado automaticamente. Com o token lido do Claude Code
  isso é omitido de propósito: um refresh token rotativo invalidaria o login
  dele. Entradas separadas no armazenamento seguro.
- Se o login próprio expirar, ele é removido e informado, em vez de recair em
  silêncio no Claude Code. Uma simples perturbação (rede, 5xx, limitação) o
  deixa, por outro lado, intacto.
- Cadeia de reserva: login próprio → token armazenado → variável de ambiente →
  Claude Code. Se a API recusar uma fonte, a consulta segue para a próxima.

### Operação

- Intervalo de consulta de no mínimo 180 segundos, espera exponencial após
  falhas, o `Retry-After` do servidor tem prioridade.
- Apenas uma instância por sessão de login.
- Inicialização automática com o Windows, desligável.
- Verificação de atualizações pelas releases do GitHub. Nada é baixado nem
  executado: apenas informado e, se desejado, aberta a página da versão.
- Erros nas ações da área de notificação não encerram mais o aplicativo, mas vão
  parar com seu contexto no `crash.log`.

### Constatações que moldaram o projeto

- **`claude setup-token` não serve para esta finalidade.** Esses tokens são
  válidos e funcionam com `/v1/messages`, mas não carregam `user:profile`. O
  endpoint de uso os recusa com HTTP 403. Essa era a suposição original do
  projeto, e está refutada.
- **O endpoint de tokens fica em `platform.claude.com`**, não mais em
  `console.anthropic.com`, onde responde HTTP 404.
- **O `User-Agent` é obrigatório.** Sem um user agent do Claude Code, o endpoint
  de uso limita permanentemente com HTTP 429.
- Compilado com trimming e compressão: 21 MB em vez de 93 MB, início em 2,3 em
  vez de 7,2 segundos, 87 em vez de 136 MB de memória. O trimming vence nos três
  eixos: o código removido também não precisa ser carregado e compilado.

### Limitações conhecidas

- O pacote **não é assinado**. O Windows SmartScreen informa um editor
  desconhecido na primeira execução.
- Quanto tempo o login próprio sobrevive a uma pausa longa é desconhecido: a
  Anthropic não documenta a vida útil do refresh token.
- O processo de login usa o ID de cliente OAuth publicamente conhecido do Claude
  Code, já que a Anthropic não oferece registro de aplicativos próprios. Não é
  um caminho oficialmente suportado; pode mudar a qualquer momento.
- O macOS está preparado, mas não implementado.
