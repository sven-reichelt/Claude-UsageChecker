# Registo de alterações

Tradução de [CHANGELOG.md](../../CHANGELOG.md). O inglês é a fonte; havendo
divergência, prevalece o texto inglês.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e a
numeração [Semantic Versioning](https://semver.org/lang/pt-BR/).

## [Não publicado]

## [0.7.1] – 2026-08-20

### Alterado
- Versão de manutenção. Preparação para versões de teste; no dia a dia nada
  muda.

## [0.7.0] – 2026-08-20

### Alterado
- **O menu da área de notificação passa a ser desenhado pela própria
  aplicação.** O Windows desenha menus de contexto com o tipo de letra do
  sistema e sem moldura própria; ao lado das janelas desta aplicação parecia
  outro programa. Agora tem a mesma moldura, o mesmo tipo de letra e os mesmos
  espaçamentos.
- **O menu indica a versão.** A entrada passa a dizer «Acerca do Claude
  UsageChecker 0.7.0 …». É a primeira coisa que se pergunta a quem comunica
  um problema.

## [0.6.4] – 2026-08-20

### Corrigido
- **Uma janela cuja reposição já tinha vencido não tinha uma frase própria.**
  O bloco de duração era inserido num espaço que espera uma duração, dando
  «Sessão: 39 % - ainda já». Os quatro pontos que falam de tempo restante
  passam a ter uma frase própria: «reposição prevista».

## [0.6.3] – 2026-08-20

### Corrigido
- **A janela de detalhes ficava abaixo do centro do ecrã quando havia uma
  atualização disponível.** É criada uma vez e reutilizada, pelo que
  `CenterScreen` só valia na primeira abertura; o aviso chega segundos depois
  e torna-a cerca de cem pixels mais alta, crescendo para baixo. Passa a ser
  recentrada sempre que o conteúdo muda de tamanho.

## [0.6.2] – 2026-08-20

### Alterado
- **O ícone da área de notificação mostra em que estado está.** Sem sessão:
  cinzento. Com sessão e tudo dentro do limite: um visto verde; a partir do
  limiar de aviso, uma interrogação âmbar; do crítico, uma exclamação vermelha.
  Antes isso ficava apenas a cargo da cor. Um sinal por estado: a dezasseis
  pixels, dois já não se distinguem.

### Corrigido
- **O crédito adicional era apresentado cem vezes maior e na unidade errada.** A
  API indica `used_credits: 2276`, e não são 2276 créditos mas 22,76 EUR: um
  valor na unidade mais pequena da moeda. **A moeda vem da conta** — USD, BRL,
  consoante o caso —, tal como o número de casas decimais, porque nem todas as
  moedas têm duas. Passa a ser lido o campo `spend`, que diz o que os seus
  números significam.

## [0.6.1] – 2026-08-20

### Alterado
- Versão de manutenção.

## [0.6.0] – 2026-08-20

### Corrigido
- **Os limites semanais por modelo não apareciam.** Quem tem um limite do Fable
  não o via em lado nenhum – nem na dica, nem no menu de contexto, nem na janela
  de detalhes – embora o próprio Claude o indique. A causa: a aplicação lia os
  campos `seven_day_opus` e `seven_day_sonnet`, que trazem o nome do modelo no
  identificador. Ambos estão agora vazios, e não existe qualquer campo
  `seven_day_fable`.

  A API fornece os mesmos valores também numa lista `limits`, que nomeia o
  modelo no conteúdo (`scope.model.display_name`). Essa lista passa a ter
  precedência; os campos antigos ficam como recurso. **Qualquer modelo futuro
  aparecerá por si**, sem alteração aqui. Detalhes em
  [docs/api-research.md](../api-research.md).

  O ícone na área de notificação passa igualmente a considerar estes limites –
  antes mantinha-se verde enquanto uma quota de modelo já estava esgotada.

### Adicionado
- **Nove idiomas.** Alemão, inglês, espanhol, francês, italiano, português
  (Brasil e Portugal em separado), russo e chinês simplificado. No primeiro
  arranque a aplicação segue o idioma do sistema; pode ser mudado na janela de
  instalação – onde a escolha produz efeito de imediato e é assumida por
  **ambos** os botões – e mais tarde a qualquer momento nas definições.

  Com o idioma muda também a cultura para números, datas e horas: quem coloca a
  interface em francês não espera aí datas alemãs.

  **O registo de alterações também está traduzido.** O resumo mostrado após uma
  atualização surge, por isso, no mesmo idioma da interface. O inglês é a
  fonte e consta de [CHANGELOG.md](../../CHANGELOG.md); as traduções, entre elas
  a alemã, estão em [docs/changelog/](.).

  Não são traduzidos os nomes de produtos e modelos: «Claude UsageChecker»,
  «Claude Code» e o nome do modelo vindo da API – «Fable» chama-se Fable em
  qualquer idioma.
- **Os limiares de aviso e crítico são configuráveis.** A partir de que
  utilização o ícone fica amarelo, e a partir de qual fica vermelho, define-se
  agora nas definições em vez de estar fixo no código (predefinições
  inalteradas: 75 % e 90 %). Um limiar de aviso acima do crítico é recusado em
  vez de corrigido em silêncio: nunca chegaria a aplicar-se.
- **Resumo das novidades após uma atualização.** No primeiro arranque de uma
  nova versão, a aplicação mostra o que mudou desde a versão anteriormente
  executada. As versões intermédias saltadas são incluídas. A fonte é o registo
  que acompanha o programa, sem acesso à rede: o resumo fica disponível mesmo
  sem ligação e mostra forçosamente o estado que pertence à versão em execução.
  No primeiro arranque de todos é omitido.
- **«Acerca do Claude UsageChecker» no menu de contexto.** Mostra o ícone, a
  versão, uma breve descrição e conduz à página do projeto. A partir daí também
  se alcança o registo completo.

### Alterado
- **O idioma do projeto é o inglês.** Documentação, comentários, identificadores
  e nomes de testes: tudo no repositório, exceto os textos alemães da interface
  e o histórico de commits até aqui. A razão é simples: é um repositório
  público, e quem o encontrar deveria conseguir lê-lo. A documentação em alemão
  prossegue em paralelo em [docs/de/](../de/).
- A versão executada em último lugar fica anotada no ficheiro de definições
  (`lastRunVersion`). É a única indicação pela qual a aplicação consegue
  reconhecer uma atualização: o executável em si desconhece o que corria antes
  dele.

  As versões anteriores não conheciam esse campo. Quem atualiza a partir de uma
  delas nada tem anotado; nesse caso decide a existência do ficheiro de
  definições: prova que a aplicação já correu, e são mostradas as novidades da
  versão em curso. Sem esse ramo, precisamente a versão que introduz o resumo
  não mostraria nenhum.
- `MonitorOptions` já não transporta os limiares. O monitor nunca os leu: obtém
  valores, não os julga. O julgamento ocorre num único sítio, no
  `TrayIconSeverityResolver`, a partir das definições do utilizador. Dois
  sítios para o mesmo dado seriam um convite a mexer mais tarde no botão errado.
- O `PollInterval` calculado já não é escrito no ficheiro de definições. Aí
  nunca era lido; apenas parecia uma segunda indicação sobre o intervalo de
  consulta, capaz de contradizer a primeira.
- **A janela de definições mantém-se no ecrã.** Cresce com o seu conteúdo e não
  é redimensionável; num ecrã baixo saía pela margem inferior levando consigo o
  botão «Guardar». Agora há duas salvaguardas: a linha de botões fica ancorada
  por baixo da área deslocável e permanece visível por mais baixo que seja o
  ecrã, e a janela é medida depois de composta e deslocada para cima caso ainda
  ultrapasse. Limitar a altura não chegava: o Avalonia centra a janela pela
  altura que tem ao abrir, e o conteúdo cresce depois.

### Removido
- **A introdução manual de um token** saiu das definições. Não podia servir a
  ninguém: o único token que se podia colar vem de `claude setup-token`, e
  falta-lhe o âmbito `user:profile` exigido pelo ponto de acesso. Os tokens que
  funcionam — o da instalação do Claude Code e o do início de sessão da própria
  aplicação — não são escritos à mão. Um token guardado por uma versão anterior
  continua a ser lido; desaparece apenas a forma de acrescentar um.
  Justificação em [docs/api-research.md](../api-research.md).

### Documentação
- **Modelos para relatos de erro e pedidos de funcionalidade** em
  `.github/ISSUE_TEMPLATE/`, além de um modelo para pull requests e
  [CONTRIBUTING.md](../../CONTRIBUTING.md) – em inglês, para que um relato possa
  também chegar de fora do espaço de língua alemã. Os formulários perguntam pela
  versão, sistema operativo, subscrição e origem do token, e avisam
  expressamente para não colar qualquer token.
- As notas sobre a API ([docs/api-research.md](../api-research.md)) registam o
  novo formato de resposta, incluindo os campos que ficam por utilizar e porquê.

## [0.5.0] – 2026-08-19

### Alterado
- O destino da instalação é agora
  `%LOCALAPPDATA%\Programs\ClaudeUsageChecker` em vez de
  `%USERPROFILE%\ClaudeUsageChecker`. É o local previsto pelo Windows para
  aplicações sem direitos de administrador: aí encontram-se também o VS Code e o
  Signal. A raiz do perfil do utilizador fica assim livre, onde ninguém espera
  programas ao lado de documentos e transferências.

  **As instalações já existentes não se mudam sozinhas.** Continuam a correr do
  local antigo. Para mudar basta abrir as definições e guardar: com a caixa de
  arranque automático assinalada, a cópia segue para o novo destino. O
  diretório antigo pode depois ser eliminado à mão.

## [0.4.2] – 2026-08-19

### Corrigido
- Quem saltava a instalação no primeiro arranque e mais tarde apenas assinalava
  «Iniciar com o Windows» obtinha uma entrada de arranque automático a apontar
  para a pasta de transferências – sem valor logo à primeira limpeza dessa
  pasta. A caixa acarreta agora igualmente a mudança, com aviso prévio sobre o
  caminho de destino e o reinício.
- **Desmarcá-la**, pelo contrário, deixa a aplicação onde está. É removida
  apenas a entrada de arranque automático; uma vez instalada, assim permanece.

## [0.4.1] – 2026-08-19

### Corrigido
- As pastas de extração de versões anteriores ficavam no diretório temporário.
  Um ficheiro único comprimido não consegue carregar as suas bibliotecas nativas
  a partir do pacote: o runtime .NET extrai-as para
  `%TEMP%\.net\ClaudeUsageChecker\<identificador>`, e como o identificador
  depende do conteúdo, cada versão obtinha uma pasta própria. Cerca de 16 MB por
  atualização, a acumular sem limite. A aplicação limpa-as agora por si.

### Documentação
- [SECURITY.md](../../SECURITY.md) enumera integralmente o que a aplicação
  guarda e onde, e o que ficaria após uma desinstalação.

## [0.4.0] – 2026-08-19

### Adicionado
- **Instalação permanente.** Se a aplicação correr fora do seu destino, propõe
  uma única vez, no primeiro arranque, copiar-se para
  `%USERPROFILE%\ClaudeUsageChecker`, configurar o arranque automático e
  reiniciar a partir daí. O motivo não é gosto pela ordem: o arranque
  automático, o ícone afixado na área de notificação e a autoatualização
  dependem todos do caminho do executável; se este estiver na pasta de
  transferências, os três quebram assim que essa pasta for limpa.
- O arranque automático é ativado juntamente com a instalação e aponta para o
  caminho de destino, não para o local de partida. Desativável nas definições.

### Alterado
- A janela de detalhes surge centrada no ecrã e traz uma moldura fina da cor do
  ícone em vez do caixilho do sistema.

### Adicionado
- Um teste verifica se a moldura recebe realmente a sua cor. Um
  `DynamicResource` não resolúvel ficaria, de outro modo, vazio em silêncio.

## [0.3.3] – 2026-08-19

### Alterado
- O ficheiro publicado tem o mesmo nome em todas as versões:
  `ClaudeUsageChecker.exe` em vez de `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  A autoatualização escreve a nova versão no caminho do ficheiro em execução: um
  nome com versão passaria depois a afirmar uma versão errada. E o Windows
  memoriza a fixação na área de notificação por caminho: se o nome não se
  mantivesse igual, o ícone voltaria a parar na área de excedentes após cada
  atualização.

## [0.3.2] – 2026-08-19

### Corrigido
- Os botões do aviso de atualização saíam da janela. Lado a lado precisavam de
  cerca de 420 píxeis, e a janela tem 380 de largura: «Abrir a página da versão»
  só se lia a meio. Estão agora um por baixo do outro.

### Adicionado
- Testes que revelam transbordos na janela de detalhes. Medem a colocação real
  após um ciclo completo de disposição e comparam a margem direita de cada
  elemento com a largura da janela. Nem o tamanho pretendido dos controlos nem o
  da janela servem para isso: o Avalonia limita ambos ao valor indicado, pelo
  que um transbordo nem sequer pode aí surgir.

## [0.3.1] – 2026-08-19

### Alterado
- A interface escreve tremas como tremas. Antes lia-se «Auf Aktualisierungen
  pruefen», «Gueltig bis» ou «Der Browser liess sich nicht oeffnen» – essas
  transliterações vinham do desenvolvimento e nada tinham que fazer no ecrã. 36
  cadeias afetadas.
- A mensagem sobre a falta de permissão de acesso remete para as definições
  também onde antes exigia guardar um token.

### Adicionado
- Um teste verifica a codificação de caracteres desde o ficheiro de origem até à
  interface. Um erro de codificação nota-se assim na execução dos testes em vez
  de junto do utilizador.

## [0.3.0] – 2026-08-19

A primeira versão capaz de se atualizar a si própria. A partir daqui basta um
clique: a transferência manual deixa de ser necessária.

### Corrigido
- As versões são mostradas com três componentes. A quarta provém da versão de
  assembly e nada diz: «A versão 0.2.0.0 está atualizada» apenas confundia.

### Adicionado
- **Atualização com um clique.** «Instalar agora e reiniciar» transfere a nova
  versão, verifica a sua soma SHA-256 face à publicada, substitui o ficheiro em
  execução e reinicia. Um aviso que é preciso tratar à mão acaba, na prática,
  por ficar por tratar.
  - Se a soma de verificação não corresponder ou faltar, nada é instalado nem
    executado.
  - O endereço provém da resposta do GitHub relativa a este repositório; os
    endereços sem HTTPS são descartados.
  - Apenas após um clique expresso, nunca em silêncio em segundo plano.
  - A substituição aproveita o facto de o Windows permitir mudar o nome de um
    ficheiro em execução. Se a colocação falhar, a mudança de nome é revertida.

### Alterado
- «Mostrar detalhes» foi retirado do menu de contexto. O clique esquerdo no
  ícone abre a janela de detalhes, e os números constam de qualquer modo das
  linhas de estado acima: a entrada apenas oferecia o mesmo caminho uma segunda
  vez.
- O aviso sobre a falta de permissão de acesso refere primeiro a sessão própria.
  Antes lia-se «Inicia sessão no Claude Code» – um conselho que ninguém podia
  seguir numa máquina sem Claude Code.

## [0.2.0] – 2026-08-19

Primeira publicação. Ficheiro único autónomo para Windows x64, 21 MB, sem
necessidade de runtime .NET.

### Visualização

- Limite de sessão de 5 horas e limites semanais (total, Opus, Sonnet) a partir
  de `GET /api/oauth/usage`: valores oficiais, não estimativas.
- Dica com a utilização, a hora de reposição e o tempo restante. Se a reposição
  cair noutro dia, o dia da semana vem antes; a partir de uma semana, a data –
  uma simples hora seria ambígua para o limite semanal.
- Menu de contexto com **todos** os limites indicados.
- Janela de detalhes com barras de progresso, horas de reposição, créditos
  adicionais (`extra_usage`) e a origem do token efetivamente utilizada.
- Ícone da área de notificação com código de cores: normal, tenso, crítico.

### Sessão

- **Sessão própria através de OAuth com PKCE** (RFC 7636, S256): torna a
  aplicação independente de uma instalação do Claude Code em execução. A única
  permissão pedida é `user:profile`; expressamente **não** `user:inference` nem
  `org:create_api_key`.
- Sem servidor web local: o código é colado à mão em vez de recebido por um
  reencaminhamento para `localhost`. Nenhuma porta aberta.
- O token próprio é renovado automaticamente. Com o token lido do Claude Code
  isso é deliberadamente omitido: um refresh token rotativo invalidaria a sessão
  dele. Entradas separadas no armazenamento seguro.
- Se a sessão própria expirar, é removida e comunicada, em vez de recair em
  silêncio no Claude Code. Uma mera perturbação (rede, 5xx, limitação) deixa-a,
  pelo contrário, intacta.
- Cadeia de recurso: sessão própria → token guardado → variável de ambiente →
  Claude Code. Se a API recusar uma origem, a consulta avança para a seguinte.

### Funcionamento

- Intervalo de consulta de pelo menos 180 segundos, espera exponencial após
  falhas, o `Retry-After` do servidor tem precedência.
- Apenas uma instância por sessão iniciada.
- Arranque automático com o Windows, desativável.
- Procura de atualizações através das publicações do GitHub. Nada é transferido
  nem executado: apenas comunicado e, se desejado, aberta a página da versão.
- Os erros nas ações da área de notificação já não terminam a aplicação, antes
  vão parar com o respetivo contexto ao `crash.log`.

### Constatações que moldaram o projeto

- **`claude setup-token` não serve para este fim.** Esses tokens são válidos e
  funcionam com `/v1/messages`, mas não têm `user:profile`. O ponto final de
  utilização recusa-os com HTTP 403. Era o pressuposto inicial do projeto, e
  está refutado.
- **O ponto final dos tokens está em `platform.claude.com`**, já não em
  `console.anthropic.com`, onde responde HTTP 404.
- **O `User-Agent` é obrigatório.** Sem um user agent do Claude Code, o ponto
  final de utilização limita permanentemente com HTTP 429.
- Compilado com trimming e compressão: 21 MB em vez de 93 MB, arranque em 2,3 em
  vez de 7,2 segundos, 87 em vez de 136 MB de memória. O trimming vence nos três
  eixos: o código removido também não precisa de ser carregado e compilado.

### Limitações conhecidas

- O pacote **não está assinado**. O Windows SmartScreen indica um editor
  desconhecido no primeiro arranque.
- Quanto tempo a sessão própria sobrevive a uma pausa prolongada é desconhecido:
  a Anthropic não documenta a validade do refresh token.
- O processo de início de sessão usa o ID de cliente OAuth publicamente
  conhecido do Claude Code, uma vez que a Anthropic não permite registar
  aplicações próprias. Não é uma via oficialmente suportada; pode mudar a
  qualquer momento.
- O macOS está preparado, mas não implementado.
