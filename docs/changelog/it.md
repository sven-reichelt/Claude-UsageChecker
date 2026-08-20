# Registro delle modifiche

Traduzione di [CHANGELOG.md](../../CHANGELOG.md). L'inglese è la fonte; in caso
di divergenza fa fede il testo inglese.

Il formato segue [Keep a Changelog](https://keepachangelog.com/it/1.1.0/), la
numerazione [Semantic Versioning](https://semver.org/lang/it/).

## [Non pubblicato]

## [0.7.1] – 2026-08-20

### Aggiunto
- **Il pulsante di aggiornamento cerca per strada una nuova versione.** Una
  pressione invece di due. Disattivabile nelle impostazioni, per chi preferisce
  non toccare la rete più del necessario.
- **Le impostazioni sono disposte su due colonne.** Su una sola la finestra
  diventava alta e stretta: sullo schermo di un portatile occorreva scorrere
  per impostazioni che stanno comodamente affiancate.

### Modificato
- Preparazione delle versioni di prova. Nell'uso quotidiano non se ne vede
  nulla.

### Corretto
- **Il menu dell'area di notifica cresce con il suo contenuto.** Aveva una
  larghezza fissa e la riga dell'utilizzo aggiuntivo non ci stava: porta due
  importi e una valuta, e andava a capo. In un menu in cui ogni altra riga è
  un limite, una riga spezzata sembra due.
- **Correzioni di traduzione.** Sei lingue chiamavano ancora «crediti»
  l'utilizzo aggiuntivo benché la cifra sia denaro, e tutte affermavano che un
  registro delle modifiche non tradotto viene mostrato in tedesco: viene
  mostrato in inglese. I messaggi di errore dell'archivio credenziali di
  Windows erano fissi in tedesco; ora seguono la lingua dell'interfaccia.

### Sicurezza
- La pagina della release ricavata dalla risposta di GitHub è ora sottoposta
  allo stesso criterio degli indirizzi di download: solo https.
- Le action dei workflow sono fissate agli hash dei commit anziché a tag
  mobili. Un tag può essere spostato da chi amministra il repository
  dell'action; un hash no. Conta soprattutto per l'action di pubblicazione di
  terzi, che gira con permesso di scrittura nel workflow che compila
  l'eseguibile pubblicato.

## [0.7.0] – 2026-08-20

### Modificato
- **Il menu dell'area di notifica lo disegna ora l'applicazione stessa.**
  Windows disegna i menu contestuali con il carattere di sistema e senza una
  cornice propria: accanto alle finestre di questa applicazione sembrava un
  altro programma. Ora ha la stessa cornice, lo stesso carattere e le stesse
  spaziature.
- **Il menu indica la versione.** La voce ora recita «Informazioni su Claude
  UsageChecker 0.7.0 …». È la prima cosa che si chiede a chi segnala un
  problema.

## [0.6.4] – 2026-08-20

### Corretto
- **Per una finestra il cui reset era già dovuto non esisteva una frase
  corretta.** Il blocco per una durata veniva inserito in uno spazio che ne
  attende una: «Sessione: 39 % - ancora subito». I quattro punti che parlano
  di tempo rimanente hanno ora una frase propria: «reset atteso».

## [0.6.3] – 2026-08-20

### Corretto
- **La finestra dei dettagli stava sotto il centro dello schermo quando era
  disponibile un aggiornamento.** Viene creata una volta e riutilizzata,
  quindi `CenterScreen` agiva solo alla prima apertura; l'avviso arriva pochi
  secondi dopo e la fa crescere di un centinaio di pixel verso il basso. Ora
  viene ricentrata a ogni cambiamento di dimensione.

## [0.6.2] – 2026-08-20

### Modificato
- **L'icona nell'area di notifica dice in che stato si trova.** Disconnessa:
  grigia. Connessa e tutto nei limiti: un segno di spunta verde; dalla soglia
  di avviso un punto interrogativo ambra, da quella critica un punto
  esclamativo rosso. Prima ci pensava il solo colore. Un segno per stato: a
  sedici pixel due non si distinguono più.

### Corretto
- **Il credito aggiuntivo veniva mostrato cento volte più grande e nell'unità
  sbagliata.** L'API riporta `used_credits: 2276`, e non sono 2276 crediti ma
  22,76 EUR: un importo nell'unità più piccola della sua valuta. **La valuta
  viene dal conto** - USD, BRL, a seconda dei casi - così come il numero di
  decimali, perché non tutte le valute ne hanno due. Ora viene letto il campo
  `spend`, che dichiara che cosa significano le sue cifre.

## [0.6.1] – 2026-08-20

### Modificato
- Versione di manutenzione.

## [0.6.0] – 2026-08-20

### Corretto
- **I limiti settimanali legati a un modello non comparivano.** Chi ha un limite
  Fable non lo vedeva da nessuna parte – né nel suggerimento, né nel menu
  contestuale, né nella finestra dei dettagli – benché Claude stesso lo indichi.
  Il motivo: l'applicazione leggeva i campi `seven_day_opus` e
  `seven_day_sonnet`, che portano il nome del modello nell'identificatore.
  Entrambi sono ormai vuoti, e un campo `seven_day_fable` non esiste.

  L'API fornisce gli stessi valori anche in un elenco `limits`, che nomina il
  modello nel contenuto (`scope.model.display_name`). Quell'elenco ha ora la
  precedenza; i vecchi campi restano come ripiego. **Ogni modello futuro comparirà
  da sé**, senza modifiche qui. Dettagli in
  [docs/api-research.md](../api-research.md).

  Anche l'icona nell'area di notifica tiene conto di questi limiti: prima
  restava verde mentre una quota di modello era già esaurita.

### Aggiunto
- **Nove lingue.** Tedesco, inglese, spagnolo, francese, italiano, portoghese
  (Brasile e Portogallo separatamente), russo e cinese semplificato. Al primo
  avvio l'applicazione segue la lingua del sistema; si cambia nella finestra di
  installazione – dove la scelta ha effetto immediato ed è recepita da
  **entrambi** i pulsanti – e in seguito in qualsiasi momento nelle
  impostazioni.

  Con la lingua cambia anche la cultura per numeri, date e orari: chi imposta
  l'interfaccia in francese non si aspetta lì date tedesche.

  **Anche il registro delle modifiche è tradotto.** Il riepilogo che compare
  dopo un aggiornamento appare quindi nella stessa lingua dell'interfaccia. L'inglese
  è la fonte e si trova in [CHANGELOG.md](../../CHANGELOG.md); le traduzioni,
  tedesco compreso, stanno sotto [docs/changelog/](.).

  Non vengono tradotti i nomi di prodotti e modelli: «Claude UsageChecker»,
  «Claude Code» e il nome del modello fornito dall'API – «Fable» si chiama Fable
  in ogni lingua.
- **Le soglie di avviso e critica sono configurabili.** Da quale livello di
  utilizzo l'icona diventa gialla, e da quale rossa, si imposta ora nelle
  impostazioni anziché essere fissato nel codice (valori predefiniti invariati:
  75 % e 90 %). Una soglia di avviso superiore a quella critica viene rifiutata
  anziché corretta in silenzio: non scatterebbe mai.
- **Riepilogo delle novità dopo un aggiornamento.** Al primo avvio di una nuova
  versione, l'applicazione mostra che cosa è cambiato dalla versione eseguita in
  precedenza. Le versioni intermedie saltate sono incluse. La fonte è il
  registro che viaggia con il programma, senza accesso alla rete: il riepilogo è
  quindi disponibile anche offline e mostra per forza lo stato che appartiene
  alla versione in esecuzione. Al primissimo avvio viene omesso.
- **«Informazioni su Claude UsageChecker» nel menu contestuale.** Mostra
  l'icona, la versione, una breve descrizione e porta alla pagina del progetto.
  Da lì è raggiungibile anche il registro completo.

### Modificato
- **La lingua del progetto è l'inglese.** Documentazione, commenti,
  identificatori e nomi dei test: tutto nel repository tranne i testi tedeschi
  dell'interfaccia e la cronologia dei commit finora. Il motivo è semplice: è un
  repository pubblico, e chi lo trova dovrebbe poterlo leggere. La
  documentazione in tedesco prosegue in parallelo sotto [docs/de/](../de/).
- La versione eseguita per ultima viene annotata nel file delle impostazioni
  (`lastRunVersion`). È l'unico dato dal quale l'applicazione può riconoscere un
  aggiornamento: l'eseguibile in sé non sa che cosa girava prima di lui.

  Le versioni precedenti non conoscevano quel campo. Chi aggiorna da una di esse
  non ha nulla di annotato; in quel caso decide la presenza del file delle
  impostazioni: dimostra che l'applicazione è già stata eseguita, e vengono
  mostrate le novità della versione in corso. Senza quel ramo, proprio la
  versione che introduce il riepilogo non ne mostrerebbe nessuno.
- `MonitorOptions` non porta più le soglie. Il monitor non le ha mai lette:
  procura valori, non li giudica. Il giudizio avviene in un solo punto, nel
  `TrayIconSeverityResolver`, a partire dalle impostazioni dell'utente. Due
  punti per lo stesso dato sarebbero un invito a girare più tardi la manopola
  sbagliata.
- Il `PollInterval` calcolato non viene più scritto nel file delle impostazioni.
  Lì non è mai stato letto; sembrava soltanto una seconda indicazione
  sull'intervallo di lettura, capace di contraddire la prima.
- **La finestra delle impostazioni resta sullo schermo.** Cresce con il proprio
  contenuto e non è ridimensionabile; su uno schermo basso sporgeva dal margine
  inferiore portandosi via il pulsante «Salva». Ora lo impediscono due cose: la
  riga dei pulsanti è ancorata sotto l'area scorrevole e resta visibile per
  quanto basso sia lo schermo, e la finestra viene misurata a disposizione
  avvenuta e spostata verso l'alto se sporge ancora. Limitare l'altezza non
  bastava: Avalonia centra la finestra in base all'altezza che ha all'apertura,
  e il contenuto cresce dopo.

### Rimosso
- **L'inserimento manuale di un token** è sparito dalle impostazioni. Non poteva
  servire a nessuno: l'unico token da incollare proviene da
  `claude setup-token`, e gli manca l'ambito `user:profile` richiesto
  dall'endpoint. I token che funzionano - quello dell'installazione di Claude
  Code e quello dell'accesso proprio dell'applicazione - non si digitano a mano.
  Un token memorizzato da una versione precedente continua a essere letto;
  sparisce soltanto il modo di aggiungerne uno. Motivazione in
  [docs/api-research.md](../api-research.md).

### Documentazione
- **Modelli per segnalazioni di errori e richieste di funzionalità** in
  `.github/ISSUE_TEMPLATE/`, oltre a un modello per le pull request e
  [CONTRIBUTING.md](../../CONTRIBUTING.md) – in inglese, perché una segnalazione
  possa arrivare anche da fuori dell'area di lingua tedesca. I moduli chiedono
  versione, sistema operativo, abbonamento e origine del token, e mettono
  espressamente in guardia dall'incollare un token.
- Le note sull'API ([docs/api-research.md](../api-research.md)) fissano il
  nuovo formato della risposta, compresi i campi che restano inutilizzati e il
  perché.

## [0.5.0] – 2026-08-19

### Modificato
- La destinazione dell'installazione è ora
  `%LOCALAPPDATA%\Programs\ClaudeUsageChecker` anziché
  `%USERPROFILE%\ClaudeUsageChecker`. È il luogo previsto da Windows per le
  applicazioni senza diritti di amministratore: lì si trovano anche VS Code e
  Signal. La radice del profilo utente resta così libera, dove nessuno si
  aspetta programmi accanto a documenti e download.

  **Le installazioni già esistenti non si spostano da sole.** Continuano a
  funzionare dalla vecchia posizione. Per spostarle basta aprire le impostazioni
  e salvare: con la casella di avvio automatico selezionata, la copia va nella
  nuova posizione. La vecchia cartella può poi essere eliminata a mano.

## [0.4.2] – 2026-08-19

### Corretto
- Chi saltava l'installazione al primo avvio e in seguito spuntava solo «Avvia
  con Windows» otteneva una voce di avvio automatico che puntava alla cartella
  dei download: priva di valore già alla prima pulizia di quella cartella. La
  spunta comporta ora anche lo spostamento, con avviso preventivo sul percorso
  di destinazione e sul riavvio.
- **Toglierla**, al contrario, lascia l'applicazione dov'è. Viene rimossa solo
  la voce di avvio automatico; una volta installata, resta installata.

## [0.4.1] – 2026-08-19

### Corretto
- Le cartelle di estrazione delle versioni precedenti restavano nella directory
  temporanea. Un file singolo compresso non può caricare le proprie librerie
  native dal pacchetto: il runtime .NET le estrae in
  `%TEMP%\.net\ClaudeUsageChecker\<identificativo>`, e poiché l'identificativo
  dipende dal contenuto, ogni versione otteneva una cartella propria. Circa
  16 MB per aggiornamento, che si accumulavano senza limite. L'applicazione ora
  le rimuove da sé.

### Documentazione
- [SECURITY.md](../../SECURITY.md) elenca per intero che cosa l'applicazione
  memorizza e dove, e che cosa resterebbe dopo una disinstallazione.

## [0.4.0] – 2026-08-19

### Aggiunto
- **Installazione permanente.** Se l'applicazione viene eseguita al di fuori
  della sua destinazione, propone una sola volta al primo avvio di copiarsi in
  `%USERPROFILE%\ClaudeUsageChecker`, di impostare l'avvio automatico e di
  riavviarsi da lì. Il motivo non è l'amore per l'ordine: l'avvio automatico,
  l'icona fissata nell'area di notifica e l'aggiornamento automatico dipendono
  tutti dal percorso dell'eseguibile; se si trova nella cartella dei download,
  tutti e tre si rompono non appena quella cartella viene svuotata.
- L'avvio automatico viene attivato insieme all'installazione e punta al
  percorso di destinazione, non al luogo di partenza. Disattivabile nelle
  impostazioni.

### Modificato
- La finestra dei dettagli compare al centro dello schermo e porta un bordo
  sottile del colore dell'icona anziché la cornice di sistema.

### Aggiunto
- Un test verifica che il bordo riceva davvero il suo colore. Una
  `DynamicResource` non risolvibile resterebbe altrimenti vuota in silenzio.

## [0.3.3] – 2026-08-19

### Modificato
- Il file pubblicato ha lo stesso nome in ogni versione:
  `ClaudeUsageChecker.exe` anziché `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  L'aggiornamento automatico scrive la nuova versione nel percorso del file in
  esecuzione: un nome con la versione dichiarerebbe poi una versione sbagliata.
  E Windows memorizza per percorso l'ancoraggio nell'area di notifica: se il
  nome non restasse uguale, dopo ogni aggiornamento l'icona finirebbe di nuovo
  nell'area di overflow.

## [0.3.2] – 2026-08-19

### Corretto
- I pulsanti dell'avviso di aggiornamento uscivano dalla finestra. Affiancati
  richiedevano circa 420 pixel, la finestra ne è larga 380: «Apri la pagina
  della versione» era leggibile solo a metà. Ora stanno uno sotto l'altro.

### Aggiunto
- Test che scoprono le fuoriuscite nella finestra dei dettagli. Misurano la
  collocazione effettiva dopo un ciclo di layout completo e confrontano il bordo
  destro di ogni elemento con la larghezza della finestra. Né la dimensione
  desiderata dei controlli né quella della finestra servono allo scopo: Avalonia
  limita entrambe al valore indicato, cosicché una fuoriuscita non può proprio
  comparirvi.

## [0.3.1] – 2026-08-19

### Modificato
- L'interfaccia scrive le dieresi come dieresi. Prima vi si leggeva «Auf
  Aktualisierungen pruefen», «Gueltig bis» oppure «Der Browser liess sich nicht
  oeffnen»: quelle traslitterazioni venivano dallo sviluppo e non avevano nulla
  a che fare con lo schermo. 36 stringhe interessate.
- Il messaggio sulla mancanza di diritti di accesso rimanda alle impostazioni
  anche là dove prima pretendeva un token da memorizzare.

### Aggiunto
- Un test verifica la codifica dei caratteri dal file sorgente fino
  all'interfaccia. Un errore di codifica emerge così nell'esecuzione dei test
  anziché presso l'utente.

## [0.3.0] – 2026-08-19

La prima versione capace di aggiornarsi da sé. Da qui in poi basta un clic: il
download manuale non serve più.

### Corretto
- Le versioni vengono mostrate con tre cifre. La quarta proviene dalla versione
  di assembly e non dice nulla: «La versione 0.2.0.0 è aggiornata» creava solo
  confusione.

### Aggiunto
- **Aggiornamento con un clic.** «Installa adesso e riavvia» scarica la nuova
  versione, ne verifica la somma SHA-256 rispetto a quella pubblicata,
  sostituisce il file in esecuzione e riavvia. Un avviso da sbrigare a mano, in
  pratica, resta lì.
  - Se la somma di controllo non corrisponde o manca, non viene installato né
    eseguito nulla.
  - L'indirizzo proviene dalla risposta di GitHub relativa a questo repository;
    gli indirizzi senza HTTPS vengono scartati.
  - Solo dopo un clic esplicito, mai in silenzio in secondo piano.
  - La sostituzione sfrutta il fatto che Windows consente di rinominare un file
    in esecuzione. Se la collocazione fallisce, la rinomina viene annullata.

### Modificato
- «Mostra dettagli» è stato tolto dal menu contestuale. Il clic sinistro
  sull'icona apre la finestra dei dettagli, e i numeri stanno comunque nelle
  righe di stato sopra: la voce offriva soltanto una seconda volta la stessa
  strada.
- L'avviso sulla mancanza di diritti di accesso nomina per primo l'accesso
  proprio. Prima vi si leggeva «Accedi in Claude Code», un consiglio che nessuno
  poteva seguire su una macchina senza Claude Code.

## [0.2.0] – 2026-08-19

Prima pubblicazione. File singolo autonomo per Windows x64, 21 MB, senza
runtime .NET.

### Visualizzazione

- Limite di sessione di 5 ore e limiti settimanali (totale, Opus, Sonnet) da
  `GET /api/oauth/usage`: valori autorevoli, non stime.
- Suggerimento con utilizzo, ora di reset e tempo rimanente. Se il reset cade in
  un altro giorno, davanti compare il giorno della settimana; da una settimana
  in poi, la data – una semplice ora sarebbe ambigua per il limite settimanale.
- Menu contestuale con **tutti** i limiti segnalati.
- Finestra dei dettagli con barre di avanzamento, orari di reset, crediti
  aggiuntivi (`extra_usage`) e la fonte del token effettivamente usata.
- Icona dell'area di notifica con codice colore: normale, tesa, critica.

### Accesso

- **Accesso proprio tramite OAuth con PKCE** (RFC 7636, S256): rende
  l'applicazione indipendente da un'installazione di Claude Code in esecuzione.
  L'unico diritto richiesto è `user:profile`; espressamente **non**
  `user:inference` e **non** `org:create_api_key`.
- Senza server web locale: il codice viene incollato a mano anziché ricevuto
  tramite un reindirizzamento a `localhost`. Nessuna porta aperta.
- Il token proprio viene rinnovato automaticamente. Per il token letto da Claude
  Code ciò è deliberatamente omesso: un refresh token rotante ne invaliderebbe
  l'accesso. Voci separate nell'archivio sicuro.
- Se l'accesso proprio scade, viene rimosso e segnalato, anziché ripiegare in
  silenzio su Claude Code. Un semplice disturbo (rete, 5xx, limitazione) lo
  lascia invece intatto.
- Catena di ripiego: accesso proprio → token memorizzato → variabile d'ambiente
  → Claude Code. Se l'API rifiuta una fonte, la richiesta passa alla successiva.

### Funzionamento

- Intervallo di lettura di almeno 180 secondi, attesa esponenziale dopo i
  fallimenti, il `Retry-After` del server ha la precedenza.
- Una sola istanza per sessione di accesso.
- Avvio automatico con Windows, disattivabile.
- Controllo degli aggiornamenti tramite le release di GitHub. Non viene
  scaricato né eseguito nulla: solo segnalato e, su richiesta, aperta la pagina
  della versione.
- Gli errori nelle azioni dell'area di notifica non terminano più
  l'applicazione, ma finiscono con il loro contesto in `crash.log`.

### Constatazioni che hanno plasmato il progetto

- **`claude setup-token` non è adatto a questo scopo.** Tali token sono validi e
  funzionano con `/v1/messages`, ma non portano `user:profile`. L'endpoint di
  utilizzo li respinge con HTTP 403. Era l'ipotesi iniziale del progetto, ed è
  confutata.
- **L'endpoint dei token si trova su `platform.claude.com`**, non più su
  `console.anthropic.com`, dove risponde HTTP 404.
- **Lo `User-Agent` è obbligatorio.** Senza uno user agent di Claude Code
  l'endpoint di utilizzo limita in modo permanente con HTTP 429.
- Compilato con trimming e compressione: 21 MB invece di 93 MB, avvio in 2,3
  invece di 7,2 secondi, 87 invece di 136 MB di memoria. Il trimming vince su
  tutti e tre i fronti: il codice rimosso non deve nemmeno essere caricato e
  compilato.

### Limitazioni note

- Il pacchetto **non è firmato**. Windows SmartScreen segnala un editore
  sconosciuto al primo avvio.
- Quanto a lungo l'accesso proprio sopravviva a una pausa prolungata è ignoto:
  Anthropic non documenta la durata del refresh token.
- La procedura di accesso usa l'ID client OAuth pubblicamente noto di Claude
  Code, dato che Anthropic non offre la registrazione di applicazioni proprie.
  Non è una via ufficialmente supportata; può cambiare in qualsiasi momento.
- macOS è predisposto, ma non realizzato.
